/*
 * Inventory.cs - script by ThunderWire Games
 * ver. 1.6
*/

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ThunderWire.Helpers;
using ThunderWire.Utility;

/// <summary>
/// Main Inventory Script
/// </summary>
public class Inventory : Singleton<Inventory> {

    private HFPS_GameManager gameManager;
    private ItemSwitcher switcher;
    private InventoryContainer currentContainer;
    private ObjectiveManager objectives;
    private UIFader fader;

    public class ShortcutModel
    {
        public Item item;
        public int slot;
        public KeyCode shortcutKey;

        public ShortcutModel(Item item, int slot, KeyCode key)
        {
            this.item = item;
            this.slot = slot;
            this.shortcutKey = key;
        }
    }

    [HideInInspector]
    public List<GameObject> Slots = new List<GameObject>();
    [HideInInspector]
    public List<ShortcutModel> Shortcuts = new List<ShortcutModel>();

    [HideInInspector]
    public List<ContainerItemData> FixedContainerData = new List<ContainerItemData>();
    [HideInInspector]
    public List<ContainerItem> ContainterItemsCache = new List<ContainerItem>();

    private List<Item> AllItems = new List<Item>();
    private List<InventoryItem> ItemsCache = new List<InventoryItem>();

    [Tooltip("Database of all inventory items.")]
    public InventoryScriptable inventoryDatabase;

    [Header("Panels")]
    public GameObject ContainterPanel;
    public GameObject ItemInfoPanel;

    [Header("Contents")]
    public GameObject SlotsContent;
    public GameObject ContainterContent;

    [Space(7)]
    public Text ItemLabel;
    public Text ItemDescription;
    public Text ContainerNameText;
    public Text ContainerEmptyText;
    public Text ContainerCapacityText;
    public Image InventoryNotification;
    public Button TakeBackButton;

    [Header("Contex Menu")]
    public GameObject contexMenu;
    public Button contexUse;
    public Button contexCombine;
    public Button contexExamine;
    public Button contexDrop;
    public Button contexStore;
    public Button contexShortcut;

    [Header("Inventory Prefabs")]
    public GameObject inventorySlot;
    public GameObject inventoryItem;
    public GameObject containerItem;

    [Header("Slot Settings")]
    public Sprite slotsNormal;
    public Sprite slotWithItem;
    public Sprite slotItemSelect;
    public Sprite slotItemOutline;

    [Header("Inventory Items")]
    public int slotAmount;
    public int cornerSlot;
    public int maxSlots = 16;

    [Header("Inventory Settings")]
    public KeyCode TakeBackKey = KeyCode.Space;
    public bool ContainerTakeBackOne;
    public int itemDropStrenght = 10;
    public Color slotDisabled = Color.white;

    private bool notiFade;
    private bool isPressed;
    private bool isKeyUp;
    private bool isFixed;
    private bool canDeselect;

    private bool shortcutBind;
    private int selectedBind;

    [HideInInspector]
    public bool isDragging;

    [HideInInspector]
    public bool isStoring;

    [HideInInspector]
    public int selectedSlotID;

    [HideInInspector]
    public int selectedSwitcherID = -1;

    private ContainerItem selectedCoItem;
    private MonoBehaviour selectedScript;

    public ItemSwitcher GetSwitcher()
    {
        return switcher;
    }

    void Awake()
    {
        if (!inventoryDatabase) { Debug.LogError("Inventory Database does not set!"); return; }

        for (int i = 0; i < inventoryDatabase.ItemDatabase.Count; i++)
        {
            AllItems.Add(new Item(i, inventoryDatabase.ItemDatabase[i]));
        }

        for (int i = 0; i < slotAmount; i++)
        {
            GameObject slot = Instantiate(inventorySlot);
            Slots.Add(slot);
            slot.GetComponent<InventorySlot>().inventory = this;
            slot.GetComponent<InventorySlot>().slotID = i;
            slot.transform.SetParent(SlotsContent.transform);
            slot.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
        }
    }

    void Start() {
        gameManager = GetComponent<HFPS_GameManager>();
        objectives = GetComponent<ObjectiveManager>();
        fader = new UIFader();

        ItemLabel.text = "";
        ItemDescription.text = "";
        ShowContexMenu(false);
        ItemInfoPanel.SetActive(false);
        TakeBackButton.gameObject.SetActive(false);
        canDeselect = true;

        selectedSlotID = -1;
    }

    void Update()
    {
        if (!switcher) {
            switcher = gameManager.scriptManager.GetScript<ItemSwitcher>();
        }
        else
        {
            selectedSwitcherID = switcher.currentItem;
        }

        if (!gameManager.TabButtonPanel.activeSelf)
        {
            selectedScript = null;
            ShowContexMenu(false);
            ItemInfoPanel.SetActive(false);
            isStoring = false;
            shortcutBind = false;
            fader.fadeOut = true;

            foreach (var item in ContainterContent.GetComponentsInChildren<ContainerItem>())
            {
                Destroy(item.gameObject);
            }

            if (currentContainer)
            {
                currentContainer.isOpened = false;
                currentContainer = null;
            }

            ContainterPanel.SetActive(false);
            TakeBackButton.gameObject.SetActive(false);
            objectives.ShowObjectives(true);

            foreach (var slot in Slots)
            {
                slot.GetComponent<InventorySlot>().isCombining = false;
                slot.GetComponent<InventorySlot>().isCombinable = false;
                slot.GetComponent<InventorySlot>().contexVisible = false;
            }

            StopAllCoroutines();

            ContainterItemsCache.Clear();
            InventoryNotification.gameObject.SetActive(false);
            notiFade = false;
        }

        if (currentContainer != null || isFixed)
        {
            if (isFixed)
            {
                selectedCoItem = ContainterItemsCache.SingleOrDefault(item => item.IsSelected());
            }
            else if(currentContainer.IsSelecting())
            {
                selectedCoItem = currentContainer.GetSelectedItem();
            }
            else
            {
                selectedCoItem = null;
            }

            if (selectedCoItem != null)
            {
                TakeBackButton.gameObject.SetActive(true);
                Vector3 itemPos = TakeBackButton.transform.position;
                itemPos.y = selectedCoItem.transform.position.y;
                TakeBackButton.transform.position = itemPos;

                if (TakeBackKey != KeyCode.Mouse0)
                {
                    isKeyUp = true;
                }

                if (Input.GetKeyUp(TakeBackKey) && !isKeyUp)
                {
                    isKeyUp = true;
                }
                else if (isKeyUp)
                {
                    if (Input.GetKeyDown(TakeBackKey) && !isPressed)
                    {
                        TakeBackToInventory();
                        isPressed = true;
                        isKeyUp = false;
                    }
                    else if (isPressed)
                    {
                        isPressed = false;
                    }
                }
            }
            else
            {
                if (!TakeBackButton.gameObject.GetComponent<UIRaycastEvent>().pointerEnter)
                {
                    TakeBackButton.gameObject.SetActive(false);
                }

                isKeyUp = false;
            }

            if (!isFixed)
            {
                if (currentContainer.GetContainerCount() < 1)
                {
                    ContainerEmptyText.text = currentContainer.containerName.TitleCase() + " is Empty!";
                    ContainerEmptyText.gameObject.SetActive(true);
                }

                ContainerCapacityText.text = string.Format("Capacity {0}/{1}", currentContainer.GetContainerCount(), currentContainer.containerSpace);
            }
            else
            {
                if (FixedContainerData.Count < 1)
                {
                    ContainerEmptyText.gameObject.SetActive(true);
                }

                ContainerCapacityText.text = string.Format("Items Count: {0}", FixedContainerData.Count);
            }
        }

        if (shortcutBind && selectedBind == selectedSlotID && selectedBind > -1)
        {
            foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(kcode))
                {
                    if (kcode == KeyCode.Alpha1 || kcode == KeyCode.Alpha2 || kcode == KeyCode.Alpha3 || kcode == KeyCode.Alpha4)
                    {
                        InventoryItemData itemData = GetSlotItemData(selectedSlotID);
                        ShortcutBind(itemData.itemID, itemData.slotID, kcode);
                    }
                }
            }
        }
        else
        {
            if (Shortcuts.Count > 0)
            {
                for (int i = 0; i < Shortcuts.Count; i++)
                {
                    int slotID = Shortcuts[i].slot;

                    if (!HasSlotItem(slotID) && !isDragging)
                    {
                        Shortcuts.RemoveAt(i);
                        break;
                    }

                    if (Input.GetKeyDown(Shortcuts[i].shortcutKey))
                    {
                        UseItem(Shortcuts[i].slot);
                    }
                }
            }

            //fader.fadeOut = true;
            shortcutBind = false;
            selectedBind = -1;
        }

        if (!fader.fadeCompleted && notiFade)
        {
            Color colorN = InventoryNotification.color;
            Color colorT = InventoryNotification.transform.GetComponentInChildren<Text>().color;
            colorN.a = fader.GetFadeAlpha();
            colorT.a = fader.GetFadeAlpha();
            InventoryNotification.color = colorN;
            InventoryNotification.transform.GetComponentInChildren<Text>().color = colorT;
        }
        else
        {
            InventoryNotification.gameObject.SetActive(false);
            notiFade = false;
        }
    }

    /// <summary>
    /// Deselect current selected Item
    /// </summary>
    public void DeselectContainerItem()
    {
        if (currentContainer != null && currentContainer.IsSelecting())
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// Callback for UI TakeBack Button
    /// </summary>
    public void TakeBackToInventory()
    {
        if (!isFixed)
        {
            currentContainer.TakeBack(!ContainerTakeBackOne);
        }
        else
        {
            if (CheckInventorySpace())
            {
                GameObject destroyObj = selectedCoItem.gameObject;
                Item containerItem = selectedCoItem.item;

                if (!ContainerTakeBackOne)
                {
                    AddItem(containerItem.ID, selectedCoItem.amount, selectedCoItem.customData);

                    if (containerItem.isStackable)
                    {
                        FixedContainerData.RemoveAll(x => x.item.ID == selectedCoItem.item.ID);
                        ContainterItemsCache.RemoveAll(x => x.item.ID == selectedCoItem.item.ID);
                        Destroy(destroyObj);
                    }
                    else
                    {
                        int itemIndex = ContainterItemsCache.IndexOf(selectedCoItem);
                        FixedContainerData.RemoveAt(itemIndex);
                        ContainterItemsCache.RemoveAt(itemIndex);
                        Destroy(destroyObj);
                    }
                }
                else
                {
                    int itemIndex = ContainterItemsCache.IndexOf(selectedCoItem);

                    if (containerItem.itemType != ItemType.Weapon && containerItem.itemType != ItemType.Bullets)
                    {
                        AddItem(containerItem.ID, 1, selectedCoItem.customData);

                        if (selectedCoItem.amount == 1)
                        {
                            FixedContainerData.RemoveAt(itemIndex);
                            ContainterItemsCache.RemoveAt(itemIndex);
                            Destroy(destroyObj);
                        }
                        else
                        {
                            FixedContainerData[itemIndex].amount--;
                            selectedCoItem.amount--;
                        }
                    }
                    else
                    {
                        AddItem(containerItem.ID, selectedCoItem.amount, selectedCoItem.customData);
                        FixedContainerData.RemoveAt(itemIndex);
                        ContainterItemsCache.RemoveAt(itemIndex);
                        Destroy(destroyObj);
                    }
                }
            }
            else
            {
                DeselectSelected();
                ShowNotification("No Space in Inventory!");
            }
        }

        TakeBackButton.gameObject.SetActive(false);
        TakeBackButton.gameObject.GetComponent<UIRaycastEvent>().pointerEnter = false;
    }

    /// <summary>
    /// Function to show normal Inventory Container
    /// </summary>
    public void ShowInventoryContainer(InventoryContainer container, ContainerItemData[] containerItems, string name = "CONTAINER")
    {
        if (!string.IsNullOrEmpty(name))
        {
            ContainerNameText.text = name.ToUpper();
        }
        else
        {
            ContainerNameText.text = "CONTAINER";
        }

        if (containerItems.Length > 0)
        {
            ContainerEmptyText.gameObject.SetActive(false);

            foreach (var citem in containerItems)
            {
                GameObject coItem = Instantiate(containerItem, ContainterContent.transform);
                ContainerItemData itemData = new ContainerItemData(citem.item, citem.amount, citem.customData);
                ContainerItem item = coItem.GetComponent<ContainerItem>();
                item.item = citem.item;
                item.amount = citem.amount;
                item.customData = citem.customData;
                coItem.name = "CoItem_" + citem.item.Title.Replace(" ", "");
                ContainterItemsCache.Add(coItem.GetComponent<ContainerItem>());
            }
        }
        else
        {
            ContainerEmptyText.text = name.TitleCase() + " is Empty!";
            ContainerEmptyText.gameObject.SetActive(true);
        }

        isFixed = false;
        currentContainer = container;
        objectives.ShowObjectives(false);
        ContainterPanel.SetActive(true);
        gameManager.ShowInventory(true);
        isStoring = true;
    }

    public Dictionary<int, Dictionary<string, object>> GetFixedContainerData()
    {
        return FixedContainerData.ToDictionary(x => x.item.ID, y => new Dictionary<string, object> { { "item_amount", y.amount }, { "item_custom", y.customData } });
    }

    /// <summary>
    /// Function to show Fixed Container
    /// </summary>
    public void ShowFixedInventoryContainer(string name = "CONTAINER")
    {
        if (!string.IsNullOrEmpty(name))
        {
            ContainerNameText.text = name.ToUpper();
        }
        else
        {
            ContainerNameText.text = "CONTAINER";
        }

        if (FixedContainerData.Count > 0)
        {
            ContainerEmptyText.gameObject.SetActive(false);

            foreach (var citem in FixedContainerData)
            {
                GameObject coItem = Instantiate(containerItem, ContainterContent.transform);
                ContainerItemData itemData = new ContainerItemData(citem.item, citem.amount);
                ContainerItem item = coItem.GetComponent<ContainerItem>();
                item.item = citem.item;
                item.amount = citem.amount;
                item.customData = citem.customData;
                coItem.name = "CoItem_" + citem.item.Title.Replace(" ", "");
                ContainterItemsCache.Add(coItem.GetComponent<ContainerItem>());
            }
        }
        else
        {
            ContainerEmptyText.text = name.TitleCase() + " is Empty!";
            ContainerEmptyText.gameObject.SetActive(true);
        }

        isFixed = true;
        objectives.ShowObjectives(false);
        ContainterPanel.SetActive(true);
        gameManager.ShowInventory(true);
        isStoring = true;
    }

    /// <summary>
    /// Callback for UI Store Button
    /// </summary>
    public void StoreSelectedItem()
    {
        InventoryItemData itemData = GetSlotItemData(selectedSlotID);

        if (!isFixed)
        {
            if (selectedSlotID != -1 && currentContainer != null)
            {
                if (currentContainer.ContainsItemID(itemData.item.ID) && itemData.item.isStackable)
                {
                    currentContainer.AddItemAmount(itemData.item, itemData.m_amount);
                    RemoveSelectedItem(true);
                }
                else
                {
                    if (currentContainer.GetContainerCount() < currentContainer.containerSpace)
                    {
                        ContainerEmptyText.gameObject.SetActive(false);
                        currentContainer.StoreItem(itemData.item, itemData.m_amount, itemData.customData);

                        if (switcher.currentItem == itemData.item.useSwitcherID)
                        {
                            switcher.DeselectItems();
                        }

                        RemoveSelectedItem(true);
                    }
                    else
                    {
                        ShowNotification("No Space in Container!");
                        DeselectSelected();
                    }
                }
            }
        }
        else
        {
            if (selectedSlotID != -1)
            {
                if (FixedContainerData.Any(item => item.item.ID == itemData.item.ID) && itemData.item.isStackable)
                {
                    foreach (var item in FixedContainerData)
                    {
                        if(item.item.ID == itemData.item.ID)
                        {
                            item.amount += itemData.m_amount;
                            GetContainerItem(itemData.item.ID).amount = item.amount;
                        }
                    }
                }
                else
                {
                    ContainerEmptyText.gameObject.SetActive(false);
                    StoreFixedContainerItem(itemData.item, itemData.m_amount, itemData.customData);

                    if (switcher.currentItem == itemData.item.useSwitcherID)
                    {
                        switcher.DeselectItems();
                    }
                }

                RemoveSelectedItem(true);
            }
        }
    }

    void StoreFixedContainerItem(Item item, int amount, CustomItemData custom)
    {
        GameObject coItem = Instantiate(containerItem, ContainterContent.transform);
        ContainerItem citem = coItem.GetComponent<ContainerItem>();
        citem.inventoryContainer = null;
        citem.item = item;
        citem.amount = amount;
        citem.customData = custom;
        coItem.name = "CoItem_" + item.Title.Replace(" ", "");
        FixedContainerData.Add(new ContainerItemData(item, amount, custom));
        ContainterItemsCache.Add(coItem.GetComponent<ContainerItem>());
    }

    /// <summary>
    /// Get UI ContainerItem Object
    /// </summary>
    public ContainerItem GetContainerItem(int id)
    {
        foreach (var item in ContainterItemsCache)
        {
            if(item.item.ID == id)
            {
                return item;
            }
        }

        Debug.LogError($"Item with ID ({id}) does not found!");
        return null;
    }

    /// <summary>
    /// Start Shortcut Bind process
    /// </summary>
    public void BindShortcutItem()
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            Slots[i].GetComponent<InventorySlot>().isSelected = false;
            Slots[i].GetComponent<InventorySlot>().contexVisible = false;
        }

        ShowContexMenu(false);
        ShowNotificationFixed("Select 1, 2, 3, 4 to bind item to a shortcut key.");
        selectedBind = selectedSlotID;
        shortcutBind = true;
        canDeselect = false;
    }

    /// <summary>
    /// Bind new or Exchange Inventory Shortcut
    /// </summary>
    public void ShortcutBind(int itemID, int slotID, KeyCode kcode)
    {
        Item item = GetItem(itemID);

        if (Shortcuts.Count > 0) {
            if (Shortcuts.All(s => s.slot != slotID && s.shortcutKey != kcode))
            {
                //Shortcut does not exist
                Shortcuts.Add(new ShortcutModel(item, slotID, kcode));
                GetSlotItemData(slotID).shortKey = ShortcutKeyToInt(kcode);
            }
            else
            {
                //Shortcut already exist
                for (int i = 0; i < Shortcuts.Count; i++)
                {
                    if (Shortcuts.Any(s => s.slot == slotID))
                    {
                        if (Shortcuts[i].slot == slotID)
                        {
                            //Change shortcut key
                            if (Shortcuts.Any(s => s.shortcutKey == kcode))
                            {
                                //Find equal shortcut with key and exchange it
                                foreach (var equal in Shortcuts)
                                {
                                    if (equal.shortcutKey == kcode)
                                    {
                                        equal.shortcutKey = Shortcuts[i].shortcutKey;
                                        GetSlotItemData(equal.slot).shortKey = ShortcutKeyToInt(Shortcuts[i].shortcutKey);
                                    }
                                }
                            }

                            //Change actual shortcut key
                            Shortcuts[i].shortcutKey = kcode;
                            GetSlotItemData(Shortcuts[i].slot).shortKey = ShortcutKeyToInt(Shortcuts[i].shortcutKey);
                            break;
                        }
                    }
                    else if (Shortcuts[i].shortcutKey == kcode)
                    {
                        //Change shortcut item
                        GetSlotItemData(Shortcuts[i].slot).shortKey = -1;
                        GetSlotItemData(slotID).shortKey = ShortcutKeyToInt(kcode);
                        Shortcuts[i].slot = slotID;
                        Shortcuts[i].item = item;
                        break;
                    }
                }
            }
        }
        else
        {
            Shortcuts.Add(new ShortcutModel(item, slotID, kcode));
            GetSlotItemData(slotID).shortKey = ShortcutKeyToInt(kcode);
        }

        DeselectSelected();
        fader.fadeOut = true;
        shortcutBind = false;
        canDeselect = true;
    }

    /// <summary>
    /// Update Shortcut slot with binded KeyCode
    /// </summary>
    public void UpdateShortcut(int kcode, int slotID)
    {
        foreach (var shortcut in Shortcuts)
        {
            if(ShortcutKeyToInt(shortcut.shortcutKey) == kcode)
            {
                shortcut.slot = slotID;
            }
        }
    }

    /// <summary>
    /// Get Shortcut KeyCode as Int
    /// </summary>
    int ShortcutKeyToInt(KeyCode kcode)
    {
        if (kcode == KeyCode.Alpha1)
        {
            return 1;
        }
        else if (kcode == KeyCode.Alpha2)
        {
            return 2;
        }
        else if (kcode == KeyCode.Alpha3)
        {
            return 3;
        }
        else if (kcode == KeyCode.Alpha4)
        {
            return 4;
        }

        return -1;
    }

    /// <summary>
    /// Function to Open Inventory with Highlighted Items and Select Item
    /// </summary>
    public void OnInventorySelect(int[] highlight, string[] tags, MonoBehaviour script, string selectText = "", string nullText = "")
    {
        if(highlight.Length > 0 && ItemsCache.Any(x => highlight.Any(y => x.item.ID.Equals(y))))
        {
            selectedScript = script;

            canDeselect = false;
            gameManager.ShowInventory(true);

            if(selectText != string.Empty)
            {
                ShowNotificationFixed(selectText);
            }

            for (int i = 0; i < Slots.Count; i++)
            {
                InventorySlot slot = Slots[i].GetComponent<InventorySlot>();

                slot.isCombining = true;
                slot.isCombinable = false;
                slot.isItemSelect = true;

                if (slot.slotItem != null)
                {
                    if (highlight.Any(x => slot.slotItem.ID.Equals(x)))
                    {
                        if (tags.Length > 0)
                        {
                            if (tags.Any(x => slot.itemData.customData.itemTag.Equals(x)))
                            {
                                slot.GetComponent<Image>().color = Color.white;
                                slot.isCombinable = true;
                            }
                        }
                        else
                        {
                            slot.GetComponent<Image>().color = Color.white;
                            slot.isCombinable = true;
                        }
                    }
                    else
                    {
                        slot.GetComponent<Image>().color = slotDisabled;
                    }
                }
            }

            ShowContexMenu(false);
        }
        else
        {
            if(nullText != string.Empty)
            {
                gameManager.AddSingleMessage(nullText, "NoItems");
            }
        }
    }

    /// <summary>
    /// Get Item from Database
    /// </summary>
    public Item GetItem(int ID)
    {
        return inventoryDatabase.ItemDatabase.Where(item => item.ID == ID).Select(item => new Item(item.ID, item)).SingleOrDefault();
    }

    /// <summary>
    /// Function to add new item to specific slot
    /// </summary>
    public void AddItemToSlot(int slotID, int itemID, int amount = 1, CustomItemData customData = null)
    {
        Item itemToAdd = GetItem(itemID);
        CustomItemData data = new CustomItemData();

        if (CheckInventorySpace())
        {
            if (itemToAdd.isStackable && HasSlotItem(slotID, itemID))
            {
                InventoryItemData itemData = GetItemData(itemToAdd.ID, slotID);
                itemData.m_amount += amount;
            }
            else
            {
                for (int i = 0; i < Slots.Count; i++)
                {
                    if (i == slotID)
                    {
                        GameObject item = Instantiate(inventoryItem, Slots[i].transform);
                        InventoryItemData itemData = item.GetComponent<InventoryItemData>();
                        itemData.item = itemToAdd;
                        itemData.m_amount = amount;
                        itemData.slotID = i;
                        if (customData != null) {itemData.customData = customData; data = customData; }
                        Slots[i].GetComponent<InventorySlot>().slotItem = itemToAdd;
                        Slots[i].GetComponent<InventorySlot>().itemData = itemData;
                        Slots[i].transform.GetChild(0).GetComponent<Image>().sprite = slotItemOutline;
                        Slots[i].GetComponent<Image>().sprite = slotWithItem;
                        Slots[i].GetComponent<Image>().enabled = true;
                        item.GetComponent<Image>().sprite = itemToAdd.itemSprite;
                        item.GetComponent<RectTransform>().position = Vector2.zero;
                        item.name = itemToAdd.Title;
                        ItemsCache.Add(new InventoryItem(itemToAdd, data));
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Function to add new item
    /// </summary>
    public void AddItem(int itemID, int amount, CustomItemData customData = null)
    {
        Item itemToAdd = GetItem(itemID);
        CustomItemData data = new CustomItemData();

        if (CheckInventorySpace() || CheckItemInventory(itemID))
        {
            if (itemToAdd.isStackable && CheckItemInventory(itemToAdd.ID) && GetItemData(itemToAdd.ID) != null)
            {
                InventoryItemData itemData = GetItemData(itemToAdd.ID);
                itemData.m_amount += amount;
            }
            else
            {
                for (int i = 0; i < Slots.Count; i++)
                {
                    if (Slots[i].transform.childCount == 1)
                    {
                        GameObject item = Instantiate(inventoryItem, Slots[i].transform);
                        InventoryItemData itemData = item.GetComponent<InventoryItemData>();
                        itemData.item = itemToAdd;
                        itemData.m_amount = amount;
                        itemData.slotID = i;
                        if (customData != null) { itemData.customData = customData; data = customData; }
                        Slots[i].GetComponent<InventorySlot>().slotItem = itemToAdd;
                        Slots[i].GetComponent<InventorySlot>().itemData = itemData;
                        Slots[i].transform.GetChild(0).GetComponent<Image>().sprite = slotItemOutline;
                        Slots[i].GetComponent<Image>().sprite = slotWithItem;
                        Slots[i].GetComponent<Image>().enabled = true;
                        item.GetComponent<Image>().sprite = itemToAdd.itemSprite;
                        item.GetComponent<RectTransform>().position = Vector2.zero;
                        item.name = itemToAdd.Title;
                        ItemsCache.Add(new InventoryItem(itemToAdd, data));
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Remove Item from Slot
    /// </summary>
    public void RemoveSlotItem(int slotID, bool all = false)
    {
        InventoryItemData data = GetSlotItemData(slotID);
        Item itemToRemove = data.item;

        if (itemToRemove.isStackable && HasSlotItem(slotID, itemToRemove.ID) && !all)
        {
            data.m_amount--;
            data.textAmount.text = data.m_amount.ToString();

            if (data.m_amount <= 0)
            {
                Destroy(Slots[slotID].transform.GetChild(1).gameObject);
                RemoveFromCache(itemToRemove, data.customData);
                DeselectSelected();
            }

            if (data.m_amount == 1)
            {
                data.textAmount.text = "";
            }
        }
        else
        {
            Destroy(Slots[slotID].transform.GetChild(1).gameObject);
            RemoveFromCache(itemToRemove, data.customData);
            DeselectSelected();
        }
    }

    /// <summary>
    /// Remove one or all Item stacks by Item ID
    /// </summary>
    public void RemoveItem(int ID, bool all = false, bool lastItem = false)
    {
        Item itemToRemove = GetItem(ID);
        int slotID;

        if (lastItem)
        {
            slotID = GetItemSlotID(itemToRemove.ID, true);
        }
        else
        {
            slotID = GetItemSlotID(itemToRemove.ID);
        }

        if (itemToRemove.isStackable && CheckItemInventory(itemToRemove.ID) && !all)
        {
            InventoryItemData data = Slots[slotID].GetComponentInChildren<InventoryItemData>();
            data.m_amount--;
            data.textAmount.text = data.m_amount.ToString();

            if (data.m_amount <= 0)
            {
                Destroy(Slots[slotID].transform.GetChild(1).gameObject);
                RemoveFromCache(itemToRemove);
                DeselectSelected();
            }

            if (data.m_amount == 1)
            {
                data.textAmount.text = "";
            }
        }
        else
        {
            Destroy(Slots[slotID].transform.GetChild(1).gameObject);
            RemoveFromCache(itemToRemove);
            DeselectSelected();
        }
    }

    /// <summary>
    /// Remove one or all Item stacks from selected Slot
    /// </summary>
    public void RemoveSelectedItem(bool all = false)
    {
        int slot = selectedSlotID;
        Item item = GetSlotItem(slot);

        if (item.isStackable && CheckItemInventory(item.ID) && !all)
        {
            InventoryItemData data = Slots[slot].GetComponentInChildren<InventoryItemData>();
            data.m_amount--;
            data.textAmount.text = data.m_amount.ToString();

            if (data.m_amount <= 0)
            {
                Destroy(Slots[slot].transform.GetChild(1).gameObject);
                RemoveFromCache(item);
                DeselectSelected();
            }

            if (data.m_amount == 1)
            {
                data.textAmount.text = "";
            }
        }
        else
        {
            Destroy(Slots[slot].transform.GetChild(1).gameObject);
            RemoveFromCache(item);
            DeselectSelected();
        }
    }

    /// <summary>
    /// Remove specific item amount by ItemID
    /// </summary>
    public void RemoveItemAmount(int ID, int Amount)
    {
        if (CheckItemInventory(ID))
        {
            InventoryItemData data = Slots[GetItemSlotID(ID)].GetComponentInChildren<InventoryItemData>();

            if (data.m_amount > Amount)
            {
                data.m_amount = data.m_amount - Amount;
                data.transform.parent.GetChild(0).GetChild(0).GetComponent<Text>().text = data.m_amount.ToString();
            }
            else
            {
                RemoveItem(ID, true, true);
            }
        }
    }

    /// <summary>
    /// Remove selected slot Item Amount
    /// </summary>
    public void RemoveSelectedItemAmount(int Amount)
    {
        int slot = selectedSlotID;
        Item item = GetSlotItem(slot);

        if (CheckItemInventory(item.ID))
        {
            InventoryItemData data = Slots[slot].GetComponentInChildren<InventoryItemData>();

            if (data.m_amount > Amount)
            {
                data.m_amount -= Amount;
                data.transform.parent.GetChild(0).GetChild(0).GetComponent<Text>().text = data.m_amount.ToString();
            }
            else
            {
                Destroy(Slots[slot].transform.GetChild(1).gameObject);
                RemoveFromCache(item);
                DeselectSelected();
            }
        }
    }

    /// <summary>
    /// Remove Item from current Items Cache
    /// </summary>
    private void RemoveFromCache(Item item, CustomItemData customData = null, bool all = false)
    {
        if (all)
        {
            if (customData != null && customData.itemTag != null)
            {
                ItemsCache.RemoveAll(x => x.item.ID.Equals(item.ID) && x.customData.itemTag.Equals(customData.itemTag));
            }
            else
            {
                ItemsCache.RemoveAll(x => x.item.ID.Equals(item.ID));
            }
        }
        else
        {
            int index = -1;

            if (customData != null && customData.itemTag != null)
            {
                index = ItemsCache.FindIndex(x => x.item.ID.Equals(item.ID) && x.customData.itemTag.Equals(customData.itemTag));
            }
            else
            {
                index = ItemsCache.FindIndex(x => x.item.ID.Equals(item.ID));
            }

            if(index != -1)
            {
                ItemsCache.RemoveAt(index);
            }
        }
    }

    /// <summary>
    /// Use Selected Item
    /// </summary>
    public void UseSelectedItem()
    {
        UseItem();
    }

    /// <summary>
    /// Use Selected Item
    /// </summary>
    /// <param name="slotItem">Slot with item</param>
    public void UseItem(int slotItem = -1)
    {
        Item usableItem = null;

        if (slotItem != -1)
        {
            if (HasSlotItem(slotItem))
            {
                usableItem = GetSlotItem(slotItem);
            }
        }
        else if (usableItem == null)
        {
            usableItem = GetSlotItem(selectedSlotID);
            slotItem = selectedSlotID;
        }

        if (usableItem == null)
        {
            Debug.LogError("[Inventory Use] Cannot use a null Item!");
            return;
        }

        if (GetItemAmount(usableItem.ID) < 2 || usableItem.useItemSwitcher)
        {
            ShowContexMenu(false);

            if (usableItem.useItemSwitcher)
            {
                DeselectSelected();
            }
        }

        if (usableItem.doActionUse)
        {
            TriggerItemAction(slotItem, usableItem.useSwitcherID);
        }

        if (usableItem.itemType == ItemType.Heal)
        {
            gameManager.healthManager.ApplyHeal(usableItem.healAmount);

            if (!gameManager.healthManager.isMaximum)
            {
                if (usableItem.useSound)
                {
                    Tools.PlayOneShot2D(Tools.MainCamera().transform.position, usableItem.useSound, usableItem.soundVolume);
                }

                if (slotItem != -1)
                {
                    if (usableItem.doActionUse && !usableItem.customActions.actionRemove)
                    {
                        RemoveSlotItem(slotItem);
                    }
                    else
                    {
                        RemoveSlotItem(slotItem);
                    }
                }
                else
                {
                    Debug.LogError("[Inventory] slotItem parameter cannot be (-1)!");
                }
            }
        }

        if (usableItem.itemType == ItemType.Light)
        {
            switcher.currentLightObject = usableItem.useSwitcherID;
        }

        if (usableItem.itemType == ItemType.Weapon || usableItem.useItemSwitcher)
        {
            switcher.SelectItem(usableItem.useSwitcherID);
            switcher.weaponItem = usableItem.useSwitcherID;
        }
    }

    /// <summary>
    /// Drop selected slot Item to ground
    /// </summary>
    public void DropItemGround()
    {
        InteractiveItem interactiveItem = null;
        InventoryItemData itemData = GetSlotItemData(selectedSlotID);
        Item item = itemData.item;

        Transform dropPos = PlayerController.Instance.GetComponentInChildren<PlayerFunctions>().inventoryDropPos;
        GameObject dropObject = GetDropObject(item);

        if (item.itemType == ItemType.Weapon || item.useItemSwitcher)
        {
            if (switcher.currentItem == item.useSwitcherID)
            {
                switcher.DisableItems();
            }
        }

        if (item.itemType == ItemType.Light && switcher.currentLightObject == item.useSwitcherID)
        {
            switcher.currentLightObject = -1;
        }

        GameObject worldItem = null;

        if (GetItemAmount(item.ID) >= 2 && item.itemType != ItemType.Weapon)
        {
            worldItem = Instantiate(item.packDropObject, dropPos.position, dropPos.rotation);
            worldItem.name = "PackDrop_" + dropObject.name;

            if (worldItem.GetComponent<InteractiveItem>())
            {
                interactiveItem = worldItem.GetComponent<InteractiveItem>();
            }

            if (interactiveItem)
            {
                if (string.IsNullOrEmpty(interactiveItem.ItemName))
                {
                    interactiveItem.ItemName = "Sack of " + item.Title;
                }

                if (interactiveItem.messageType != InteractiveItem.MessageType.None && string.IsNullOrEmpty(interactiveItem.Message))
                {
                    interactiveItem.Message = "Sack of " + item.Title;
                }

                interactiveItem.ItemType = InteractiveItem.Type.InventoryItem;
                interactiveItem.InventoryID = item.ID;
            }
            else
            {
                Debug.LogError($"[Inventory Drop] {worldItem.name} does not have InteractiveItem script");
            }
        }
        else if(GetItemAmount(item.ID) == 1 || item.itemType == ItemType.Weapon)
        {
            worldItem = Instantiate(dropObject, dropPos.position, dropPos.rotation);
            worldItem.name = "Drop_" + dropObject.name;

            if(itemData.customData.storedTexPath != string.Empty)
            {
                Texture tex = Resources.Load<Texture2D>(itemData.customData.storedTexPath);
                worldItem.GetComponentInChildren<MeshRenderer>().material.SetTexture("_MainTex", tex);
            }

            if (worldItem.GetComponent<InteractiveItem>())
            {
                interactiveItem = worldItem.GetComponent<InteractiveItem>();
            }
        }

        if (interactiveItem && interactiveItem.customData != null)
        {
            interactiveItem.customData = itemData.customData;
        }

        if(worldItem && worldItem.GetComponent<Rigidbody>())
        {
            worldItem.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        Physics.IgnoreCollision(worldItem.GetComponent<Collider>(), Tools.MainCamera().transform.root.GetComponent<Collider>());

        worldItem.GetComponent<Rigidbody>().AddForce(Tools.MainCamera().transform.forward * (itemDropStrenght * 10));
        worldItem.GetComponent<InteractiveItem>().disableType = InteractiveItem.DisableType.Destroy;

        if (worldItem.GetComponent<SaveObject>())
        {
            Destroy(worldItem.GetComponent<SaveObject>());
        }

        if (GetItemAmount(item.ID) < 2 || item.useItemSwitcher || item.itemType == ItemType.Bullets)
        {
            ShowContexMenu(false);
        }

        if (GetItemAmount(item.ID) > 1)
        {
            worldItem.GetComponent<InteractiveItem>().Amount = GetItemAmount(item.ID);
            RemoveSelectedItem(true);
        }
        else
        {
            RemoveSelectedItem(true);
        }
    }

    /// <summary>
    /// Callback for CombineItem UI Button
    /// </summary>
    public void CombineItem()
    {
        canDeselect = false;

        for (int i = 0; i < Slots.Count; i++)
        {
            Slots[i].GetComponent<InventorySlot>().isCombining = true;

            if (!IsCombineSlot(i))
            {
                Slots[i].GetComponent<Image>().color = slotDisabled;
                Slots[i].GetComponent<InventorySlot>().isCombinable = false;
            }
            else
            {
                Slots[i].GetComponent<InventorySlot>().isCombinable = true;
            }
        }

        ShowContexMenu(false);
    }

    /// <summary>
    /// Check if slot has item which is combinable
    /// </summary>
    bool IsCombineSlot(int slotID)
    {
        InventoryScriptable.ItemMapper.CombineSettings[] combineSettings = Slots[selectedSlotID].GetComponentInChildren<InventoryItemData>().item.combineSettings;

        foreach (var id in combineSettings)
        {
            if (Slots[slotID].GetComponent<InventorySlot>().itemData != null)
            {
                InventorySlot slot = Slots[slotID].GetComponent<InventorySlot>();

                if (slot.itemData.item.ID == id.combineWithID && slot.itemData.customData.canCombine)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Check if item has partner item to combine
    /// </summary>
    public bool HasCombinePartner(Item Item)
    {
        InventoryScriptable.ItemMapper.CombineSettings[] combineSettings = Item.combineSettings;
        return ItemsCache.Any(item => combineSettings.Any(item2 => item.item.ID == item2.combineWithID) && CanAnyCombine(item.item));
    }

    bool CanAnyCombine(Item item)
    {
        var itemDatas = Slots.Where(x => x.GetComponentInChildren<InventoryItemData>()).Select(x => x.GetComponentInChildren<InventoryItemData>()).ToArray();
        return itemDatas.Any(x => x.itemID == item.ID && x.customData.canCombine);
    }

    /// <summary>
    /// Function to combine selected Item with second Item
    /// </summary>
    public void CombineWith(Item SecondItem, int slotID)
    {
        if (selectedScript != null)
        {
            if(selectedScript is IItemSelect)
            {
                InventoryItemData data = GetSlotItemData(slotID);
                RemoveSlotItem(slotID);
                (selectedScript as IItemSelect).OnItemSelect(SecondItem.ID, data.customData);
                selectedScript = null;
                gameManager.ShowInventory(false);
            }
        }
        else if(selectedSlotID != -1)
        {
            int firstItemSlot = selectedSlotID;
            int secondItemSlot = slotID;

            if (slotID != selectedSlotID)
            {
                Item SelectedItem = Slots[selectedSlotID].GetComponentInChildren<InventoryItemData>().item;
                InventoryScriptable.ItemMapper.CombineSettings[] selectedCombineSettings = SelectedItem.combineSettings;

                int CombinedItemID = -1;
                int CombineSwitcherID = -1;

                foreach (var item in selectedCombineSettings)
                {
                    if (item.combineWithID == SecondItem.ID)
                    {
                        CombinedItemID = item.resultCombineID;
                        CombineSwitcherID = item.combineSwitcherID;
                    }
                }

                for (int i = 0; i < Slots.Count; i++)
                {
                    Slots[i].GetComponent<InventorySlot>().isCombining = false;
                    Slots[i].GetComponent<InventorySlot>().isCombinable = false;
                    Slots[i].GetComponent<Image>().color = Color.white;
                }

                if (SelectedItem.combineSound)
                {
                    Tools.PlayOneShot2D(Tools.MainCamera().transform.position, SelectedItem.combineSound, SelectedItem.soundVolume);
                }
                else
                {
                    if (SecondItem.combineSound)
                    {
                        Tools.PlayOneShot2D(Tools.MainCamera().transform.position, SecondItem.combineSound, SecondItem.soundVolume);
                    }
                }

                if (SelectedItem.doActionCombine)
                {
                    TriggerItemAction(firstItemSlot, CombineSwitcherID);
                }
                if (SecondItem.doActionCombine)
                {
                    TriggerItemAction(secondItemSlot, CombineSwitcherID);
                }

                if (SelectedItem.itemType == ItemType.ItemPart && SelectedItem.isCombinable)
                {
                    int switcherID = GetItem(SelectedItem.combineSettings[0].combineWithID).useSwitcherID;
                    GameObject MainObject = switcher.ItemList[switcherID];

                    MonoBehaviour script = MainObject.GetComponents<MonoBehaviour>().SingleOrDefault(sc => sc.GetType().GetField("CanReload") != null);
                    FieldInfo info = script.GetType().GetField("CanReload");

                    if (info != null)
                    {
                        bool canReload = Parser.Convert<bool>(script.GetType().InvokeMember("CanReload", BindingFlags.GetField, null, script, null).ToString());

                        if (canReload)
                        {
                            MainObject.SendMessage("Reload", SendMessageOptions.DontRequireReceiver);
                            RemoveSlotItem(firstItemSlot);
                        }
                        else
                        {
                            gameManager.AddMessage("Cannot reload yet!");
                            DeselectSelected();
                        }
                    }
                    else
                    {
                        Debug.Log(MainObject.name + " object does not have script with CanReload property!");
                    }
                }
                else if (SelectedItem.isCombinable)
                {
                    if (SelectedItem.combineGetSwItem && CombineSwitcherID != -1)
                    {
                        if (CombineSwitcherID != -1)
                        {
                            switcher.SelectItem(CombineSwitcherID);
                        }
                    }

                    if (SelectedItem.combineGetItem && CombinedItemID != -1)
                    {
                        int a_count = GetSlotItemData(firstItemSlot).m_amount;
                        int b_count = GetSlotItemData(secondItemSlot).m_amount;

                        if (!CheckInventorySpace())
                        {
                            if (a_count > 1 && b_count > 1)
                            {
                                gameManager.AddSingleMessage("No Inventory Space!", "inv_space", true);
                                return;
                            }
                        }

                        if (a_count < 2 && b_count >= 2)
                        {
                            if (!SelectedItem.combineNoRemove)
                            {
                                StartCoroutine(WaitForRemoveAddItem(secondItemSlot, CombinedItemID));
                            }
                            else
                            {
                                AddItem(CombinedItemID, 1);
                            }
                        }
                        if (a_count >= 2 && b_count < 2)
                        {
                            if (!SecondItem.combineNoRemove)
                            {
                                StartCoroutine(WaitForRemoveAddItem(secondItemSlot, CombinedItemID));
                            }
                            else
                            {
                                AddItem(CombinedItemID, 1);
                            }
                        }
                        if (a_count < 2 && b_count < 2)
                        {
                            if (!SelectedItem.combineNoRemove)
                            {
                                StartCoroutine(WaitForRemoveAddItem(secondItemSlot, CombinedItemID));
                            }
                            else
                            {
                                AddItem(CombinedItemID, 1);
                            }
                        }
                        if (a_count >= 2 && b_count >= 2)
                        {
                            AddItem(CombinedItemID, 1);
                        }
                    }

                    if (!SelectedItem.combineNoRemove && !SelectedItem.customActions.actionRemove)
                    {
                        RemoveSlotItem(firstItemSlot);
                    }
                    if (!SecondItem.combineNoRemove && !SecondItem.customActions.actionRemove)
                    {
                        RemoveSlotItem(secondItemSlot);
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Selected Slot ID cannot be null!");
        }

        ResetSlotProperties();
        selectedSlotID = -1;
        canDeselect = true;
    }

    /// <summary>
    /// Function to Trigger Item Actions
    /// </summary>
    void TriggerItemAction(int itemSlot, int switcherID = -1)
    {
        Item SelectedItem = GetSlotItem(itemSlot);

        bool trigger = false;

        if (SelectedItem.useActionType != ItemAction.None)
        {
            if (SelectedItem.useActionType == ItemAction.Increase)
            {
                InventoryItemData itemData = GetSlotItemData(itemSlot);
                int num = int.Parse(itemData.customData.storedValue);
                num++;
                itemData.customData.storedValue = num.ToString();

                if (num >= SelectedItem.customActions.triggerValue)
                {
                    trigger = true;
                }
            }
            else if (SelectedItem.useActionType == ItemAction.Decrease)
            {
                InventoryItemData itemData = GetSlotItemData(itemSlot);
                int num = int.Parse(itemData.customData.storedValue);
                num--;
                itemData.customData.storedValue = num.ToString();

                if (num <= SelectedItem.customActions.triggerValue)
                {
                    trigger = true;
                }
            }
            else if (SelectedItem.useActionType == ItemAction.ItemValue)
            {
                IItemValueProvider itemValue = switcher.ItemList[switcherID].GetComponent<IItemValueProvider>();
                if (itemValue != null)
                {
                    if (GetSlotItem(itemSlot).Description.RegexMatch('{', '}', "value"))
                    {
                        itemValue.OnSetValue(GetSlotItemData(itemSlot).customData.storedValue);
                    }
                }
            }
        }

        if (trigger)
        {
            if (SelectedItem.customActions.actionRemove)
            {
                RemoveSlotItem(itemSlot);
            }
            if (SelectedItem.customActions.actionAddItem)
            {
                AddItem(SelectedItem.customActions.triggerAddItem, 1, new CustomItemData() { storedValue = SelectedItem.customActions.addItemValue });
            }
            if (SelectedItem.customActions.actionRestrictCombine)
            {
                GetSlotItemData(itemSlot).customData.canCombine = false;
            }
            if (SelectedItem.customActions.actionRestrictUse)
            {
                GetSlotItemData(itemSlot).customData.canUse = false;
            }
        }
    }

    /// <summary>
    /// Wait until old Item will be removed, then add new Item
    /// </summary>
    IEnumerator WaitForRemoveAddItem(int oldItemSlot, int newItem)
    {
        int oldItemCount = GetSlotItemData(oldItemSlot).m_amount;

        if (oldItemCount < 2)
        {
            yield return new WaitUntil(() => !HasSlotItem(oldItemSlot));
            AddItemToSlot(oldItemSlot, newItem);
        }
        else
        {
            AddItem(newItem, 1);
        }
    }

    /// <summary>
    /// Callback for Examine Item Button
    /// </summary>
    public void ExamineItem()
    {
        InventoryItemData itemData = GetSlotItemData(selectedSlotID);
        Item item = itemData.item;
        gameManager.TabButtonPanel.SetActive(false);
        gameManager.ShowCursor(false);

        if (item.dropObject && item.dropObject.GetComponent<InteractiveItem>())
        {
            GameObject examine = Instantiate(GetDropObject(item));

            if (itemData.customData.storedTexPath != string.Empty)
            {
                Texture tex = Resources.Load<Texture2D>(itemData.customData.storedTexPath);
                examine.GetComponentInChildren<MeshRenderer>().material.SetTexture("_MainTex", tex);
            }

            gameManager.scriptManager.gameObject.GetComponent<ExamineManager>().ExamineObject(examine);
        }
    }

    /// <summary>
    /// Get Item Drop Object by Item
    /// </summary>
    public GameObject GetDropObject(Item item)
    {
        return AllItems.Where(x => x.ID == item.ID).Select(x => x.dropObject).SingleOrDefault();
    }

    /// <summary>
    /// Function to set specific item amount
    /// </summary>
    public void SetItemAmount(int ID, int Amount)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].transform.childCount > 1)
            {
                if (Slots[i].GetComponentInChildren<InventoryItemData>().item.ID == ID)
                {
                    Slots[i].GetComponentInChildren<InventoryItemData>().m_amount = Amount;
                }
            }
        }
    }

    /// <summary>
    /// Function to expand item slots
    /// </summary>
    public void ExpandSlots(int SlotsAmount)
    {
        int extendedSlots = slotAmount + SlotsAmount;

        if (extendedSlots > maxSlots)
        {
            gameManager.WarningMessage("Cannot carry more backpacks");
            return;
        }

        for (int i = slotAmount; i < extendedSlots; i++)
        {
            GameObject slot = Instantiate(inventorySlot);
            Slots.Add(slot);
            slot.GetComponent<InventorySlot>().inventory = this;
            slot.GetComponent<InventorySlot>().slotID = i;
            slot.transform.SetParent(SlotsContent.transform);
            slot.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
        }

        slotAmount = extendedSlots;
    }

    /// <summary>
    /// Check if there is space in Inevntory
    /// </summary>
    public bool CheckInventorySpace()
    {
        return Slots.Any(x => x.transform.childCount < 2);
    }

    /// <summary>
    /// Check if Item is in Inventory by ID
    /// </summary>
    public bool CheckItemInventory(int ID)
    {
        return ItemsCache.Any(x => x.item.ID == ID);
    }

    /// <summary>
    /// Check if Item is in Inventory and is Stackable by Item ID
    /// </summary>
    public bool CheckItemInventoryStack(int ID)
    {
        return ItemsCache.Any(x => x.item.ID == ID && x.item.isStackable);
    }

    /// <summary>
    /// Check if Switcher Item is in Inventory
    /// </summary>
    public bool CheckSWIDInventory(int SwitcherID)
    {
        return ItemsCache.Any(x => x.item.useSwitcherID == SwitcherID);
    }

    /// <summary>
    /// Check if slot has specific item
    /// </summary>
    bool HasSlotItem(int slotID, int itemID)
    {
        if (Slots[slotID].GetComponentInChildren<InventoryItemData>())
        {
            return Slots[slotID].GetComponentInChildren<InventoryItemData>().item.ID == itemID;
        }

        return false;
    }

    /// <summary>
    /// Check if slot has any item
    /// </summary>
    bool HasSlotItem(int slotID)
    {
        if (Slots[slotID].GetComponentInChildren<InventoryItemData>())
        {
            return Slots[slotID].GetComponentInChildren<InventoryItemData>().item.ID != -1;
        }

        return false;
    }

    /// <summary>
    /// Get specific InventoryItemData
    /// </summary>
    InventoryItemData GetItemData(int itemID, int slotID = -1)
    {
        if (slotID != -1)
        {
            if (HasSlotItem(slotID, itemID))
            {
                return Slots[slotID].GetComponentInChildren<InventoryItemData>();
            }
        }
        else
        {
            foreach (var slot in Slots)
            {
                if (slot.GetComponentInChildren<InventoryItemData>() && slot.GetComponentInChildren<InventoryItemData>().item.ID == itemID)
                {
                    return slot.GetComponentInChildren<InventoryItemData>();
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Get InventoryItemData from slot
    /// </summary>
    InventoryItemData GetSlotItemData(int slotID)
    {
        if (Slots[slotID].GetComponentInChildren<InventoryItemData>())
        {
            return Slots[slotID].GetComponentInChildren<InventoryItemData>();
        }

        return null;
    }

    /// <summary>
    /// Get InventorySlot by Item ID
    /// </summary>
    InventorySlot GetItemSlot(int itemID)
    {
        foreach (var slot in Slots)
        {
            if (slot.GetComponentInChildren<InventoryItemData>() && slot.GetComponentInChildren<InventoryItemData>().item.ID == itemID)
            {
                return slot.GetComponent<InventorySlot>();
            }
        }

        return null;
    }

    /// <summary>
    /// Get slot object by Item ID
    /// </summary>
    GameObject GetItemSlotObject(int itemID)
    {
        foreach (var slot in Slots)
        {
            if (slot.GetComponentInChildren<InventoryItemData>() && slot.GetComponentInChildren<InventoryItemData>().item.ID == itemID)
            {
                return slot;
            }
        }

        return null;
    }

    /// <summary>
    /// Get Slot ID by Inventory ID
    /// </summary>
    /// <param name="reverse">Check slots from last to first?</param>
    int GetItemSlotID(int itemID, bool reverse = false)
    {
        if (!reverse)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].GetComponentInChildren<InventoryItemData>() && Slots[i].GetComponentInChildren<InventoryItemData>().item.ID == itemID)
                {
                    return i;
                }
            }
        }
        else
        {
            for (int i = Slots.Count - 1; i > 0; i--)
            {
                if (Slots[i].GetComponentInChildren<InventoryItemData>() && Slots[i].GetComponentInChildren<InventoryItemData>().item.ID == itemID)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Get Item from Slot ID
    /// </summary>
    Item GetSlotItem(int slotID)
    {
        if (Slots[slotID].GetComponentInChildren<InventoryItemData>())
        {
            return Slots[slotID].GetComponentInChildren<InventoryItemData>().item;
        }

        return null;
    }

    /// <summary>
    /// Get Item Amount by Item ID
    /// </summary>
    public int GetItemAmount(int itemID)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].GetComponentInChildren<InventoryItemData>() && Slots[i].GetComponentInChildren<InventoryItemData>().item.ID == itemID)
            {
                return Slots[i].GetComponentInChildren<InventoryItemData>().m_amount;
            }
        }

        return -1;
    }

    /// <summary>
    /// Reset all slot properties
    /// </summary>
    public void ResetSlotProperties()
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            InventorySlot slot = Slots[i].GetComponent<InventorySlot>();

            slot.isItemSelect = false;
            slot.isCombining = false;
            slot.isCombinable = false;
            slot.isSelected = false;
            slot.contexVisible = false;
            slot.isTriggered = false;

            if (Slots[i].transform.childCount > 1)
            {
                if (slot.itemData != null)
                {
                    slot.itemData.selected = false;
                }
            }
        }
    }

    /// <summary>
    /// Deselect specific slot
    /// </summary>
	public void Deselect(int slotID){
        Slots[slotID].GetComponent<Image>().color = Color.white;

        if (Slots[slotID].transform.childCount > 1)
        {
            Slots[slotID].GetComponentInChildren<InventoryItemData>().selected = false;
        }

		ItemLabel.text = "";
		ItemDescription.text = "";
        ShowContexMenu(false);
        ResetSlotProperties();

        selectedSlotID = -1;
	}

    /// <summary>
    /// Deselect selected Item
    /// </summary>
    public void DeselectSelected()
    {
        if (canDeselect)
        {
            ResetSlotProperties();

            if (selectedSlotID != -1)
            {
                if (Slots[selectedSlotID].GetComponentInChildren<InventoryItemData>())
                {
                    Slots[selectedSlotID].GetComponentInChildren<InventoryItemData>().selected = false;
                }

                Slots[selectedSlotID].GetComponent<Image>().color = Color.white;
                TakeBackButton.gameObject.SetActive(false);
                ShowContexMenu(false);
                ItemLabel.text = "";
                ItemDescription.text = "";
                selectedSlotID = -1;
            }

            if (ContainterItemsCache.Count > 0)
            {
                foreach (var item in ContainterItemsCache)
                {
                    item.Deselect();
                }
            }

            ItemInfoPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Show selected Item Contex Menu
    /// </summary>
    public void ShowContexMenu(bool show, Item item = null, int slot = -1, bool ctx_use = true, bool ctx_combine = true, bool ctx_examine = true, bool ctx_drop = true, bool ctx_shortcut = false, bool ctx_store = false)
    {
        InventoryItemData itemData = null;

        if (show && item != null && slot > -1) {
            Vector3[] corners = new Vector3[4];
            Slots[slot].GetComponent<RectTransform>().GetWorldCorners(corners);
            int[] cornerSlots = Enumerable.Range(0, maxSlots + 1).Where(x => x % cornerSlot == 0).ToArray();
            int n_slot = slot + 1;

            if (slot > -1)
            {
                itemData = GetSlotItemData(slot);
            }

            if (!cornerSlots.Contains(n_slot))
            {
                contexMenu.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                contexMenu.transform.position = corners[2];
            }
            else
            {
                contexMenu.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
                contexMenu.transform.position = corners[1];
            }
        }

        if (item != null)
        {
            bool use = item.isUsable && ctx_use && itemData.customData.canUse;
            bool combine = item.isCombinable && ctx_combine && itemData.customData.canCombine;
            bool examine = item.canInspect && ctx_examine;
            bool drop = item.isDroppable && ctx_drop;
            bool shortcut = item.canBindShortcut && ctx_shortcut;
            bool store = ctx_store;

            if (!show)
            {
                contexUse.gameObject.SetActive(false);
                contexCombine.gameObject.SetActive(false);
                contexExamine.gameObject.SetActive(false);
                contexDrop.gameObject.SetActive(false);
                contexShortcut.gameObject.SetActive(false);
                contexStore.gameObject.SetActive(false);
            }
            else
            {
                contexUse.gameObject.SetActive(use);
                contexCombine.gameObject.SetActive(combine);
                contexExamine.gameObject.SetActive(examine);
                contexDrop.gameObject.SetActive(drop);
                contexShortcut.gameObject.SetActive(shortcut);
                contexStore.gameObject.SetActive(store);
            }

            if (use || combine || examine || drop || store)
            {
                contexMenu.SetActive(true);
            }
            else
            {
                contexMenu.SetActive(false);
            }
        }
        else
        {
            contexMenu.SetActive(false);
        }
    }

    /// <summary>
    /// Show timed UI Notification
    /// </summary>
    public void ShowNotification(string text)
    {
        InventoryNotification.transform.GetComponentInChildren<Text>().text = text;
        InventoryNotification.gameObject.SetActive(true);
        notiFade = true;
        StartCoroutine(fader.StartFadeIO(InventoryNotification.color.a, 1.2f, 0.8f, 3, 4, UIFader.FadeOutAfter.Time));
    }

    /// <summary>
    /// Show fixed UI Notification (Bool Fade Out)
    /// </summary>
    public void ShowNotificationFixed(string text)
    {
        InventoryNotification.transform.GetComponentInChildren<Text>().text = text;
        InventoryNotification.gameObject.SetActive(true);
        notiFade = true;
        fader.fadeOut = false;
        StartCoroutine(fader.StartFadeIO(InventoryNotification.color.a, 1.2f, 0.8f, 3, 3, UIFader.FadeOutAfter.Bool));
    }
}

public class InventoryItem
{
    public Item item;
    public CustomItemData customData;

    public InventoryItem(Item item, CustomItemData data)
    {
        this.item = item;
        customData = data;
    }
}

public class Item
{
    //Main
    public int ID { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public ItemType itemType { get; set; }
    public ItemAction useActionType { get; set; }
    public Sprite itemSprite { get; set; }
    public GameObject dropObject { get; set; }
    public GameObject packDropObject { get; set; }

    //Toggles
    public bool isStackable { get; set; }
    public bool isUsable { get; set; }
    public bool isCombinable { get; set; }
    public bool isDroppable { get; set; }
    public bool canInspect { get; set; }
    public bool canBindShortcut { get; set; }
    public bool combineGetItem { get; set; }
    public bool combineNoRemove { get; set; }
    public bool combineGetSwItem { get; set; }
    public bool useItemSwitcher { get; set; }
    public bool showContainerDesc { get; set; }
    public bool doActionUse { get; set; }
    public bool doActionCombine { get; set; }

    //Sounds
    public AudioClip useSound { get; set; }
    public AudioClip combineSound { get; set; }
    public float soundVolume { get; set; }

    //Settings
    public int maxItemCount { get; set; }
    public int useSwitcherID { get; set; }
    public int healAmount { get; set; }

    //Use Action Settings
    public InventoryScriptable.ItemMapper.CustomActionSettings customActions { get; set; }

    //Combine Settings
    public InventoryScriptable.ItemMapper.CombineSettings[] combineSettings { get; set; }

    public Item()
    {
        ID = 0;
    }

    public Item(int itemId, InventoryScriptable.ItemMapper mapper)
    {
        ID = itemId;
        Title = mapper.Title;
        Description = mapper.Description;
        itemType = mapper.itemType;
        useActionType = mapper.useActionType;
        itemSprite = mapper.itemSprite;
        dropObject = mapper.dropObject;
        packDropObject = mapper.packDropObject;

        isStackable = mapper.itemToggles.isStackable;
        isUsable = mapper.itemToggles.isUsable;
        isCombinable = mapper.itemToggles.isCombinable;
        isDroppable = mapper.itemToggles.isDroppable;
        canInspect = mapper.itemToggles.canInspect;
        canBindShortcut = mapper.itemToggles.canBindShortcut;
        combineGetItem = mapper.itemToggles.CombineGetItem;
        combineNoRemove = mapper.itemToggles.CombineNoRemove;
        combineGetSwItem = mapper.itemToggles.CombineGetSwItem;
        useItemSwitcher = mapper.itemToggles.UseItemSwitcher;
        showContainerDesc = mapper.itemToggles.ShowContainerDesc;
        doActionUse = mapper.itemToggles.doActionUse;
        doActionCombine = mapper.itemToggles.doActionCombine;

        useSound = mapper.itemSounds.useSound;
        combineSound = mapper.itemSounds.combineSound;
        soundVolume = mapper.itemSounds.soundVolume;

        maxItemCount = mapper.itemSettings.maxItemCount;
        useSwitcherID = mapper.itemSettings.useSwitcherID;
        healAmount = mapper.itemSettings.healAmount;

        customActions = mapper.useActionSettings;

        combineSettings = mapper.combineSettings;
    }
}
