using UnityEngine;

// skyImage(하위 스카이 스프라이트들의 부모)가 화면 해상도/종횡비/카메라 FOV(줌)와 무관하게
// 항상 화면 전체를 덮도록(CSS의 background-size: cover와 동일한 방식) 매 프레임 스케일을
// 다시 계산한다. 원본 스프라이트 비율이 화면 비율과 달라도 항상 화면을 꽉 채우며, 남는 쪽
// 여백 없이 넘치는 쪽은 화면 밖으로 자연히 잘린다.
public class SkyZoomScaler : MonoBehaviour
{
    public Camera targetCamera;
    public Transform skyImage;

    [Tooltip("화면 채우기(Cover) 계산 기준으로 쓸 스프라이트. 비워두면 skyImage의 자식 중 첫 SpriteRenderer를 자동으로 찾습니다. 모든 하위 스카이 이미지가 같은 원본 크기라고 가정합니다.")]
    public SpriteRenderer referenceSprite;

    // skyImage.localPosition.x/y를 "배율 1일 때" 기준으로 환산해 둔 값. skyImage가 카메라의
    // 광학 중심(로컬 0,0)에 정확히 있지 않고 약간 오프셋되어 배치된 경우(수평선을 맞추기 위해
    // 아래로 내려놓는 등), 이 오프셋도 coverScale과 함께 비례해서 커지지 않으면 줌 레벨에 따라
    // 위/아래 중 한쪽은 화면을 못 덮고(빈 공간) 반대쪽은 필요 이상으로 넘치는 비대칭이 생긴다.
    private Vector2 nativeOffset;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (referenceSprite == null && skyImage != null)
            referenceSprite = skyImage.GetComponentInChildren<SpriteRenderer>();

        if (skyImage != null)
        {
            Vector3 initialScale = skyImage.localScale;
            Vector3 initialPos = skyImage.localPosition;

            nativeOffset = new Vector2(
                Mathf.Approximately(initialScale.x, 0f) ? 0f : initialPos.x / initialScale.x,
                Mathf.Approximately(initialScale.y, 0f) ? 0f : initialPos.y / initialScale.y
            );
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null || skyImage == null || referenceSprite == null || referenceSprite.sprite == null)
            return;

        // Perspective 카메라 기준, skyImage가 있는 z 깊이에서 화면을 꽉 채우는 데 필요한
        // 월드 단위 크기를 구한다. 매 프레임 현재 FOV로 다시 계산하므로, 줌 연출(FOV 변경)이
        // 일어나도 별도의 보정 없이 이 계산 하나로 항상 화면을 덮는 크기가 유지된다.
        float distance = Mathf.Abs(skyImage.localPosition.z);
        float frustumHeight = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float frustumWidth = frustumHeight * targetCamera.aspect;

        // Sprite.bounds는 트랜스폼과 무관하게 스프라이트 자체 기준 크기라 "스케일 1일 때"
        // 월드 크기를 그대로 준다. 기준 스프라이트 자신의 localScale(부모인 skyImage 기준
        // 상대값)까지 곱해 실제 1배 기준 크기를 구한다.
        Vector2 nativeSize = referenceSprite.sprite.bounds.size;
        Vector3 childScale = referenceSprite.transform.localScale;
        float nativeWorldWidth = nativeSize.x * childScale.x;
        float nativeWorldHeight = nativeSize.y * childScale.y;

        if (nativeWorldWidth <= 0f || nativeWorldHeight <= 0f)
            return;

        // 세로/가로 중 더 크게 요구되는 배율을 골라(cover) 원본 비율(예: 2048x1400)이
        // 화면 비율과 달라도 항상 화면 전체를 채운다.
        float coverScale = Mathf.Max(frustumWidth / nativeWorldWidth, frustumHeight / nativeWorldHeight);

        // z는 건드리지 않는다 - 자식 하늘 레이어들의 localPosition.z가 서로 다른 깊이
        // 오프셋으로 쓰이는데, 부모 z 스케일까지 줄어들면 그 오프셋도 같이 줄어들어
        // Perspective 카메라 기준 자식들이 실제보다 훨씬 가까이 끌려와 버린다.
        skyImage.localScale = new Vector3(coverScale, coverScale, skyImage.localScale.z);

        // skyImage 자신이 카메라 광학 중심(로컬 0,0)에 있지 않고 오프셋되어 있다면(예: y가
        // 살짝 아래로 내려가 있는 경우), 그 오프셋도 coverScale에 비례해서 같이 키워야
        // "카메라 중심과 정렬되어 있던 스카이 텍스처 안의 그 지점"이 줌 레벨이 달라져도 계속
        // 카메라 중심에 맞춰진다. 이걸 안 하면(예전 코드) 오프셋이 고정된 월드 단위 크기로
        // 남아서, 화면을 덮는 데 필요한 크기가 커지거나 작아질 때 위/아래 중 한쪽은 모자라고
        // (빈 공간) 반대쪽은 남는(잘려나가는 것처럼 보이는) 비대칭이 생긴다.
        skyImage.localPosition = new Vector3(
            nativeOffset.x * coverScale,
            nativeOffset.y * coverScale,
            skyImage.localPosition.z
        );
    }
}
