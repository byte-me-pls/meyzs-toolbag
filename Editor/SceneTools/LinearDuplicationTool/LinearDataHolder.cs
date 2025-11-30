#if UNITY_EDITOR
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    [CreateAssetMenu(fileName = "LinearDataHolder", menuName = "MeyzToolbag/Linear Data Holder")]
    public class LinearDataHolder : ScriptableObject
    {
        public Vector3 offset = new Vector3(2, 0, 0);
        public bool useLocalSpace = false;
        public int count = 5;
    }
}
#endif