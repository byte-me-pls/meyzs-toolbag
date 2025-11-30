#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace MeyzsToolBag.Editor.Performance
{
    public static partial class TextureAtlasPackerTool
    {
        // ====== MaxRects inner class ======
        private class MaxRectsPacker
        {
            private readonly int width;
            private readonly int height;
            private readonly bool allowRotation;
            private readonly List<Rect> freeRects = new List<Rect>();
            private readonly List<Rect> usedRects = new List<Rect>();

            public MaxRectsPacker(int w, int h, bool allowRotation = false)
            {
                width = w;
                height = h;
                this.allowRotation = allowRotation;
                freeRects.Add(new Rect(0, 0, w, h));
            }

            public Rect Insert(int rw, int rh)
            {
                var best = FindPositionForNewNodeBestAreaFit(rw, rh);
                if (best.width == 0) return best;

                for (int i = 0; i < freeRects.Count; i++)
                {
                    if (SplitFreeNode(freeRects[i], best))
                    {
                        freeRects.RemoveAt(i);
                        i--;
                    }
                }

                PruneFreeList();
                usedRects.Add(best);
                return best;
            }

            private Rect FindPositionForNewNodeBestAreaFit(int w, int h)
            {
                var best = new Rect(0, 0, 0, 0);
                int bestArea = int.MaxValue;

                foreach (var r in freeRects)
                {
                    if (r.width >= w && r.height >= h)
                    {
                        int area = (int)(r.width * r.height) - w * h;
                        if (area < bestArea)
                        {
                            best = new Rect(r.x, r.y, w, h);
                            bestArea = area;
                        }
                    }

                    if (allowRotation && r.width >= h && r.height >= w)
                    {
                        int area = (int)(r.width * r.height) - h * w;
                        if (area < bestArea)
                        {
                            best = new Rect(r.x, r.y, h, w);
                            bestArea = area;
                        }
                    }
                }

                return best;
            }

            private bool SplitFreeNode(Rect free, Rect used)
            {
                if (!free.Overlaps(used)) return false;

                if (used.x < free.x + free.width && used.x + used.width > free.x)
                {
                    if (used.y > free.y && used.y < free.y + free.height)
                    {
                        var newNode = free;
                        newNode.height = used.y - newNode.y;
                        freeRects.Add(newNode);
                    }

                    if (used.y + used.height < free.y + free.height)
                    {
                        var newNode = free;
                        newNode.y = used.y + used.height;
                        newNode.height = free.y + free.height - (used.y + used.height);
                        freeRects.Add(newNode);
                    }
                }

                if (used.y < free.y + free.height && used.y + used.height > free.y)
                {
                    if (used.x > free.x && used.x < free.x + free.width)
                    {
                        var newNode = free;
                        newNode.width = used.x - newNode.x;
                        freeRects.Add(newNode);
                    }

                    if (used.x + used.width < free.x + free.width)
                    {
                        var newNode = free;
                        newNode.x = used.x + used.width;
                        newNode.width = free.x + free.width - (used.x + used.width);
                        freeRects.Add(newNode);
                    }
                }

                return true;
            }

            private void PruneFreeList()
            {
                for (int i = 0; i < freeRects.Count; i++)
                {
                    for (int j = i + 1; j < freeRects.Count; j++)
                    {
                        if (IsContainedIn(freeRects[i], freeRects[j]))
                        {
                            freeRects.RemoveAt(i);
                            i--;
                            break;
                        }

                        if (IsContainedIn(freeRects[j], freeRects[i]))
                        {
                            freeRects.RemoveAt(j);
                            j--;
                        }
                    }
                }
            }

            private bool IsContainedIn(Rect a, Rect b)
            {
                return a.x >= b.x && a.y >= b.y &&
                       a.x + a.width <= b.x + b.width &&
                       a.y + a.height <= b.y + b.height;
            }
        }

        // ====== Skyline inner class ======
        private class SkylinePacker
        {
            private readonly int width;
            private readonly int height;
            private readonly List<Sky> skyline = new List<Sky> { new Sky(0, 0, 0, 0) };

            private struct Sky
            {
                public int x, y, w, h;

                public Sky(int x, int y, int w, int h)
                {
                    this.x = x;
                    this.y = y;
                    this.w = w;
                    this.h = h;
                }
            }

            public SkylinePacker(int w, int h)
            {
                width = w;
                height = h;
                skyline.Clear();
                skyline.Add(new Sky(0, 0, w, 0));
            }

            public Rect Insert(int rw, int rh)
            {
                int bestX = 0, bestY = int.MaxValue, bestIdx = -1;
                for (int i = 0; i < skyline.Count; i++)
                {
                    int y = GetLevel(i, rw);
                    if (y + rh <= height)
                    {
                        if (y < bestY || (y == bestY && skyline[i].x < bestX))
                        {
                            bestX = skyline[i].x;
                            bestY = y;
                            bestIdx = i;
                        }
                    }
                }

                if (bestIdx == -1) return new Rect();

                var rect = new Rect(bestX, bestY, rw, rh);
                AddLevel(bestIdx, bestX, bestY + rh, rw);
                return rect;
            }

            private int GetLevel(int idx, int w)
            {
                int x = skyline[idx].x;
                int y = skyline[idx].y;
                int avail = w;
                int i = idx;
                while (avail > 0 && i < skyline.Count)
                {
                    y = Mathf.Max(y, skyline[i].y);
                    avail -= (i + 1 < skyline.Count ? skyline[i + 1].x - skyline[i].x : width - skyline[i].x);
                    i++;
                }

                return y;
            }

            private void AddLevel(int idx, int x, int y, int w)
            {
                skyline.Insert(idx + 1, new Sky(x, y, 0, 0));

                // merge same height segments
                for (int i = 0; i < skyline.Count - 1; i++)
                {
                    if (skyline[i].y == skyline[i + 1].y && skyline[i].x <= skyline[i + 1].x)
                    {
                        skyline.RemoveAt(i + 1);
                        i--;
                    }
                }
            }
        }
    }
}
#endif
