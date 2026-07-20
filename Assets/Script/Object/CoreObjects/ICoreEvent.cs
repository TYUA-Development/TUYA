using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICoreEvent
{
    public void OnCoreEvent(bool isPressed = true);
}