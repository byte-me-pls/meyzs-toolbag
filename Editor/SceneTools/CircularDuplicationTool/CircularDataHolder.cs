#if UNITY_EDITOR
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    [CreateAssetMenu(fileName = "CircularDataHolder", menuName = "MeyzToolbag/Circular Data Holder")]
    public class CircularDataHolder : ScriptableObject
    {
        public int count = 6;
        public float radius = 3f;
        public Vector3 center = Vector3.zero;
        public float startAngle = 0f;
        public float endAngle = 360f;
        public bool faceCenter = true;
    }
}
#endif