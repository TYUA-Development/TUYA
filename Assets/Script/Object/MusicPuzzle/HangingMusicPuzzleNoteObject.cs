using System.Collections;
using UnityEngine;

public class HangingMusicPuzzleNoteObject : MonoBehaviour
{
    [Header("Puzzle")]
    [SerializeField] private MusicPuzzleAreaController puzzleController;
    [SerializeField] private int currentNoteIndex;
    [SerializeField] private int noteStateCount = 4;
    [SerializeField] private bool cycleOnPropellerHit = true;

    [Header("Arrow")]
    [SerializeField] private string arrowTag = "Arrow";
    [SerializeField] private Collider2D propellerHitCollider;

    [Header("Dots")]
    [SerializeField] private SpriteRenderer[] dotBaseRenderers = new SpriteRenderer[4];
    [SerializeField] private SpriteRenderer[] dotActiveRenderers = new SpriteRenderer[4];
    [SerializeField] private float dotActiveFadeTime = 0.2f;

    [Header("Propeller")]
    [SerializeField] private Transform propellerRoot;
    [SerializeField] private float propellerSpinSpeed = 360f;
    [SerializeField] private float propellerDeceleration = 360f;
    [Tooltip("애니메이션 길이가 아니라, 다음 피격을 막는 잠금 시간(초)")]
    [SerializeField] private float propellerSpinTime = 0.25f;

    [Header("Body Physics")]
    [SerializeField] private Rigidbody2D bodyRigidbody;
    [SerializeField] private Vector2 hitForce = new Vector2(0.4f, 1.2f);
    [SerializeField] private float hitTorque = 8f;
    [SerializeField] private ForceMode2D hitForceMode = ForceMode2D.Impulse;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip propellerHitClip;
    [Range(0f, 1f)]
    [SerializeField] private float propellerHitVolume = 1f;
    [SerializeField] private float propellerHitDelay = 0f;
    [SerializeField] private AudioClip chainSwingClip;
    [Range(0f, 1f)]
    [SerializeField] private float chainSwingVolume = 1f;
    [SerializeField] private float chainSwingDelay = 0.05f;
    [SerializeField] private float currentNoteDelay = 0.05f;

    [Header("Sprites For Builder")]
    [SerializeField] private Sprite bodySprite;
    [SerializeField] private Sprite dotBaseSprite;
    [SerializeField] private Sprite dotActiveSprite;
    [SerializeField] private Sprite propellerSprite;
    [SerializeField] private Sprite chainLinkASprite;
    [SerializeField] private Sprite chainLinkBSprite;

    [Header("Builder Roots")]
    [SerializeField] private Transform chainAnchor;
    [SerializeField] private Transform generatedChainRoot;
    [SerializeField] private Transform bodyRoot;

    [Header("Chain Physics")]
    [SerializeField] private int chainLinkCount = 12;
    [SerializeField] private float chainLinkSpacing = 0.25f;
    [SerializeField] private float chainLinkMass = 0.08f;
    [SerializeField] private float chainLinkLinearDrag = 1f;
    [SerializeField] private float chainLinkAngularDrag = 1f;
    [SerializeField] private float bodyMass = 4f;
    [SerializeField] private float bodyLinearDrag = 1.5f;
    [SerializeField] private float bodyAngularDrag = 2f;
    [SerializeField] private float hingeLimitAngle = 25f;

    [Header("Generated Chain")]
    [SerializeField] private Rigidbody2D[] chainLinkRigidbodies;
    [SerializeField] private HingeJoint2D[] chainJoints;

    private Coroutine propellerSpinCoroutine;
    private Coroutine[] dotActiveFadeCoroutines;
    private bool hitLocked;

    public int CurrentNoteIndex => currentNoteIndex;

    public void SetPuzzleController(MusicPuzzleAreaController controller)
    {
        puzzleController = controller;
    }

    public Transform GetActiveDotTransform()
    {
        if (dotActiveRenderers == null || currentNoteIndex < 0 || currentNoteIndex >= dotActiveRenderers.Length)
            return null;

        SpriteRenderer renderer = dotActiveRenderers[currentNoteIndex];
        return renderer != null ? renderer.transform : null;
    }

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
        bodyRigidbody = GetComponentInChildren<Rigidbody2D>();
    }

    private void Awake()
    {
        WirePropellerHitProxy();
        UpdateDots(true);
    }

    private void OnValidate()
    {
        noteStateCount = Mathf.Max(1, noteStateCount);
        currentNoteIndex = Mathf.Clamp(currentNoteIndex, 0, noteStateCount - 1);
        EnsureLength(ref dotBaseRenderers, noteStateCount);
        EnsureLength(ref dotActiveRenderers, noteStateCount);
        chainLinkCount = Mathf.Max(1, chainLinkCount);
        chainLinkSpacing = Mathf.Max(0.01f, chainLinkSpacing);
        hingeLimitAngle = Mathf.Clamp(hingeLimitAngle, 0f, 179f);
        UpdateDots(true);
    }

    public void HandlePropellerHit(Vector2 hitPoint, Vector2 arrowDirection)
    {
        if (hitLocked)
            return;

        if (puzzleController != null && puzzleController.IsPuzzleSolved)
            return;

        StartCoroutine(PropellerHitRoutine(hitPoint, arrowDirection));
    }

    public void SetNoteIndex(int noteIndex, bool playNote)
    {
        currentNoteIndex = Mathf.Clamp(noteIndex, 0, noteStateCount - 1);
        UpdateDots();

        if (playNote)
            PlayCurrentNote();
    }

    public bool IsArrowObject(GameObject hitObject)
    {
        if (hitObject == null)
            return true;

        if (!string.IsNullOrEmpty(arrowTag) && hitObject.CompareTag(arrowTag))
            return true;

        if (hitObject.GetComponent<Arrow>() != null)
            return true;

        if (hitObject.GetComponentInParent<Arrow>() != null)
            return true;

        return false;
    }

    [ContextMenu("Wire Propeller Hit Proxy")]
    public void WirePropellerHitProxy()
    {
        if (propellerHitCollider == null && propellerRoot != null)
            propellerHitCollider = propellerRoot.GetComponentInChildren<Collider2D>();

        if (propellerHitCollider == null)
            return;

        propellerHitCollider.isTrigger = true;

        MusicPuzzlePropellerHitProxy proxy = propellerHitCollider.GetComponent<MusicPuzzlePropellerHitProxy>();
        if (proxy == null)
            proxy = propellerHitCollider.gameObject.AddComponent<MusicPuzzlePropellerHitProxy>();

        proxy.SetOwner(this);
    }

    [ContextMenu("Build Chain From Settings")]
    public void BuildChainFromSettings()
    {
        EnsureBuilderRoots();
        ClearGeneratedChain();

        Rigidbody2D previousBody = EnsureAnchorRigidbody();
        chainLinkRigidbodies = new Rigidbody2D[chainLinkCount];
        chainJoints = new HingeJoint2D[chainLinkCount + 1];

        Vector3 startPosition = chainAnchor.position;

        for (int i = 0; i < chainLinkCount; i++)
        {
            GameObject link = new GameObject($"ChainLink_{i + 1:00}");
            link.transform.SetParent(generatedChainRoot, false);
            link.transform.position = startPosition + Vector3.down * chainLinkSpacing * (i + 1);

            SpriteRenderer renderer = link.AddComponent<SpriteRenderer>();
            renderer.sprite = i % 2 == 0 ? chainLinkASprite : chainLinkBSprite;

            Rigidbody2D rb = link.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = chainLinkMass;
            rb.drag = chainLinkLinearDrag;
            rb.angularDrag = chainLinkAngularDrag;

            BoxCollider2D collider = link.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;
            collider.size = renderer.sprite != null ? renderer.sprite.bounds.size : new Vector2(0.12f, 0.22f);

            HingeJoint2D joint = link.AddComponent<HingeJoint2D>();
            ConfigureJoint(joint, previousBody, Vector2.up * chainLinkSpacing * 0.5f, Vector2.down * chainLinkSpacing * 0.5f);

            chainLinkRigidbodies[i] = rb;
            chainJoints[i] = joint;
            previousBody = rb;
        }

        Rigidbody2D body = EnsureBodyRigidbody();
        HingeJoint2D bodyJoint = body.GetComponent<HingeJoint2D>();
        if (bodyJoint == null)
            bodyJoint = body.gameObject.AddComponent<HingeJoint2D>();

        ConfigureJoint(bodyJoint, previousBody, Vector2.up * chainLinkSpacing * 0.5f, Vector2.down * chainLinkSpacing * 0.5f);
        chainJoints[chainJoints.Length - 1] = bodyJoint;

        bodyRoot.position = startPosition + Vector3.down * chainLinkSpacing * (chainLinkCount + 1);
        bodyRigidbody = body;
    }

    private IEnumerator PropellerHitRoutine(Vector2 hitPoint, Vector2 arrowDirection)
    {
        hitLocked = true;

        if (cycleOnPropellerHit)
            currentNoteIndex = (currentNoteIndex + 1) % noteStateCount;

        UpdateDots();
        PlayDelayed(propellerHitClip, propellerHitVolume, propellerHitDelay);
        PlayDelayed(chainSwingClip, chainSwingVolume, chainSwingDelay);
        PlayCurrentNote();
        ApplyBodyImpulse();
        SpinPropeller(GetSpinDirection(hitPoint, arrowDirection));

        yield return new WaitForSeconds(Mathf.Max(0.05f, propellerSpinTime));
        hitLocked = false;
    }

    private float GetSpinDirection(Vector2 hitPoint, Vector2 arrowDirection)
    {
        if (propellerRoot == null)
            return 1f;

        Vector2 offset = hitPoint - (Vector2)propellerRoot.position;
        float torque = offset.x * arrowDirection.y - offset.y * arrowDirection.x;

        return torque >= 0f ? 1f : -1f;
    }

    private void PlayCurrentNote()
    {
        if (puzzleController != null)
            puzzleController.PlayNoteByIndex(currentNoteIndex, 1f, currentNoteDelay);
    }

    private void ApplyBodyImpulse()
    {
        if (bodyRigidbody == null)
            return;

        bodyRigidbody.AddForce(hitForce, hitForceMode);
        bodyRigidbody.AddTorque(hitTorque, hitForceMode);
    }

    private void SpinPropeller(float direction)
    {
        if (propellerRoot == null)
            return;

        if (propellerSpinCoroutine != null)
            StopCoroutine(propellerSpinCoroutine);

        propellerSpinCoroutine = StartCoroutine(SpinPropellerRoutine(direction));
    }

    private IEnumerator SpinPropellerRoutine(float direction)
    {
        float currentSpeed = propellerSpinSpeed * direction;

        while (Mathf.Abs(currentSpeed) > 0.01f)
        {
            propellerRoot.Rotate(0f, 0f, currentSpeed * Time.deltaTime);
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, propellerDeceleration * Time.deltaTime);
            yield return null;
        }

        propellerSpinCoroutine = null;
    }

    private void UpdateDots(bool instant = false)
    {
        if (dotBaseRenderers != null)
        {
            for (int i = 0; i < dotBaseRenderers.Length; i++)
            {
                if (dotBaseRenderers[i] != null)
                    dotBaseRenderers[i].enabled = true;
            }
        }

        if (dotActiveRenderers == null)
            return;

        EnsureDotFadeArray();

        for (int i = 0; i < dotActiveRenderers.Length; i++)
        {
            SpriteRenderer renderer = dotActiveRenderers[i];
            if (renderer == null)
                continue;

            bool active = i == currentNoteIndex;

            if (instant || !Application.isPlaying)
            {
                SetDotAlpha(renderer, active ? 1f : 0f);
                renderer.enabled = active;
                continue;
            }

            StartDotFade(i, renderer, active);
        }
    }

    private void EnsureDotFadeArray()
    {
        if (dotActiveFadeCoroutines == null || dotActiveFadeCoroutines.Length != dotActiveRenderers.Length)
            dotActiveFadeCoroutines = new Coroutine[dotActiveRenderers.Length];
    }

    private void StartDotFade(int index, SpriteRenderer renderer, bool active)
    {
        if (dotActiveFadeCoroutines[index] != null)
            StopCoroutine(dotActiveFadeCoroutines[index]);

        dotActiveFadeCoroutines[index] = StartCoroutine(DotFadeRoutine(index, renderer, active));
    }

    private IEnumerator DotFadeRoutine(int index, SpriteRenderer renderer, bool active)
    {
        renderer.enabled = true;

        float startAlpha = renderer.color.a;
        float targetAlpha = active ? 1f : 0f;
        float duration = Mathf.Max(0.001f, dotActiveFadeTime);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            SetDotAlpha(renderer, Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(timer / duration)));
            yield return null;
        }

        SetDotAlpha(renderer, targetAlpha);
        renderer.enabled = active;
        dotActiveFadeCoroutines[index] = null;
    }

    private static void SetDotAlpha(SpriteRenderer renderer, float alpha)
    {
        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }

    private void PlayDelayed(AudioClip clip, float volume, float delay)
    {
        if (audioSource == null || clip == null)
            return;

        StartCoroutine(PlayDelayedRoutine(clip, volume, delay));
    }

    private IEnumerator PlayDelayedRoutine(AudioClip clip, float volume, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void EnsureBuilderRoots()
    {
        if (chainAnchor == null)
        {
            GameObject anchor = new GameObject("ChainAnchor");
            anchor.transform.SetParent(transform, false);
            chainAnchor = anchor.transform;
        }

        if (generatedChainRoot == null)
        {
            GameObject chainRoot = new GameObject("GeneratedChainLinks");
            chainRoot.transform.SetParent(transform, false);
            generatedChainRoot = chainRoot.transform;
        }

        if (bodyRoot == null)
        {
            GameObject body = new GameObject("Body");
            body.transform.SetParent(transform, false);
            bodyRoot = body.transform;

            GameObject bodySpriteObject = new GameObject("BodySprite");
            bodySpriteObject.transform.SetParent(bodyRoot, false);
            SpriteRenderer bodyRenderer = bodySpriteObject.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = bodySprite;
        }
    }

    private void ClearGeneratedChain()
    {
        if (generatedChainRoot == null)
            return;

        for (int i = generatedChainRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = generatedChainRoot.GetChild(i);

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private Rigidbody2D EnsureAnchorRigidbody()
    {
        Rigidbody2D rb = chainAnchor.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = chainAnchor.gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Static;
        return rb;
    }

    private Rigidbody2D EnsureBodyRigidbody()
    {
        Rigidbody2D rb = bodyRoot.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = bodyRoot.gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = bodyMass;
        rb.drag = bodyLinearDrag;
        rb.angularDrag = bodyAngularDrag;

        if (bodyRoot.GetComponent<Collider2D>() == null)
            bodyRoot.gameObject.AddComponent<BoxCollider2D>();

        return rb;
    }

    private void ConfigureJoint(HingeJoint2D joint, Rigidbody2D connectedBody, Vector2 anchor, Vector2 connectedAnchor)
    {
        joint.connectedBody = connectedBody;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = anchor;
        joint.connectedAnchor = connectedAnchor;
        joint.useLimits = true;
        joint.limits = new JointAngleLimits2D
        {
            min = -hingeLimitAngle,
            max = hingeLimitAngle
        };
    }

    private static void EnsureLength<T>(ref T[] array, int length)
    {
        if (array == null)
        {
            array = new T[length];
            return;
        }

        if (array.Length != length)
            System.Array.Resize(ref array, length);
    }
}
