using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ParticlePreset", menuName = "Custom/Particle Preset")]
public class ParticleScriptable : ScriptableObject
{
    public enum ParticlePulse
    {
        None,
        Big,
        Small,
        Pulse,
    }

    [Header("Position")]
    public Transform position;

    [Header("Spawn")]
    public Sprite image;
    public float createCycle;
    public float survivalCycle;

    [Header("Option")]
    public bool spin;
    public float spinSpeed;

    [Space(10)]
    public bool pulse;
    public ParticlePulse type;
    public float pulseSpeed;
    public float pulseSize;
}