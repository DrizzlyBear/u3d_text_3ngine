// <license file="scroll_test.cs" repository="https://github.com/DrizzlyBear/u3d_text_3ngine">
//     MIT License https://github.com/DrizzlyBear/u3d_text_3ngine/blob/master/LICENSE
// </license>

using System.Linq;
using UnityEngine;
using Random = System.Random;
using Stopwatch = System.Diagnostics.Stopwatch;

public class scroll_test : MonoBehaviour
{
    enum TestMode
    {
        HoldTop,
        DownSwing,
        HoldBottom,
        UpSwing,
    }

    private const double percentFull = 1.0;

    private static Random r = new Random();
    public static double linesPerSecond = 1;
    public static int height = 0;

    private u3d_text_3ngine engine;
    private Stopwatch stopwatch;
    private double accumulator = 0;
    private TestMode mode = TestMode.UpSwing;

    private long msSinceChange = 0;
    private int lastCorrCount = 0;

    // Use this for initialization
    void Start()
    {
        engine = GetComponent<u3d_text_3ngine>();
        stopwatch = new Stopwatch();
        stopwatch.Reset();
    }

    // Update is called once per frame
    void Update()
    {
        long elapsed = stopwatch.ElapsedMilliseconds;
        stopwatch.Reset();
        stopwatch.Start();

        accumulator += elapsed;

        if (accumulator > 1000)
        {
            accumulator = 1000;
        }

        TestMode orgMode = mode;

        switch (mode)
        {
            case TestMode.UpSwing:
                {
                    ++linesPerSecond;

                    if (engine.CorruptedLines < (engine.HeightChars * 0.4))
                    {
                        mode = TestMode.HoldTop;
                    }
                }
                break;
            case TestMode.HoldTop:
                {
                    if (engine.CorruptedLines < (engine.HeightChars * 0.2))
                    {
                        mode = TestMode.UpSwing;
                    }

                    if (msSinceChange >= 2000)
                    {
                        mode = TestMode.DownSwing;
                    }
                }
                break;
            case TestMode.DownSwing:
                {
                    --linesPerSecond;

                    if ((engine.CorruptedLines == 0) || (engine.CorruptedLines < lastCorrCount))
                    {
                        mode = TestMode.HoldBottom;
                    }
                }
                break;
            case TestMode.HoldBottom:
                {
                    if ((engine.CorruptedLines > (engine.HeightChars * 0.1)) && (engine.CorruptedLines >= lastCorrCount))
                    {
                        mode = TestMode.DownSwing;
                    }

                    if (msSinceChange >= 5000)
                    {
                        mode = TestMode.UpSwing;
                    }
                }
                break;
        }

        if (linesPerSecond < 1)
        {
            linesPerSecond = 1;
        }

        if (linesPerSecond > (engine.HeightChars * 60))
        {
            linesPerSecond = (engine.HeightChars / 60) + 1;
        }

        lastCorrCount = engine.CorruptedLines;

        if (orgMode != mode)
        {
            msSinceChange = 0;
        }
        else
        {
            msSinceChange += elapsed;
        }

        double msPerLine = 1000.0 / linesPerSecond;

        while (accumulator > msPerLine)
        {
            char fgColor = u3d_text_3ngine.HackmudColors.ElementAt(r.Next(u3d_text_3ngine.HackmudColors.Count)).Key;
            char bgColor = u3d_text_3ngine.HackmudColors.ElementAt(r.Next(u3d_text_3ngine.HackmudColors.Count)).Key;

            double rand = r.NextDouble();
            string newLine = "";
            if(rand < 0.65)
            {
                newLine = string.Join("", Enumerable.Range(0, engine.WidthChars)
                    .Select(cur => r.NextDouble() <= percentFull ? string.Format("<c{0}{1}{2}>", 
                    u3d_text_3ngine.HackmudColors.ElementAt(r.Next(u3d_text_3ngine.HackmudColors.Count)).Key, 
                    u3d_text_3ngine.HackmudColors.ElementAt(r.Next(u3d_text_3ngine.HackmudColors.Count)).Key, 
                    (char)(r.Next(26) + 'a')) : " ")
                    .ToArray());
            }
            else if (rand < 0.75)
            {
                newLine = string.Format("<c{0}{1}this is a colored string>", fgColor, bgColor);
            }
            else if (rand < 0.85)
            {
                char fgColor2 = u3d_text_3ngine.HackmudColors.ElementAt(r.Next(u3d_text_3ngine.HackmudColors.Count)).Key;
                char bgColor2 = u3d_text_3ngine.HackmudColors.ElementAt(r.Next(u3d_text_3ngine.HackmudColors.Count)).Key;

                newLine = string.Format("<c{0}{1}this is a <c{2}{3}broken nested color string>", fgColor, bgColor,
                                                                                                 fgColor2, bgColor2);
            }
            else if (rand < 0.95)
            {
                char fgColor2 = u3d_text_3ngine.HackmudColors.ElementAt(r.Next(u3d_text_3ngine.HackmudColors.Count)).Key;
                char bgColor2 = u3d_text_3ngine.HackmudColors.ElementAt(r.Next(u3d_text_3ngine.HackmudColors.Count)).Key;

                newLine = string.Format("<c{0}{1}this is a <c{2}{3}doubly <c{4}{5}broken nested color string>",
                                        fgColor, bgColor, fgColor2, bgColor2, fgColor, fgColor2);
            }
            else
            {
                char fgColor2 = u3d_text_3ngine.HackmudColors.ElementAt(r.Next(u3d_text_3ngine.HackmudColors.Count)).Key;
                char bgColor2 = u3d_text_3ngine.HackmudColors.ElementAt(r.Next(u3d_text_3ngine.HackmudColors.Count)).Key;

                newLine = string.Format("<c{0}{1}this is a <c{2}{3}nested> colored string>", fgColor, bgColor,
                                                                                             fgColor2, bgColor2);
            }
            

            engine.DisplayText = engine.DisplayText.Skip(1).Concat(new string[] { newLine }).ToArray();

            accumulator -= msPerLine;
        }

        height = engine.HeightChars;
    }
}
