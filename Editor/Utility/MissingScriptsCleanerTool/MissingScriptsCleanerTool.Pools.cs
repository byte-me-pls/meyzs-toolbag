#if UNITY_EDITOR
using System.Collections.Generic;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Simple List pool to reduce GC pressure.
    /// </summary>
    public static partial class MissingScriptsCleanerTool
    {
        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> pool = new Stack<List<T>>(64);

            public static List<T> Get()
            {
                if (pool.Count > 0)
                {
                    var l = pool.Pop();
                    if (l.Capacity > 4096) l.Capacity = 4096;
                    return l;
                }
                return new List<T>(16);
            }

            public static void Release(List<T> list)
            {
                list.Clear();
                pool.Push(list);
            }
        }
    }
}
#endif