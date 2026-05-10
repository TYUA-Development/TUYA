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
            StartCoroutine(RotateCircleCoroutine(data.circle, data.addAngle));
        }
    }

    private IEnumerator RotateCircleCoroutine(GameObject target, float addAngle)
    {
        Transform circle = target.transform;

        Quaternion startRot = circle.localRotation;

        Quaternion targetRot =
            startRot * Quaternion.Euler(0f, 0f, addAngle);

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