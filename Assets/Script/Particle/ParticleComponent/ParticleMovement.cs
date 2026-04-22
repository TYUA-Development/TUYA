using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleMovement : MonoBehaviour, IParticleComponent
{
    private List<Vector3> pattern;
    private float speed;
    private int index;
    private Vector3 originPos;
    private Vector3 targetPos;
    private Vector3 initPos;
    private GameObject parent;

    public void Reset()
    {
        index = 0;
        transform.position = originPos;

        if (pattern == null || pattern.Count == 0)
            return;

        targetPos = originPos + pattern[index];
    }

    public void Init(List<Vector3> pattern, float speed, Vector3 initPos, GameObject self)
    {
        this.pattern = pattern;
        this.speed = speed;

        index = 0;
        originPos = transform.position;
        parent = self;

        if(parent != null) 
            originPos = parent.transform.position;

        this.initPos = initPos;
        targetPos = transform.position + pattern[index] + initPos;
    }

    public void OnSpawn()
    {
        index = 0;
        originPos = transform.position;

        if (pattern == null || pattern.Count == 0)
            return;

        if (parent != null)
            originPos = parent.transform.position;

        targetPos = originPos + pattern[index];
    }

    // Update is called once per frame
    void Update()
    {
        if (pattern == null || pattern.Count == 0)
            return;
        
        float distance = Vector3.Distance(transform.position, targetPos);

        if(distance > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPos;
            index = (index + 1) % pattern.Count;
            targetPos = transform.position + pattern[index];

        }
    }
}
