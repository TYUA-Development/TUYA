using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.ParticleSystem;


public class ParticleManager : MonoBehaviour
{
    [Header("TargetObject에 파티클을 생성할 대상, Particles에 파티클 넣기.\nparticles과 대상은 같은 index이여야한다. \n만약 대상 없이 맵에 뿌린다면 TargetObject를 none으로 설정 시 자동.")]
    [Space(10)]
    public List<GameObject> targetObject;
    [Header("Particle 생성은 Project/Create/Custom/Particle Preset")]
    public List<ParticleScriptable> particles;

    // Particle을 저장할 ObjectPool
    public List<Queue<GameObject>> particleObjectPool;

    // Particle의 생성 주기를 계산하고 생산명령을 내리는 emitter와 emitter 정보로 해당 particle의 index를 구하는 Dictionary
    public List<ParticleEmitter> emitters;
    public Dictionary<ParticleEmitter, int> DicParticleId;

    public Dictionary<int, GameObject> DicParticle;

    // 조건이 맞지 않다면 Particle Manager 파괴
    private void Awake()
    {
        if (!Init())
            Destroy(this.gameObject);
    }

    // 기본적인 List, Dictionary 초기화 및 ObjectPool 초기화
    private void Start()
    {
        particleObjectPool = new List<Queue<GameObject>>();
        emitters = new List<ParticleEmitter>();
        DicParticleId = new Dictionary<ParticleEmitter, int>();
        DicParticle = new Dictionary<int, GameObject>();

        ObjectPoolInit();
    }

    // 매번 현재 존재하는 Particle들의 
    private void Update()
    {
        // emitters를 통해 Particle의 생성 주기를 체크하고 파티클 생성
        for(int i = 0; i < emitters.Count; i++)
        {
            emitters[i].Tick(Time.deltaTime);
        }

    }

    // 오류 방지를 위해 ParticleManager의 필수 조건 확인
    public bool Init()
    {
        if(targetObject.Count != particles.Count)
        {
            Debug.Log("****** TargetObject, Particles Count not Match ******");
            return false;
        }
        return true;
    }

    // ObejctPool 초기화 함수
    public void ObjectPoolInit()
    {
        // Particle의 종류만큼 반복
        for(int i = 0; i < particles.Count; i++)
        {
            // ObjectPool이 될 queue 초기화
            Queue<GameObject> queue = new Queue<GameObject>();

            // 현재 Particle의 정보 가져오기
            ParticleScriptable scriptable = particles[i];

            // 현재 Particle을 관리할 emitter 생성 및 초기화
            ParticleEmitter emitter = new ParticleEmitter();
            emitter.Init(scriptable, SpawnParticle);
            emitter.SetParticle(true);

            // emitter로 어떤 particle인지 알아내기 위해 id와 매핑
            DicParticleId[emitter] = i;

            // 초기화한 emitter을 추가
            emitters.Add(emitter);

            // ObjectPool에 초기에 생성해서 넣을 파티클들 생성 및 초기화
            for (int j = 0; j < Mathf.RoundToInt(1 / (scriptable.createCycle / scriptable.survivalCycle)) * scriptable.createCount; j++)
            {
                // 새로운 Object 생성
                GameObject ob = new GameObject();
                // particleScript를 추가해 기본 Particle로 설정
                ParticleScript script = ob.AddComponent<ParticleScript>();
                // particle로 초기화
                script.Init();

                // 파티클의 이미지를 설정
                SpriteRenderer sprite = ob.AddComponent<SpriteRenderer>();
                sprite.sprite = scriptable.image;

                // 만약 spin 기능이 설정되어있다면
                if (scriptable.spin != ParticleScriptable.SpinDirection.None)
                {
                    // spin component를 해당 ob에 추가 및 초기화
                    ParticleSpin spin = ob.AddComponent<ParticleSpin>();
                    spin.Init(scriptable.spin, scriptable.spinSpeed);
                    // component로 저장
                    script.particleComponents.Add(spin);
                }

                // 만약 pulse 기능이 설정되어있다면
                if (scriptable.pulseType != ParticleScriptable.ParticlePulseType.None)
                {
                    // pulse component를 해당 ob에 추가 및 초기화
                    ParticlePulse pulse = ob.AddComponent<ParticlePulse>();
                    pulse.Init(scriptable.pulseType, scriptable.pulseSpeed, scriptable.pulseTime, scriptable.pulseCount);
                    // component로 저장
                    script.particleComponents.Add(pulse);
                }

                // 만약 fade 기능이 설정되어있다면
                if (scriptable.fadeType != ParticleScriptable.ParticleFadeType.None)
                {
                    // fade component를 해당 ob에 추가 및 초기화
                    ParticleFade fade = ob.AddComponent<ParticleFade>();
                    fade.Init(sprite, scriptable.fadeType, scriptable.fadeSpeed, scriptable.fadeInterval, scriptable.fadeTime, scriptable.fadeCount);
                    // component로 저장
                    script.particleComponents.Add(fade);
                }

                // 게임 오브젝트 비활성화
                ob.SetActive(false);
                // ObjectPool에 추가
                queue.Enqueue(ob);

                // id와 파티클을 매핑
                DicParticle[i] = ob;
            }

            // objectPool을 objectPool 리스트에 저장
            particleObjectPool.Add(queue);
            Debug.Log(particleObjectPool.Count);

        }
    }

    // particle을 objectPool에서 꺼내오는 함수
    // emitter가 생성주기 카운트가 끝나면 바인딩한 함수에 본인을 넘겨서 Object 꺼내기
    public void SpawnParticle(ParticleEmitter emitter)
    {
        int particleId = DicParticleId[emitter];

        // 생성 개수만큼 Object 꺼내기
        for(int i = 0; i < particles[particleId].createCount; i++)
        {
            GameObject scriptable;

            // 만약 objectPool에 Object가 있다면 꺼내기
            if (particleObjectPool[DicParticleId[emitter]].Count > 0)
                scriptable = particleObjectPool[DicParticleId[emitter]].Dequeue();
            // 없다면 새로 생성하기
            else
            {
                scriptable = Instantiate(DicParticle[particleId]);
                scriptable.GetComponent<ParticleScript>().Reset();
            }
            
            // 꺼낸 Object 활성화하기
            scriptable.SetActive(true);

            // Object의 데이터를 초기화
            scriptable.GetComponent<ParticleScript>().Init(scriptable, targetObject[particleId], particles[particleId], ReturnParticle, particleId);
        }
    }

    // 파티클의 생존 시간이 끝나면 해당 파티클의 component들을 리셋하고 ObjectPool에 되돌려 넣어주는 함수
    public void ReturnParticle(GameObject particle, int particleId)
    {
        // 해당 파티클의 Component들을 불러오기 위해 ParticleScript 가져오기
        ParticleScript script = particle.GetComponent<ParticleScript>();
        // 해당 파티클의 IParticleComponent들 불러와서 리셋
        foreach (IParticleComponent component in script.particleComponents)
        {
            component.Reset();
        }
        
        // 해당 파티클 Object 비활성화
        particle.SetActive(false);

        // 오브젝트 풀에 되돌려놓기
        particleObjectPool[particleId].Enqueue(particle);
    }
}
