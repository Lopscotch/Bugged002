using UnityEngine;

public class ScriptManager : Singleton<ScriptManager> {

    [Header("Main Scripts")]
    public HFPS_GameManager m_GameManager;
    public InputController m_InputController;

    [Header("Cameras")]
    public Camera MainCamera;
    public Camera ArmsCamera;

    [Header("Other")]
    public AudioSource AmbienceSource;
    public AudioSource SoundEffects;

    // All Important Scripts
    private ItemSwitcher bl_ItemSwitcher;
    private InteractManager bl_InteractManager;
    private Inventory bl_Inventory;
    private PlayerFunctions bl_PlayerFunctions;

    [HideInInspector] public bool ScriptEnabledGlobal;
    [HideInInspector] public bool ScriptGlobalState;

    private void Awake()
    {
        bl_Inventory = m_GameManager.inventoryScript;
        bl_ItemSwitcher = GetComponentInChildren<ItemSwitcher>(true);
        bl_InteractManager = GetComponent<InteractManager>();
        bl_PlayerFunctions = GetComponent<PlayerFunctions>();
    }

    void Start()
    {
        ScriptEnabledGlobal = true;
        ScriptGlobalState = true;
    }

    public T GetScript<T>() where T : MonoBehaviour
    {
        return (T)ReturnScript(typeof(T));
    }

    private object ReturnScript(System.Type type)
    {
        if(type == typeof(ItemSwitcher))
        {
            return bl_ItemSwitcher;
        }
        if (type == typeof(InputController))
        {
            return m_InputController;
        }
        if (type == typeof(HFPS_GameManager))
        {
            return m_GameManager;
        }
        if (type == typeof(InteractManager))
        {
            return bl_InteractManager;
        }
        if (type == typeof(Inventory))
        {
            return bl_Inventory;
        }
        if (type == typeof(PlayerFunctions))
        {
            return bl_PlayerFunctions;
        }

        return null;
    }
}
