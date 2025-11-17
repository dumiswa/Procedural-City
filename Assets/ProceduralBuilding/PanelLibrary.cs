using UnityEngine;

[CreateAssetMenu(menuName = "Procedural City/Panel Library")]
public class PanelLibrary : ScriptableObject
{
    [Header("Wall Panels")]
    public GameObject fullWindowPanel;
    public GameObject oneWindowPanel;
    public GameObject twoWindowPanel;
    public GameObject noWindowPanel;

    [Header("Corner Panel")]
    public GameObject cornerPanel;

    [Header("Materials for variation")]
    public Material[] materials;

    public GameObject GetRandomWallPanel()
    {
        GameObject[] set = new GameObject[]
        {
            fullWindowPanel,
            oneWindowPanel,
            twoWindowPanel,
            noWindowPanel
        };

        return set[Random.Range(0, set.Length)];
    }

    public Material GetRandomMaterial()
    {
        return materials[Random.Range(0, materials.Length)];
    }
}
