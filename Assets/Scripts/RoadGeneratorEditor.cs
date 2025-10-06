using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StreetLayoutGenerator))]
public class SreetLayoutGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        StreetLayoutGenerator gen = (StreetLayoutGenerator)target;
        GUILayout.Space(10);

        if (GUILayout.Button("Randomize Layout"))
        {
            gen.RandomizeSeed();
        }

        if (GUILayout.Button("Regenerate Current"))
        {
            gen.GenerateCity();
        }

        if (GUILayout.Button("Clear Roads"))
        {
            gen.ClearRoads();
        }
    }
}