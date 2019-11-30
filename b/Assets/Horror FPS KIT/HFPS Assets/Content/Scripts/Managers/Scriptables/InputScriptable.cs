using System;
using System.Collections.Generic;
using UnityEngine;

public class InputScriptable : ScriptableObject {

    [Serializable]
    public class InputMaper
    {
        public string InputName;
        public KeyCode DefaultKey;
    }

    public bool RewriteConfig;

    public List<InputMaper> inputMap = new List<InputMaper>();
}
