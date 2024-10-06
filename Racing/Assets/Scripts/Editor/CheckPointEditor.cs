using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CheckPoint))]
public class CheckPointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CheckPoint script = (CheckPoint)target;
        
        if (GUILayout.Button("Set Wideness"))
        {
            script.SetWideness();
        }
    }
}
