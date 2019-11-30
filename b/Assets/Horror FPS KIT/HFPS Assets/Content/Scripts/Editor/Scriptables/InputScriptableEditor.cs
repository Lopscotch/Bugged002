using UnityEditor;
using Malee.Editor;

[CustomEditor(typeof(InputScriptable)), CanEditMultipleObjects]
public class InputScriptableEditor : Editor
{
    InputScriptable database;
    ReorderableList re_list;

    SerializedProperty prop_rewrite;
    SerializedProperty list;

    private void OnEnable()
    {
        database = target as InputScriptable;
        list = serializedObject.FindProperty("inputMap");
        prop_rewrite = serializedObject.FindProperty("RewriteConfig");
        re_list = new ReorderableList(list);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(prop_rewrite);
        EditorGUILayout.Space();

        re_list.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}