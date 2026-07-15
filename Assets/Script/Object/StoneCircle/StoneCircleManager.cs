using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct StoneCircleData
{
    public GameObject circle;
    public float addAngle;
}

[System.Serializable]
public struct StoneCircleTrigger
{
    public GameObject triggerObject;
    public List<StoneCircleData> connections;
}

public class StoneCircleManager : MonoBehaviour
{
    public List<StoneCircleTrigger> circleTriggers;

    public float rotateDuration = 1.0f;

    private readonly Dictionary<GameObject, Quaternion> currentTargetRotation = new Dictionary<GameObject, Quaternion>();
    private readonly Dictionary<GameObject, Coroutine> circleRotateCoroutines = new Dictionary<GameObject, Coroutine>();

    private void Start()
    {
        for (int i = 0; i < circleTriggers.Count; i++)
        {
            CircleHitObject hitObject = circleTriggers[i].triggerObject.GetComponent<CircleHitObject>();

            hitObject.Init(this, i);
        }
    }

    public void RotateCircles(int triggerId)
    {
        StoneCircleTrigger target = circleTriggers[triggerId];

        foreach (StoneCircleData data in target.connections)
        {
            GameObject circle = data.circle;

            Quaternion startRot = currentTargetRotation.TryGetValue(circle, out Quaternion loggedRot)
                ? loggedRot
                : circle.transform.localRotation;

            Quaternion targetRot = startRot * Quaternion.Euler(0f, 0f, data.addAngle);
            currentTargetRotation[circle] = targetRot;

            if (circleRotateCoroutines.TryGetValue(circle, out Coroutine running) && running != null)
            {
                StopCoroutine(running);
            }

            circleRotateCoroutines[circle] = StartCoroutine(RotateCircleCoroutine(circle, targetRot));
        }
    }

    private IEnumerator RotateCircleCoroutine(GameObject target, Quaternion targetRot)
    {
        Transform circle = target.transform;

        Quaternion startRot = circle.localRotation;

        float time = 0f;

        while (time < rotateDuration)
        {
            circle.localRotation = Quaternion.Lerp(
                startRot,
                targetRot,
                time / rotateDuration);

            time += Time.deltaTime;

            yield return null;
        }

        circle.localRotation = targetRot;
    }
}