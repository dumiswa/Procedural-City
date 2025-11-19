using UnityEngine;

public class StreetLayoutGenerator : MonoBehaviour
{
    #region Inpector

    [Header("Prefabs")]
    [SerializeField] private GameObject roadPrefab;
    [SerializeField] private GameObject buildablePrefab;
    [SerializeField] private GameObject[,] buildableGrid;
    [SerializeField] private GameObject buildingWallPanel;
    [SerializeField] private GameObject buildingCornerPanel;

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
    private int seed;
    private int skyscrapersSpawned = 0;

    #endregion

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

    [ContextMenu("Randomize Layout")]
    public void RandomizeSeed()
    {
        ClearRoads();
        seed = System.DateTime.Now.Millisecond + Random.Range(0, 99999);
        Random.InitState(seed);
        GenerateCity();
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
        {
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
        {
            for (int c = 0; c < cols; c++)
            {
                float dx = c - center.x;
                float dy = r - center.y;
                float distSq = dx * dx + dy * dy;

                if (!createCore || distSq > radiusSq)
                {
                    for (int t = 0; t < gridThickness && InBounds(r + t, c); t++)
                        roadMap[r + t, c] = 1;
                }
            }
        }

        for (int c = 0; c < cols; c += blockSpacing)
        {
            for (int r = 0; r < rows; r++)
            {
                float dx = c - center.x;
                float dy = r - center.y;
                float distSq = dx * dx + dy * dy;

                if (!createCore || distSq > radiusSq)
                {
                    for (int t = 0; t < gridThickness && InBounds(r, c + t); t++)
                        roadMap[r, c + t] = 1;
                }
            }
        }
    }

    private int EvaluateWallCount(float d)
    {
        if (d < 0.2f) return Random.Range(6, 9);
        if (d < 0.5f) return Random.Range(4, 7);
        return Random.Range(2, 5);
    }

    private int EvaluateFloors(float d)
    {
        if (d < 0.2f) return Random.Range(30, 50);
        if (d < 0.5f) return Random.Range(10, 20);
        return Random.Range(3, 7);
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
                else if (d < 0.5f)
                {
                    buildingCount = Random.Range(1, 3);
                }
                else
                {
                    buildingCount = Random.Range(3, 7);
                }

                float tileMinX = startC * TILE_WORLD_SIZE + offsetX;
                float tileMaxX = (endC + 1) * TILE_WORLD_SIZE + offsetX;
                float tileMinZ = startR * TILE_WORLD_SIZE + offsetZ;
                float tileMaxZ = (endR + 1) * TILE_WORLD_SIZE + offsetZ;

                int tileWidthTiles = endC - startC + 1;
                int tileHeightTiles = endR - startR + 1;

                Vector3 tilePos = new Vector3(
                    (startC + endC + 1) * 0.5f * TILE_WORLD_SIZE + offsetX - TILE_WORLD_SIZE * 0.5f,
                    0f,
                    (startR + endR + 1) * 0.5f * TILE_WORLD_SIZE + offsetZ - TILE_WORLD_SIZE * 0.5f
                );

                var block = Instantiate(buildablePrefab, tilePos, Quaternion.identity, transform);
                block.transform.localScale = new Vector3(tileWidthTiles, 1f, tileHeightTiles);

                var centers = new System.Collections.Generic.List<Vector3>();
                var footprints = new System.Collections.Generic.List<float>();

                for (int i = 0; i < buildingCount; i++)
                {
                    int wc = EvaluateWallCount(d);
                    int fl = EvaluateFloors(d);

                    bool fits = false;
                    float footprint = 0f;
                    float half = 0f;

                    while (wc > 0 && !fits)
                    {
                        footprint = wc * 4f + 4f;
                        half = footprint * 0.5f;

                        if (footprint <= (tileMaxX - tileMinX) &&
                            footprint <= (tileMaxZ - tileMinZ))
                            fits = true;
                        else
                            wc--;
                    }

                    if (!fits || wc < 1)
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

                        float centerX = px - half;
                        float centerZ = pz + half;
                        Vector3 centerPos = new Vector3(centerX, 0f, centerZ);

                        bool tooClose = false;
                        for (int j = 0; j < centers.Count; j++)
                        {
                            float dist = Vector3.Distance(centerPos, centers[j]);
                            float req = (half + footprints[j] * 0.5f) - 0.5f;
                            if (dist < req)
                            {
                                tooClose = true;
                                break;
                            }
                        }

                        if (tooClose)
                            continue;

                        centers.Add(centerPos);
                        footprints.Add(footprint);

                        var root = new GameObject("Building");
                        root.transform.SetParent(block.transform);
                        root.transform.position = new Vector3(px - 4.8f, 0f, pz - 0.5f);

                        var b = root.AddComponent<BuildingGenerator>();
                        b.wallPanel = buildingWallPanel;
                        b.cornerPanel = buildingCornerPanel;
                        b.wallCount = wc;
                        b.floors = fl;

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
        {
            for (int c = 0; c < cols; c++)
            {
                if (roadMap[r, c] == 0) continue;

                Vector3 pos = new Vector3(c * TILE_WORLD_SIZE + offsetX, 0f, r * TILE_WORLD_SIZE + offsetZ);
                Instantiate(roadPrefab, pos, Quaternion.identity, transform);
            }
        }
    }

    private bool InBounds(int row, int col)
    {
        return row >= 0 && row < rows && col >= 0 && col < cols;
    }
}
