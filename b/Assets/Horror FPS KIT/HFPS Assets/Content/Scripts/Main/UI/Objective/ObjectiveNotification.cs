using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveNotification : MonoBehaviour {

    public void CompleteObjective()
    {
        Destroy(gameObject);
    }
}
