/* SaveGameHandler.cs by ThunderWire Games
 * Version 3.1
 */

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ThunderWire.JsonManager;
using ThunderWire.Helpers;
using HFPS.Prefs;
using UnityEngine;

[Serializable]
public class SaveableDataPair
{
    public enum DataBlockType { ISaveable, Attribute }

    public DataBlockType BlockType = DataBlockType.ISaveable;
    public string BlockKey;
    public MonoBehaviour Instance;
    public string[] FieldData;

    public SaveableDataPair(DataBlockType type, string key, MonoBehaviour instance, string[] fileds)
    {
        BlockType = type;
        BlockKey = key;
        Instance = instance;
        FieldData = fileds;
    }
}

/// <summary>
/// Main script for Save/Load System
/// </summary>
public class SaveGameHandler : Singleton<SaveGameHandler> {

    public SaveLoadScriptable SaveLoadSettings;

    [Tooltip("Serialize player data between scenes.")]
    public bool dataBetweenScenes;

    [Tooltip("Not necessary, if you does not want Fade when scene starts or switch, leave this blank.")]
    public UIFadePanel fadeControl;

    public SaveableDataPair[] saveableDataPairs;

    private ItemSwitcher switcher;
    private Inventory inventory;
    private ObjectiveManager objectives;
    private GameObject player;

    [HideInInspector]
    public string lastSave;

    void Start()
    {
        inventory = GetComponent<Inventory>();
        objectives = GetComponent<ObjectiveManager>();
        player = GetComponent<HFPS_GameManager>().Player;
        switcher = player.GetComponentInChildren<ScriptManager>().GetScript<ItemSwitcher>();

        JsonManager.Settings(SaveLoadSettings, true);

        if (saveableDataPairs.Any(pair => pair.Instance == null))
        {
            Debug.LogError("[SaveGameHandler] Some of Saveable Instances are missing or it's destroyed. Please select Setup SaveGame again from the Tools menu!");
            return;
        }

        if (Prefs.Exist(Prefs.LOAD_STATE))
        {
            int loadstate = Prefs.Game_LoadState();

            if(loadstate == 0)
            {
                DeleteNextLvlData();
            }
            else if (loadstate == 1 && Prefs.Exist(Prefs.LOAD_SAVE_NAME))
            {
                string filename = Prefs.Game_SaveName();

                if (File.Exists(JsonManager.GetFilePath(FilePath.GameSavesPath) + filename))
                {
                    JsonManager.DeserializeData(filename);
                    string loadScene = (string)JsonManager.Json()["scene"];
                    lastSave = filename;

                    if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == loadScene)
                    {
                        LoadSavedSceneData(true);
                    }
                }
                else
                {
                    Debug.Log("<color=yellow>[SaveGameHandler]</color> Could not find load file: " + filename);
                    Prefs.Game_LoadState(0);
                }
            }
            else if(loadstate == 2 && Prefs.Exist(Prefs.LOAD_SAVE_NAME) && dataBetweenScenes)
            {
                JsonManager.ClearArray();
                Prefs.Game_SaveName("_NextSceneData.sav");

                if (File.Exists(JsonManager.GetFilePath(FilePath.GameDataPath) + "_NextSceneData.sav"))
                {
                    JsonManager.DeserializeData(FilePath.GameDataPath, "_NextSceneData.sav");
                    LoadSavedSceneData(false);
                }
            }
        }
    }

    void DeleteNextLvlData()
    {
        if (File.Exists(JsonManager.GetFilePath(FilePath.GameDataPath) + "_NextSceneData.sav"))
        {
            File.Delete(JsonManager.GetFilePath(FilePath.GameDataPath) + "_NextSceneData.sav");
        }
    }

    /* SAVE GAME SECTION */
    public void SaveGame(bool allData)
    {
        JsonManager.ClearArray();
        Dictionary<string, object> playerData = new Dictionary<string, object>();
        Dictionary<string, object> slotData = new Dictionary<string, object>();
        Dictionary<string, object> shortcutData = new Dictionary<string, object>();
        Dictionary<string, object> objectivesData = new Dictionary<string, object>();

        /* PLAYER PAIRS */
        if (allData)
        {
            JsonManager.AddPair("scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            JsonManager.AddPair("dateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            playerData.Add("playerPosition", player.transform.position);
            playerData.Add("cameraRotation", player.GetComponentInChildren<MouseLook>().GetRotation());
        }

        playerData.Add("playerHealth", player.GetComponent<HealthManager>().Health);
        /* END PLAYER PAIRS */

        /* ITEMSWITCHER PAIRS */
        Dictionary<string, object> switcherData = new Dictionary<string, object>
        {
            { "switcherActiveItem", switcher.currentItem },
            { "switcherLightObject", switcher.currentLightObject },
            { "switcherWeaponItem", switcher.weaponItem }
        };

        foreach (var Item in switcher.ItemList)
        {
            Dictionary<string, object> ItemInstances = new Dictionary<string, object>();

            foreach (var Instance in Item.GetComponents<MonoBehaviour>().Where(x => typeof(ISaveableArmsItem).IsAssignableFrom(x.GetType())).ToArray())
            {
                ItemInstances.Add(Instance.GetType().Name.Replace(" ", "_"), (Instance as ISaveableArmsItem).OnSave());
                switcherData.Add("switcher_item_" + Item.name.ToLower().Replace(" ", "_"), ItemInstances);
            }
        }
        /* END ITEMSWITCHER PAIRS */

        /* INVENTORY PAIRS */
        foreach (var slot in inventory.Slots)
        {
            if (slot.GetComponent<InventorySlot>().itemData != null)
            {
                InventoryItemData itemData = slot.GetComponent<InventorySlot>().itemData;
                Dictionary<string, object> itemDataArray = new Dictionary<string, object>
                {
                    { "slotID", itemData.slotID },
                    { "itemID", itemData.item.ID },
                    { "itemAmount", itemData.m_amount },
                    { "itemData", itemData.customData }
                };

                slotData.Add("inv_slot_" + inventory.Slots.IndexOf(slot), itemDataArray);
            }
            else
            {
                slotData.Add("inv_slot_" + inventory.Slots.IndexOf(slot), "null");
            }
        }

        Dictionary<string, object> inventoryData = new Dictionary<string, object>
        {
            { "inv_slots_count", inventory.Slots.Count },
            { "slotsData", slotData }
        };

        /* INVENTORY SHORTCUTS PAIRS */
        if (inventory.Shortcuts.Count > 0)
        {
            Dictionary<string, object> shortcutsData = new Dictionary<string, object>();
            foreach (var shortcut in inventory.Shortcuts)
            {
                Dictionary<string, object> shortcutsDataPairs = new Dictionary<string, object>
                {
                    { "itemID", shortcut.item.ID },
                    { "shortcutKey", shortcut.shortcutKey.ToString() }
                };

                shortcutsData.Add("shortcut_" + shortcut.slot, shortcutsDataPairs);
            }

            inventoryData.Add("shortcutsData", shortcutsData);
        }

        /* INVENTORY FIXED CONTAINER PAIRS */
        if (inventory.FixedContainerData.Count > 0)
        {
            inventoryData.Add("fixedContainerData", inventory.GetFixedContainerData());
        }
        /* END INVENTORY PAIRS */

        /* OBJECTIVE PAIRS */
        if (objectives.objectiveCache.Count > 0)
        {
            foreach (var obj in objectives.objectiveCache)
            {
                Dictionary<string, object> objectiveData = new Dictionary<string, object>
                {
                    { "toComplete", obj.toComplete },
                    { "isCompleted", obj.isCompleted }
                };

                objectivesData.Add(obj.identifier.ToString(), objectiveData);
            }
        }
        /* END OBJECTIVE PAIRS */

        //Add data pairs to serialization buffer
        JsonManager.AddPair("playerData", playerData);
        JsonManager.AddPair("itemSwitcherData", switcherData);
        JsonManager.AddPair("inventoryData", inventoryData);
        JsonManager.AddPair("objectivesData", objectivesData);

        //Add all saveables
        if (allData && saveableDataPairs.Length > 0)
        {
            foreach (var Pair in saveableDataPairs)
            {
                if(Pair.BlockType == SaveableDataPair.DataBlockType.ISaveable)
                {
                    var data = (Pair.Instance as ISaveable).OnSave();
                    if (data != null)
                    {
                        JsonManager.AddPair(Pair.BlockKey, data);
                    }
                }
                else if (Pair.BlockType == SaveableDataPair.DataBlockType.Attribute)
                {
                    Dictionary<string, object> attributeFieldPairs = new Dictionary<string, object>();

                    if (Pair.FieldData.Length > 0)
                    {
                        foreach (var Field in Pair.FieldData)
                        {
                            FieldInfo fieldInfo = Pair.Instance.GetType().GetField(Field);

                            if (fieldInfo.FieldType == typeof(Color) || fieldInfo.FieldType == typeof(KeyCode))
                            {
                                if (fieldInfo.FieldType == typeof(Color))
                                {
                                    attributeFieldPairs.Add(GetAttributeKey(fieldInfo), string.Format("#{0}", ColorUtility.ToHtmlStringRGBA((Color)Pair.Instance.GetType().InvokeMember(Field, BindingFlags.GetField, null, Pair.Instance, null))));
                                }
                                else
                                {
                                    attributeFieldPairs.Add(GetAttributeKey(fieldInfo), Pair.Instance.GetType().InvokeMember(Field, BindingFlags.GetField, null, Pair.Instance, null).ToString());
                                }
                            }
                            else
                            {
                                attributeFieldPairs.Add(GetAttributeKey(fieldInfo), Pair.Instance.GetType().InvokeMember(Field, BindingFlags.GetField, null, Pair.Instance, null));
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("Empty Fields Data: " + Pair.BlockKey);
                    }

                    JsonManager.AddPair(Pair.BlockKey, attributeFieldPairs);
                }
            }
        }

        //Serialize all pairs from buffer
        SerializeSaveData(!allData);
    }

    /* LOAD SECTION */
    void LoadSavedSceneData(bool allData)
    {
        if (allData)
        {
            var posToken = JsonManager.Json()["playerData"]["playerPosition"];
            player.transform.position = posToken.ToObject<Vector3>();

            var rotToken = JsonManager.Json()["playerData"]["cameraRotation"];
            player.GetComponentInChildren<MouseLook>().SetRotation(rotToken.ToObject<Vector2>());
        }

        var healthToken = JsonManager.Json()["playerData"]["playerHealth"];
        player.GetComponent<HealthManager>().Health = (float)healthToken;

        switcher.currentLightObject = (int)JsonManager.Json()["itemSwitcherData"]["switcherLightObject"];
        switcher.weaponItem = (int)JsonManager.Json()["itemSwitcherData"]["switcherWeaponItem"];

        //Deserialize ItemSwitcher Item Data
        foreach (var Item in switcher.ItemList)
        {
            JToken ItemToken = JsonManager.Json()["itemSwitcherData"]["switcher_item_" + Item.name.ToLower().Replace(" ", "_")];

            foreach (var Instance in Item.GetComponents<MonoBehaviour>().Where(x => typeof(ISaveableArmsItem).IsAssignableFrom(x.GetType())).ToArray())
            {
                (Instance as ISaveableArmsItem).OnLoad(ItemToken[Instance.GetType().Name.Replace(" ", "_")]);
            }
        }

        //Deserialize ItemSwitcher ActiveItem
        int switchID = (int)JsonManager.Json()["itemSwitcherData"]["switcherActiveItem"];
        if (switchID != -1)
        {
            switcher.ActivateItem(switchID);
        }

        //Deserialize Inventory Data
        StartCoroutine(DeserializeInventory(JsonManager.Json()["inventoryData"]));

        //Deserialize Objectives
        if (JsonManager.HasKey("objectivesData"))
        {
            Dictionary<int, Dictionary<string, string>> objectivesData = JsonManager.Json<Dictionary<int, Dictionary<string, string>>>(JsonManager.Json()["objectivesData"].ToString());

            foreach (var obj in objectivesData)
            {
                objectives.AddObjectiveModel(new ObjectiveModel(obj.Key, int.Parse(obj.Value["toComplete"]), bool.Parse(obj.Value["isCompleted"])));
            }
        }

        //Deserialize saveables 
        if (allData)
        {
            foreach (var Pair in saveableDataPairs)
            {
                JToken token = JsonManager.Json()[Pair.BlockKey];

                if (token == null) continue;

                if (Pair.BlockType == SaveableDataPair.DataBlockType.ISaveable)
                {
                    if (Pair.Instance.GetType() == typeof(SaveObject) && (Pair.Instance as SaveObject).saveType == SaveObject.SaveType.ObjectActive)
                    {
                        bool enabled = token["obj_enabled"].ToObject<bool>();
                        Pair.Instance.gameObject.SetActive(enabled);
                    }
                    else
                    {
                        (Pair.Instance as ISaveable).OnLoad(token);
                    }
                }
                else if (Pair.BlockType == SaveableDataPair.DataBlockType.Attribute)
                {
                    foreach (var Field in Pair.FieldData)
                    {
                        SetValue(Pair.Instance, Pair.Instance.GetType().GetField(Field), JsonManager.Json()[Pair.BlockKey][GetAttributeKey(Pair.Instance.GetType().GetField(Field))]);
                    }
                }
            }
        }
    }

    /* LOAD SECTION INVENTORY */
    private IEnumerator DeserializeInventory(JToken token)
    {
        yield return new WaitUntil(() => inventory.Slots.Count > 0);

        int slotsCount = (int)token["inv_slots_count"];
        int neededSlots = slotsCount - inventory.Slots.Count;

        if(neededSlots != 0)
        {
            inventory.ExpandSlots(neededSlots);
        }

        for (int i = 0; i < inventory.Slots.Count; i++)
        {
            JToken slotToken = token["slotsData"]["inv_slot_" + i];
            string slotString = slotToken.ToString();

            if (slotString != "null")
            {
                inventory.AddItemToSlot((int)slotToken["slotID"], (int)slotToken["itemID"], (int)slotToken["itemAmount"], slotToken["itemData"].ToObject<CustomItemData>());
            }
        }

        //Deserialize Shortcuts
        if (token["shortcutsData"] != null && token["shortcutsData"].HasValues)
        {
            Dictionary<string, Dictionary<string, string>> shortcutsData = token["shortcutsData"].ToObject<Dictionary<string, Dictionary<string, string>>>();

            foreach (var shortcut in shortcutsData)
            {
                int slot = int.Parse(shortcut.Key.Split('_')[1]);
                inventory.ShortcutBind(int.Parse(shortcut.Value["itemID"]), slot, (KeyCode)Enum.Parse(typeof(KeyCode), shortcut.Value["shortcutKey"]));
            }
        }

        //Deserialize FixedContainer
        if (token["fixedContainerData"] != null && token["fixedContainerData"].HasValues)
        {
            var fixedContainerData = token["fixedContainerData"].ToObject<Dictionary<int, JToken>>();

            foreach (var item in fixedContainerData)
            {
                inventory.FixedContainerData.Add(new ContainerItemData(inventory.GetItem(item.Key), (int)item.Value["item_amount"], item.Value["item_custom"].ToObject<CustomItemData>()));
            }
        }
    }

    string GetAttributeKey(FieldInfo Field)
    {
        SaveableField saveableAttr = Field.GetCustomAttributes(typeof(SaveableField), false).Cast<SaveableField>().SingleOrDefault();

        if (string.IsNullOrEmpty(saveableAttr.CustomKey))
        {
            return Field.Name.Replace(" ", string.Empty);
        }
        else
        {
            return saveableAttr.CustomKey;
        }
    }

    void SetValue(object instance, FieldInfo fInfo, JToken token)
    {
        Type type = fInfo.FieldType;
        string value = token.ToString();
        if (type == typeof(string)) fInfo.SetValue(instance, value);
        if (type == typeof(int)) fInfo.SetValue(instance, int.Parse(value));
        if (type == typeof(uint)) fInfo.SetValue(instance, uint.Parse(value));
        if (type == typeof(long)) fInfo.SetValue(instance, long.Parse(value));
        if (type == typeof(ulong)) fInfo.SetValue(instance, ulong.Parse(value));
        if (type == typeof(float)) fInfo.SetValue(instance, float.Parse(value));
        if (type == typeof(double)) fInfo.SetValue(instance, double.Parse(value));
        if (type == typeof(bool)) fInfo.SetValue(instance, bool.Parse(value));
        if (type == typeof(char)) fInfo.SetValue(instance, char.Parse(value));
        if (type == typeof(short)) fInfo.SetValue(instance, short.Parse(value));
        if (type == typeof(byte)) fInfo.SetValue(instance, byte.Parse(value));
        if (type == typeof(Vector2)) fInfo.SetValue(instance, token.ToObject(type));
        if (type == typeof(Vector3)) fInfo.SetValue(instance, token.ToObject(type));
        if (type == typeof(Vector4)) fInfo.SetValue(instance, token.ToObject(type));
        if (type == typeof(Quaternion)) fInfo.SetValue(instance, token.ToObject(type));
        if (type == typeof(KeyCode)) fInfo.SetValue(instance, Parser.Convert<KeyCode>(value));
        if (type == typeof(Color)) fInfo.SetValue(instance, Parser.Convert<Color>(value));
    }

    public void SaveNextSceneData(string scene)
    {
        Prefs.Game_LoadState(2);
        Prefs.Game_LevelName(scene);
        JsonManager.ClearArray();
        SaveGame(false);
    }

    async void SerializeSaveData(bool betweenScenes)
    {
        string filepath = JsonManager.GetFilePath(FilePath.GameSavesPath);
        GetComponent<HFPS_GameManager>().ShowSaveNotification();

        if (!betweenScenes)
        {
            if (Directory.Exists(filepath))
            {
                DirectoryInfo di = new DirectoryInfo(filepath);
                FileInfo[] fi = di.GetFiles("Save?.sav");

                if (fi.Length > 0)
                {
                    string SaveName = "Save" + fi.Length;
                    lastSave = SaveName + ".sav";
                    FileStream file = new FileStream(JsonManager.GetCurrentPath() + SaveName + ".sav", FileMode.OpenOrCreate);
                    await Task.Run(() => JsonManager.SerializeJsonDataAsync(file));
                }
                else
                {
                    lastSave = "Save0.sav";
                    FileStream file = new FileStream(JsonManager.GetCurrentPath() + "Save0.sav", FileMode.OpenOrCreate);
                    await Task.Run(() => JsonManager.SerializeJsonDataAsync(file));
                }
            }
            else
            {
                Directory.CreateDirectory(JsonManager.GetCurrentPath());

                lastSave = "Save0.sav";
                FileStream file = new FileStream(JsonManager.GetCurrentPath() + "Save0.sav", FileMode.OpenOrCreate);
                await Task.Run(() => JsonManager.SerializeJsonDataAsync(file));
            }

            Prefs.Game_SaveName(lastSave);
            DeleteNextLvlData();
        }
        else
        {
            DeleteNextLvlData();
            FileStream file = new FileStream(JsonManager.GetFilePath(FilePath.GameDataPath) + "_NextSceneData.sav", FileMode.OpenOrCreate);
            await Task.Run(() => JsonManager.SerializeJsonDataAsync(file, true));
        }
    }
}