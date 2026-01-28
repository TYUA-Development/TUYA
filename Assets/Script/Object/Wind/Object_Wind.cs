using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Object_Wind : MonoBehaviour
{
    public float windPower;

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

        return;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!collision.TryGetComponent(out Rigidbody2D rb))
            return;

        colliderList[collision] = rb;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        colliderList.Remove(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        foreach (var rb in colliderList.Values)
        {
            rb.velocity += power * Time.deltaTime;
            Debug.Log("바람 발동");
        }
    }
}
