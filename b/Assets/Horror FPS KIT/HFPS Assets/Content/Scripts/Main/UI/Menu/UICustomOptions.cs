/* UICustomOptions.cs by ThunderWire Studio
 * Version 1.1
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using ThunderWire.Utility;

/// <summary>
/// Script which control options in runtime
/// </summary>
public class UICustomOptions : MonoBehaviour {

    private ConfigHandler configHandler;
    private InputController inputController;

    [Header("Main")]
    public GraphicScriptable graphicSettings;
    [Tooltip("You can leave this empty, if Post Processing is in Main Camera.")]
    public PostProcessVolume postProcess;

    [Header("Objects")]
    public GameObject DuplicateInputGo;
    public Button ApplyButton;
    public Button BackButton;

    [Header("General")]
    public Slider VolumeSlider;
    public Slider SensitivitySlider;

    [Header("General Graphic")]
    public Toggle GrainToggle;
    public Toggle BloomToggle;
    public Toggle MotionBlurToggle;
    public Toggle AmbientOccToggle;

    [Header("Advanced Graphic")]
    public Dropdown ResolutionDropdown;
    private int ResolutionLevel = 0;
    private bool resChanged = false;
    public Dropdown QualityDropdown;
    private int QualityLevel = 0;
    private bool qChanged = false;
    public Dropdown AntialiasingDropdown;
    private int AntialiasingLevel = 0;
    private bool antiaChanged = false;
    public Dropdown AnisotropicDropdown;
    private int AnisotropicLevel = 0;
    private bool anisoChanged = false;
    public Dropdown TextureQualityDropdown;
    private int TextureQualityLevel = 0;
    private bool tqChanged = false;
    public Dropdown SkinWeightsDropdown;
    private int SkinWeightsLevel = 0;
    private bool swChanged = false;
    public Dropdown VSyncDropdown;
    private int VSyncLevel = 0;
    private bool vsyncChanged = false;
    public Dropdown ShadowResolutionDropdown;
    private int ShadowResolutionLevel = 0;
    private bool swResChanged = false;
    public Dropdown ShadowQualityDropdown;
    private int ShadowQualityLevel = 0;
    private bool swQChanged = false;
    public Dropdown ShadowCascadesDropdown;
    private int ShadowCascadesLevel = 0;
    private bool swCasChanged = false;
    public Dropdown ShadowProjecionDropdown;
    private int ShadowProjecionLevel = 0;
    private bool swProjChanged = false;

    public Slider ShadowDistanceSlider;
    public Toggle FullscreenToggle;
    private bool fullscreenChanged = false;
    public Text shadowDistanceText;

    [HideInInspector] public string RewriteKeycode;

    private bool fullscreen;
    private bool custom;
    private bool isLoaded;

    Resolution[] Resolutions;

    void Awake()
    {
        Resolutions = Screen.resolutions.Select(resolution => new Resolution { width = resolution.width, height = resolution.height }).Distinct().Reverse().ToArray();

        if (GetComponent<ConfigHandler>() && GetComponent<InputController>())
        {
            configHandler = GetComponent<ConfigHandler>();
            inputController = GetComponent<InputController>();
        }
        else
        {
            Debug.LogError("[Options] UICustomOptions does not work without ConfigHandler and InputController script!");
        }

        if (!postProcess)
        {
            if (Tools.MainCamera().GetComponent<PostProcessVolume>())
            {
                postProcess = Tools.MainCamera().GetComponent<PostProcessVolume>();
            }
            else
            {
                Debug.LogError("[PostProcessing] Could not find PostProcessing");
            }
        }
    }

    void Start()
    {
        if (!configHandler && !inputController) return;

        if (postProcess)
        {
            if (postProcess.sharedProfile.HasSettings<Grain>())
            {
                GrainToggle.isOn = postProcess.sharedProfile.GetSetting<Grain>().enabled.value;
            }
            else
            {
                GrainToggle.isOn = false;
            }

            if (postProcess.sharedProfile.HasSettings<Bloom>())
            {
                BloomToggle.isOn = postProcess.sharedProfile.GetSetting<Bloom>().enabled.value;
            }
            else
            {
                BloomToggle.isOn = false;
            }

            if (postProcess.sharedProfile.HasSettings<MotionBlur>())
            {
                MotionBlurToggle.isOn = postProcess.profile.GetSetting<MotionBlur>().enabled.value;
            }
            else
            {
                MotionBlurToggle.isOn = false;
            }

            if (postProcess.sharedProfile.HasSettings<AmbientOcclusion>())
            {
                AmbientOccToggle.isOn = postProcess.profile.GetSetting<AmbientOcclusion>().enabled.value;
            }
            else
            {
                AmbientOccToggle.isOn = false;
            }
        }

        SetResolutions();
        SetQualities();
        UpdateOptions(!GetComponent<HFPS_GameManager>());
        SetDropdownListeners();

        ResolutionDropdown.value = GetCurrentResolution();
        FullscreenToggle.isOn = Screen.fullScreen;
        fullscreen = Screen.fullScreen;

        resChanged = false;
        fullscreenChanged = false;
    }

    void Update()
    {
        if (!configHandler && !inputController) return;

        shadowDistanceText.text = ShadowDistanceSlider.value.ToString();

        if (!qChanged && isLoaded)
        {
            if (antiaChanged || anisoChanged || tqChanged || swChanged || vsyncChanged || swResChanged || swQChanged || swCasChanged || swProjChanged)
            {
                custom = true;
                QualityLevel = GetCustomQuality();
                QualityDropdown.value = GetCustomQuality();
            }
        }
    }

    private void SetResolutions()
    {
        ResolutionDropdown.options.Clear();

        for (int i = 0; i < Resolutions.Length; i++)
        {
            ResolutionDropdown.options.Add(new Dropdown.OptionData(Resolutions[i].width + "x" + Resolutions[i].height));
        }

        ResolutionDropdown.RefreshShownValue();
    }

    int GetCurrentResolution()
    {
        for (int i = 0; i < ResolutionDropdown.options.Count; i++)
        {
            string[] ress = ResolutionDropdown.options[i].text.Split(new char[] { 'x' });
            if (Screen.width.ToString() == ress[0] && Screen.height.ToString() == ress[1])
            {
                return i;
            }
        }
        return 0;
    }

    private void SetQualities()
    {
        QualityDropdown.options.Clear();

        for (int i = 0; i < graphicSettings.qualityLevels.Count; i++)
        {
            QualityDropdown.options.Add(new Dropdown.OptionData(graphicSettings.qualityLevels[i].QualityName));
        }

        QualityDropdown.options.Add(new Dropdown.OptionData("Custom"));
        QualityDropdown.RefreshShownValue();
    }

    private int GetCustomQuality()
    {
        return QualityDropdown.options.Count - 1;
    }

    private void UpdateOptions(bool Apply)
    {
        if (configHandler.ContainsSection("Graphic") && configHandler.ContainsSection("Game"))
        {
            DeserializeSettings(Apply);
        }
        else
        {
            QualityDropdown.value = QualitySettings.GetQualityLevel();
            QualityLevel = QualitySettings.GetQualityLevel();
            AntialiasingDropdown.value = DDAntialiasingLevel(QualitySettings.antiAliasing);
            AnisotropicDropdown.value = (int)QualitySettings.anisotropicFiltering;
            TextureQualityDropdown.value = DDTextureQuality(QualitySettings.masterTextureLimit);
            SkinWeightsDropdown.value = DDSkinWeightsLevel((int)QualitySettings.skinWeights);
            VSyncDropdown.value = QualitySettings.vSyncCount;
            ShadowResolutionDropdown.value = (int)QualitySettings.shadowResolution;
            ShadowQualityDropdown.value = (int)QualitySettings.shadows;
            ShadowCascadesDropdown.value = DDSCascadesLevel(QualitySettings.shadowCascades);
            ShadowProjecionDropdown.value = (int)QualitySettings.shadowProjection;
            ShadowDistanceSlider.value = QualitySettings.shadowDistance;
            SerializeGraphic();
            isLoaded = true;
        }
    }

    private void SetDropdownListeners()
    {
        ResolutionDropdown.onValueChanged.AddListener(delegate { OnResolutionDropdownChange(ResolutionDropdown); });
        QualityDropdown.onValueChanged.AddListener(delegate { OnQualityDropdownChange(QualityDropdown); });
        AntialiasingDropdown.onValueChanged.AddListener(delegate { OnAntialiasingDropdownChange(AntialiasingDropdown); });
        AnisotropicDropdown.onValueChanged.AddListener(delegate { OnAnisotropicDropdownChange(AnisotropicDropdown); });
        TextureQualityDropdown.onValueChanged.AddListener(delegate { OnTextureQualityDropdownChange(TextureQualityDropdown); });
        SkinWeightsDropdown.onValueChanged.AddListener(delegate { OnBlendWeightsDropdownChange(SkinWeightsDropdown); });
        VSyncDropdown.onValueChanged.AddListener(delegate { OnVSyncDropdownChange(VSyncDropdown); });
        ShadowResolutionDropdown.onValueChanged.AddListener(delegate { OnShadowResolutionDropdownChange(ShadowResolutionDropdown); });
        ShadowQualityDropdown.onValueChanged.AddListener(delegate { OnShadowQualityDropdownChange(ShadowQualityDropdown); });
        ShadowCascadesDropdown.onValueChanged.AddListener(delegate { OnShadowCascadesDropdownChange(ShadowCascadesDropdown); });
        ShadowProjecionDropdown.onValueChanged.AddListener(delegate { OnShadowProjecionDropdownChange(ShadowProjecionDropdown); });
        FullscreenToggle.onValueChanged.AddListener(delegate { OnFullscreenChange(FullscreenToggle); });
    }

    void OnDestroy()
    {
        ResolutionDropdown.onValueChanged.RemoveAllListeners();
        QualityDropdown.onValueChanged.RemoveAllListeners();
        AntialiasingDropdown.onValueChanged.RemoveAllListeners();
        AnisotropicDropdown.onValueChanged.RemoveAllListeners();
        TextureQualityDropdown.onValueChanged.RemoveAllListeners();
        SkinWeightsDropdown.onValueChanged.RemoveAllListeners();
        VSyncDropdown.onValueChanged.RemoveAllListeners();
        ShadowResolutionDropdown.onValueChanged.RemoveAllListeners();
        ShadowQualityDropdown.onValueChanged.RemoveAllListeners();
        ShadowCascadesDropdown.onValueChanged.RemoveAllListeners();
        ShadowProjecionDropdown.onValueChanged.RemoveAllListeners();
    }

    public void Rewrite()
    {
        inputController.Rewrite(RewriteKeycode);
        DuplicateInputGo.SetActive(false);
    }

    public void BackRewrite()
    {
        inputController.BackRewrite();
        DuplicateInputGo.SetActive(false);
    }

    public void ApplyAllSettings()
    {
        ApplyGeneral();
        ApplyGraphic(true);
        ApplyControls();

        configHandler.ShowSpinner(1.5f);
    }

    private void ApplyGeneral()
    {
        if (configHandler)
        {
            configHandler.Serialize("Game", "Volume", VolumeSlider.value.ToString());
            configHandler.Serialize("Game", "Sensitivity", SensitivitySlider.value.ToString());
        }
    }

    private void ApplyGraphic(bool Serialize)
    {
        if (!Serialize)
        {
            antiaChanged = true;
            anisoChanged = true;
            tqChanged = true;
            swChanged = true;
            vsyncChanged = true;
            swResChanged = true;
            swQChanged = true;
            swCasChanged = true;
            swProjChanged = true;
        }

        if (postProcess)
        {
            if (postProcess.profile.HasSettings<Grain>())
            {
                postProcess.sharedProfile.GetSetting<Grain>().enabled.Override(GrainToggle.isOn);
                postProcess.profile.GetSetting<Grain>().enabled.Override(GrainToggle.isOn);
            }

            if (postProcess.profile.HasSettings<Bloom>())
            {
                postProcess.sharedProfile.GetSetting<Bloom>().enabled.Override(BloomToggle.isOn);
                postProcess.profile.GetSetting<Bloom>().enabled.Override(BloomToggle.isOn);
            }

            if (postProcess.profile.HasSettings<MotionBlur>())
            {
                postProcess.sharedProfile.GetSetting<MotionBlur>().enabled.Override(MotionBlurToggle.isOn);
                postProcess.profile.GetSetting<MotionBlur>().enabled.Override(MotionBlurToggle.isOn);
            }

            if (postProcess.profile.HasSettings<AmbientOcclusion>())
            {
                postProcess.sharedProfile.GetSetting<AmbientOcclusion>().enabled.Override(AmbientOccToggle.isOn);
                postProcess.profile.GetSetting<AmbientOcclusion>().enabled.Override(AmbientOccToggle.isOn);
            }
        }

        if (fullscreenChanged)
        {
            Screen.fullScreen = fullscreen;
            fullscreenChanged = false;
        }

        if (resChanged)
        {
            Resolution resolution = Resolutions[ResolutionLevel];
            Screen.SetResolution(resolution.width, resolution.height, fullscreen);
            resChanged = false;
        }

        if (antiaChanged)
        {
            QualitySettings.antiAliasing = ConvertAALevel(AntialiasingLevel);
            antiaChanged = false;
        }
        if (anisoChanged)
        {
            QualitySettings.anisotropicFiltering = (AnisotropicFiltering)AnisotropicLevel;
            anisoChanged = false;
        }
        if (tqChanged)
        {
            QualitySettings.masterTextureLimit = ConvertTQLevel(TextureQualityLevel);
            tqChanged = false;
        }
        if (swChanged)
        {
            QualitySettings.skinWeights = (SkinWeights)ConvertSkinWeights(SkinWeightsLevel);
            swChanged = false;
        }
        if (vsyncChanged)
        {
            QualitySettings.vSyncCount = VSyncLevel;
            vsyncChanged = false;
        }
        if (swResChanged)
        {
            QualitySettings.shadowResolution = (ShadowResolution)ShadowResolutionLevel;
            swResChanged = false;
        }
        if (swQChanged)
        {
            QualitySettings.shadows = (ShadowQuality)ShadowQualityLevel;
            swQChanged = false;
        }
        if (swCasChanged)
        {
            QualitySettings.shadowCascades = ConvertSCLevel(ShadowCascadesLevel);
            swCasChanged = false;
        }
        if (swProjChanged)
        {
            QualitySettings.shadowProjection = (ShadowProjection)ShadowProjecionLevel;
            swProjChanged = false;
        }

        if (Serialize) { SerializeGraphic(); }
    }

    private void ApplyControls()
    {
        if (GetComponent<InputController>())
        {
            GetComponent<InputController>().RefreshInputs();
        }
        else
        {
            Debug.LogError("Options Error: InputController script does not found!");
        }
    }

    public void SetGraphicSettings()
    {
        string qualityName = QualityDropdown.options[QualityDropdown.value].text;

        foreach (var qualityEntry in graphicSettings.qualityLevels)
        {
            if (qualityEntry.QualityName == qualityName)
            {
                AntialiasingDropdown.value = (int)qualityEntry.m_Antialiasing;
                AnisotropicDropdown.value = (int)qualityEntry.m_AnisotropicFiltering;
                TextureQualityDropdown.value = (int)qualityEntry.m_TextureQuality;
                SkinWeightsDropdown.value = (int)qualityEntry.m_SkinWeights;
                VSyncDropdown.value = (int)qualityEntry.m_VSync;
                ShadowResolutionDropdown.value = (int)qualityEntry.m_ShadowResolution;
                ShadowQualityDropdown.value = (int)qualityEntry.m_ShadowQuality;
                ShadowCascadesDropdown.value = (int)qualityEntry.m_ShadowCascades;
                ShadowProjecionDropdown.value = (int)qualityEntry.m_ShadowProjection;
                ShadowDistanceSlider.value = qualityEntry.m_ShadowDistance;
            }
        }
    }

    private void SerializeGraphic()
    {
        configHandler.Serialize("Graphic", new Dictionary<string, string>
        {
            { "PostProcessing_Grain", GrainToggle.isOn.ToString() },
            { "PostProcessing_Bloom", BloomToggle.isOn.ToString() },
            { "PostProcessing_MotionBlur", MotionBlurToggle.isOn.ToString() },
            { "PostProcessing_AmbientOcclusion", AmbientOccToggle.isOn.ToString() },
            { "GraphicQuality", QualityDropdown.value.ToString() },
            { "Antialiasing", AntialiasingDropdown.value.ToString() },
            { "Anisotropic", AnisotropicDropdown.value.ToString() },
            { "TextureQuality", TextureQualityDropdown.value.ToString() },
            { "SkinWeight", SkinWeightsDropdown.value.ToString() },
            { "VSync", VSyncDropdown.value.ToString() },
            { "ShadowResolution", ShadowResolutionDropdown.value.ToString() },
            { "ShadowQuality", ShadowQualityDropdown.value.ToString() },
            { "ShadowCascades", ShadowCascadesDropdown.value.ToString() },
            { "ShadowProjecion", ShadowProjecionDropdown.value.ToString() },
            { "ShadowDistance", ShadowDistanceSlider.value.ToString() }
        });
    }

    private void DeserializeSettings(bool Apply)
    {
        GrainToggle.isOn = configHandler.Deserialize<bool>("Graphic", "PostProcessing_Grain");
        BloomToggle.isOn = configHandler.Deserialize<bool>("Graphic", "PostProcessing_Bloom");
        MotionBlurToggle.isOn = configHandler.Deserialize<bool>("Graphic", "PostProcessing_MotionBlur");
        AmbientOccToggle.isOn = configHandler.Deserialize<bool>("Graphic", "PostProcessing_AmbientOcclusion");

        QualityLevel = configHandler.Deserialize<int>("Graphic", "GraphicQuality");
        QualityDropdown.value = QualityLevel;
        AntialiasingLevel = configHandler.Deserialize<int>("Graphic", "Antialiasing");
        AntialiasingDropdown.value = AntialiasingLevel;
        AnisotropicLevel = configHandler.Deserialize<int>("Graphic", "Anisotropic");
        AnisotropicDropdown.value = AnisotropicLevel;
        TextureQualityLevel = configHandler.Deserialize<int>("Graphic", "TextureQuality");
        TextureQualityDropdown.value = TextureQualityLevel;
        SkinWeightsLevel = configHandler.Deserialize<int>("Graphic", "SkinWeight");
        SkinWeightsDropdown.value = SkinWeightsLevel;
        VSyncLevel = configHandler.Deserialize<int>("Graphic", "VSync");
        VSyncDropdown.value = VSyncLevel;
        ShadowResolutionLevel = configHandler.Deserialize<int>("Graphic", "ShadowResolution");
        ShadowResolutionDropdown.value = ShadowResolutionLevel;
        ShadowQualityLevel = configHandler.Deserialize<int>("Graphic", "ShadowQuality");
        ShadowQualityDropdown.value = ShadowQualityLevel;  
        ShadowCascadesLevel = configHandler.Deserialize<int>("Graphic", "ShadowCascades");
        ShadowCascadesDropdown.value = ShadowCascadesLevel;
        ShadowProjecionLevel = configHandler.Deserialize<int>("Graphic", "ShadowProjecion");
        ShadowProjecionDropdown.value = ShadowProjecionLevel;
        ShadowDistanceSlider.value = configHandler.Deserialize<int>("Graphic", "ShadowDistance");

        VolumeSlider.value = configHandler.Deserialize<float>("Game", "Volume");
        SensitivitySlider.value = configHandler.Deserialize<float>("Game", "Sensitivity");

        ApplyGraphic(Apply);

        isLoaded = true;
    }


    int ConvertAALevel(int Level)
    {
        switch (Level)
        {
            case 0:
                return 0;
            case 1:
                return 2;
            case 2:
                return 4;
            case 3:
                return 8;
        }
        return 0;
    }

    int ConvertTQLevel(int Level)
    {
        switch (Level)
        {
            case 0:
                return 3;
            case 1:
                return 2;
            case 2:
                return 1;
            case 3:
                return 0;
        }
        return 0;
    }

    int ConvertSkinWeights(int Level)
    {
        switch (Level)
        {
            case 0:
                return 1;
            case 1:
                return 2;
            case 2:
                return 4;
            case 3:
                return 255;
        }
        return 0;
    }

    int ConvertSCLevel(int Level)
    {
        switch (Level)
        {
            case 0:
                return 0;
            case 1:
                return 2;
            case 2:
                return 4;
        }
        return 0;
    }

    int DDAntialiasingLevel(int Level)
    {
        switch (Level)
        {
            case 0:
                return 0;
            case 2:
                return 1;
            case 4:
                return 2;
            case 8:
                return 3;
        }
        return 0;
    }

    int DDTextureQuality(int Level)
    {
        switch (Level)
        {
            case 0:
                return 3;
            case 1:
                return 2;
            case 2:
                return 1;
            case 3:
                return 0;
        }
        return 0;
    }

    int DDSkinWeightsLevel(int Level)
    {
        switch (Level)
        {
            case 1:
                return 0;
            case 2:
                return 1;
            case 4:
                return 2;
            case 255:
                return 3;
        }
        return 0;
    }

    int DDSCascadesLevel(int Level)
    {
        switch (Level)
        {
            case 0:
                return 0;
            case 2:
                return 1;
            case 4:
                return 2;
        }
        return 0;
    }

    public void OnFullscreenChange(Toggle toggle)
    {
        fullscreen = toggle.isOn;
        fullscreenChanged = true;
    }

    /* ON DROPDOWN CHANGE */
    public void OnResolutionDropdownChange(Dropdown dropdown)
    {
        ResolutionLevel = dropdown.value;
        resChanged = true;
    }
    public void OnQualityDropdownChange(Dropdown dropdown)
    {
        if (!custom)
        {
            SetGraphicSettings();
            qChanged = true;
        }
        QualityLevel = dropdown.value;
    }
    public void OnAntialiasingDropdownChange(Dropdown dropdown)
    {
        AntialiasingLevel = dropdown.value;
        antiaChanged = true;
    }
    public void OnAnisotropicDropdownChange(Dropdown dropdown)
    {
        AnisotropicLevel = dropdown.value;
        anisoChanged = true;
    }
    public void OnTextureQualityDropdownChange(Dropdown dropdown)
    {
        TextureQualityLevel = dropdown.value;
        tqChanged = true;
    }
    public void OnBlendWeightsDropdownChange(Dropdown dropdown)
    {
        SkinWeightsLevel = dropdown.value;
        swChanged = true;
    }
    public void OnVSyncDropdownChange(Dropdown dropdown)
    {
        VSyncLevel = dropdown.value;
        vsyncChanged = true;
    }
    public void OnShadowResolutionDropdownChange(Dropdown dropdown)
    {
        ShadowResolutionLevel = dropdown.value;
        swResChanged = true;
    }
    public void OnShadowQualityDropdownChange(Dropdown dropdown)
    {
        ShadowQualityLevel = dropdown.value;
        swQChanged = true;
    }
    public void OnShadowCascadesDropdownChange(Dropdown dropdown)
    {
        ShadowCascadesLevel = dropdown.value;
        swCasChanged = true;
    }
    public void OnShadowProjecionDropdownChange(Dropdown dropdown)
    {
        ShadowProjecionLevel = dropdown.value;
        swProjChanged = true;
    }
}
