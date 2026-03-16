using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindMillObject : MonoBehaviour, IArrowHit
{
    private StonePillarManager manager;
    private int windMillId;

    public void OnHit()
    {
        manager.PillarMove(windMillId);
    }

    public void Init(StonePillarManager manager, int windMillId)
    {
        this.manager = manager;
        this.windMillId = windMillId;
    }
}
