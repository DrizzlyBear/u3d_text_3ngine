// <license file="u3d_text_3ngine.cs" repository="https://github.com/DrizzlyBear/u3d_text_3ngine">
//     MIT License https://github.com/DrizzlyBear/u3d_text_3ngine/blob/master/LICENSE
// </license>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using Random = System.Random;
using Stopwatch = System.Diagnostics.Stopwatch;

/// <summary>
/// Class used to create a large block of high-performance text
/// </summary>
[AddComponentMenu("UI/u3d_text_3ngine")]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class u3d_text_3ngine : MonoBehaviour
{
    #region Static / Constant

    /// <summary>
    /// The maximum width we can use without getting errors in Unity about the mesh renderer
    /// </summary>
    /// <remarks>1010101010 - 1 ... not sure what this number is but any higher causes exceptions....</remarks>
    private const int maxWidth = 681;

    /// <summary>
    /// Maximum number of lines to cache in-case they re-appear
    /// </summary>
    /// <remarks>Can adjust this to cache animation lines down the road...</remarks>
    private const int maxCachedLines = 10;

    /// <summary>
    /// Maximum amount of time a line can be corrupted before re-randomizing the corruption
    /// </summary>
    private const int maxCorruptionDurationMs = 100;

    /// <summary>
    /// Maximum CPU time to use in percent at 60FPS
    /// </summary>
    private const int maxCpuTimePercent = 10;

    /// <summary>
    /// Color lookup used for standard hackmud color letters
    /// </summary>
    public static readonly Dictionary<char, Color32> HackmudColors = new Dictionary<char, Color32>() {
        { 'A', HexToColor("FFFFFF") }, { 'B', HexToColor("CACACA") }, { 'C', HexToColor("9B9B9B") },
        { 'D', HexToColor("FF0000") }, { 'E', HexToColor("FF8383") }, { 'F', HexToColor("FF8000") },
        { 'G', HexToColor("F3AA6F") }, { 'H', HexToColor("FBC803") }, { 'I', HexToColor("FFD863") },
        { 'J', HexToColor("FFF404") }, { 'K', HexToColor("F3F998") }, { 'L', HexToColor("1EFF00") },
        { 'M', HexToColor("B3FF9B") }, { 'N', HexToColor("00FFFF") }, { 'O', HexToColor("8FE6FF") },
        { 'P', HexToColor("0070DD") }, { 'Q', HexToColor("A4E3FF") }, { 'R', HexToColor("0000FF") },
        { 'S', HexToColor("7AB2F4") }, { 'T', HexToColor("B035EE") }, { 'U', HexToColor("E6C4FF") },
        { 'V', HexToColor("FF00EC") }, { 'W', HexToColor("FF96E0") }, { 'X', HexToColor("FF0070") },
        { 'Y', HexToColor("FF6A98") }, { 'Z', HexToColor("0C112B") }, { 'a', HexToColor("000000") },
        { 'b', HexToColor("3F3F3F") }, { 'c', HexToColor("676767") }, { 'd', HexToColor("7D0000") },
        { 'e', HexToColor("8E3434") }, { 'f', HexToColor("A34F00") }, { 'g', HexToColor("725437") },
        { 'h', HexToColor("A88600") }, { 'i', HexToColor("B2934A") }, { 'j', HexToColor("939500") },
        { 'k', HexToColor("495225") }, { 'l', HexToColor("299400") }, { 'm', HexToColor("23381B") },
        { 'n', HexToColor("00535B") }, { 'o', HexToColor("324A4C") }, { 'p', HexToColor("0073A6") },
        { 'q', HexToColor("385A6C") }, { 'r', HexToColor("010067") }, { 's', HexToColor("507AA1") },
        { 't', HexToColor("601C81") }, { 'u', HexToColor("43314C") }, { 'v', HexToColor("8C0069") },
        { 'w', HexToColor("973984") }, { 'x', HexToColor("880024") }, { 'y', HexToColor("762E4A") },
        { 'z', HexToColor("101215") }, { '0', HexToColor("9B9B9B") }, { '1', HexToColor("FFFFFF") },
        { '2', HexToColor("1EFF00") }, { '3', HexToColor("0070DD") }, { '4', HexToColor("B035EE") },
        { '5', HexToColor("FF8000") }, { '6', HexToColor("FF8000") }, { '7', HexToColor("FF8000") },
        { '8', HexToColor("FF8000") }, { '9', HexToColor("FF8000") },
    };

    /// <summary>
    /// The color to use as default for uncolored text
    /// </summary>
    public static readonly Color32 defaultForegroundColor = HackmudColors['S'];
    public static readonly Color32 defaultBackgroundColor = HexToColor("121015");

    /// <summary>
    /// Hackmud corruption characters
    /// </summary>
    public static readonly char[] corruption_chars = new char[] { (char)162, (char)164, (char)166, (char)167, (char)168,
                                                                  (char)169, (char)170, (char)193, (char)195 };

    /// <summary>
    /// Used for accumulating how many chars were rendered this frame
    /// </summary>
    public static Dictionary<u3d_text_3ngine, int> TotalChars = new Dictionary<u3d_text_3ngine, int>();

    /// <summary>
    /// Helper method to convert hex color strings to Unity Color32 structs
    /// </summary>
    /// <param name="hex">The hex color string to convert</param>
    /// <returns>The Unity Color32 struct that represents the specified color</returns>
    private static Color32 HexToColor(string hex)
    {
        return new Color32(Convert.ToByte(hex.Substring(0, 2), 16),
                           Convert.ToByte(hex.Substring(2, 2), 16),
                           Convert.ToByte(hex.Substring(4, 2), 16),
                           255);
    }

    /// <summary>
    /// Unity's random number generator is bad
    /// </summary>
    private static Random r = new Random();

    #endregion

    #region Constructor / Destructor

    /// <summary>
    /// Use this for initialization, called once per instantiation
    /// </summary>
    void Start()
    {
        // Add our object to the global char count metrics
        TotalChars.Add(this, 0);

        // Ensure to calibrate on first iteration
        RecalibrateSize = true;

        // Create the template TMP object
        template = new GameObject("tmp_template");
        template.transform.parent = transform;
        TextMeshProUGUI templateGui = template.AddComponent<TextMeshProUGUI>();
        templateGui.font = FontAsset;
        templateGui.fontSharedMaterial = FontAsset.material;
        templateGui.fontSize = FontSize;

        // Disable the template so we don't see it
        DisableGuiObject(template);

        // Ensure this is set to something. The processing logic will clean it up better as long as it's not null.
        DisplayText = new string[] { };
    }

    /// <summary>
    /// Called if destroied
    /// </summary>
    void OnDestroy()
    {
        // Remove our object from the global char count metrics
        TotalChars.Remove(this);
    }

    #endregion

    #region Private Members

    /// <summary>
    /// The Template TMP object to copy and create our lines with
    /// </summary>
    private GameObject template;

    /// <summary>
    /// The text that only the successfully created lines represent
    /// </summary>
    private string[] lineText;

    /// <summary>
    /// The TMP objects currently displayed on the screen (may include corruption lines)
    /// </summary>
    private GameObject[] lines = new GameObject[] { };

    /// <summary>
    /// The TMP objects not currently being displayed on the screen (other than corruption lines)
    /// </summary>
    /// <remarks>Ordered by age, with first being oldest, and last being most recent</remarks>
    private List<GameObject> unusedLinesAge = new List<GameObject>();

    /// <summary>
    /// The TMP objects not currently being displayed on the screen (other than corruption lines)
    /// </summary>
    /// <remarks>Stored by original string they represent</remarks>
    private Dictionary<string, List<GameObject>> unusedLinesLookup = new Dictionary<string, List<GameObject>>();

    /// <summary>
    /// The corruption lines that are not currently on the screen
    /// </summary>
    private List<GameObject> unusedCorruptedLines = new List<GameObject>();

    /// <summary>
    /// The vertical line offsets for each line
    /// </summary>
    private float[] lineOffsets = new float[] { };

    /// <summary>
    /// Timestamp when corruption was first added, or last time corruption duration hit max
    /// </summary>
    private DateTime lastCorruptionChange = DateTime.Now;

    /// <summary>
    /// Used to limit the amount of CPU usage per frame
    /// </summary>
    private Stopwatch stopwatch = new Stopwatch();

    /// <summary>
    /// Current width of our draw area
    /// </summary>
    private float curWidth = 0;

    /// <summary>
    /// Current height of our draw area
    /// </summary>
    private float curHeight = 0;

    #endregion

    #region Public Members

    #region Unity Editor Visible

    /// <summary>
    /// The font asset to user in this object
    /// </summary>
    public TMP_FontAsset FontAsset;

    // TODO: allow this to be changed runtime, update template, then do normal recalibrate
    /// <summary>
    /// The font size to use for this object
    /// </summary>
    public float FontSize;

    #endregion

    /// <summary>
    /// Gets or sets the text that we are currently trying to display
    /// </summary>
    public string[] DisplayText { get; set; }

    /// <summary>
    /// Gets the width of the object in chars
    /// </summary>
    public int WidthChars { get; private set; }

    /// <summary>
    /// Gets the height of the object in chars
    /// </summary>
    public int HeightChars { get; private set; }

    /// <summary>
    /// Gets or sets a value which indicates that a resize has happened and we need to reinitialize our objects
    /// </summary>
    public bool RecalibrateSize { get; set; }

    /// <summary>
    /// Gets a value indicating the current number of lines which were deferred due to lack of CPU cycles
    /// </summary>
    public int CorruptedLines { get; private set; }

    #endregion

    #region Public Methods

    #region Unity Methods

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    public void Update()
    {
        // Check if the size has changed, and recalibrate the game objects if it has
        CheckRecalibrate();

        // Figure out which lines are correct, put any incorrect uncorrupted lines into unusedLines
        ClearIncorrectLines();

        // If corruption has been displayed too long, move all corruption lines to unused
        CheckCorruptionTime();

        // Of the remaining incorrect lines, find any we have cached and put them in the right place
        GetCachedLines();

        // Of the remaining incorrect lines, get the oldest cached line
        // and update a random line, until we've reached our CPU quota
        UpdateIncorrectLines();

        // If/when time runs out, fill the rest of the incorrect, non-corruption lines with a random unused corruption line
        FillCorruptionLines();

        // Disable unused objects
        DisableUnusedObjects();
    }

    #endregion

    #endregion

    #region Private Methods

    /// <summary>
    /// Check if the size has changed, and recalibrate the game objects if it has
    /// </summary>
    private void CheckRecalibrate()
    {
        // Get the rect we're drawing in, and fit the template to it for measuring
        RectTransform objectRect = gameObject.GetComponent<RectTransform>();

        // If the size has changed, then recalibrate
        if ((objectRect.rect.width != curWidth) || (objectRect.rect.height != curHeight))
        {
            RecalibrateSize = true;
        }

        // Return if recalibration is not requested
        if (!RecalibrateSize)
        {
            return;
        }

        // Ensure the template is enabled
        EnableGuiObject(template);

        // Fit the template to our rect for measuring
        RectTransform templateRect = template.GetComponent<RectTransform>();
        templateRect.localPosition = new Vector3(0, 0);
        templateRect.sizeDelta = new Vector2(objectRect.rect.width, objectRect.rect.height);

        TextMeshProUGUI templateGui = template.GetComponent<TextMeshProUGUI>();
        templateGui.SetText("AB\nC");
        templateGui.ForceMeshUpdate();

        // Measure the char width and height
        float charWidth = templateGui.textInfo.characterInfo[1].topLeft.x - templateGui.textInfo.characterInfo[0].topLeft.x;
        float charHeight = templateGui.textInfo.characterInfo[0].topLeft.y - templateGui.textInfo.characterInfo[3].topLeft.y;

        // Get the width and height our area in chars
        int newWidthChars = Math.Max(1, (int)Mathf.Floor(objectRect.rect.width / charWidth));
        int newHeightChars = Math.Max(1, (int)Mathf.Floor(objectRect.rect.height / charHeight));

        // Save the width and height of our screen area so we don't recalibrate again
        curWidth = objectRect.rect.width;
        curHeight = objectRect.rect.height;

        // If it's already correct, then don't process anything
        if ((WidthChars == newWidthChars) && (HeightChars == newHeightChars))
        {
            // Disable the template because we no longer need it
            DisableGuiObject(template);

            // Recalibration complete, don't do it again next frame
            RecalibrateSize = false;

            return;
        }

        // HeightChars will only be 0 on first calibration
        if (HeightChars > 0)
        {
            // Move all lines out of our on-screen collections
            unusedLinesAge.AddRange(Enumerable.Range(0, HeightChars)
                .Where(cur => lineText[cur] != null).Select(cur => lines[cur]));

            unusedCorruptedLines.AddRange(Enumerable.Range(0, HeightChars)
                .Where(cur => lineText[cur] == null).Select(cur => lines[cur]));
        }

        // Save the new values
        WidthChars = newWidthChars;
        HeightChars = newHeightChars;

        // Save the total size for debug/analysis purposes
        TotalChars[this] = WidthChars * HeightChars;

        // Any larger than this width appears to throw exceptions, so don't let it happen.
        if (WidthChars > maxWidth)
        {
            WidthChars = maxWidth;
        }

        // Save the y offset so we can move corruption lines and cached lines around easier
        lineOffsets = Enumerable.Range(0, HeightChars)
            .Select(cur => objectRect.rect.height - (charHeight * cur + objectRect.rect.height / 2)).ToArray();

        // Clear the lines and line texts, and set to new size.
        lines = new GameObject[HeightChars];
        lineText = new string[HeightChars];

        // Remove any lines we no longer need
        if (unusedLinesAge.Count > (HeightChars + maxCachedLines))
        {
            unusedLinesAge.Skip(HeightChars + maxCachedLines).ToList().ForEach(cur =>
            {
                DisableGuiObject(cur);
                Destroy(cur);
            });

            unusedLinesAge = unusedLinesAge.Take(HeightChars + maxCachedLines).ToList();
        }

        if (unusedCorruptedLines.Count > HeightChars)
        {
            unusedCorruptedLines.Skip(HeightChars).ToList().ForEach(cur =>
            {
                DisableGuiObject(cur);
                Destroy(cur);
            });

            unusedCorruptedLines = unusedCorruptedLines.Take(HeightChars).ToList();
        }

        // Initialize new lines if we need them
        while (unusedLinesAge.Count < (HeightChars + maxCachedLines))
        {
            // Create objects for the main on-screen lines
            GameObject newLine = Instantiate(template, transform);
            newLine.name = "tmp_pool_line";
            unusedLinesAge.Add(newLine);
        }

        while (unusedCorruptedLines.Count < HeightChars)
        {
            // Create objects for the corrupted lines
            GameObject newLine = Instantiate(template, transform);
            newLine.name = "tmp_corruption_line";
            unusedCorruptedLines.Add(newLine);
        }

        // Recreate meshes and update position for all lines (because scaling can ruin them)
        unusedLinesAge.ForEach(cur =>
        {
            RectTransform curRect = cur.GetComponent<RectTransform>();
            curRect.sizeDelta = new Vector2(objectRect.rect.width, 0);
            curRect.localPosition = new Vector3(0, 0);

            UpdateLine(cur, "");
        });

        unusedCorruptedLines.ForEach(cur =>
        {
            RectTransform curRect = cur.GetComponent<RectTransform>();
            curRect.sizeDelta = new Vector2(objectRect.rect.width, 0);
            curRect.localPosition = new Vector3(0, 0);

            // Generate corruption text and create the mesh and vertex colors for it.
            string corrText = string.Join("", Enumerable.Range(0, WidthChars)
                .Select(curCharNum => r.NextDouble() > 0.4 ?
                        string.Format("<c{0}{1}{2}>", HackmudColors.ElementAt(r.Next(HackmudColors.Count)).Key,
                                      HackmudColors.ElementAt(r.Next(HackmudColors.Count)).Key,
                                      corruption_chars[r.Next(corruption_chars.Length)]) :
                        " ").ToArray());

            UpdateLine(cur, corrText);
        });

        // Save the lines in our lookup
        unusedLinesLookup = new Dictionary<string, List<GameObject>> { { "", new List<GameObject>(unusedLinesAge) } };

        // Disable the template because we no longer need it
        DisableGuiObject(template);

        // Recalibration complete, don't do it again next frame
        RecalibrateSize = false;
    }

    /// <summary>
    /// Figure out which lines are correct, put any incorrect uncorrupted lines into unusedLines
    /// </summary>
    private void ClearIncorrectLines()
    {
        // Ensure the length of the DisplayText array is correct
        if (DisplayText.Length > HeightChars)
        {
            DisplayText = DisplayText.Take(HeightChars).ToArray();
        }

        if (DisplayText.Length < HeightChars)
        {
            DisplayText = DisplayText.Concat(Enumerable.Range(0, HeightChars - DisplayText.Length).Select(cur => "")).ToArray();
        }

        // Loop through all lines to check if they are correct
        for (int i = 0; i < HeightChars; ++i)
        {
            if ((lineText[i] != null) && (DisplayText[i] != lineText[i]))
            {
                // Line does not match the requested value, so throw it in the cache (maybe it scrolled?)
                CacheUnusedLine(lineText[i], lines[i]);
                lineText[i] = null;
                lines[i] = null;
            }
        }
    }

    /// <summary>
    /// If corruption has been displayed too long, move all corruption lines to unused
    /// </summary>
    private void CheckCorruptionTime()
    {
        // Get the currently displayed corrupted lines.
        int[] corruptedLines = Enumerable.Range(0, HeightChars)
            .Where(cur => lineText[cur] == null && lines[cur] != null).ToArray();

        // No corrupted lines, then do nothing
        if (corruptedLines.Length == 0)
        {
            return;
        }

        // If the corruption has expired, then remove it,
        // and later new random corruption will be made (gives it kindof a glitchy feel)
        if ((DateTime.Now - lastCorruptionChange).TotalMilliseconds > maxCorruptionDurationMs)
        {
            foreach (int curIndex in corruptedLines)
            {
                unusedCorruptedLines.Add(lines[curIndex]);
                lines[curIndex] = null;
            }
        }
    }

    /// <summary>
    /// Of the remaining incorrect lines, find any we have cached and put them in the right place
    /// </summary>
    private void GetCachedLines()
    {
        // Get the remaining empty lines
        int[] incorrectLines = Enumerable.Range(0, HeightChars).Where(cur => lineText[cur] == null).ToArray();

        foreach (int curIndex in incorrectLines)
        {
            // Look for the line in our cache, incase it scrolled, or is commonly reused.
            List<GameObject> cachedLineList = new List<GameObject>();
            if (unusedLinesLookup.TryGetValue(DisplayText[curIndex], out cachedLineList))
            {
                if (lines[curIndex] != null)
                {
                    // Currently a corrupted line, so store it
                    unusedCorruptedLines.Add(lines[curIndex]);
                    lines[curIndex] = null;
                }

                // The last one should be the most recently used
                GameObject lineToUse = cachedLineList.Last();

                // Move the line into position
                RectTransform lineToUseRect = lineToUse.GetComponent<RectTransform>();
                lineToUseRect.localPosition = new Vector3(0, lineOffsets[curIndex]);

                // save the line in our on-screen collections
                lineText[curIndex] = DisplayText[curIndex];
                lines[curIndex] = cachedLineList.Last();

                // Enable the line in unity
                EnableGuiObject(lineToUse);

                // Removed the line from our off-screen collections
                if (cachedLineList.Count > 1)
                {
                    cachedLineList.Remove(lineToUse);
                }
                else
                {
                    unusedLinesLookup.Remove(DisplayText[curIndex]);
                }

                unusedLinesAge.Remove(lineToUse);
            }
        }
    }

    /// <summary>
    /// Of the remaining incorrect lines, get the oldest cached line and update a random line, until we've reached our CPU quota
    /// </summary>
    private void UpdateIncorrectLines()
    {
        // Get the remaining incorrect lines and shuffle them up,
        // so that it's a bit random how corruption appears if we run out of time.
        int[] incorrectLines = Enumerable.Range(0, HeightChars).Where(cur => lineText[cur] == null).ToArray();
        Shuffle(incorrectLines);

        int newLinesCreated = 0;

        // Start timing, because this is the CPU intensive stuff right here
        stopwatch.Reset();
        stopwatch.Start();

        foreach (int curIndex in incorrectLines)
        {
            // Get the first in the cache, which should be the oldest used line, most stale, so we can repurpose
            GameObject lineToUse = unusedLinesAge[newLinesCreated++];

            // Move the line into position
            RectTransform lineToUseRect = lineToUse.GetComponent<RectTransform>();
            lineToUseRect.localPosition = new Vector3(0, lineOffsets[curIndex]);

            if (lines[curIndex] != null)
            {
                // There is a corruption line here we need to store
                unusedCorruptedLines.Add(lines[curIndex]);
                lines[curIndex] = null;
            }

            // Save the line in our on-screen collection
            lines[curIndex] = lineToUse;
            lineText[curIndex] = DisplayText[curIndex];

            // Enable the line in unity
            EnableGuiObject(lineToUse);

            // Update the Unity mesh and vertex colors
            UpdateLine(lineToUse, DisplayText[curIndex]);

            // If we occupy more than whatever percent of a 60th of a second, then break out
            if (stopwatch.ElapsedMilliseconds > (1000.0 / 60.0 * (maxCpuTimePercent / 100.0)))
            {
                break;
            }
        }

        // Dont leave the stopwatch running
        stopwatch.Reset();

        // Remove the now used lines from our unused line collections
        HashSet <GameObject> nowUsedLines = new HashSet<GameObject>(unusedLinesAge.Take(newLinesCreated));
        unusedLinesAge = unusedLinesAge.Skip(newLinesCreated).ToList();
        unusedLinesLookup = unusedLinesLookup
            .Select(cur => new KeyValuePair<string, List<GameObject>>(cur.Key, cur.Value
                .Where(curList => !nowUsedLines.Contains(curList)).ToList()))
            .Where(cur => cur.Value.Count > 0)
            .ToDictionary(cur => cur.Key, cur => cur.Value);
    }

    /// <summary>
    /// Updates the Unity mesh and vertex colors for the specified line
    /// </summary>
    /// <param name="lineToUse">The TMP game object to update</param>
    /// <param name="curText">The full content string with color codes</param>
    private void UpdateLine(GameObject lineToUse, string curText)
    {
        // Get the TMP object
        TextMeshProUGUI lineGui = lineToUse.GetComponent<TextMeshProUGUI>();

        // Used to store the display characters and display colors per character
        StringBuilder sb = new StringBuilder();
        Color32[] fgColors = new Color32[maxWidth];
        Color32[] bgColors = new Color32[maxWidth];

        // Used to track the current color for both background and foreground colors
        Stack<Color32> bgColorStack = new Stack<Color32>();
        Stack<Color32> fgColorStack = new Stack<Color32>();
        fgColorStack.Push(defaultForegroundColor);
        bgColorStack.Push(defaultBackgroundColor);

        int tagOpens = 0;
        int tagCloses = 0;
        for (int pass = 0; pass < 2; pass++)
        {
            // Used to track escaped characters in the input string
            bool escaped = false;
            int depth = 0;
            int mismatch = tagOpens - tagCloses;
            for (int x = 0; x < curText.Length; x++)
            {
                bool processChar = false;

                char c = curText[x];
                switch (c)
                {
                    case '<':
                        // If escaped or too close to end of string to form a proper color code, just write it out
                        if (escaped || ((x + 3) >= curText.Length))
                        {
                            processChar = true;
                            break;
                        }

                        char next = curText[x + 1];
                        // If this isn't the open color char code, treat as plain text
                        if (next != 'c')
                        {
                            processChar = true;
                            break;
                        }
                        char fgChar = curText[x + 2];
                        char bgChar = curText[x + 3];

                        // If the color code specified isn't valid, just treat this as plain text
                        if (fgChar != '*' && HackmudColors.ContainsKey(fgChar) == false)
                        {
                            processChar = true;
                            break;
                        }
                        if (bgChar != '*' && HackmudColors.ContainsKey(bgChar) == false)
                        {
                            processChar = true;
                            break;
                        }

                        if (pass > 0 && mismatch > 0)
                        {
                            mismatch--;
                            processChar = true;
                            break;
                        }

                        // We have a valid color code and we parsed it, so jump forward to the next non-consumed character
                        x += 3;

                        depth++;
                        tagOpens++;

                        // If we're doing first pass, don't modify stacks
                        if (pass == 0)
                        {
                            break;
                        }

                        // A wildcard '*' char does not overwrite the last color code, so defer to current coloring on the stack
                        Color32 fgColor = fgChar == '*' ? fgColorStack.Peek() : HackmudColors[fgChar];
                        Color32 bgColor = bgChar == '*' ? bgColorStack.Peek() : HackmudColors[bgChar];

                        fgColorStack.Push(fgColor);
                        bgColorStack.Push(bgColor);

                        break;
                    case '>':
                        // If this is escaped or if we don't have any active tags, treat as plain text
                        if (escaped || depth == 0)
                        {
                            processChar = true;
                            break;
                        }

                        // Color tag ended, decrement depth, increment tagCloses, and pop the most recent bg/fg colors
                        depth--;
                        tagCloses++;

                        // If we're doing first pass, don't modify stacks
                        if (pass == 0)
                        {
                            break;
                        }

                        fgColorStack.Pop();
                        bgColorStack.Pop();
                        break;
                    case '\\':
                        if (escaped == false)
                        {
                            escaped = true;
                            break;
                        }
                        else
                        {
                            processChar = true;
                            break;
                        }
                    default:
                        processChar = true;
                        break;
                }

                // First pass needs to skip the write logic
                if (processChar && pass > 0)
                {
                    // If processChar is true, we are no longer escaped, so just set to false every time
                    escaped = false;
                    sb.Append(c);
                    if (sb.Length < maxWidth)
                    {
                        // Because some characters don't get displayed,
                        // make sure we associate the color arrays with the actual string length
                        fgColors[sb.Length - 1] = fgColorStack.Peek();
                        bgColors[sb.Length - 1] = bgColorStack.Peek();
                    }
                }
            }
        }

        string newVal = sb.ToString();

        // Ensure we're not over the max length
        if (newVal.Length > WidthChars)
        {
            newVal = newVal.Substring(0, WidthChars);
        }

        // Update the line text and regenerate the mesh
        lineGui.SetText(newVal);
        lineGui.ForceMeshUpdate();

        // Get the unity components that we need to update
        CanvasRenderer renderer = lineToUse.GetComponent<CanvasRenderer>();
        Mesh mesh = lineGui.textInfo.meshInfo[0].mesh;

        // Create a color array representing the correct number of verticies
        // This needs to be done after ForceMeshUpdate, which is why we don't create this array correctly the first time
        // IMPORTANT!!! mesh.vertices is O(n), NOT O(1)!!!!!
        int length = mesh.vertices.Length;
        Color32[] finalColors = new Color32[length];

        // Only process visible characters, as non-visible characters don't have verticies
        foreach (TMP_CharacterInfo curCharInfo in lineGui.textInfo.characterInfo.Where(cur => cur.isVisible))
        {
            // Get the color and the start vertex
            Color32 charMeshColor = fgColors[curCharInfo.index];
            int startIndex = curCharInfo.vertexIndex;

            // Update the vertex colors
            finalColors[startIndex + 0] = charMeshColor;
            finalColors[startIndex + 1] = charMeshColor;
            finalColors[startIndex + 2] = charMeshColor;
            finalColors[startIndex + 3] = charMeshColor;
        }

        // Update the colors on the final object
        mesh.colors32 = finalColors;
        renderer.SetMesh(mesh);
    }

    /// <summary>
    /// Sets the specified color on the specified range in the specified color array
    /// </summary>
    /// <param name="colors">The array to set colors within</param>
    /// <param name="color">The color to set in the array</param>
    /// <param name="startIndex">The start index in the array to set the color</param>
    /// <param name="length">The length of elements in the array to set the color</param>
    private void SetColors(Color32[] colors, char color, int startIndex, int length)
    {
        Color32 meshColor = HackmudColors[color];

        for (int i = 0; i < length; ++i)
        {
            colors[i + startIndex] = meshColor;
        }
    }

    /// <summary>
    /// If/when time runs out, fill the rest of the incorrect, non-corruption lines with a random unused corruption line
    /// </summary>
    private void FillCorruptionLines()
    {
        // Get the remaining incorrect lines
        int[] incorrectLines = Enumerable.Range(0, HeightChars).Where(cur => lines[cur] == null).ToArray();

        if ((CorruptedLines > 0) && 
            (Enumerable.Range(0, HeightChars).Where(cur => lines[cur] != null && lineText[cur] == null).Count() == 0))
        {
            // Either first corruption is appearing, or corruption max time was hit
            lastCorruptionChange = DateTime.Now;
        }

        // Loop through the lines and fill them all with the already initalized corruption lines.
        foreach (int curIndex in incorrectLines)
        {
            // Take a line from the unused collection
            int corrIndex = r.Next(unusedCorruptedLines.Count);
            GameObject lineToUse = unusedCorruptedLines[corrIndex];
            unusedCorruptedLines.RemoveAt(corrIndex);

            // Move the line into position
            RectTransform lineToUseRect = lineToUse.GetComponent<RectTransform>();
            lineToUseRect.localPosition = new Vector3(0, lineOffsets[curIndex]);

            // Add it to the used collection, lineText of null means it's a corruption line, not a "real" line.
            lines[curIndex] = lineToUse;
            lineText[curIndex] = null;

            // Ensure this object is enabled
            EnableGuiObject(lineToUse);
        }
    }

    /// <summary>
    /// Disables all lines that we know aren't on screen
    /// </summary>
    private void DisableUnusedObjects()
    {
        // Save the total number of corrupted lines for Debug/Analysis
        CorruptedLines = HeightChars - unusedCorruptedLines.Count;

        unusedLinesAge.ForEach(cur => DisableGuiObject(cur));
        unusedCorruptedLines.ForEach(cur => DisableGuiObject(cur));
    }

    /// <summary>
    /// Puts the specified line into the unused line cache
    /// </summary>
    /// <param name="text">The original text for the line</param>
    /// <param name="line">The line object</param>
    private void CacheUnusedLine(string text, GameObject line)
    {
        unusedLinesAge.Add(line);

        List<GameObject> cachedLineList = new List<GameObject>();
        if (!unusedLinesLookup.TryGetValue(text, out cachedLineList))
        {
            cachedLineList = new List<GameObject>();
            unusedLinesLookup[text] = cachedLineList;
        }

        cachedLineList.Add(line);
    }

    /// <summary>
    /// Disabled a single line
    /// </summary>
    /// <param name="curObj">The line to disable</param>
    private void DisableGuiObject(GameObject curObj)
    {
        curObj.GetComponent<CanvasRenderer>().cull = true;
    }

    /// <summary>
    /// Enables a single line
    /// </summary>
    /// <param name="curObj">The line to enable</param>
    private void EnableGuiObject(GameObject curObj)
    {
        curObj.GetComponent<CanvasRenderer>().cull = false;
    }

    /// <summary>
    /// Shuffles an array (for random corruption appearance)
    /// </summary>
    /// <typeparam name="T">The type of the array to shuffle</typeparam>
    /// <param name="curArray">The array to shuffle</param>
    private void Shuffle<T>(T[] curArray)
    {
        for (int i = 0; i < curArray.Length; i++)
        {
            int idx = r.Next(curArray.Length);

            T curVar = curArray[i];
            curArray[i] = curArray[idx];
            curArray[idx] = curVar;
        }
    }

    #endregion
}
