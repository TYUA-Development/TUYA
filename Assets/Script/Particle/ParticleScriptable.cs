using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ParticlePreset", menuName = "Custom/Particle Preset")]
public class ParticleScriptable : ScriptableObject
{
    public enum ParticlePulseType
    {
        None,
        Big,
        Small,
        PulseUp,
        PulseDown,
    }

    public enum SpinDirection
    {
        None,
        Right,
        Left,
    }

    public enum ParticleFadeType
    {
        None,
        FadeIn,
        FadeOut,
        FadeInOut,
        FadeOutIn,
    }


    [Header("Position")]
    [Tooltip("현재 화면에 랜덤 위치")]
    public bool random;
    [Tooltip("생성 위치(targetObject가 있다면 target 기준으로 position 위치에 생성)")]
    public Vector3 position;
    
    [Header("Spawn")]
    [Tooltip("생성할 Particle 이미지")]
    public Sprite image;
    [Tooltip("Particle의 생성 주기")]
    public float createCycle;
    [Tooltip("Particle의 생존 시간")]
    public float survivalCycle;
    [Tooltip("Particle이 한번에 생성될 갯수")]
    public int createCount = 1;

    [Header("Option")]
    [Tooltip("Particle이 회전할 방향(None은 회전 안함)")]
    public SpinDirection spin;
    [Tooltip("Particle의 회전 속도")]
    public float spinSpeed;

    [Space(10)]
    [Tooltip("Particle이 커졌다 작아졌다 여부\nPulseUp은 커졌다 작아지고 PulseDown은 작아졌다 커짐.")]
    public ParticlePulseType pulseType;
    [Tooltip("Particle의 pulse 속도")]
    public float pulseSpeed;
    [Tooltip("Particle이 얼만큼 커졌다가 작아질지")]
    public float pulseTime;
    [Tooltip("Particle이 pulse를 몇번 실행할지")]
    public int pulseCount = 1;

    [Space(10)]
    [Tooltip("Particle이 점점 흐려지거나 점점 선명해짐")]
    public ParticleFadeType fadeType;
    [Tooltip("Particle이 흐려지는 속도(1이 선명, 0이 투명)")]
    public float fadeSpeed;
    [Tooltip("Particle이 흐려지는 간격")]
    public float fadeInterval;
    [Tooltip("Particle이 흐려지는 시간")]
    public float fadeTime;
    [Tooltip("Particle이 몇번 fadein fadeout을 반복할지")]
    public int fadeCount = 1;
}