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

    private void Awake()
    {
        if (!Init())
            Destroy(this.gameObject);
    }

    private void Start()
    {
        particleObjectPool = new List<Queue<GameObject>>();
        emitters = new List<ParticleEmitter>();
        DicParticleId = new Dictionary<ParticleEmitter, int>();
        DicParticle = new Dictionary<int, GameObject>();

        ObjectPoolInit();
    }

    private void Update()
    {
        for(int i = 0; i < emitters.Count; i++)
        {
            emitters[i].Tick(Time.deltaTime, this);
        }

    }

    public bool Init()
    {
        if(targetObject.Count != particles.Count)
        {
            Debug.Log("****** TargetObject, Particles Count not Match ******");
            return false;
        }
        return true;
    }

    public void ObjectPoolInit()
    {
        for(int i = 0; i < particles.Count; i++)
        {
            Queue<GameObject> queue = new Queue<GameObject>();

            ParticleScriptable scriptable = particles[i];

            ParticleEmitter emitter = new ParticleEmitter();
            emitter.Init(scriptable);
            emitter.SetParticle(true);

            DicParticleId[emitter] = i;

            emitters.Add(emitter);


            for (int j = 0; j < Mathf.RoundToInt(1 / (scriptable.createCycle / scriptable.survivalCycle)) * scriptable.createCount; j++)
            {
                GameObject ob = new GameObject();
                ParticleScript script = ob.AddComponent<ParticleScript>();
                script.Init();

                SpriteRenderer sprite = ob.AddComponent<SpriteRenderer>();
                sprite.sprite = scriptable.image;

                if (scriptable.spin != ParticleScriptable.SpinDirection.None)
                {
                    ParticleSpin spin = ob.AddComponent<ParticleSpin>();
                    spin.Init(scriptable.spin, scriptable.spinSpeed);
                    script.particleComponents.Add(spin);
                }

                if (scriptable.pulseType != ParticleScriptable.ParticlePulseType.None)
                {
                    ParticlePulse pulse = ob.AddComponent<ParticlePulse>();
                    pulse.Init(scriptable.pulseType, scriptable.pulseSpeed, scriptable.pulseTime, scriptable.pulseCount);
                    script.particleComponents.Add(pulse);
                }

                if (scriptable.fadeType != ParticleScriptable.ParticleFadeType.None)
                {
                    ParticleFade fade = ob.AddComponent<ParticleFade>();
                    fade.Init(sprite, scriptable.fadeType, scriptable.fadeSpeed, scriptable.fadeInterval, scriptable.fadeTime, scriptable.fadeCount);
                    script.particleComponents.Add(fade);
                }

                ob.SetActive(false);
                queue.Enqueue(ob);

                DicParticle[i] = ob;
            }

            Debug.Log(queue.Count);

            particleObjectPool.Add(queue);
            Debug.Log(particleObjectPool.Count);

        }
    }

    public void SpawnParticle(ParticleEmitter emitter)
    {
        int particleId = DicParticleId[emitter];

        for(int i = 0; i < particles[particleId].createCount; i++)
        {
            GameObject scriptable;

            if (particleObjectPool[DicParticleId[emitter]].Count > 0)
                scriptable = particleObjectPool[DicParticleId[emitter]].Dequeue();
            else
            {
                scriptable = Instantiate(DicParticle[particleId]);
                scriptable.GetComponent<ParticleScript>().Reset();
                //particleObjectPool[DicParticleId[emitter]].Enqueue(scriptable);
            }
                
            scriptable.SetActive(true);

            scriptable.GetComponent<ParticleScript>().Init(scriptable, targetObject[particleId], particles[particleId], ReturnParticle, particleId);
        }
    }

    public void ReturnParticle(GameObject particle, int particleId)
    {
        ParticleScript script = particle.GetComponent<ParticleScript>();
        foreach (IParticleComponent component in script.particleComponents)
        {
            component.Reset();
        }
        
        particle.SetActive(false);

        particleObjectPool[particleId].Enqueue(particle);
    }
}
