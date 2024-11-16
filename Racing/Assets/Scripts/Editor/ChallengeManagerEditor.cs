using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChallengeManager))]
public class ChallengeManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ChallengeManager script = (ChallengeManager)target;
        
        if (GUILayout.Button("Set Vacant ID"))
        {
            script.SetVacantId();
        }
    }
}
