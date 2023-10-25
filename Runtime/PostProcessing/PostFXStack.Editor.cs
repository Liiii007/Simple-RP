using UnityEditor;
using UnityEngine;

public partial class PostFXStack
{
    partial void CheckApplySceneViewState();

#if UNITY_EDITOR
    partial void CheckApplySceneViewState()
    {
        if (_camera.cameraType == CameraType.SceneView &&
            !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
        {
            _settings = null;
        }
    }
#endif
}