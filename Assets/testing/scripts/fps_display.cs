// This scripts is stolen and modified from here: http://wiki.unity3d.com/index.php?title=FramesPerSecond

using System.Linq;
using UnityEngine;

public class fps_display : MonoBehaviour
{
    float deltaTime = 0.0f;

    void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 0, w, h * 2 / 25);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 25;
        style.normal.textColor = new Color(0.0f, 0.0f, 1.0f, 1.0f);
        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        string text = string.Format("{0:0.0} ms ({1:0.} fps) - Chars: {2} - Lines: {3}", msec, fps, u3d_text_3ngine.TotalChars.Sum(cur => cur.Value), scroll_test.linesPerSecond);
        GUI.Label(rect, text, style);
    }
}