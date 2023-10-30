using UnityEditor;
using UnityEngine;

namespace SimpleRP.Runtime.PostProcessing
{
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
}