using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ForestIntroController : MonoBehaviour
{
    [Header("Player")]
    public Transform player;
    public Rigidbody2D playerRigidbody;
    public Transform startPoint;
    public Transform targetPoint;

    [Header("Facing")]
    [Tooltip("방향을 바꿀 대상. 비워두면 Player를 사용함")]
    public Transform facingTarget;

    [Tooltip("투야가 오른쪽을 볼 때 localScale.x 값")]
    public float rightFacingScaleX = -1f;

    [Tooltip("투야가 왼쪽을 볼 때 localScale.x 값")]
    public float leftFacingScaleX = 1f;

    [Header("Disable During Intro")]
    public MonoBehaviour[] playerControlScripts;
    public MonoBehaviour[] cameraFollowScripts;

    [Header("Camera")]
    public Camera mainCamera;

    [Tooltip("카메라가 실제로 움직이는 오브젝트. CameraRig가 있으면 CameraRig, 없으면 Main Camera")]
    public Transform cameraRootToMove;

    [Tooltip("인트로 시작 때 카메라가 있을 위치")]
    public Transform introCameraPoint;

    [Header("Camera Zoom - Orthographic")]
    [Tooltip("2D Orthographic 카메라용. 작을수록 확대")]
    public float introOrthographicSize = 3.0f;

    [Tooltip("2D Orthographic 카메라용. 평소 게임 화면 크기")]
    public float normalOrthographicSize = 5.5f;

    [Header("Camera Zoom - Perspective FOV")]
    [Tooltip("Perspective 카메라용. 작을수록 확대")]
    public float introFieldOfView = 35f;

    [Tooltip("Perspective 카메라용. 평소 시야각")]
    public float normalFieldOfView = 60f;

    [Header("Movement")]
    public float moveSpeed = 2.8f;
    public Animator playerAnimator;

    [Tooltip("Animator Bool 파라미터 이름")]
    public string moveBoolName = "IsMove";

    [Header("Letterbox Bars")]
    public RectTransform topBar;
    public RectTransform bottomBar;
    public float barMoveDistance = 260f;
    public float barOutTime = 1.2f;

    [Header("Scene Fade")]
    [Tooltip("Forest 씬에 있는 검정 FadeImage")]
    public Image sceneFadeImage;

    [Tooltip("씬 시작 후 검정 화면이 걷히는 시간")]
    public float sceneFadeOutTime = 1.2f;

    [Header("Timing")]
    public float startDelay = 0.3f;
    public float movingEndWaitTime = 0.45f;
    public float waitBeforeOpenBars = 0.2f;

    private Vector2 topBarStartPos;
    private Vector2 bottomBarStartPos;

    private void Start()
    {
        if (facingTarget == null)
            facingTarget = player;

        if (cameraRootToMove == null && mainCamera != null)
            cameraRootToMove = mainCamera.transform;

        if (topBar != null)
            topBarStartPos = topBar.anchoredPosition;

        if (bottomBar != null)
            bottomBarStartPos = bottomBar.anchoredPosition;

        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        // 조작 / 카메라 Follow 끄기
        SetScriptsEnabled(playerControlScripts, false);
        SetScriptsEnabled(cameraFollowScripts, false);

        // 플레이어 시작 위치로 이동
        if (player != null && startPoint != null)
        {
            player.position = startPoint.position;
        }

        // Rigidbody 초기화
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        // 카메라를 인트로 위치로 이동
        if (cameraRootToMove != null && introCameraPoint != null)
        {
            SetCameraRootPositionXY(introCameraPoint.position);
        }

        // 카메라 줌인 상태로 시작
        SetIntroZoom();

        // 검정 바 시작 위치 고정
        if (topBar != null)
            topBar.anchoredPosition = topBarStartPos;

        if (bottomBar != null)
            bottomBar.anchoredPosition = bottomBarStartPos;

        // Forest 씬 진입 직후 검정 화면으로 덮기
        SetSceneFadeAlpha(1f);

        // 검정 화면이 걷히면서 Forest 인트로 화면 보이기
        yield return StartCoroutine(FadeSceneFromBlack());

        // 잠깐 정적
        yield return new WaitForSeconds(startDelay);

        // 이동 방향에 맞춰 투야 방향 설정
        if (player != null && targetPoint != null)
        {
            FaceMoveDirection(targetPoint.position.x - player.position.x);
        }

        // Idle → MovingStart → Move
        SetMoveAnimation(true);

        // Y축 고정, X축으로만 이동
        float fixedY = player.position.y;
        float targetX = targetPoint.position.x;

        while (Mathf.Abs(player.position.x - targetX) > 0.03f)
        {
            float newX = Mathf.MoveTowards(
                player.position.x,
                targetX,
                moveSpeed * Time.deltaTime
            );

            Vector2 newPosition = new Vector2(newX, fixedY);

            if (playerRigidbody != null)
            {
                playerRigidbody.MovePosition(newPosition);
            }
            else
            {
                player.position = newPosition;
            }

            yield return null;
        }

        // 도착 위치 정확히 고정
        Vector3 finalPos = player.position;
        finalPos.x = targetPoint.position.x;
        finalPos.y = fixedY;
        player.position = finalPos;

        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        // Move → MovingEnd → Idle
        SetMoveAnimation(false);

        yield return new WaitForSeconds(movingEndWaitTime);
        yield return new WaitForSeconds(waitBeforeOpenBars);

        // 검정 바 열기 + 줌 정상화
        yield return StartCoroutine(OpenBarsAndZoomOut());

        // 마지막 속도 정리
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        // 끝날 때도 이동 방향 유지
        if (player != null && startPoint != null && targetPoint != null)
        {
            FaceMoveDirection(targetPoint.position.x - startPoint.position.x);
        }

        // 카메라 Follow / 조작 다시 켜기
        SetScriptsEnabled(cameraFollowScripts, true);
        SetScriptsEnabled(playerControlScripts, true);
    }

    private IEnumerator FadeSceneFromBlack()
    {
        if (sceneFadeImage == null)
            yield break;

        float timer = 0f;

        while (timer < sceneFadeOutTime)
        {
            timer += Time.deltaTime;

            float t = timer / sceneFadeOutTime;
            t = Mathf.Clamp01(t);
            t = Mathf.SmoothStep(0f, 1f, t);

            SetSceneFadeAlpha(1f - t);

            yield return null;
        }

        SetSceneFadeAlpha(0f);
        sceneFadeImage.raycastTarget = false;
    }

    private IEnumerator OpenBarsAndZoomOut()
    {
        float timer = 0f;

        float startOrthoSize = 0f;
        float startFOV = 0f;

        if (mainCamera != null)
        {
            startOrthoSize = mainCamera.orthographicSize;
            startFOV = mainCamera.fieldOfView;
        }

        Vector2 topStart = topBarStartPos;
        Vector2 bottomStart = bottomBarStartPos;

        Vector2 topEnd = topStart + new Vector2(0f, barMoveDistance);
        Vector2 bottomEnd = bottomStart - new Vector2(0f, barMoveDistance);

        while (timer < barOutTime)
        {
            timer += Time.deltaTime;

            float t = timer / barOutTime;
            t = Mathf.Clamp01(t);
            t = Mathf.SmoothStep(0f, 1f, t);

            // 카메라 줌 정상화
            if (mainCamera != null)
            {
                if (mainCamera.orthographic)
                {
                    mainCamera.orthographicSize = Mathf.Lerp(
                        startOrthoSize,
                        normalOrthographicSize,
                        t
                    );
                }
                else
                {
                    mainCamera.fieldOfView = Mathf.Lerp(
                        startFOV,
                        normalFieldOfView,
                        t
                    );
                }
            }

            // 위 검정 바 열기
            if (topBar != null)
            {
                topBar.anchoredPosition = Vector2.Lerp(
                    topStart,
                    topEnd,
                    t
                );
            }

            // 아래 검정 바 열기
            if (bottomBar != null)
            {
                bottomBar.anchoredPosition = Vector2.Lerp(
                    bottomStart,
                    bottomEnd,
                    t
                );
            }

            yield return null;
        }

        SetNormalZoom();

        if (topBar != null)
            topBar.anchoredPosition = topEnd;

        if (bottomBar != null)
            bottomBar.anchoredPosition = bottomEnd;
    }

    private void SetIntroZoom()
    {
        if (mainCamera == null)
            return;

        if (mainCamera.orthographic)
        {
            mainCamera.orthographicSize = introOrthographicSize;
        }
        else
        {
            mainCamera.fieldOfView = introFieldOfView;
        }
    }

    private void SetNormalZoom()
    {
        if (mainCamera == null)
            return;

        if (mainCamera.orthographic)
        {
            mainCamera.orthographicSize = normalOrthographicSize;
        }
        else
        {
            mainCamera.fieldOfView = normalFieldOfView;
        }
    }

    private void SetSceneFadeAlpha(float alpha)
    {
        if (sceneFadeImage == null)
            return;

        Color color = sceneFadeImage.color;
        color.a = alpha;
        sceneFadeImage.color = color;
    }

    private void SetCameraRootPositionXY(Vector3 targetPosition)
    {
        if (cameraRootToMove == null)
            return;

        Vector3 pos = cameraRootToMove.position;
        pos.x = targetPosition.x;
        pos.y = targetPosition.y;
        cameraRootToMove.position = pos;
    }

    private void SetScriptsEnabled(MonoBehaviour[] scripts, bool enabled)
    {
        if (scripts == null)
            return;

        foreach (MonoBehaviour script in scripts)
        {
            if (script != null)
                script.enabled = enabled;
        }
    }

    private void SetMoveAnimation(bool isMoving)
    {
        if (playerAnimator == null)
            return;

        if (string.IsNullOrEmpty(moveBoolName))
            return;

        playerAnimator.SetBool(moveBoolName, isMoving);
    }

    private void FaceMoveDirection(float directionX)
    {
        if (facingTarget == null)
            return;

        Vector3 scale = facingTarget.localScale;

        if (directionX > 0f)
        {
            scale.x = rightFacingScaleX;
        }
        else if (directionX < 0f)
        {
            scale.x = leftFacingScaleX;
        }

        facingTarget.localScale = scale;
    }
}