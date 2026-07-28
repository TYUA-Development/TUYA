using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Rope를 직접 건드리지 않고, 별도로 참조를 받아와 끊어짐을 감지 -> 일정 시간 대기 -> 재생성하는 스크립트.
// 로프에 매달린 박스는 끊어질 때마다 새로 생성되며, 방금 떨어진 박스는 한 턴(다음 재생성까지)
// 동안 그대로 남아있다가 그 다음 재생성 시점에 제거된다.
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
    [SerializeField] private float regenerateDelay = 3f;
    [SerializeField] private float glowFadeDuration = 1f;

    [Header("Segment Collapse")]
    [Tooltip("끊어진 지점에서 바깥쪽으로 한 단계(세그먼트 1~2개)씩 사라지는 간격")]
    [SerializeField] private float segmentDisappearStepDelay = 0.15f;
    [Tooltip("세그먼트 하나가 사라지는 데 걸리는 페이드아웃 시간")]
    [SerializeField] private float segmentDisappearFadeDuration = 0.1f;

    [Header("Glow")]
    [SerializeField] private Color glowColor = Color.white;

    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

    private Material flashMaterial;
    private bool isRegenerating;

    private void Awake()
    {
        Shader flashShader = Shader.Find("Custom/SpriteFlash");
        if (flashShader != null)
            flashMaterial = new Material(flashShader);
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
    }

    private void Update()
    {
        if (isRegenerating || rope == null)
            return;

        if (rope.IsCut)
            StartCoroutine(RegenerateRoutine());
    }

    private IEnumerator RegenerateRoutine()
    {
        isRegenerating = true;

        yield return new WaitForSeconds(regenerateDelay);

        yield return StartCoroutine(CollapseRopeSegments());

        List<GameObject> newlySpawnedBoxes = AdvanceHangingBoxes();

        rope.BuildRope();

        yield return StartCoroutine(PlayGlowFade(newlySpawnedBoxes));

        isRegenerating = false;
    }

    // 끊어진 지점(가장 앵커에 가까운 IsCut 세그먼트)을 기준으로 양쪽으로 한 단계씩
    // 확장하며 세그먼트를 순차적으로 페이드아웃-제거한다.
    private IEnumerator CollapseRopeSegments()
    {
        RopeSegment[] segments = rope.Segments;
        if (segments == null || segments.Length == 0)
            yield break;

        int pivot = FindTopmostCutIndex(segments);
        if (pivot < 0)
            yield break;

        foreach (List<int> step in BuildCollapseSteps(pivot, segments.Length))
        {
            foreach (int index in step)
            {
                if (segments[index] != null)
                    StartCoroutine(FadeAndDestroySegment(segments[index], segmentDisappearFadeDuration));
            }

            yield return new WaitForSeconds(segmentDisappearStepDelay);
        }
    }

    private static int FindTopmostCutIndex(RopeSegment[] segments)
    {
        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] != null && segments[i].IsCut)
                return i;
        }

        return -1;
    }

    private static List<List<int>> BuildCollapseSteps(int pivot, int count)
    {
        var steps = new List<List<int>>();
        int upper = pivot - 1;
        int lower = pivot;

        while (upper >= 0 || lower < count)
        {
            var step = new List<int>();
            if (upper >= 0) step.Add(upper);
            if (lower < count) step.Add(lower);
            steps.Add(step);

            upper--;
            lower++;
        }

        return steps;
    }

    private IEnumerator FadeAndDestroySegment(RopeSegment segment, float duration)
    {
        if (segment == null)
            yield break;

        SpriteRenderer sr = segment.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Color start = sr.color;
            float elapsed = 0f;
            while (elapsed < duration && sr != null)
            {
                elapsed += Time.deltaTime;
                sr.color = new Color(start.r, start.g, start.b, Mathf.Lerp(start.a, 0f, elapsed / duration));
                yield return null;
            }
        }

        if (segment != null)
            Destroy(segment.gameObject);
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

        return Instantiate(slot.boxPrefab, slot.spawnPosition, slot.spawnRotation);
    }

    private void RemoveBox(GameObject box)
    {
        if (box == null)
            return;

        if (box.TryGetComponent(out DisappearMethod disappear))
            disappear.PlayAndDestroy();
        else
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
