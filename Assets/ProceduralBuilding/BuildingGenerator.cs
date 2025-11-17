using System.Collections;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    public GameObject wallPanel;
    public GameObject cornerPanel;

    public int floors = 4;
    public int wallCount = 4;
    public int floorHeight = 4;

    private GameObject _lastCorner;

    private float _wallDepth;    // 4 units
    private float _cornerDepth;  // 2 units
    private const float Shift = 6f; // CORNER (2) + WALL (4)

    private void Start()
    {
        _wallDepth = wallPanel.GetComponentInChildren<Renderer>().bounds.size.z;
        _cornerDepth = cornerPanel.GetComponentInChildren<Renderer>().bounds.size.z;

        StartCoroutine(GenerateBuildingAsync());
        //GenerateBuilding();
    }

    private IEnumerator GenerateBuildingAsync()
    {
        for (var floor = 0; floor < floors; floor++)
        {
            float yOffset = floor * floorHeight;

            var floorParent = new GameObject($"Floor_{floor}");
            floorParent.transform.parent = transform;

            GenerateFloor(yOffset, floorParent.transform);

            yield return null;
        }
    }

    private void GenerateFloor(float yOffset, Transform parent)
    {
        GenerateFirstFace(yOffset, parent);        // first face 
        GenerateAdjacentFace(new Vector3(-Shift, 0, 0), yOffset, parent); // Face 2: -X
        GenerateAdjacentFace(new Vector3(0, 0, -Shift), yOffset, parent); // Face 3: -Z
        GenerateAdjacentFace(new Vector3(Shift, 0, 0), yOffset, parent);  // Face 4: +X
    }

    //First face
    private void GenerateFirstFace(float yOffset, Transform parent)
    {
        var p = transform.position + transform.forward * _cornerDepth;
        p.y = transform.position.y + yOffset;

        // wall panels
        for (var i = 0; i < wallCount; i++)
        {
            var wp = new Vector3(p.x, transform.position.y + yOffset, p.z);
            Instantiate(wallPanel, wp, transform.rotation, parent);
            p += transform.forward * _wallDepth;
        }

        // last corner
        _lastCorner = Instantiate(
            cornerPanel,
            new Vector3(p.x, transform.position.y + yOffset, p.z - 2f),
            transform.rotation,
            parent
        );
    }

    //Faces 2,3 and 4
    private void GenerateAdjacentFace(Vector3 positionOffset, float yOffset, Transform parent)
    {
        var pos = new Vector3(
            _lastCorner.transform.position.x + positionOffset.x,
            transform.position.y + yOffset,
            _lastCorner.transform.position.z + positionOffset.z
        );

        // turn 90 degrees right
        var rot = _lastCorner.transform.rotation * Quaternion.Euler(0, -90f, 0);

        // wall panels
        for (var i = 0; i < wallCount; i++)
        {
            Instantiate(wallPanel, pos, rot, parent);
            pos += rot * Vector3.forward * _wallDepth;
        }

        // last corner becomes next starting point
        _lastCorner = Instantiate(
            cornerPanel,
            pos - rot * Vector3.forward * 2f,
            rot,
            parent
        );
    }
}
