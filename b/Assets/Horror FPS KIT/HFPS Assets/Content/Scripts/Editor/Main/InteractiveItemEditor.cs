using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(InteractiveItem)), CanEditMultipleObjects]
public class InteractiveItemEditor : Editor {

    //Types
    public SerializedProperty ItemType_Prop;
    public SerializedProperty ExamineType_Prop;
    public SerializedProperty ExamineRotate_Prop;
    public SerializedProperty MessageType_Prop;
    public SerializedProperty DisableType_Prop;

    public SerializedProperty Amount_Prop;
    public SerializedProperty WeaponID_Prop;
    public SerializedProperty InventoryID_Prop;
    public SerializedProperty BackpackExpand_Prop;
    public SerializedProperty MarkAsLight_Prop;
    public SerializedProperty PickupSwitch_Prop;
    public SerializedProperty floatingIcon_prop;
    public SerializedProperty showItemName_prop;

    public SerializedProperty MessageText_prop;
    public SerializedProperty MessageTime_prop;
    public SerializedProperty MessageTips_prop;

    public SerializedProperty PickupSound_Prop;
    public SerializedProperty ExamineSound_Prop;
    public SerializedProperty Volume_Prop;

    //Examine
    public SerializedProperty ExaminetName_Prop;
    public SerializedProperty ExamineDistance_Prop;
    public SerializedProperty cameraFace_Prop;
    public SerializedProperty faceRotation_Prop;

    public SerializedProperty colDisable_Prop;
    public SerializedProperty colEnable_Prop;

    public SerializedProperty readText_Prop;
    public SerializedProperty textSize_Prop;
    public SerializedProperty examineCol_Prop;
    public SerializedProperty enableCursor_Prop;

    public SerializedProperty itemTag_Prop;
    public SerializedProperty itemValue_Prop;
    public SerializedProperty itemPath_Prop;


    void OnEnable () {
		ItemType_Prop = serializedObject.FindProperty ("ItemType");
        ExamineType_Prop = serializedObject.FindProperty("examineType");
        ExamineRotate_Prop = serializedObject.FindProperty("examineRotate");
        MessageType_Prop = serializedObject.FindProperty("messageType");
        DisableType_Prop = serializedObject.FindProperty("disableType");
        Amount_Prop = serializedObject.FindProperty("Amount");
		WeaponID_Prop = serializedObject.FindProperty ("WeaponID");
		InventoryID_Prop = serializedObject.FindProperty ("InventoryID");
		BackpackExpand_Prop = serializedObject.FindProperty ("BackpackExpand");
        MarkAsLight_Prop = serializedObject.FindProperty("markLightObject");
        PickupSwitch_Prop = serializedObject.FindProperty("pickupSwitch");
        PickupSound_Prop = serializedObject.FindProperty("PickupSound");
        Volume_Prop = serializedObject.FindProperty("Volume");
        floatingIcon_prop = serializedObject.FindProperty("floatingIconEnabled");
        showItemName_prop = serializedObject.FindProperty("showItemName");

        MessageText_prop = serializedObject.FindProperty("Message");
        MessageTime_prop = serializedObject.FindProperty("MessageTime");
        MessageTips_prop = serializedObject.FindProperty("MessageTips");

        //Examine
        ExaminetName_Prop = serializedObject.FindProperty("ItemName");
        ExamineSound_Prop = serializedObject.FindProperty("ExamineSound");
        ExamineDistance_Prop = serializedObject.FindProperty("ExamineDistance");
        cameraFace_Prop = serializedObject.FindProperty("faceToCamera");
        faceRotation_Prop = serializedObject.FindProperty("faceRotation");
        colDisable_Prop = serializedObject.FindProperty("CollidersDisable");
        colEnable_Prop = serializedObject.FindProperty("CollidersEnable");
        readText_Prop = serializedObject.FindProperty("paperReadText");
        textSize_Prop = serializedObject.FindProperty("textSize");
        examineCol_Prop = serializedObject.FindProperty("examineCollect");
        enableCursor_Prop = serializedObject.FindProperty("enableCursor");

        itemTag_Prop = serializedObject.FindProperty("CustomTag");
        itemValue_Prop = serializedObject.FindProperty("CustomValue");
        itemPath_Prop = serializedObject.FindProperty("CustomPath");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        InteractiveItem.Type type = (InteractiveItem.Type)ItemType_Prop.enumValueIndex;
        InteractiveItem.ExamineType exmType = (InteractiveItem.ExamineType)ExamineType_Prop.enumValueIndex;
        InteractiveItem.MessageType msg = (InteractiveItem.MessageType)MessageType_Prop.enumValueIndex;

        EditorGUILayout.PropertyField(ItemType_Prop);

        if (type != InteractiveItem.Type.OnlyExamine)
        {
            EditorGUILayout.PropertyField(MessageType_Prop);
            EditorGUILayout.PropertyField(DisableType_Prop);

            if (msg != InteractiveItem.MessageType.None)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Message Options", EditorStyles.boldLabel);

                switch (msg)
                {
                    case InteractiveItem.MessageType.Hint:
                        EditorGUILayout.PropertyField(MessageText_prop, new GUIContent("Hint Message"));
                        break;
                    case InteractiveItem.MessageType.PickupHint:
                        EditorGUILayout.PropertyField(MessageText_prop, new GUIContent("Item Name"));
                        break;
                    case InteractiveItem.MessageType.Message:
                        EditorGUILayout.PropertyField(MessageText_prop, new GUIContent("Message"));
                        break;
                    case InteractiveItem.MessageType.ItemName:
                        EditorGUILayout.PropertyField(MessageText_prop, new GUIContent("Item Name"));
                        break;
                }

                if (msg != InteractiveItem.MessageType.Message && msg != InteractiveItem.MessageType.ItemName)
                {
                    EditorGUILayout.PropertyField(MessageTime_prop);
                    EditorGUILayout.PropertyField(MessageTips_prop, true);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Item Options", EditorStyles.boldLabel);

            switch (type)
            {
                case InteractiveItem.Type.InventoryItem:
                    EditorGUILayout.PropertyField(InventoryID_Prop, new GUIContent("Inventory ID"));
                    EditorGUILayout.PropertyField(Amount_Prop, new GUIContent("Item Amount"));
                    EditorGUILayout.PropertyField(MarkAsLight_Prop, new GUIContent("Default Light"));
                    break;

                case InteractiveItem.Type.ArmsItem:
                    EditorGUILayout.PropertyField(InventoryID_Prop, new GUIContent("Inventory ID"));
                    EditorGUILayout.PropertyField(WeaponID_Prop, new GUIContent("Arms ID"));
                    EditorGUILayout.PropertyField(Amount_Prop, new GUIContent("Item Amount"));
                    EditorGUILayout.PropertyField(PickupSwitch_Prop, new GUIContent("Auto Switch"));
                    EditorGUILayout.PropertyField(MarkAsLight_Prop, new GUIContent("Default Light"));
                    break;

                case InteractiveItem.Type.BackpackExpand:
                    EditorGUILayout.PropertyField(BackpackExpand_Prop, new GUIContent("Expand Amount"));
                    break;
            }
        }

        EditorGUILayout.PropertyField(showItemName_prop, new GUIContent("UIInfo Item Name"));
        EditorGUILayout.PropertyField(floatingIcon_prop, new GUIContent("Enable Floating Icon"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Examine Options", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(ExamineType_Prop);

        if (exmType != InteractiveItem.ExamineType.None)
        {
            EditorGUILayout.PropertyField(ExamineRotate_Prop);

            if (exmType == InteractiveItem.ExamineType.Object || exmType == InteractiveItem.ExamineType.AdvancedObject)
            {
                EditorGUILayout.PropertyField(ExaminetName_Prop, new GUIContent("Examine Name"));
            }

            EditorGUILayout.PropertyField(ExamineDistance_Prop, new GUIContent("Examine Distance"));
            EditorGUILayout.PropertyField(enableCursor_Prop, new GUIContent("Enable Cursor"));

            if (enableCursor_Prop.boolValue)
            {
                EditorGUILayout.PropertyField(examineCol_Prop, new GUIContent("Click Collect"));
            }

            EditorGUILayout.Space();

            if (exmType == InteractiveItem.ExamineType.AdvancedObject)
            {
                EditorGUILayout.PropertyField(colDisable_Prop, new GUIContent("Colliders Disable"), true);
                EditorGUILayout.PropertyField(colEnable_Prop, new GUIContent("Colliders Enable"), true);
            }

            if (exmType == InteractiveItem.ExamineType.Paper)
            {
                EditorGUILayout.PropertyField(readText_Prop, new GUIContent("Paper Text"));
                EditorGUILayout.PropertyField(textSize_Prop, new GUIContent("Text Size"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(cameraFace_Prop, new GUIContent("Face To Camera"));

            if (cameraFace_Prop.boolValue)
            {
                EditorGUILayout.PropertyField(faceRotation_Prop, new GUIContent("Face Rotation"));
            }
        }
        else
        {
            if (showItemName_prop.boolValue)
            {
                EditorGUILayout.PropertyField(ExaminetName_Prop, new GUIContent("Examine Name"));
            }
        }

        if(type == InteractiveItem.Type.InventoryItem)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Custom Item Data", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(itemTag_Prop, new GUIContent("Item Tag"), true);
            EditorGUILayout.PropertyField(itemValue_Prop, new GUIContent("Stored Value"), true);
            EditorGUILayout.PropertyField(itemPath_Prop, new GUIContent("Stored Texture Path"), true);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sounds", EditorStyles.boldLabel);

        if (type != InteractiveItem.Type.OnlyExamine)
        {
            EditorGUILayout.PropertyField(PickupSound_Prop, new GUIContent("Pickup Sound"));
        }
        if (exmType != InteractiveItem.ExamineType.None)
        {
            EditorGUILayout.PropertyField(ExamineSound_Prop, new GUIContent("Examine Sound"));
        }
        EditorGUILayout.PropertyField(Volume_Prop, new GUIContent("Sounds Volume"));

        serializedObject.ApplyModifiedProperties();
    }
}
