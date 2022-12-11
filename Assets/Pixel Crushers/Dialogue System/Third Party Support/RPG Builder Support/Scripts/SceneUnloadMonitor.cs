using UnityEngine;

namespace PixelCrushers.DialogueSystem.RPGBuilderSupport
{
    /// <summary>
    /// If present in a scene, performs actions required by Pixel Crushers 
    /// Save System when unloading a scene. Assumes scene is being unloaded
    /// when this component is disabled. DialogueSystemRPGBuilderBridge
    /// automatically adds this component to scenes.
    /// </summary>
    public class SceneUnloadMonitor : MonoBehaviour
    {
        private void OnDisable()
        {
            SaveSystem.BeforeSceneChange();
            SaveSystem.RecordSavedGameData();
        }
    }
}
