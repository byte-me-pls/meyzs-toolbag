#if UNITY_EDITOR
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    [CreateAssetMenu(fileName = "GridDataHolder", menuName = "MeyzToolbag/Grid Data Holder")]
    public class GridDataHolder : ScriptableObject
    {
        public Vector2Int gridCount = new Vector2Int(3, 3);
        public Vector2 gridSpacing = new Vector2(2f, 2f);
        public bool alternateRows = false;
    }
}
#endif