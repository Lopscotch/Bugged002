using UnityEditor;
using Malee.Editor;

[CustomEditor(typeof(GraphicScriptable)), CanEditMultipleObjects]
public class GraphicScriptableEditor : Editor
{
    GraphicScriptable graphic;
    ReorderableList re_list;
    SerializedProperty list;

    private void OnEnable()
    {
        graphic = target as GraphicScriptable;
        list = serializedObject.FindProperty("qualityLevels");
        re_list = new ReorderableList(list);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        re_list.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}
