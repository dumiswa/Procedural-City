using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public class StreetLayoutGenerator : MonoBehaviour
{
    #region Inpector

    [Header("Prefab")]
    [SerializeField] private GameObject roadPrefab;

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
    [Tooltip("City center thickness.")]
    [SerializeField] private float centerThickness = 0.7f;
    [Tooltip("Should the center be filled or simply an outline?")]
    [SerializeField] private bool isCenterFilled = false;

    [Header("Main Streets")]
    [Tooltip("Configure the 4 main streets starting in the city center.")]
    [SerializeField, Range(1, 300)] private int mainStreetLength = 20;
    [Tooltip("Thickness of the main streets.")]
    [SerializeField, Range(1, 10)] private int mainStreetThickness = 1;

    [Header("Grid Layout")]
    [Tooltip("Spacing between blocks in tiles.")]
    [SerializeField, Range(2, 20)] private int blockSpacing = 5;
    [Tooltip("Thickness of the grid streets.")]
    [SerializeField, Range(1, 5)] private int gridThickness = 1;


    private int[,] roadMap;
    private Vector2 center;
    private int seed;

    #endregion

    #region Unity LifeCycle

    private void Start()
    {
        GenerateCity();
    }

    #endregion

    #region GUI Component

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

        // Actually generate the city
        BuildCore();
        BuildMainStreets();
        BuildGridLayout();
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

    #endregion

    #region Layout Generation

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
                float modifier = centerThickness;

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

    private void BuildMainStreets()
    {
        int cx = Mathf.RoundToInt(center.x);
        int cy = Mathf.RoundToInt(center.y);

        for (int i = -mainStreetThickness / 2; i <= mainStreetThickness / 2; i++)
        {
            // North
            for (int r = 0; r <= mainStreetLength; r++)
            {
                int rr = cy + r;
                int cc = cx + i;
                if (InBounds(rr, cc)) roadMap[rr, cc] = 1;
            }

            // South
            for (int r = 0; r <= mainStreetLength; r++)
            {
                int rr = cy - r;
                int cc = cx + i;
                if (InBounds(rr, cc)) roadMap[rr, cc] = 1;
            }

            // East
            for (int c = 0; c <= mainStreetLength; c++)
            {
                int rr = cy + i;
                int cc = cx + c;
                if (InBounds(rr, cc)) roadMap[rr, cc] = 1;
            }

            // West
            for (int c = 0; c <= mainStreetLength; c++)
            {
                int rr = cy + i;
                int cc = cx - c;
                if (InBounds(rr, cc)) roadMap[rr, cc] = 1;
            }
        }
    }

    private void BuildGridLayout()
    {
        float radiusSq = centerRadius * centerRadius;

        // Horizontal streets
        for (int r = 0; r < rows; r += blockSpacing)
        {
            for (int c = 0; c < cols; c++)
            {
                float dx = c - center.x;
                float dy = r - center.y;
                float distSq = dx * dx + dy * dy;

                if (distSq > radiusSq && InBounds(r, c))
                {
                    for (int t = 0; t < gridThickness; t++)
                    {
                        int rr = r + t;
                        if (InBounds(rr, c))
                            roadMap[rr, c] = 1;
                    }
                }
            }
        }

        // Vertical streets
        for (int c = 0; c < cols; c += blockSpacing)
        {
            for (int r = 0; r < rows; r++)
            {
                float dx = c - center.x;
                float dy = r - center.y;
                float distSq = dx * dx + dy * dy;

                if (distSq > radiusSq && InBounds(r, c))
                {
                    for (int t = 0; t < gridThickness; t++)
                    {
                        int cc = c + t;
                        if (InBounds(r, cc))
                            roadMap[r, cc] = 1;
                    }
                }
            }
        }
    }

    private void SpawnRoads()
    {
        float offsetX = -center.x * tileWorldSize;
        float offsetZ = -center.y * tileWorldSize;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (roadMap[r, c] == 0) continue;

                Vector3 pos = new Vector3(c * tileWorldSize + offsetX, 0f, r * tileWorldSize + offsetZ);
                Instantiate(roadPrefab, pos, Quaternion.identity, transform);
            }
        }
    }

    private bool InBounds(int row, int col)
    {
        return row >= 0 && row < rows && col >= 0 && col < cols;
    }

    #endregion
}

