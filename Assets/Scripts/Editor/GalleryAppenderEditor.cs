using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GalleryAppender))]
public class GalleryAppenderEditor : Editor
{
    private GalleryAppender script;

    private void OnEnable()
    {
        script = (GalleryAppender)target;
    }

    public override void OnInspectorGUI ()
    {
        serializedObject.Update ();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("append"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("root"), true);
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("method"), true);

        switch (serializedObject.FindProperty("method").intValue)
        {
            case 1:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textAsset"), true);
                GUILayout.Label("See 'GalleryAppenderTextAssetExample' as an example");
                
                if (GUILayout.Button ("Convert")) 
                {
                    script.EditorTool();
                    EditorUtility.SetDirty (script);
                }
                break;
            case 2:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("runtimeFolder"), true);
                GUILayout.Label("Entries will always be null. Collection made at runtime on Append");
                break;
            case 3:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("runtimeFolder"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("assetToConvert"), true);
                
                if (GUILayout.Button ("Convert")) 
                {
                    script.EditorTool();
                    EditorUtility.SetDirty (script);
                }
                break;
            case 4:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("assetGroupLabels"), true);
                GUILayout.Label("Entries will always be null. Collection made at runtime on Append");
                break;
            case 5:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("assetToConvert"), true);
                GUILayout.Label("Entries must be addressable");
                
                if (GUILayout.Button ("Convert")) 
                {
                    script.EditorTool();
                    EditorUtility.SetDirty (script);
                }
                break;
            default:
                break;
        }
        
        GUILayout.Label("All entries must include file extensions; EG png/jpg/mp4/mov/");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("entries"), true);
        
        serializedObject.ApplyModifiedProperties ();
    }
}
