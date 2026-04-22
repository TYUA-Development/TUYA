using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class ParticleScript : MonoBehaviour
{
    // 미리 본인의 GameObject들을 캐싱
    protected GameObject self;
    // 생존 시간
    private float LifeTime;
    // 해당 파티클이 어떤 파티클인지 구별하기 위한 Id
    private int particleId;

    // Return 함수를 실행시키기 위해 함수 저장.(Manager를 참조하지 않기 위해 함수 바인딩)
    private Action<GameObject, int> callback;

    // 현재 Particle의 Component들을 저장할 리스트.(성능을 위해 컴포넌트 미리 캐싱)
    public List<IParticleComponent> particleComponents;

    public Vector3 basicScale;

    public void Init()
    {
        particleComponents = new List<IParticleComponent>();
        basicScale = transform.localScale;
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
            // 현재 화면에서 랜덤한 위치로 Position 지정
            self.transform.position = Camera.main.ViewportToWorldPoint(new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, 10f));
        }

        LifeTime = scriptable.survivalCycle;
        this.particleId = particleId;

        callback = returnObject;
        self.transform.localScale = basicScale * scriptable.scale;
    }

    // LifeTime이 끝나면 Particle 회수
    private void Update()
    {
        LifeTime -= Time.deltaTime;
        if (LifeTime < 0)
            callback(self, particleId);
    }

    // Particle이 ObjectPool에 없어서 추가할 시 컴포넌트들을 초기화 시켜주는 함수
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

// Particle들이 가질 컴포넌트들이 상속 받을 interface
public interface IParticleComponent
{
    public void Reset();
}