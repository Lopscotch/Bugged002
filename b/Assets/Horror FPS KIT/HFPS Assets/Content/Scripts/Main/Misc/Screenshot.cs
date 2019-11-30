using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Screenshot : MonoBehaviour
{
    const string path = "Assets/Screenshots/";
    public KeyCode screesnhotKey;

    private bool isTaken;
    private int count;

    void Update()
    {
        if (Input.GetKeyDown(screesnhotKey) && !isTaken)
        {
            TakeScreenshot();
            isTaken = true;
        }
        else if (isTaken)
        {
            isTaken = false;
        }
    }

    void TakeScreenshot()
    {
        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }

        string name = path + "Screenshot_" + count + ".png";
        ScreenCapture.CaptureScreenshot(name);
        Debug.Log("Captured: " + name);
        count++;
    }
}
