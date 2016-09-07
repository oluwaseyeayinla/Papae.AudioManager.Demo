using UnityEngine;
using UnityEditor;

public class Snapshot
{
    public static string fileName = "Screenshot - ";
    public static int startNumber = 1;
    [MenuItem("Tools/Take Screenshot of Game View %^s")]
    static void TakeScreenshot()
    {
        int number = startNumber;
        string name = "" + number;

        while (System.IO.File.Exists(fileName + name + ".png"))
        {
            number++;
            name = number.ToString("00");
        }

        startNumber = number + 1;

        Application.CaptureScreenshot(fileName + name + ".png");
    }
}

