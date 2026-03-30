using System.Collections.Generic;
using UnityEngine;

public class ParallaxManager : MonoBehaviour
{
    public static ParallaxManager Instance { get; private set; }

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float globalStrength = 1f;

    private readonly List<ParallaxImage> activeImages = new List<ParallaxImage>();

    private Vector3 cameraStartPosition;
    private Vector3 previousCameraPosition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        if (cameraTransform != null)
        {
            cameraStartPosition = cameraTransform.position;
            previousCameraPosition = cameraTransform.position;
        }
    }

    public void Register(ParallaxImage image)
    {
        if (image == null)
            return;

        if (!activeImages.Contains(image))
            activeImages.Add(image);
    }

    public void Unregister(ParallaxImage image)
    {
        if (image == null)
            return;

        activeImages.Remove(image);
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        Vector3 currentCameraPosition = cameraTransform.position;

        if ((currentCameraPosition - previousCameraPosition).sqrMagnitude < 0.000001f)
            return;

        Vector3 totalCameraDelta = currentCameraPosition - cameraStartPosition;

        for (int i = activeImages.Count - 1; i >= 0; i--)
        {
            ParallaxImage image = activeImages[i];

            if (image == null)
            {
                activeImages.RemoveAt(i);
                continue;
            }

            image.ApplyParallaxFromStart(totalCameraDelta, globalStrength);
        }

        previousCameraPosition = currentCameraPosition;
    }
}