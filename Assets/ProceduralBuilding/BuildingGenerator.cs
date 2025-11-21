using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    public List<GameObject> wallModules;
    public GameObject cornerPanel;

    public int floors = 4;
    public int wallCount = 4;
    public int floorHeight = 4;

    private GameObject _lastCorner;
    private float _wallDepth;
    private float _cornerDepth;
    private const float Shift = 6f;

    private readonly List<Renderer> lod0Renderers = new List<Renderer>();
    private Renderer lod1Renderer;

    private void Start()
    {
        if (wallModules == null || wallModules.Count == 0 || cornerPanel == null)
            return;

        var sample = wallModules[0].GetComponentInChildren<Renderer>();
        _wallDepth = sample.bounds.size.z;
        _cornerDepth = cornerPanel.GetComponentInChildren<Renderer>().bounds.size.z;

        if (GetComponent<LODGroup>() == null)
            gameObject.AddComponent<LODGroup>();

        StartCoroutine(GenerateBuildingAsync());
    }

    private IEnumerator GenerateBuildingAsync()
    {
        for (int floor = 0; floor < floors; floor++)
        {
            float yOffset = floor * floorHeight;
            var floorParent = new GameObject("Floor_" + floor);
            floorParent.transform.parent = transform;

            GenerateFloor(yOffset, floorParent.transform);

            yield return null;
        }

        GenerateRoof();
        SetupLODGroups();
    }

    private void GenerateFloor(float yOffset, Transform parent)
    {
        GenerateFirstFace(yOffset, parent);
        GenerateAdjacentFace(new Vector3(-Shift, 0, 0), yOffset, parent);
        GenerateAdjacentFace(new Vector3(0, 0, -Shift), yOffset, parent);
        GenerateAdjacentFace(new Vector3(Shift, 0, 0), yOffset, parent);
    }

    private void AddRenderersRecursive(GameObject obj)
    {
        var rs = obj.GetComponentsInChildren<Renderer>();
        foreach (var r in rs)
            lod0Renderers.Add(r);
    }


    private void GenerateFirstFace(float yOffset, Transform parent)
    {
        var p = transform.position + transform.forward * _cornerDepth;
        p.y = transform.position.y + yOffset;

        for (int i = 0; i < wallCount; i++)
        {
            GameObject module = wallModules[Random.Range(0, wallModules.Count)];
            var wp = Instantiate(module, new Vector3(p.x, p.y, p.z), transform.rotation, parent);
            AddRenderersRecursive(wp);
            p += transform.forward * _wallDepth;
        }

        var c = Instantiate(
            cornerPanel,
            new Vector3(p.x, transform.position.y + yOffset, p.z - 2f),
            transform.rotation,
            parent
        );

        _lastCorner = c;
        AddRenderersRecursive(c);
    }

    private void GenerateAdjacentFace(Vector3 offset, float yOffset, Transform parent)
    {
        var pos = new Vector3(
            _lastCorner.transform.position.x + offset.x,
            transform.position.y + yOffset,
            _lastCorner.transform.position.z + offset.z
        );

        var rot = _lastCorner.transform.rotation * Quaternion.Euler(0, -90f, 0);

        for (int i = 0; i < wallCount; i++)
        {
            GameObject module = wallModules[Random.Range(0, wallModules.Count)];
            var wp = Instantiate(module, pos, rot, parent);
            AddRenderersRecursive(wp);
            pos += rot * Vector3.forward * _wallDepth;
        }

        var c = Instantiate(
            cornerPanel,
            pos - rot * Vector3.forward * 2f,
            rot,
            parent
        );

        _lastCorner = c;
        AddRenderersRecursive(c);
    }

    private void GenerateRoof()
    {
        float width = wallCount * 4 + 4;
        float depth = wallCount * 4 + 4;
        float height = floors * floorHeight + 0.01f;

        GameObject roof = new GameObject("Roof");
        roof.transform.parent = transform;

        MeshFilter mf = roof.AddComponent<MeshFilter>();
        MeshRenderer mr = roof.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, height, 0),
            new Vector3(width, height, 0),
            new Vector3(0, height, depth),
            new Vector3(width, height, depth)
        };

        int[] triangles = new int[] { 0, 2, 1, 2, 3, 1 };

        Vector2[] uv = new Vector2[]
        {
            new Vector2(0,0),
            new Vector2(1,0),
            new Vector2(0,1),
            new Vector2(1,1)
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        mf.mesh = mesh;

        mr.sharedMaterial = wallModules[0].GetComponentInChildren<Renderer>().sharedMaterial;

        roof.transform.position = new Vector3(
            transform.position.x - width,
            transform.position.y,
            transform.position.z - 4f
        );

        AddRenderersRecursive(roof);
    }

    private void SetupLODGroups()
    {
        var group = GetComponent<LODGroup>();
        if (group == null || lod0Renderers.Count == 0)
            return;

        Bounds bounds = lod0Renderers[0].bounds;
        for (int i = 1; i < lod0Renderers.Count; i++)
            bounds.Encapsulate(lod0Renderers[i].bounds);

        GameObject lod1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lod1.transform.SetParent(transform);
        lod1.transform.position = bounds.center;
        lod1.transform.localRotation = Quaternion.identity;
        lod1.transform.localScale = bounds.size;

        var collider = lod1.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        lod1Renderer = lod1.GetComponent<Renderer>();
        lod1Renderer.sharedMaterial = lod0Renderers[0].sharedMaterial;

        LOD lod0 = new LOD(0.20f, lod0Renderers.ToArray());
        LOD lod1Level = new LOD(0.05f, new Renderer[] { lod1Renderer });

        group.SetLODs(new LOD[] { lod0, lod1Level });
        group.RecalculateBounds();
    }
}
