using UnityEngine;

public class StreetLayoutGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("Number of rows and columns in the city grid.")]
    [SerializeField] private int rows = 60;
    [SerializeField] private int cols = 60;

    [Tooltip("World-space distance between road tiles (set this to match prefab size).")]
    [SerializeField] private float tileWorldSize = 10f;

    [Tooltip("Offset of the city in world space.")]
    [SerializeField] private Vector3 origin = Vector3.zero;

    [Header("City Center")] 
    [Tooltip("Radius of center in tiles.")] 
    [SerializeField, Range(2, 30)] private int centerRadius = 10;
    [Tooltip("Should the center be filled or simply an outline?")]
    [SerializeField] private bool isCenterFilled = false;

    [Header("Street Density")]
    [Tooltip("Average distance between main roads (in tiles).")]
    [SerializeField] private int baseSpacing = 4;
    [Range(0f, 1f)][SerializeField] private float fillChance = 0.35f;

    [Header("Outskirts")]
    [Tooltip("Number of tentacle-like outer roads.")]
    [SerializeField] private int tentacleCount = 10;
    [Tooltip("Length (in tiles) for each tentacle road.")]
    [SerializeField] private int tentacleLength = 20;

    [Header("Prefab")]
    [SerializeField] private GameObject roadPrefab;

    private int[,] roadMap;
    private Vector2 center;
    private int seed;


    private void Start()
    {
        GenerateCity();
    }

    [ContextMenu("Regenerate City")]
    public void GenerateCity()
    {
        ClearRoads();

        if (roadPrefab == null)
        {
            Debug.LogWarning("No roadPrefab assigned!");
            return;
        }

        foreach (Transform child in transform)
        {
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }

        roadMap = new int[rows, cols];
        center = new Vector2(cols / 2f, rows / 2f);

        BuildCore();
        //BuildTransition();
        //BuildOutskirts();
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
        var children = new System.Collections.Generic.List<GameObject>();

        foreach (Transform child in transform)
            children.Add(child.gameObject);

        foreach (var child in children)
        {
            if (child != null)
                DestroyImmediate(child);
        }

        roadMap = null;
    }




    private void BuildCore()
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float dx = c - center.x;
                float dy = r - center.y;
                float distanceSq = dx * dx + dy * dy;
                float radiusSq = centerRadius * centerRadius;
                float modifier = 0.7f;

                // Only outline (like a ring)
                float inner = (centerRadius - modifier) * (centerRadius - modifier);
                float outer = (centerRadius + modifier) * (centerRadius + modifier);
                if (distanceSq >= inner && distanceSq <= outer)
                    roadMap[r, c] = 1;

                if (isCenterFilled)
                {
                    // Fill everything inside the circle

                    if (distanceSq <= radiusSq)
                        roadMap[r, c] = 1;
                }
            }
        }
        
    }




    /*private void BuildTransition()
    {
        for (int row = 0; row < rows; row += baseSpacing)
        {
            for (int col = 0; col < cols; col += baseSpacing)
            {
                Vector2 pos = new Vector2(col, row);
                float dist = Vector2.Distance(pos, center);
                float normalized = dist / (cols * centerRadius);
                float weight = Mathf.Clamp01(normalized);


                int jRow = Mathf.Clamp(Mathf.RoundToInt(row + Random.Range(-1f, 1f) * irregularity * baseSpacing), 0, rows - 1);
                int jCol = Mathf.Clamp(Mathf.RoundToInt(col + Random.Range(-1f, 1f) * irregularity * baseSpacing), 0, cols - 1);


                float chance = Mathf.Lerp(fillChance, 0.05f, weight);

                if (Random.value < chance)
                {
                    if (weight < circularity)
                    {
                        roadMap[jRow, jCol] = 1;
                    }
                    else
                    {

                        for (int x = -1; x <= 1; x++)
                            if (InBounds(jRow, jCol + x)) roadMap[jRow, jCol + x] = 1;
                        for (int y = -1; y <= 1; y++)
                            if (InBounds(jRow + y, jCol)) roadMap[jRow + y, jCol] = 1;
                    }
                }
            }
        }
    }


    private void BuildOutskirts()
    {
        int radius = Mathf.RoundToInt(cols * cityRadius);

        for (int i = 0; i < tentacleCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 start = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            int row = Mathf.RoundToInt(start.y);
            int col = Mathf.RoundToInt(start.x);

            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            for (int step = 0; step < tentacleLength; step++)
            {
                if (!InBounds(row, col)) break;

                roadMap[row, col] = 1;

                dir += new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f));
                dir.Normalize();

                row += Mathf.RoundToInt(dir.y);
                col += Mathf.RoundToInt(dir.x);
            }
        }
    }*/

    private void SpawnRoads()
    {
        if (roadMap == null) return;

        Vector3 baseOrigin = origin;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (roadMap[r, c] == 0) continue;

                Quaternion rot = Quaternion.identity;
                Vector3 pos = baseOrigin + new Vector3(c * tileWorldSize, 0f, r * tileWorldSize);
                Instantiate(roadPrefab, pos, rot, transform);
            }
        }
    }

    private bool InBounds(int row, int col)
    {
        return row >= 0 && row < rows && col >= 0 && col < cols;
    }
}

