using UnityEngine;
using UnityEditor;

namespace Papae
{
    public class EditorUtilities
    {
        public static string fileName = "Editor Screenshot ";
        public static int startNumber = 1;
        [MenuItem("Snapshot/Take Screenshot of Game View %^s")]
        static void TakeScreenshot()
        {
            int number = startNumber;
            string name = "" + number;

            while (System.IO.File.Exists(fileName + name + ".png"))
            {
                number++;
                name = "" + number;
            }

            startNumber = number + 1;

            Application.CaptureScreenshot(fileName + name + ".png");
        }
    }
}
