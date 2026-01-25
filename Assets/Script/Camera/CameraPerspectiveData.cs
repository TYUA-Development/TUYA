using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Data
{
    [Header("Image")]
    public Sprite ObjectSprite;

    [Header("Distance")]
    public int Distance;

    [Header("Position")]
    public Vector3 Position;

    [Header("Order in Layer")]
    public int OrderValue;
}

[CreateAssetMenu(fileName = "CameraPerspectiveSettings", menuName = "Camera/Camera Perspective Settings")]
public class CameraPerspectiveData : ScriptableObject
{
    [Header("DATAS")]
    public List<Data> datas;
}
