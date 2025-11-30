#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class PlayModeSaverTool
    {
        // Core functionality methods
        private static void CaptureSnapshot(string snapshotName)
        {
            if (isCapturing || settings == null) return;

            isCapturing = true;
            var snapshot = new PlayModeSnapshot
            {
                name = snapshotName,
                scenePath = SceneManager.GetActiveScene().path
            };

            try
            {
                // Only capture components from watched objects
                foreach (var watchedObj in settings.watchedObjects)
                {
                    if (!watchedObj.isEnabled || watchedObj.gameObject == null) continue;

                    var components = watchedObj.gameObject.GetComponents<Component>();
                    foreach (var component in components)
                    {
                        if (ShouldWatchComponent(component, watchedObj))
                        {
                            var componentSnapshot = SerializeComponent(component);
                            if (componentSnapshot != null)
                                snapshot.componentSnapshots.Add(componentSnapshot);
                        }
                    }
                }

                snapshots.Add(snapshot);

                // Limit snapshots
                while (snapshots.Count > settings.maxSnapshots && settings.maxSnapshots > 0)
                {
                    var oldestNonCheckpoint = snapshots.FirstOrDefault(s => !s.isCheckpoint);
                    if (oldestNonCheckpoint != null)
                        snapshots.Remove(oldestNonCheckpoint);
                    else
                        break;
                }

                if (settings.showToastNotifications)
                {
                    Debug.Log($"PlayModeSaver: Captured snapshot '{snapshotName}' with {snapshot.componentSnapshots.Count} components");
                }
            }
            finally
            {
                isCapturing = false;
            }
        }

        private static bool ShouldWatchComponent(Component component, WatchedObject watchedObj)
        {
            if (component == null) return false;

            string typeName = component.GetType().Name;

            if (watchedObj.watchAllComponents)
            {
                return true;
            }
            else
            {
                return watchedObj.watchedComponentTypes.Contains(typeName);
            }
        }

        private static ComponentSnapshot SerializeComponent(Component component)
        {
            try
            {
                var snapshot = new ComponentSnapshot
                {
                    gameObjectPath = GetGameObjectPath(component.gameObject),
                    componentType = component.GetType().Name,
                    instanceID = component.GetInstanceID(),
                    isEnabled = component is Behaviour behaviour ? behaviour.enabled : true,
                    serializedData = SerializeComponentWithSerializedObject(component)
                };

                return snapshot;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to serialize component {component.GetType().Name}: {e.Message}");
                return null;
            }
        }
    }
}
#endif
