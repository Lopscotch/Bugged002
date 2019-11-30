using System.Linq;
using UnityEditor;

[CustomEditor(typeof(SaveGameHandler)), CanEditMultipleObjects]
public class SaveGameHandlerEditor : Editor
{
    private SerializedProperty p_SaveLoadSettings;
    private SerializedProperty p_dataBetweenScenes;
    private SerializedProperty p_fadeControl;
    private SerializedProperty p_saveableDataPairs;
    private SaveGameHandler handler;

    void OnEnable()
    {
        handler = target as SaveGameHandler;
        p_SaveLoadSettings = serializedObject.FindProperty("SaveLoadSettings");
        p_dataBetweenScenes = serializedObject.FindProperty("dataBetweenScenes");
        p_fadeControl = serializedObject.FindProperty("fadeControl");
        p_saveableDataPairs = serializedObject.FindProperty("saveableDataPairs");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (handler.SaveLoadSettings != null)
        {
            if (handler.saveableDataPairs.Length > 0)
            {
                if (handler.saveableDataPairs.All(pair => pair.Instance != null))
                {
                    EditorGUILayout.HelpBox("Saveables: " + p_saveableDataPairs.arraySize, MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Some of saveable instances are missing! Please find scenes saveables again!", MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("If you want to find saveable objects, select Saveables Manager from Tools menu!", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Missing SaveLoadSettings!", MessageType.Error);
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Main Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(p_SaveLoadSettings);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Other Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(p_dataBetweenScenes);
        EditorGUILayout.PropertyField(p_fadeControl);

        serializedObject.ApplyModifiedProperties();
    }
}
