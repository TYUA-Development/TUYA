using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageUIInput : MonoBehaviour
{
    public static event Action OnEscapePressed;

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButtonDown("Cancel"))
        {
            OnEscapePressed?.Invoke();
        }
    }
}
