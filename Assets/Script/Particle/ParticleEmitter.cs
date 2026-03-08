using UnityEngine;

public class ParticleEmitter
{
    private float timer;
    private float createCycle;
    private int createCount;

    private bool isParticle;

    public void Init(ParticleScriptable scriptable)
    {
        createCycle = scriptable.createCycle;
        createCount = scriptable.createCount;

        Debug.Log(createCycle);
        Debug.Log(createCount);
    }

    public void Tick(float deltaTime, ParticleManager manager)
    {
        if (!isParticle)
        {
            return;
        }

        timer += deltaTime;

        while(timer > createCycle)
        {
            timer -= createCycle;

            for(int i = 0; i < createCount; ++i)
            {
                manager.SpawnParticle(this);
            }
        }
    }

    public void SetParticle(bool particleSetting)
    {
        isParticle = particleSetting;
    }
}
