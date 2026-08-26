using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ShockWaveFullScreenPassFeature : FullScreenPassRendererFeature
{
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.camera.cameraType == CameraType.SceneView)
        {
            return;
        }

        base.AddRenderPasses(renderer, ref renderingData);
    }
}
