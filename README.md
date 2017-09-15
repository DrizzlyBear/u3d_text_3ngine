# u3d_text_3ngine
Custom text engine package for per-char coloring and perf improvements.

### Asset Creation
`/Assets/u3d_text_3ngine/` is the folder for creating the asset. Instructions on asset creation TBD.

### Setup
We are using unity `Unity 5.5.0f3` for this asset, so install that if you don't heff it already.
Start unity and open an existing project. Select the git repo's root directory and it should open the project.

You will need to import the `TextMesh Pro` asset yourself to get this working. To import `TextMesh Pro`:
1. Go to the `Window` menu at the top, then `Asset Store` (or press `Ctrl+9`)
2. You should in theory be able to find it by searching for `TextMesh Pro`, but I had some problems. If you do follow below, otherwise skip to step 5
3. on the right, click `Unity Essentials` (near bottom) then `beta content`
4. Scroll down a bit and you will see `TextMesh Pro`
5. On the `TextMesh Pro` page click `download`, or `import`, or `download` then `import`... I forget...
6. Import everything to the default location. It should make the folder `/Assets/TextMesh Pro/`

> some history, this plugin is old as heck and very well tested, unity has purchased it and they intend to add it into their base product. From this perspective it is a Beta addition to the unity product. Again it is an older product, and does not appear to be a buggy beta thing.

### Running
After the import is complete, to run it:

1. Open up `/Asssets/testing/scenes/stress_test_1.unity` in the asset browser in the bottom

2. Click the play button at the top

   \- OR \-

   Click `file`, then `Build Settings`, select your settings and click `build and run`.

> *** Note: don't save the exe in the git area (default) or git will probably pick up parts of it. Please don't check-in junk...
