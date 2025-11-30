#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    [CreateAssetMenu(fileName = "SplineDataHolder", menuName = "MeyzToolbag/Spline Data Holder")]
    public class SplineDataHolder : ScriptableObject
    {
        public List<Vector3> points = new List<Vector3>();
    }
}
#endif