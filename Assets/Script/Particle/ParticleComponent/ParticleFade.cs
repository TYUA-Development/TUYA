using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ParticleScriptable;

public class ParticleFade : MonoBehaviour, IParticleComponent
{
    private ParticleFadeType fadeType;
    private SpriteRenderer spriteRenderer;
    private float speed;
    private float value;
    private float intervalTimer;
    private float interval;
    private float timer;
    private float time;
    private int fadeCount;
    private int counter;

    private Color basicColor;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            basicColor = spriteRenderer.color;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (timer < 0.0f && counter == 0)
            return;

        if (interval <= 0f)
            return;

        else if (timer < 0.0f && counter != 0)
        {
            timer = time;
            counter -= 1;
            value *= -1;
        }

        timer -= Time.deltaTime;
        intervalTimer += Time.deltaTime;

        while(intervalTimer > interval)
        {
            intervalTimer -= interval;

            Color color = spriteRenderer.color;
            color.a += value * speed;
            spriteRenderer.color = color;
        }
    }

    public void Init(SpriteRenderer sprite, ParticleFadeType type, float speed, float interval, float time, int count)
    {
        fadeType = type;

        spriteRenderer = sprite;
        this.speed = speed;

        this.interval = interval;
        intervalTimer = 0.0f;
        
        this.time = time;
        timer = time;

        fadeCount = count;
        counter = fadeCount;

        basicColor = spriteRenderer.color;

        switch (type)
        {
            case ParticleFadeType.None:
                break;
            case ParticleFadeType.FadeIn:
                value = 1;
                break;
            case ParticleFadeType.FadeOut:
                value = -1;
                break;
            case ParticleFadeType.FadeInOut:
                value = 1;
                break;
            case ParticleFadeType.FadeOutIn:
                value = -1;
                break;
            default:
                break;
        }
    }

    public void Reset()
    {
        counter = fadeCount;
        timer = time;
        intervalTimer = 0.0f;

        spriteRenderer.color = basicColor;

        switch (fadeType)
        {
            case ParticleFadeType.None:
                break;
            case ParticleFadeType.FadeIn:
                value = 1;
                break;
            case ParticleFadeType.FadeOut:
                value = -1;
                break;
            case ParticleFadeType.FadeInOut:
                value = 1;
                break;
            case ParticleFadeType.FadeOutIn:
                value = -1;
                break;
            default:
                break;
        }
    }
}
