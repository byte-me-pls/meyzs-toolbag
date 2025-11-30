#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class RandomAreaDuplicationTool
    {
        // Entry for position generation (simple or Poisson)
        private static List<Vector3> GeneratePositions(Vector3 centerPos, float radiusVal, int targetCount, float minDist, bool useOverlapAvoidance, int seedVal)
        {
            var positions = new List<Vector3>();

            if (!useOverlapAvoidance || minDist <= 0.01f)
            {
                System.Random rng = new System.Random(seedVal);
                for (int i = 0; i < targetCount; i++)
                {
                    float angle = (float)(rng.NextDouble() * 2 * Mathf.PI);
                    float distance = (float)(rng.NextDouble() * radiusVal);
                    Vector3 pos = centerPos + new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
                    positions.Add(pos);
                }
            }
            else
            {
                positions = GeneratePoissonDiskSample(centerPos, radiusVal, minDist, targetCount, seedVal);
            }

            return positions;
        }

        // Poisson disk sampling in a circle
        private static List<Vector3> GeneratePoissonDiskSample(Vector3 centerPos, float radiusVal, float minDist, int targetCount, int seedVal)
        {
            System.Random rng = new System.Random(seedVal);
            var positions = new List<Vector3>();
            var activeList = new List<Vector3>();

            float cellSize = minDist / Mathf.Sqrt(2f);
            int gridWidth = Mathf.CeilToInt((radiusVal * 2) / cellSize);
            int gridHeight = Mathf.CeilToInt((radiusVal * 2) / cellSize);
            var grid = new int[gridWidth, gridHeight];
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    grid[x, y] = -1;

            Vector2Int WorldToGrid(Vector3 worldPos)
            {
                Vector3 relative = worldPos - centerPos + new Vector3(radiusVal, 0, radiusVal);
                return new Vector2Int(
                    Mathf.FloorToInt(relative.x / cellSize),
                    Mathf.FloorToInt(relative.z / cellSize)
                );
            }

            bool IsValidPosition(Vector3 pos)
            {
                if (Vector3.Distance(pos, centerPos) > radiusVal) return false;

                var gridPos = WorldToGrid(pos);
                if (gridPos.x < 0 || gridPos.x >= gridWidth || gridPos.y < 0 || gridPos.y >= gridHeight)
                    return false;

                int searchRadius = Mathf.CeilToInt(minDist / cellSize);
                for (int x = Mathf.Max(0, gridPos.x - searchRadius); x <= Mathf.Min(gridWidth - 1, gridPos.x + searchRadius); x++)
                    for (int y = Mathf.Max(0, gridPos.y - searchRadius); y <= Mathf.Min(gridHeight - 1, gridPos.y + searchRadius); y++)
                    {
                        int pointIndex = grid[x, y];
                        if (pointIndex != -1 && Vector3.Distance(pos, positions[pointIndex]) < minDist)
                            return false;
                    }

                return true;
            }

            // seed
            Vector3 firstPoint = centerPos;
            positions.Add(firstPoint);
            activeList.Add(firstPoint);
            var gp = WorldToGrid(firstPoint);
            if (gp.x >= 0 && gp.x < gridWidth && gp.y >= 0 && gp.y < gridHeight) grid[gp.x, gp.y] = 0;

            int maxAttempts = 30;
            while (activeList.Count > 0 && positions.Count < targetCount)
            {
                int randomIndex = rng.Next(activeList.Count);
                Vector3 currentPoint = activeList[randomIndex];
                bool foundValid = false;

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    float angle = (float)(rng.NextDouble() * 2 * Mathf.PI);
                    float distance = minDist + (float)(rng.NextDouble() * minDist); // [minDist, 2*minDist]

                    Vector3 newPoint = currentPoint + new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);

                    if (IsValidPosition(newPoint))
                    {
                        positions.Add(newPoint);
                        activeList.Add(newPoint);

                        var newGridPos = WorldToGrid(newPoint);
                        if (newGridPos.x >= 0 && newGridPos.x < gridWidth && newGridPos.y >= 0 && newGridPos.y < gridHeight)
                            grid[newGridPos.x, newGridPos.y] = positions.Count - 1;

                        foundValid = true;
                        break;
                    }
                }

                if (!foundValid)
                    activeList.RemoveAt(randomIndex);
            }

            // top-up
            if (positions.Count < targetCount)
            {
                int attempts = 0;
                while (positions.Count < targetCount && attempts < targetCount * 5)
                {
                    attempts++;
                    float angle = (float)(rng.NextDouble() * 2 * Mathf.PI);
                    float distance = (float)(rng.NextDouble() * radiusVal);
                    Vector3 pos = centerPos + new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);

                    bool validDistance = true;
                    foreach (var p in positions)
                    {
                        if (Vector3.Distance(pos, p) < minDist) { validDistance = false; break; }
                    }
                    if (validDistance) positions.Add(pos);
                }
            }

            return positions;
        }
    }
}
#endif
