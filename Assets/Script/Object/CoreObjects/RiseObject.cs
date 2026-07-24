using System.Collections;
using UnityEngine;

public class RiseObject : MonoBehaviour
{
    [Header("Rise Target")]
    [Tooltip("오브젝트가 올라갈 목표 좌표(월드 좌표)")]
    public Vector3 targetPosition;

    [Tooltip("목표 좌표까지 올라가는데 걸리는 시간(초)")]
    public float riseDuration = 2f;

    [Tooltip("올라가기 전 대기하는 시간(초)")]
    public float startDelay = 0f;

    private Coroutine riseCoroutine;
    private bool hasRisen;

    public void Rise()
    {
        if (hasRisen)
            return;

        if (riseCoroutine != null)
            StopCoroutine(riseCoroutine);

        riseCoroutine = StartCoroutine(RiseRoutine());
    }

    private IEnumerator RiseRoutine()
    {
        hasRisen = true;

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        Vector3 startPosition = transform.position;
        float timer = 0f;

        while (timer < riseDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / riseDuration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        riseCoroutine = null;
    }
}
