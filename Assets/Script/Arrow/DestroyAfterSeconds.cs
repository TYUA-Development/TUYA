using UnityEngine;

public class DestroyAfterSeconds : MonoBehaviour
{
    public float destroyTime = 1.2f;

    private void Start()
    {
        Destroy(gameObject, destroyTime);
    }
}