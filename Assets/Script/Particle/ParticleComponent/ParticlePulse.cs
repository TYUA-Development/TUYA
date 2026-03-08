using UnityEngine;
using static ParticleScriptable;

public class ParticlePulse : MonoBehaviour, IParticleComponent
{
    private ParticlePulseType pulseType;
    private float value;
    private float speed;
    private float time;
    private float timer;

    private float currentSizeX;
    private float currentSizeY;
    private Vector3 basicScale;

    private int pulseCount;
    private int counter;

    // Update is called once per frame
    void Update()
    {
        if (timer < 0.0f && counter == 0)
            return;

        else if(timer < 0.0f && counter != 0) 
        {
            timer = time;
            counter -= 1;
            value *= -1;
        }

        timer -= Time.deltaTime;

        currentSizeX += speed * value * Time.deltaTime;
        currentSizeY += speed * value * Time.deltaTime;

        Vector3 scale = new Vector3(currentSizeX, currentSizeY, 1);
        transform.localScale = scale;
    }

    public void Init(ParticlePulseType type, float speed, float time, int count)
    {
        pulseType = type;
        this.speed = speed;
        this.time = time;
        timer = time;
        pulseCount = count;
        counter = pulseCount;
        currentSizeX = transform.localScale.x;
        currentSizeY = transform.localScale.y;
        basicScale = transform.localScale;

        switch (type)
        {
            case ParticlePulseType.None:
                break;
            case ParticlePulseType.Big:
                pulseCount = 1;
                value = 1;
                break;
            case ParticlePulseType.Small:
                pulseCount = 1;
                value = -1;
                break;
            case ParticlePulseType.PulseUp:
                value = 1;
                break;
            case ParticlePulseType.PulseDown:
                value = -1;
                break;
            default:
                break;
        }
    }

    public void Reset()
    {
        timer = time;
        counter = pulseCount;

        currentSizeX = basicScale.x;
        currentSizeY = basicScale.y;
        transform.localScale = basicScale;

        switch (pulseType)
        {
            case ParticlePulseType.PulseUp:
                value = 1;
                break;
            case ParticlePulseType.PulseDown:
                value = -1;
                break;
            default:
                break;
        }
    }
}
