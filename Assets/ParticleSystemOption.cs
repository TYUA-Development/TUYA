using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class ParticleSystemOption : MonoBehaviour
{
    public GameObject player;
    private PlayerController characterController;
    private ParticleSystem particle;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
        characterController = player.GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        if(characterController.currentState is PlayerMoveState)
        {
            var emission = particle.emission;
            emission.enabled = true;
            Debug.Log("move");
        }
        else
        {
            var emission = particle.emission;
            emission.enabled = false;
            Debug.Log("not move");
        }


        if (player.transform.localScale.x > 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }
}
