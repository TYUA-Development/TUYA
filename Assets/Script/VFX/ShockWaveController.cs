using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class ShockWaveController : MonoBehaviour
{
    [Header("Timing")]
    public float duration = 0.5f;

    [Header("World-space Origin")]
    [Tooltip("물결이 퍼져나가는 기준점. 비워두면 이 오브젝트의 위치를 사용합니다.")]
    public Transform origin;

    [Tooltip("물결이 이 거리(월드 단위)까지 퍼졌을 때 화면 전체가 camera2 상태로 완전히 전환됩니다.")]
    public float maxWorldRadius = 15f;

    [Header("Cameras")]
    [Tooltip("1번 카메라(파동이 지나가기 전 기본 화면). 비워두면 Camera.main을 사용합니다. 평소에는 화면에 직접 렌더링되고, 트리거되는 동안에만 잠깐 RenderTexture로 우회합니다.")]
    public Camera camera1;

    [Tooltip("2번 카메라(파동이 지나간 자리에 보여줄 화면). 씬에는 미리 배치해두되 평소엔 비활성화 상태여야 합니다. 트리거되는 동안 이 컨트롤러가 매 프레임 camera1과 같은 위치/각도/줌/컬링마스크로 맞춰줍니다.")]
    public Camera camera2;

    [Header("Composite Display")]
    [Tooltip("camera1/camera2의 렌더 결과를 합성해서 보여줄, 화면 전체를 덮는 UI RawImage. 이 RawImage가 속한 Canvas는 반드시 Screen Space - Overlay여야 합니다(카메라에 종속된 Canvas는 자기 자신을 다시 렌더링하려는 순환이 생깁니다).")]
    public RawImage compositeDisplay;

    [Header("Edge")]
    [Tooltip("파동 경계가 부드러워지는 정도(뷰포트 단위). 값이 클수록 경계가 흐릿해집니다.")]
    public float edgeSoftness = 0.02f;

    [Header("Ripple Distortion")]
    [Tooltip("파동 경계를 감싸는 왜곡 링의 폭(뷰포트 단위). 예전 M_ShockWave_FullScreen.mat의 _Size에 대응합니다.")]
    public float distortionSize = 0.06f;

    [Tooltip("왜곡 링 안에서 화면을 얼마나 방사 방향으로 밀어낼지(뷰포트 단위). 0이면 왜곡 없이 예전처럼 깔끔한 원형 리빌만 남습니다. 예전 M_ShockWave_FullScreen.mat의 _Strength에 대응하지만 스케일이 달라 다시 튜닝이 필요할 수 있습니다.")]
    public float distortionStrength = 0.04f;

    [Header("Camera-Exclusive Objects")]
    [Tooltip("평소(파동 트리거 전) 화면에 보이는 오브젝트 목록. 각 오브젝트는 IDivideCamera 컴포넌트(예: DivideCameraObject)를 가지고 있어야 실제로 camera2 렌더링에서 숨겨지며, 파동이 화면을 다 덮으면 영구히 꺼집니다. 이 컴포넌트가 없는 오브젝트는 리스트에 넣어도 아무 처리를 하지 않고 항상 두 카메라 모두에 보입니다.")]
    public List<GameObject> camera1OnlyObjects = new List<GameObject>();

    [Tooltip("파동이 지나간 뒤 드러날 오브젝트 목록. 각 오브젝트는 IDivideCamera 컴포넌트를 가지고 있어야 실제로 camera1 렌더링에서 숨겨지며, 평소에는 숨겨져 있다가 파동이 화면을 다 덮으면 영구히 켜집니다. 이 컴포넌트가 없는 오브젝트는 리스트에 넣어도 아무 처리를 하지 않고 항상 두 카메라 모두에 보입니다.")]
    public List<GameObject> camera2OnlyObjects = new List<GameObject>();

    private static readonly int Camera2TexID = Shader.PropertyToID("_Camera2Tex");
    private static readonly int FocalPointID = Shader.PropertyToID("_FocalPoint");
    private static readonly int ViewportRadiusID = Shader.PropertyToID("_ViewportRadius");
    private static readonly int EdgeSoftnessID = Shader.PropertyToID("_EdgeSoftness");
    private static readonly int AspectRatioID = Shader.PropertyToID("_AspectRatio");
    private static readonly int DistortionSizeID = Shader.PropertyToID("_DistortionSize");
    private static readonly int DistortionStrengthID = Shader.PropertyToID("_DistortionStrength");

    // 카메라 하나에 대해 "전용으로 보여야 할" 오브젝트들의 실제 렌더/조명/충돌 컴포넌트를
    // 모아둔다. Renderer에는 SpriteRenderer/MeshRenderer뿐 아니라 ParticleSystemRenderer도
    // 포함된다(ParticleSystemRenderer가 Renderer를 상속하므로 GetComponentsInChildren<Renderer>
    // 한 번으로 파티클 비주얼까지 함께 잡힌다). Light2D는 Renderer가 아니라서 따로 모은다.
    // 이 프로젝트는 URP 2D 조명(Light2D)만 사용하므로(레거시 3D Light는 씬에 존재하지 않음)
    // Light가 아니라 Light2D를 기준으로 수집한다.
    //
    // Light2D는 절대 Behaviour.enabled로 껐다 켜면 안 된다 - Light2D.boundingSphere는
    // LateUpdate()에서만 갱신되는데, LateUpdate는 컴포넌트가 enabled인 상태로 "그 프레임의
    // 일반 스크립트 업데이트 단계"를 맞아야만 실행된다. 이 클래스가 라이트를 켜는 시점은
    // OnBeginCameraRendering~OnEndCameraRendering 사이(렌더링 단계, 스크립트 업데이트 단계보다
    // 뒤)뿐이라, enabled를 매 프레임 껐다 켜면 LateUpdate가 영원히 실행되지 않아 boundingSphere가
    // 기본값(원점, 반지름 0)에 고정되고, URP의 Light2DCullResult가 이를 항상 프러스텀 밖으로
    // 컬링해버려 해당 카메라에서는 라이트가 절대 보이지 않는다. 그래서 enabled는 항상 true로
    // 유지하고, intensity를 0으로 낮춰서 "꺼짐"을 표현한다.
    private class CameraObjectGroup
    {
        public readonly List<Renderer> renderers = new List<Renderer>();
        public readonly List<Light2D> lights = new List<Light2D>();
        private readonly List<float> lightBaseIntensities = new List<float>();
        public readonly List<Collider2D> colliders = new List<Collider2D>();

        public void Collect(GameObject go)
        {
            renderers.AddRange(go.GetComponentsInChildren<Renderer>(true));

            foreach (Light2D light in go.GetComponentsInChildren<Light2D>(true))
            {
                light.enabled = true;
                lights.Add(light);
                lightBaseIntensities.Add(light.intensity);
            }

            colliders.AddRange(go.GetComponentsInChildren<Collider2D>(true));
        }

        public void SetVisible(bool visible)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = visible;
            }

            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i] != null)
                    lights[i].intensity = visible ? lightBaseIntensities[i] : 0f;
            }
        }

        public void SetCollidersEnabled(bool value)
        {
            for (int i = 0; i < colliders.Count; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = value;
            }
        }
    }

    private readonly CameraObjectGroup camera1Group = new CameraObjectGroup();
    private readonly CameraObjectGroup camera2Group = new CameraObjectGroup();

    private RenderTexture rt1;
    private RenderTexture rt2;
    private int rtWidth;
    private int rtHeight;
    private Material compositeMaterialInstance;
    private Coroutine shockWaveRoutine;
    private bool isTransitioning;

    // camera1/camera2가 트랜지션 동안 둘 다 targetTexture로 돌려지면, 실제 화면(Display)에
    // 직접 렌더링하는 카메라가 하나도 남지 않아 Unity가 "Display 1 No cameras rendering"을
    // 표시한다(Screen Space - Overlay Canvas가 있어도 이 placeholder를 대체하지 못한다).
    // cullingMask를 0으로 둔, 아무것도 그리지 않는 이 더미 카메라를 화면에 계속 렌더링시켜서
    // "화면에 그리는 카메라가 있다"는 사실만 만족시킨다 - 실제 화면 내용은 그 위에 그려지는
    // compositeDisplay(RawImage)가 대신 보여준다.
    private Camera passThroughCamera;

    private void Awake()
    {
        if (origin == null)
            origin = transform;

        if (camera1 == null)
            camera1 = Camera.main;

        EnsurePassThroughCamera();

        BuildGroup(camera1OnlyObjects, camera1Group);
        BuildGroup(camera2OnlyObjects, camera2Group);

        // 평소(트리거 전) 상태: camera2 전용 오브젝트는 숨기고 콜라이더도 꺼서
        // 아직 드러나지 않은/존재하지 않는 것처럼 취급한다. camera1 전용 오브젝트는
        // 씬에 배치된 상태(보통 이미 보이는 상태) 그대로 둔다.
        camera2Group.SetVisible(false);
        camera2Group.SetCollidersEnabled(false);

        if (compositeDisplay != null)
            compositeDisplay.enabled = false;

        if (camera2 != null)
        {
            camera2.enabled = false;
            camera2.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

        ReleaseRenderTextures();

        if (compositeMaterialInstance != null)
            Destroy(compositeMaterialInstance);

        if (passThroughCamera != null)
            Destroy(passThroughCamera.gameObject);
    }

    private void EnsurePassThroughCamera()
    {
        if (passThroughCamera != null)
            return;

        GameObject go = new GameObject("ShockWave_PassThroughCamera");
        go.transform.SetParent(transform, false);

        passThroughCamera = go.AddComponent<Camera>();
        passThroughCamera.clearFlags = CameraClearFlags.SolidColor;
        passThroughCamera.backgroundColor = Color.black;
        passThroughCamera.cullingMask = 0;
        passThroughCamera.depth = -100f;
        passThroughCamera.targetTexture = null;
        passThroughCamera.enabled = false;
    }

    private void BuildGroup(List<GameObject> sourceList, CameraObjectGroup group)
    {
        foreach (GameObject go in sourceList)
        {
            if (go == null)
                continue;

            if (go.GetComponent<IDivideCamera>() == null)
            {
                Debug.LogWarning($"[ShockWaveController] '{go.name}'에 IDivideCamera 컴포넌트가 없어 카메라별 숨김 처리를 적용하지 않습니다. 항상 두 카메라 모두에 보입니다.", go);
                continue;
            }

            group.Collect(go);
        }
    }

    private void Update()
    {
        // 테스트용 디버그 트리거. 실제 발동은 CoreObjectToggle 등 외부 호출을 통해 TriggerShockWave()로 이루어진다.
        if (Input.GetKeyDown(KeyCode.E))
        {
            TriggerShockWave();
        }
    }

    public void TriggerShockWave()
    {
        if (camera1 == null || camera2 == null || compositeDisplay == null)
        {
            Debug.LogWarning("[ShockWaveController] camera1/camera2/compositeDisplay가 설정되지 않아 실행할 수 없습니다.", this);
            return;
        }

        if (shockWaveRoutine != null)
            StopCoroutine(shockWaveRoutine);

        shockWaveRoutine = StartCoroutine(ShockWaveRoutine());
    }

    private IEnumerator ShockWaveRoutine()
    {
        BeginTransition();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);

            SyncCamera2();
            UpdateComposite(t);

            yield return null;
        }

        SyncCamera2();
        UpdateComposite(1f);

        EndTransition();

        shockWaveRoutine = null;
    }

    private void BeginTransition()
    {
        isTransitioning = true;

        EnsureRenderTextures();
        EnsureCompositeMaterial();

        passThroughCamera.targetDisplay = camera1.targetDisplay;
        passThroughCamera.enabled = true;

        camera1.targetTexture = rt1;
        camera2.targetTexture = rt2;
        camera2.gameObject.SetActive(true);
        camera2.enabled = true;

        SyncCamera2();

        compositeDisplay.texture = rt1;
        compositeDisplay.material = compositeMaterialInstance;
        compositeDisplay.enabled = true;

        // 트리거되는 동안에는 camera2 전용 오브젝트도 실제로 그 자리에 존재해야 하므로
        // (예: 드러날 발판을 밟을 수 있어야 하는 경우) 콜라이더를 미리 켜둔다.
        // 화면에 보일지 여부는 카메라별 렌더 훅이 매 프레임 따로 처리한다.
        camera2Group.SetCollidersEnabled(true);

        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void EndTransition()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

        // 파동이 화면을 다 덮었으므로 camera2 상태를 영구 상태로 확정(bake)하고,
        // camera1 전용 오브젝트는 영구히 꺼서 더 이상 어느 카메라에도 보이지 않게 한다.
        camera2Group.SetVisible(true);
        camera1Group.SetVisible(false);
        camera1Group.SetCollidersEnabled(false);

        camera1.targetTexture = null;
        camera2.targetTexture = null;
        camera2.enabled = false;
        camera2.gameObject.SetActive(false);

        passThroughCamera.enabled = false;

        compositeDisplay.enabled = false;

        isTransitioning = false;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        if (cam == camera1)
        {
            camera1Group.SetVisible(true);
            camera2Group.SetVisible(false);
        }
        else if (cam == camera2)
        {
            camera1Group.SetVisible(false);
            camera2Group.SetVisible(true);
        }
    }

    // 두 카메라가 모두 렌더링을 마친 뒤에는 기본(camera1) 노출 상태로 되돌려서,
    // 씬 뷰 카메라 등 제3의 카메라가 같은 프레임에 렌더링되더라도 어중간한
    // 중간 상태를 보지 않게 한다.
    private void OnEndCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        if (cam == camera2)
        {
            camera1Group.SetVisible(true);
            camera2Group.SetVisible(false);
        }
    }

    // camera2는 camera1과 완전히 동일한 시점에서 다른 오브젝트 구성만 보여줘야 하므로,
    // 트랜지션 동안 매 프레임 camera1의 트랜스폼/투영 설정을 그대로 따라간다.
    private void SyncCamera2()
    {
        Transform t1 = camera1.transform;
        camera2.transform.SetPositionAndRotation(t1.position, t1.rotation);
        camera2.orthographic = camera1.orthographic;
        camera2.orthographicSize = camera1.orthographicSize;
        camera2.fieldOfView = camera1.fieldOfView;
        camera2.nearClipPlane = camera1.nearClipPlane;
        camera2.farClipPlane = camera1.farClipPlane;
        camera2.cullingMask = camera1.cullingMask;
    }

    private void UpdateComposite(float t)
    {
        Vector3 originPos = origin.position;
        float worldRadius = Mathf.Lerp(0f, maxWorldRadius, t);

        Vector3 focal = camera1.WorldToViewportPoint(originPos);

        // 월드 반경을 화면(뷰포트) 반경으로 바꿀 때, Y축(camera1.transform.up) 오프셋을
        // 기준으로 삼는다. 셰이더에서는 X축에만 _AspectRatio를 곱해 보정하므로, 여기서
        // 기준 반경 자체는 보정 전(Y축 기준) 값으로 넘겨야 두 계산이 서로 맞아떨어진다.
        Vector3 edge = camera1.WorldToViewportPoint(originPos + camera1.transform.up * worldRadius);
        float viewportRadius = Mathf.Abs(edge.y - focal.y);

        compositeMaterialInstance.SetVector(FocalPointID, new Vector4(focal.x, focal.y, 0f, 0f));
        compositeMaterialInstance.SetFloat(ViewportRadiusID, viewportRadius);
        compositeMaterialInstance.SetFloat(EdgeSoftnessID, edgeSoftness);
        compositeMaterialInstance.SetFloat(AspectRatioID, Screen.width / (float)Screen.height);
        compositeMaterialInstance.SetFloat(DistortionSizeID, distortionSize);
        compositeMaterialInstance.SetFloat(DistortionStrengthID, distortionStrength);
    }

    // 해상도/창모드가 바뀔 수 있는 프로젝트이므로(SettingsManager 참고), 화면 크기가
    // 달라지면 RenderTexture를 다시 만든다.
    private void EnsureRenderTextures()
    {
        if (rt1 != null && rt2 != null && rtWidth == Screen.width && rtHeight == Screen.height)
            return;

        ReleaseRenderTextures();

        rtWidth = Mathf.Max(Screen.width, 1);
        rtHeight = Mathf.Max(Screen.height, 1);

        rt1 = new RenderTexture(rtWidth, rtHeight, 24) { name = "ShockWave_Camera1RT" };
        rt2 = new RenderTexture(rtWidth, rtHeight, 24) { name = "ShockWave_Camera2RT" };
    }

    private void ReleaseRenderTextures()
    {
        if (rt1 != null)
        {
            rt1.Release();
            Destroy(rt1);
            rt1 = null;
        }

        if (rt2 != null)
        {
            rt2.Release();
            Destroy(rt2);
            rt2 = null;
        }
    }

    private void EnsureCompositeMaterial()
    {
        if (compositeMaterialInstance != null)
        {
            compositeMaterialInstance.SetTexture(Camera2TexID, rt2);
            return;
        }

        Shader shader = Shader.Find("Custom/UI/ShockWaveCameraComposite");
        if (shader == null)
        {
            Debug.LogError("[ShockWaveController] Custom/UI/ShockWaveCameraComposite 셰이더를 찾을 수 없습니다.", this);
            return;
        }

        compositeMaterialInstance = new Material(shader);
        compositeMaterialInstance.SetTexture(Camera2TexID, rt2);
    }
}
