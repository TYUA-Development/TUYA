using System.Collections.Generic;
using UnityEngine;

public class Object_Wind : MonoBehaviour, ICoreEvent
{
    public float windPower;

    public bool blockPlayer;

    private Vector2 direction;
    private Vector2 power;

    private Dictionary<Collider2D, Rigidbody2D> colliderList = new Dictionary<Collider2D, Rigidbody2D> ();

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    public void Init()
    {
        float angle = transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
        direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        power = direction * windPower;
    }

    public void FixedUpdate()
    {
        foreach (var rb in colliderList.Values)
        {
            rb.velocity += power * Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (blockPlayer && collision.CompareTag("Player"))
            return;

        if (collision.TryGetComponent(out Rigidbody2D rb))
        {
            colliderList[collision] = rb;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        colliderList.Remove(collision);
    }

    public void OnCoreEvent()
    {
        
    }
}
