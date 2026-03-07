using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class ParticleScript : MonoBehaviour
{
    private GameObject self;
    private float LifeTime;
    private int particleId;

    private Action<GameObject, int> callback;

    public List<IParticleComponent> particleComponents;

    public void Init()
    {
        particleComponents = new List<IParticleComponent>();
    }

    public void Init(GameObject self, GameObject parent, ParticleScriptable scriptable, Action<GameObject, int> returnObject, int particleId)
    {
        this.self = self;

        // 위치 설정
        if(!scriptable.random)
        {
            Vector3 position = scriptable.position;

            // 부모가 있으면 부모 위치와 파티클 좌표에 부모가 없다면 월드 맵 좌표에 스폰
            if (parent != null)
            {
                position += parent.transform.position;
            }

            self.transform.position = position;
        }
        else
        {
            self.transform.position = Camera.main.ViewportToWorldPoint(new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, 10f));
        }

        LifeTime = scriptable.survivalCycle;
        this.particleId = particleId;

        callback = returnObject;
    }

    private void Update()
    {
        LifeTime -= Time.deltaTime;
        if (LifeTime < 0)
            callback(self, particleId);
    }

    public void Reset()
    {
        particleComponents = new List<IParticleComponent>();

        foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
        {
            if (component is IParticleComponent particleComponent)
            {
                particleComponents.Add(particleComponent);
            }
        }
    }
}

public interface IParticleComponent
{
    public void Reset();
}