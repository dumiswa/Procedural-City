using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StreetLayoutGenerator))]
public class StreetLayoutGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        StreetLayoutGenerator gen = (StreetLayoutGenerator)target;
        GUILayout.Space(10);

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