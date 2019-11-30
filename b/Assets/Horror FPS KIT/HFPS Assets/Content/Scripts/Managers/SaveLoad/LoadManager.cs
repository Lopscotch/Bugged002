using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using ThunderWire.JsonManager;
using HFPS.Prefs;

public class LoadManager : MonoBehaviour
{
    public const string LOAD_STATE = "LoadState";
    public const string LOAD_LEVEL_NAME = "LevelToLoad";
    public const string LOAD_SAVE_NAME = "LoadSaveName";

    public SaveLoadScriptable SaveLoadSettings;

    [Space(10)]

    public GameObject SavedGamePrefab;
    public Transform SavedGameContent;

    public Text EmptyText;
    public Button continueButton;
    public Button loadButton;
    public Button deleteButton;

    private string filepath;
    private FileInfo[] fi;
    private DirectoryInfo di;

    private string lastSave;
    private bool isSaveGame = false;

    private List<GameObject> saveCache = new List<GameObject>();
    private SavedGame selectedSave;

    void Start()
    {
        JsonManager.Settings(SaveLoadSettings);
        Time.timeScale = 1f;

        LoadSaves();
        InitializeContinue();

        loadButton.onClick.AddListener(Load);
        deleteButton.onClick.AddListener(Delete);
    }

    void LoadSaves()
    {
        filepath = JsonManager.GetCurrentPath();

        if (Directory.Exists(filepath))
        {
            di = new DirectoryInfo(filepath);
            fi = di.GetFiles("Save?.sav");

            if (fi.Length > 0)
            {
                EmptyText.gameObject.SetActive(false);

                for (int i = 0; i < fi.Length; i++)
                {
                    JsonManager.DeserializeData(fi[i].Name);
                    GameObject savedGame = Instantiate(SavedGamePrefab);
                    savedGame.transform.SetParent(SavedGameContent);
                    savedGame.transform.localScale = new Vector3(1, 1, 1);
                    savedGame.transform.SetSiblingIndex(0);
                    string scene = (string)JsonManager.Json()["scene"];
                    string date = (string)JsonManager.Json()["dateTime"];
                    savedGame.GetComponent<SavedGame>().SetSavedGame(fi[i].Name, scene, date);
                    saveCache.Add(savedGame);
                }
            }
            else
            {
                EmptyText.gameObject.SetActive(true);
            }
        }
    }

    void InitializeContinue()
    {
        if (Prefs.Exist(Prefs.LOAD_SAVE_NAME))
        {
            lastSave = Prefs.Game_SaveName();

            if (fi != null && fi.Length > 0 && fi.Any(x => x.Name == lastSave))
            {
                continueButton.interactable = true;
                isSaveGame = true;
            }
            else
            {
                if (File.Exists(JsonManager.GetFilePath(FilePath.GameDataPath) + lastSave))
                {
                    continueButton.interactable = true;
                }
                else
                {
                    continueButton.interactable = false;
                }

                isSaveGame = false;
            }
        }
    }

    public void Continue()
    {
        if (isSaveGame)
        {
            SavedGame saved = saveCache.Select(x => x.GetComponent<SavedGame>()).Where(x => x.save == lastSave).SingleOrDefault();
            Prefs.Game_LoadState(1);
            Prefs.Game_SaveName(saved.save);
            Prefs.Game_LevelName(saved.scene);
        }
        else
        {
            Prefs.Game_LoadState(2);
            Prefs.Game_SaveName(lastSave);
        }

        SceneManager.LoadScene(1);
    }

    public void Delete()
    {
        string pathToFile = filepath + selectedSave.save;
        File.Delete(pathToFile);

        foreach (Transform g in SavedGameContent)
        {
            Destroy(g.gameObject);
        }

        saveCache.Clear();
        LoadSaves();
    }

    public void Load()
    {
        Prefs.Game_LoadState(1);
        Prefs.Game_SaveName(selectedSave.save);
        Prefs.Game_LevelName(selectedSave.scene);

        SceneManager.LoadScene(1);
    }

    public void NewGame(string scene)
    {
        Prefs.Game_LoadState(0);
        Prefs.Game_SaveName(string.Empty);
        Prefs.Game_LevelName(scene);

        SceneManager.LoadScene(1);
    }

    void Update()
    {
        if(selectedSave != null)
        {
            loadButton.interactable = true;
            deleteButton.interactable = true;
        }
        else
        {
            loadButton.interactable = false;
            deleteButton.interactable = false;
        }

        if (EventSystem.current.currentSelectedGameObject)
        {
            GameObject select = EventSystem.current.currentSelectedGameObject;

            if (saveCache.Contains(select))
            {
                SelectSave(select);
            }
        }
        else
        {
            Deselect();
        }
    }

    private void SelectSave(GameObject SaveObject)
    {
        if (SaveObject.GetComponent<SavedGame>())
        {
            selectedSave = saveCache[saveCache.IndexOf(SaveObject)].GetComponent<SavedGame>();
        }
    }

    public void Deselect()
    {
        selectedSave = null;
    }

    public void Quit()
    {
        Application.Quit();
    }
}
