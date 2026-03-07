using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSpin : MonoBehaviour, IParticleComponent
{
    private float speed;
    private int spinDirection;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0f, 0f, speed * spinDirection *  Time.deltaTime);
    }

    public void Init(ParticleScriptable.SpinDirection direction, float speed)
    {
        this.speed = speed;

        switch (direction)
        {
            case ParticleScriptable.SpinDirection.None:
                break;
            case ParticleScriptable.SpinDirection.Right:
                spinDirection = -1;
                break;
            case ParticleScriptable.SpinDirection.Left:
                spinDirection = 1;
                break;
            default:
                break;
        }
    }

    public void Reset()
    {
        transform.rotation = Quaternion.identity;
    }
}
