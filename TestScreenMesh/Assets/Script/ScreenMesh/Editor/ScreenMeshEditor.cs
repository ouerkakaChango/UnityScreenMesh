using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ScreenMesh))]
public class ScreenMeshEditor : Editor
{
    ScreenMesh Target;

    void OnEnable()
    {
        Target = (ScreenMesh)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("SaveMesh"))
        {
            Target.InitMesh();
            AssetDatabase.CreateAsset(Target.mesh, "Assets/ScreenBakedQuad.asset");
        }
    }
}
