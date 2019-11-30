using Diagnostics = System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ThunderWire.JsonManager;
using UsefulTools = ThunderWire.Utility;

public class SaveGameMenu : EditorWindow
{
    private const string menuPath = "Tools/HFPS KIT/SaveGame/";
    private const string filePath = "Assets/Horror FPS Kit/HFPS Assets/Scriptables/Game Settings/";

    [MenuItem(menuPath + "Delete All Saved Games")]
    static void DeleteSavedGame()
    {
        if (Directory.Exists(GetPath()))
        {
            string[] files = Directory.GetFiles(GetPath(), "Save?.sav");
            if (files.Length > 0)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    File.Delete(files[i]);
                }

                if (files.Length > 1)
                {
                    EditorUtility.DisplayDialog("SavedGames Deleted", $"{files.Length} Saved Games was deleted successfully.", "Okay");
                }
                else
                {
                    EditorUtility.DisplayDialog("SavedGames Deleted", "Saved Game was deleted successfully.", "Okay");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Directory empty", "Folder is empty.", "Okay");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Directory not found", "Failed to find Directory:  " + GetPath(), "Okay");
        }
    }

    [MenuItem(menuPath + "Saveables Manager")]
    static void SavedGameManager()
    {
        if (SaveGameHandler.Instance != null)
        {
            EditorWindow window = GetWindow<SaveGameMenu>(true, "Saveables Editor", true);
            window.minSize = new Vector2(500, 130);
            window.Show();
        }
        else
        {
            Debug.LogError("[SaveableManager] Could not find a SaveGameHandler script!");
        }
    }

    void OnGUI()
    {
        SaveGameHandler handler = SaveGameHandler.Instance;
        SerializedObject serializedObject = new SerializedObject(handler);
        SerializedProperty list = serializedObject.FindProperty("saveableDataPairs");

        GUIStyle boxStyle = GUI.skin.GetStyle("HelpBox");
        boxStyle.fontSize = 10;
        boxStyle.alignment = TextAnchor.MiddleCenter;

        int count = handler.saveableDataPairs.Length;
        string warning = "";
        MessageType messageType = MessageType.None;

        if (count > 0 && handler.saveableDataPairs.All(pair => pair.Instance != null))
        {
            warning = "SaveGame Handler is set up successfully!";
            messageType = MessageType.Info;
        }
        else if (count > 0 && handler.saveableDataPairs.Any(pair => pair.Instance == null))
        {
            warning = "Some of saveable instances are missing! Please find scene saveables again!";
            messageType = MessageType.Error;
        }
        else if(count < 1)
        {
            warning = "In order to use SaveGame feature in your scene, you must find saveables first!";
            messageType = MessageType.Warning;
        }

        EditorGUI.HelpBox(new Rect(1, 0, EditorGUIUtility.currentViewWidth - 2, 40), warning, messageType);
        EditorGUI.HelpBox(new Rect(1, 40, EditorGUIUtility.currentViewWidth - 2, 30), "Found Saveables: " + count, MessageType.None);

        GUIContent btnTxt = new GUIContent("Find Saveables");
        var rt = GUILayoutUtility.GetRect(btnTxt, GUI.skin.button, GUILayout.Width(150), GUILayout.Height(30));
        rt.center = new Vector2(EditorGUIUtility.currentViewWidth / 2, rt.center.y);
        rt.y = 80;

        if (GUI.Button(rt, btnTxt, GUI.skin.button))
        {
            SetupSaveGame();
        }
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }

    static void SetupSaveGame()
    {
        SaveGameHandler handler = SaveGameHandler.Instance;

        if (handler != null)
        {
            Diagnostics.Stopwatch stopwatch = new Diagnostics.Stopwatch();
            stopwatch.Reset();
            stopwatch.Start();

            var saveablesQuery = from Instance in UsefulTools.Tools.FindAllSceneObjects<MonoBehaviour>()
                                 where typeof(ISaveable).IsAssignableFrom(Instance.GetType()) && !Instance.GetType().IsInterface && !Instance.GetType().IsAbstract
                                 let key = string.Format("{0}_{1}", Instance.GetType().Name, System.Guid.NewGuid().ToString("N"))
                                 select new SaveableDataPair(SaveableDataPair.DataBlockType.ISaveable, key, Instance, new string[0]);

            var attributesQuery = from Instance in UsefulTools.Tools.FindAllSceneObjects<MonoBehaviour>()
                                  let attr = Instance.GetType().GetFields().Where(field => field.GetCustomAttributes(typeof(SaveableField), false).Count() > 0 && !field.IsLiteral && field.IsPublic).Select(fls => fls.Name).ToArray()
                                  let key = string.Format("{0}_{1}", Instance.GetType().Name, System.Guid.NewGuid().ToString("N"))
                                  where attr.Count() > 0
                                  select new SaveableDataPair(SaveableDataPair.DataBlockType.Attribute, key, Instance, attr);

            SaveableDataPair[] pairs = saveablesQuery.Union(attributesQuery).ToArray();
            stopwatch.Stop();

            handler.saveableDataPairs = pairs;
            EditorUtility.SetDirty(handler);

            Debug.Log("<color=green>[Setup SaveGame Successful]</color> Found Saveable Objects: " + pairs.Length + ", Time Elapsed: " + stopwatch.ElapsedMilliseconds + "ms");
        }
        else
        {
            Debug.LogError("[Setup SaveGame Error] To Setup SaveGame you need to Setup your scene first.");
        }
    }

    private static string GetPath()
    {
        if (Directory.Exists(filePath))
        {
            if (Directory.GetFiles(filePath).Length > 0)
            {
                return JsonManager.GetFilePath(AssetDatabase.LoadAssetAtPath<SaveLoadScriptable>(filePath + "SaveLoadSettings.asset").filePath);
            }
            return JsonManager.GetFilePath(FilePath.GameDataPath);
        }
        else
        {
            return JsonManager.GetFilePath(FilePath.GameDataPath);
        }
    }
}
