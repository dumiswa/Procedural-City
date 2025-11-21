using System.Collections.Generic;
using UnityEngine;

public class StreetLayoutGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject roadPrefab;
    [SerializeField] private GameObject buildablePrefab;
    [SerializeField] private GameObject[,] buildableGrid;
    [SerializeField] private GameObject buildingCornerPanel;

    [Header("Wall Modules by Zone")]
    public List<GameObject> coreModules;
    public List<GameObject> middleModules;
    public List<GameObject> edgeModules;

    [Header("Core Settings")]
    public Vector2Int coreWallRange = new Vector2Int(6, 9);
    public Vector2Int coreFloorRange = new Vector2Int(30, 50);

    [Header("Middle Settings")]
    public Vector2Int middleWallRange = new Vector2Int(4, 7);
    public Vector2Int middleFloorRange = new Vector2Int(10, 20);

    [Header("Edge Settings")]
    public Vector2Int edgeWallRange = new Vector2Int(2, 5);
    public Vector2Int edgeFloorRange = new Vector2Int(3, 7);

    [Header("Grid Settings")]
    [SerializeField] private int rows = 60;
    [SerializeField] private int cols = 60;
    [SerializeField] private const float TILE_WORLD_SIZE = 10f;
    [SerializeField] private Vector3 origin = Vector3.zero;

    [Header("City Center")]
    [SerializeField, Range(2, 30)] private int centerRadius = 10;
    [SerializeField] private float centerThickness = 0.7f;
    [SerializeField] private bool isCenterFilled = false;
    [SerializeField] private bool createCore = false;

    [Header("Main Streets")]
    [SerializeField, Range(1, 300)] private int mainStreetLength = 20;
    [SerializeField, Range(1, 10)] private int mainStreetThickness = 1;
    [SerializeField] private bool createMainStreets = true;

    [Header("Grid Layout")]
    [SerializeField, Range(2, 20)] private int blockSpacing = 5;
    [SerializeField, Range(1, 5)] private int gridThickness = 1;
    [SerializeField] private bool createGrid = true;

    private int[,] roadMap;
    private Vector2 center;
    private int skyscrapersSpawned = 0;

    private void Start() => GenerateCity();

    [ContextMenu("Regenerate City")]
    public void GenerateCity()
    {
        ClearRoads();

        foreach (Transform child in transform)
            DestroyImmediate(child.gameObject);

        roadMap = new int[rows, cols];
        center = new Vector2(cols * 0.5f, rows * 0.5f);

        BuildCore();
        BuildMainStreets();
        BuildGridLayout();
        SpawnBuildableTiles();
        SpawnRoads();
    }

    [ContextMenu("Clear Roads")]
    public void ClearRoads()
    {
        foreach (Transform child in transform)
            DestroyImmediate(child.gameObject);
        roadMap = null;
    }

    private void BuildCore()
    {
        if (!createCore) return;

        float radiusSq = centerRadius * centerRadius;
        float inner = (centerRadius - centerThickness) * (centerRadius - centerThickness);
        float outer = (centerRadius + centerThickness) * (centerRadius + centerThickness);

        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                float dx = c - center.x;
                float dy = r - center.y;
                float dist = dx * dx + dy * dy;

                if (dist >= inner && dist <= outer)
                    roadMap[r, c] = 1;

                if (isCenterFilled && dist <= radiusSq)
                    roadMap[r, c] = 1;
            }
    }

    private void BuildMainStreets()
    {
        if (!createMainStreets) return;

        int cx = Mathf.RoundToInt(center.x);
        int cy = Mathf.RoundToInt(center.y);

        for (int i = -mainStreetThickness / 2; i <= mainStreetThickness / 2; i++)
        {
            for (int r = 0; r <= mainStreetLength; r++)
            {
                if (InBounds(cy + r, cx + i)) roadMap[cy + r, cx + i] = 1;
                if (InBounds(cy - r, cx + i)) roadMap[cy - r, cx + i] = 1;
            }

            for (int c = 0; c <= mainStreetLength; c++)
            {
                if (InBounds(cy + i, cx + c)) roadMap[cy + i, cx + c] = 1;
                if (InBounds(cy + i, cx - c)) roadMap[cy + i, cx - c] = 1;
            }
        }
    }

    private void BuildGridLayout()
    {
        if (!createGrid) return;

        float radiusSq = centerRadius * centerRadius;

        for (int r = 0; r < rows; r += blockSpacing)
            for (int c = 0; c < cols; c++)
            {
                float dx = c - center.x;
                float dy = r - center.y;
                float distSq = dx * dx + dy * dy;

                if (!createCore || distSq > radiusSq)
                    for (int t = 0; t < gridThickness && InBounds(r + t, c); t++)
                        roadMap[r + t, c] = 1;
            }

        for (int c = 0; c < cols; c += blockSpacing)
            for (int r = 0; r < rows; r++)
            {
                float dx = c - center.x;
                float dy = r - center.y;
                float distSq = dx * dx + dy * dy;

                if (!createCore || distSq > radiusSq)
                    for (int t = 0; t < gridThickness && InBounds(r, c + t); t++)
                        roadMap[r, c + t] = 1;
            }
    }

    private int GetWallCount(float d)
    {
        if (d < 0.2f) return Random.Range(coreWallRange.x, coreWallRange.y);
        if (d < 0.5f) return Random.Range(middleWallRange.x, middleWallRange.y);
        return Random.Range(edgeWallRange.x, edgeWallRange.y);
    }

    private int GetFloors(float d)
    {
        if (d < 0.2f) return Random.Range(coreFloorRange.x, coreFloorRange.y);
        if (d < 0.5f) return Random.Range(middleFloorRange.x, middleFloorRange.y);
        return Random.Range(edgeFloorRange.x, edgeFloorRange.y);
    }

    private void SpawnBuildableTiles()
    {
        float offsetX = -center.x * TILE_WORLD_SIZE;
        float offsetZ = -center.y * TILE_WORLD_SIZE;

        for (int r = 0; r < rows; r += blockSpacing)
        {
            for (int c = 0; c < cols; c += blockSpacing)
            {
                int startR = r + gridThickness;
                int startC = c + gridThickness;

                int endR = Mathf.Min(r + blockSpacing - 1, rows - 1);
                int endC = Mathf.Min(c + blockSpacing - 1, cols - 1);

                if (endR <= startR || endC <= startC)
                    continue;

                bool hasRoadInside = false;
                for (int rr = startR; rr <= endR && !hasRoadInside; rr++)
                    for (int cc = startC; cc <= endC; cc++)
                        if (roadMap[rr, cc] == 1)
                            hasRoadInside = true;

                if (hasRoadInside)
                    continue;

                float tileCenterX = (startC + endC) * 0.5f;
                float tileCenterY = (startR + endR) * 0.5f;
                float d = Vector2.Distance(new Vector2(tileCenterX, tileCenterY), center) /
                          (Mathf.Max(rows, cols) * 0.5f);

                int buildingCount;
                if (d < 0.2f && skyscrapersSpawned < 2)
                {
                    buildingCount = 1;
                    skyscrapersSpawned++;
                }
                else if (d < 0.5f) buildingCount = Random.Range(1, 3);
                else buildingCount = Random.Range(3, 7);

                float tileMinX = startC * TILE_WORLD_SIZE + offsetX;
                float tileMaxX = (endC + 1) * TILE_WORLD_SIZE + offsetX;
                float tileMinZ = startR * TILE_WORLD_SIZE + offsetZ;
                float tileMaxZ = (endR + 1) * TILE_WORLD_SIZE + offsetZ;

                int widthTiles = endC - startC + 1;
                int heightTiles = endR - startR + 1;

                Vector3 tilePos = new Vector3(
                    (startC + endC + 1) * TILE_WORLD_SIZE * 0.5f + offsetX - TILE_WORLD_SIZE * 0.5f,
                    0f,
                    (startR + endR + 1) * TILE_WORLD_SIZE * 0.5f + offsetZ - TILE_WORLD_SIZE * 0.5f
                );

                var block = Instantiate(buildablePrefab, tilePos, Quaternion.identity, transform);
                block.transform.localScale = new Vector3(widthTiles, 1f, heightTiles);

                var centers = new List<Vector3>();
                var footprints = new List<float>();

                for (int i = 0; i < buildingCount; i++)
                {
                    int wc = GetWallCount(d);
                    int fl = GetFloors(d);

                    float footprint = wc * 4f + 4f;
                    float half = footprint * 0.5f;

                    if (footprint > tileMaxX - tileMinX || footprint > tileMaxZ - tileMinZ)
                        continue;

                    float pivotMinX = tileMinX + footprint;
                    float pivotMaxX = tileMaxX;

                    float pivotMinZ = tileMinZ;
                    float pivotMaxZ = tileMaxZ - footprint;

                    if (pivotMaxX <= pivotMinX || pivotMaxZ <= pivotMinZ)
                        continue;

                    bool placed = false;

                    for (int attempt = 0; attempt < 12 && !placed; attempt++)
                    {
                        float px = Random.Range(pivotMinX, pivotMaxX);
                        float pz = Random.Range(pivotMinZ, pivotMaxZ);

                        float cx = px - half;
                        float cz = pz + half;

                        Vector3 cpos = new Vector3(cx, 0, cz);

                        bool tooClose = false;
                        for (int j = 0; j < centers.Count; j++)
                        {
                            float dist = Vector3.Distance(cpos, centers[j]);
                            float req = (half + footprints[j] * 0.5f) - 0.5f;
                            if (dist < req)
                            {
                                tooClose = true;
                                break;
                            }
                        }

                        if (tooClose) continue;

                        centers.Add(cpos);
                        footprints.Add(footprint);

                        var root = new GameObject("Building");
                        root.transform.SetParent(block.transform);
                        root.transform.position = new Vector3(px - 4.8f, 0f, pz - 0.5f);
                        root.isStatic = true;

                        var gen = root.AddComponent<BuildingGenerator>();
                        gen.cornerPanel = buildingCornerPanel;
                        gen.wallCount = wc;
                        gen.floors = fl;

                        if (d < 0.2f) gen.wallModules = coreModules;
                        else if (d < 0.5f) gen.wallModules = middleModules;
                        else gen.wallModules = edgeModules;

                        placed = true;
                    }
                }
            }
        }
    }

    private void SpawnRoads()
    {
        float offsetX = -center.x * TILE_WORLD_SIZE;
        float offsetZ = -center.y * TILE_WORLD_SIZE;

        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (roadMap[r, c] == 1)
                    Instantiate(roadPrefab, new Vector3(c * TILE_WORLD_SIZE + offsetX, 0f, r * TILE_WORLD_SIZE + offsetZ), Quaternion.identity, transform);
    }

    private bool InBounds(int r, int c)
    {
        return r >= 0 && r < rows && c >= 0 && c < cols;
    }
}
