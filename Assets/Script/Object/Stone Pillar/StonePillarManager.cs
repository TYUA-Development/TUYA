using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public enum StonePillarDirection
{
    None = 0,
    Up = 1,
    Down = -1,
}

[System.Serializable]
public struct NodeStonePillar
{
    public int index;
}

[System.Serializable]
public struct StonePillar
{
    public int position;
    public int startStep;
    public StonePillarDirection moveDirection;
}

[System.Serializable]
public struct StoneWindMill
{
    public Vector3 windMillPosition;

    public List<NodeStonePillar> connection;
}

public class StonePillarManager : BasicObject
{
    public Sprite stonePillarImage;
    public GameObject windMill;
    public GameObject StonePillar;
    public int maxStep;
    public int minStep;
    public int stepHeight;
    public float moveDuration;

    [Header("WindMill")]
    public float windMillSpinSpeed;
    public float windMillSpinMinSpeed;

    [Space(10)]
    public List<StonePillar> pillars;
    public List<StoneWindMill> windMills;

    private List<GameObject> stonePillarObject;
    private List<GameObject> windMillObject;
    public Action[] OnMoveStart;
    public Action[] OnMoveEnd;

    private void Awake()
    {
        stonePillarObject = new List<GameObject>();
        windMillObject = new List<GameObject>();
    }

    // Start is called before the first frame update
    void Start()
    {
        OnMoveStart = new Action[pillars.Count];
        OnMoveEnd = new Action[pillars.Count];

        for(int i = 0; i < pillars.Count; i++)
        {
            Vector3 pos = new Vector3(pillars[i].position, pillars[i].startStep * stepHeight, 0);

            GameObject stonePillarOb = Instantiate(StonePillar, pos, Quaternion.identity);

            PerlinNoise noise = stonePillarOb.GetComponent<PerlinNoise>();

            if (noise != null)
            {
                OnMoveStart[i] += noise.noise;
            }

            stonePillarObject.Add(stonePillarOb);
        }

        for(int i = 0; i < windMills.Count; i++)
        {
            GameObject windMillOb = Instantiate(windMill);

            windMillOb.transform.localPosition = windMills[i].windMillPosition;


            WindMillObject windMillscript = windMillOb.GetComponent<WindMillObject>();

            windMillscript.Init(this, i);

            windMillObject.Add(windMillOb);
        }
    }

    public void PillarMove(int windMillId)
    {
        StoneWindMill target = windMills[windMillId];
        GameObject targetWindMill = windMillObject[windMillId];

        StartCoroutine(MoveWindMillCoroutine(targetWindMill, moveDuration, windMillSpinSpeed));

        for(int i = 0; i < target.connection.Count; i++)
        {
            int connectIndex = target.connection[i].index;

            StonePillar connectTarget = pillars[connectIndex];
            GameObject connectTargetPillar = stonePillarObject[connectIndex];

            StartCoroutine(MovePillarCoroutine(connectTargetPillar, GetNextPosition(connectTarget, connectTargetPillar), moveDuration, connectIndex));
        }
    }

    public Vector3 GetNextPosition(StonePillar target, GameObject targetPillar)
    {
        float ny = (int)target.moveDirection * stepHeight + targetPillar.transform.position.y;

        Vector3 npos = new Vector3(targetPillar.transform.position.x, ny, 0);

        Vector3 pos;

        if(npos.y > stepHeight * maxStep)
        {
            pos = new Vector3(npos.x, minStep * stepHeight, 0);
        }
        else if (npos.y < stepHeight * minStep)
        {
            pos = new Vector3(npos.x, maxStep * stepHeight, 0);
        }
        else
        {
            pos = npos;
        }

        return pos;
    }

    IEnumerator MovePillarCoroutine(GameObject target, Vector3 pos, float duration, int index)
    {
        Vector3 start = target.transform.position;
        float time = 0;

        while (time < duration)
        {
            target.transform.position = Vector3.Lerp(start, pos, time / duration);
            time += Time.deltaTime;

            OnMoveStart[index].Invoke();

            yield return null;
        }

        target.transform.position = pos;
        OnMoveEnd[index].Invoke();
    }

    IEnumerator MoveWindMillCoroutine(GameObject target, float duration, float speed)
    {
        float time = 0;
        float currentSpeed = 0;

        while (time < duration)
        {
            float t = time / duration;

            // 속도를 점점 감소
            currentSpeed = Mathf.Lerp(speed, windMillSpinMinSpeed, t);

            target.transform.Rotate(Vector3.forward * currentSpeed * Time.deltaTime);

            time += Time.deltaTime;

            yield return null;
        }

        time = 0;

        while (time < 0.1f)
        {
            target.transform.Rotate(Vector3.back * currentSpeed * Time.deltaTime);

            time += Time.deltaTime;
            yield return null;
        }

        time = 0;
        while (time < 0.1f)
        {
            target.transform.Rotate(Vector3.forward * currentSpeed * Time.deltaTime);

            time += Time.deltaTime;
            yield return null;
        }
    }
}
