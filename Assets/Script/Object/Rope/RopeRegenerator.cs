using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 끊어짐 감지 -> 대기 -> 세그먼트 제거는 전부 Rope가 스스로 담당한다(Rope.onCollapsed).
// 이 스크립트는 그 결과(onCollapsed)를 구독해서 그 다음 순서, 즉 매달린 박스 교체와
// 로프 재생성만 담당한다. 로프에 매달린 박스는 끊어질 때마다 새로 생성되며, 방금 떨어진
// 박스는 한 턴(다음 재생성까지) 동안 그대로 남아있다가 그 다음 재생성 시점에 제거된다.
// 로프 세그먼트와 새로 생성된 박스는 오브젝트 모양 그대로 흰색으로 발광한 뒤 서서히 원래 모습으로 페이드된다.
public class RopeRegenerator : MonoBehaviour
{
    [System.Serializable]
    public class HangingBoxSlot
    {
        [Tooltip("Rope의 Hanging Objects 배열에서 이 박스가 대응하는 인덱스")]
        public int attachmentIndex;

        [Tooltip("끊어질 때마다 새로 생성할 박스 프리팹")]
        public GameObject boxPrefab;

        [Tooltip("씬에 이미 배치되어 있는 최초 박스. 이 오브젝트의 시작 위치/회전이 이후 모든 재생성 위치로 쓰입니다.")]
        public GameObject initialBox;

        [System.NonSerialized] public GameObject currentBox;
        [System.NonSerialized] public GameObject previousFallenBox;
        [System.NonSerialized] public Vector3 spawnPosition;
        [System.NonSerialized] public Quaternion spawnRotation;
    }

    [Header("Target")]
    [SerializeField] private Rope rope;

    [SerializeField] private HangingBoxSlot[] hangingBoxes;

    [Header("Timing")]
    [SerializeField] private float glowFadeDuration = 1f;

    [Header("Glow")]
    [SerializeField] private Color glowColor = Color.white;

    [Header("Audio")]
    [Tooltip("로프가 끊어져 붕괴된 뒤 재생성(박스 교체 + 로프 재생성)이 시작되는 순간 한 번 재생할 효과음.")]
    [SerializeField] private AudioAssist regenerate_Rope;

    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

    private Material flashMaterial;

    // 소멸시키는(이전에 떨어진) 박스의 흰색 발광->페이드아웃용 머티리얼. 새로 생성된 박스가
    // 재생성 발광(flashMaterial, PlayGlowFade)을 동시에 재생 중일 수 있는데, 같은 머티리얼
    // 인스턴스를 공유하면 _FlashAmount를 서로 덮어써버리므로 별도로 둔다.
    private Material disappearFlashMaterial;

    private bool isRegenerating;

    private void Awake()
    {
        Shader flashShader = Shader.Find("Custom/SpriteFlash");
        if (flashShader != null)
        {
            flashMaterial = new Material(flashShader);
            disappearFlashMaterial = new Material(flashShader);
        }
    }

    private void Start()
    {
        if (hangingBoxes == null)
            return;

        foreach (HangingBoxSlot slot in hangingBoxes)
        {
            if (slot == null)
                continue;

            slot.currentBox = slot.initialBox;
            slot.previousFallenBox = null;

            if (slot.initialBox != null)
            {
                slot.spawnPosition = slot.initialBox.transform.position;
                slot.spawnRotation = slot.initialBox.transform.rotation;
            }
            else
            {
                Debug.LogWarning($"[RopeRegenerator] Initial Box가 지정되지 않아 재생성 위치를 알 수 없습니다 (attachmentIndex={slot.attachmentIndex}).", this);
            }
        }

        if (rope != null)
        {
            rope.onCollapsed += HandleRopeCollapsed;
            rope.onCut += HandleRopeCut;
        }
    }

    private void OnDestroy()
    {
        if (rope != null)
        {
            rope.onCollapsed -= HandleRopeCollapsed;
            rope.onCut -= HandleRopeCut;
        }
    }

    private void HandleRopeCollapsed()
    {
        if (isRegenerating)
            return;

        StartCoroutine(RegenerateRoutine());
    }

    // 로프가 끊어진 그 순간, 매달려 있던 박스를 Rope의 자식에서 즉시 떼어낸다
    // (월드 좌표/회전은 그대로 유지). 세그먼트가 사라지는 연출이 끝나기도 전에
    // 박스는 이미 독립된 오브젝트로 낙하해야 하기 때문에 onCollapsed가 아니라
    // 훨씬 이른 onCut 시점에서 처리한다.
    private void HandleRopeCut()
    {
        if (hangingBoxes == null)
            return;

        foreach (HangingBoxSlot slot in hangingBoxes)
        {
            if (slot != null && slot.currentBox != null)
                slot.currentBox.transform.SetParent(null, true);
        }
    }

    private IEnumerator RegenerateRoutine()
    {
        isRegenerating = true;

        if (regenerate_Rope != null)
            regenerate_Rope.Play();

        List<GameObject> newlySpawnedBoxes = AdvanceHangingBoxes();

        rope.BuildRope();

        yield return StartCoroutine(PlayGlowFade(newlySpawnedBoxes));

        isRegenerating = false;
    }

    private List<GameObject> AdvanceHangingBoxes()
    {
        var newlySpawnedBoxes = new List<GameObject>();

        if (hangingBoxes == null)
            return newlySpawnedBoxes;

        foreach (HangingBoxSlot slot in hangingBoxes)
        {
            if (slot == null)
                continue;

            RemoveBox(slot.previousFallenBox);

            slot.previousFallenBox = slot.currentBox;
            slot.currentBox = SpawnBox(slot);

            if (slot.currentBox == null)
            {
                Debug.LogWarning($"[RopeRegenerator] 새 박스를 생성하지 못해 attachmentIndex={slot.attachmentIndex}의 Rope 연결이 이전 박스를 그대로 유지합니다.", this);
            }
            else if (slot.currentBox.TryGetComponent(out Rigidbody2D rb))
            {
                rope.SetHangingTarget(slot.attachmentIndex, rb);
                newlySpawnedBoxes.Add(slot.currentBox);
            }
            else
            {
                Debug.LogWarning($"[RopeRegenerator] Box Prefab '{slot.boxPrefab.name}'에 Rigidbody2D가 없어 Rope에 연결할 수 없습니다.", slot.currentBox);
            }
        }

        return newlySpawnedBoxes;
    }

    private GameObject SpawnBox(HangingBoxSlot slot)
    {
        if (slot.boxPrefab == null)
        {
            Debug.LogWarning($"[RopeRegenerator] Box Prefab이 지정되지 않았습니다 (attachmentIndex={slot.attachmentIndex}).", this);
            return null;
        }

        return Instantiate(slot.boxPrefab, slot.spawnPosition, slot.spawnRotation, rope.transform);
    }

    private void RemoveBox(GameObject box)
    {
        if (box == null)
            return;

        // 소리는 박스 자신의 설정을 그대로 존중하되(있으면), 시각적으로는 재생성 발광과
        // 동일한 방식(흰색으로 밝아졌다가 서서히 사라짐)으로 통일한다.
        if (box.TryGetComponent(out BoxObject boxObject) && boxObject.disappear_Box != null)
            boxObject.disappear_Box.Play();

        StartCoroutine(FadeOutAndDestroyBox(box));
    }

    // PlayGlowFade(흰색 발광 -> 원래 색으로 페이드)와 반대 방향으로, 흰색으로 밝아졌다가
    // 서서히 투명해지며 사라진다. 셰이더의 _FlashAmount는 RGB만 섞고 알파는 그대로
    // SpriteRenderer.color.a를 따르므로(SpriteFlash.shader 참고), 알파를 같이 0으로 내리면
    // "사라짐"이 된다.
    private IEnumerator FadeOutAndDestroyBox(GameObject box)
    {
        if (box == null)
            yield break;

        if (disappearFlashMaterial == null)
        {
            Destroy(box);
            yield break;
        }

        SpriteRenderer[] renderers = box.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0)
        {
            Destroy(box);
            yield break;
        }

        var originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].color;
            renderers[i].sharedMaterial = disappearFlashMaterial;
        }

        disappearFlashMaterial.SetColor(FlashColorId, glowColor);

        float elapsed = 0f;
        while (elapsed < glowFadeDuration && box != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / glowFadeDuration);

            disappearFlashMaterial.SetFloat(FlashAmountId, 1f - t);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;

                Color c = originalColors[i];
                c.a = Mathf.Lerp(originalColors[i].a, 0f, t);
                renderers[i].color = c;
            }

            yield return null;
        }

        if (box != null)
            Destroy(box);
    }

    private IEnumerator PlayGlowFade(List<GameObject> newlySpawnedBoxes)
    {
        if (flashMaterial == null)
            yield break;

        List<SpriteRenderer> renderers = CollectRenderers(newlySpawnedBoxes);
        if (renderers.Count == 0)
            yield break;

        var originalMaterials = new Material[renderers.Count];
        for (int i = 0; i < renderers.Count; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterial;
            renderers[i].sharedMaterial = flashMaterial;
        }

        flashMaterial.SetColor(FlashColorId, glowColor);

        float elapsed = 0f;
        while (elapsed < glowFadeDuration)
        {
            elapsed += Time.deltaTime;
            float amount = 1f - Mathf.Clamp01(elapsed / glowFadeDuration);
            flashMaterial.SetFloat(FlashAmountId, amount);
            yield return null;
        }

        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null)
                renderers[i].sharedMaterial = originalMaterials[i];
        }
    }

    private List<SpriteRenderer> CollectRenderers(List<GameObject> newlySpawnedBoxes)
    {
        var renderers = new List<SpriteRenderer>();

        RopeSegment[] segments = rope.Segments;
        if (segments != null)
        {
            foreach (RopeSegment segment in segments)
            {
                if (segment == null)
                    continue;

                SpriteRenderer segmentRenderer = segment.GetComponentInChildren<SpriteRenderer>();
                if (segmentRenderer != null)
                    renderers.Add(segmentRenderer);
            }
        }

        if (newlySpawnedBoxes != null)
        {
            foreach (GameObject box in newlySpawnedBoxes)
            {
                if (box != null && box.TryGetComponent(out SpriteRenderer boxRenderer))
                    renderers.Add(boxRenderer);
            }
        }

        return renderers;
    }
}
