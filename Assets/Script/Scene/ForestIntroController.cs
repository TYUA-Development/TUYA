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
    public Transform facingTarget;
    public float rightFacingScaleX = -1f;
    public float leftFacingScaleX = 1f;

    [Header("Disable During Intro")]
    public MonoBehaviour[] playerControlScripts;
    public MonoBehaviour[] cameraFollowScripts;

    [Header("Camera")]
    public Camera mainCamera;
    public Transform cameraRootToMove;
    public Transform introCameraPoint;

    [Header("Camera Zoom - Orthographic")]
    public float introOrthographicSize = 3.0f;
    public float normalOrthographicSize = 5.5f;

    [Header("Camera Zoom - Perspective FOV")]
    public float introFieldOfView = 35f;
    public float normalFieldOfView = 60f;

    [Header("Movement")]
    public float moveSpeed = 2.8f;
    public Animator playerAnimator;
    public string moveBoolName = "IsMove";

    [Header("Letterbox Bars")]
    public RectTransform topBar;
    public RectTransform bottomBar;
    public float barMoveDistance = 260f;
    public float barOutTime = 1.2f;

    [Header("Scene Fade")]
    public Image sceneFadeImage;
    public float sceneFadeOutTime = 1.2f;

    [Header("Movement Tutorial")]
    public TutorialAreaPrompt movementTutorialPrompt;

    [Header("Timing")]
    public float startDelay = 0f;
    public float movingEndWaitTime = 0.45f;
    public float waitBeforeOpenBars = 0.2f;

    private Vector2 topBarStartPos;
    private Vector2 bottomBarStartPos;

    void Awake()
    {
        if(player == null)
        {
            player = FindObjectOfType<PlayerController>().gameObject.transform;
        }

        if(playerAnimator == null)
        {
            playerAnimator = player.gameObject.GetComponentInChildren<Animator>();
        }
    }

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
        SetScriptsEnabled(playerControlScripts, false);
        SetScriptsEnabled(cameraFollowScripts, false);

        if (player != null && startPoint != null)
            player.position = startPoint.position;

        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        if (cameraRootToMove != null && introCameraPoint != null)
            SetCameraRootPositionXY(introCameraPoint.position);

        SetIntroZoom();

        if (topBar != null)
            topBar.anchoredPosition = topBarStartPos;

        if (bottomBar != null)
            bottomBar.anchoredPosition = bottomBarStartPos;

        SetSceneFadeAlpha(1f);
        StartCoroutine(FadeSceneFromBlack());

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        if (player != null && targetPoint != null)
            FaceMoveDirection(targetPoint.position.x - player.position.x);

        SetMoveAnimation(true);

        float fixedY = player.position.y;
        float targetX = targetPoint.position.x;

        while (player != null)
        {
            float currentX = playerRigidbody != null ? playerRigidbody.position.x : player.position.x;
            if (Mathf.Abs(currentX - targetX) <= 0.03f)
                break;

            float dt = playerRigidbody != null ? Time.fixedDeltaTime : Time.deltaTime;
            float newX = Mathf.MoveTowards(currentX, targetX, moveSpeed * dt);
            Vector2 newPosition = new Vector2(newX, fixedY);

            if (playerRigidbody != null)
            {
                playerRigidbody.MovePosition(newPosition);
                yield return new WaitForFixedUpdate();
            }
            else
            {
                player.position = newPosition;
                yield return null;
            }
        }

        if (player != null && targetPoint != null)
        {
            Vector3 finalPos = player.position;
            finalPos.x = targetPoint.position.x;
            finalPos.y = fixedY;
            player.position = finalPos;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        SetMoveAnimation(false);

        yield return new WaitForSeconds(movingEndWaitTime);
        yield return new WaitForSeconds(waitBeforeOpenBars);

        yield return StartCoroutine(OpenBarsAndZoomOut());

        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        if (player != null && startPoint != null && targetPoint != null)
            FaceMoveDirection(targetPoint.position.x - startPoint.position.x);

        SetScriptsEnabled(cameraFollowScripts, true);
        SetScriptsEnabled(playerControlScripts, true);

        ShowMovementTutorial();
    }

    private void ShowMovementTutorial()
    {
        if (movementTutorialPrompt == null)
        {
            Debug.LogWarning("[ForestIntroController] Movement Tutorial Prompt�� ����ֽ��ϴ�.");
            return;
        }

        movementTutorialPrompt.hasShown = false;
        movementTutorialPrompt.isShowing = false;
        movementTutorialPrompt.ShowPrompt();
    }

    private IEnumerator FadeSceneFromBlack()
    {
        if (sceneFadeImage == null)
            yield break;

        float timer = 0f;

        while (timer < sceneFadeOutTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / sceneFadeOutTime);
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

            float t = Mathf.Clamp01(timer / barOutTime);
            t = Mathf.SmoothStep(0f, 1f, t);

            if (mainCamera != null)
            {
                if (mainCamera.orthographic)
                    mainCamera.orthographicSize = Mathf.Lerp(startOrthoSize, normalOrthographicSize, t);
                else
                    mainCamera.fieldOfView = Mathf.Lerp(startFOV, normalFieldOfView, t);
            }

            if (topBar != null)
                topBar.anchoredPosition = Vector2.Lerp(topStart, topEnd, t);

            if (bottomBar != null)
                bottomBar.anchoredPosition = Vector2.Lerp(bottomStart, bottomEnd, t);

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
            mainCamera.orthographicSize = introOrthographicSize;
        else
            mainCamera.fieldOfView = introFieldOfView;
    }

    private void SetNormalZoom()
    {
        if (mainCamera == null)
            return;

        if (mainCamera.orthographic)
            mainCamera.orthographicSize = normalOrthographicSize;
        else
            mainCamera.fieldOfView = normalFieldOfView;
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
            scale.x = rightFacingScaleX;
        else if (directionX < 0f)
            scale.x = leftFacingScaleX;

        facingTarget.localScale = scale;
    }
}