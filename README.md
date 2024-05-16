What does this do?
- Show collision
- Show collision object names

Todo:
- Homeward bone
- Using microplastics from your inventory

This is just me screwing around with the game, there is no real goal with this project.

Usage:

The process is a little involved at the moment, sorry.

If you want to use the mod, you will need to:

- Open the C# solution.
- Add references to all dlls under AnotherCrabsTreasure_Data/Managed.
- After building the new dll, copy it over to the AnotherCrabsTreasure_Data/Managed folder.
- Then crack open Assembly-CSharp.dll with dnspy (https://github.com/dnSpy/dnSpy).
- Find the Player class, and at the end of Start() (Edit with right click -> Edit Method C#), add
```new GameObject().AddComponent<Mod>();```
You will need to add a reference to your newly created dll for this to compile! Do this by clicking the small folder icon in the bottom left, after right clicking and editing the C# method.
- Go through File -> Save Module and save over Assembly-CSharp.dll
- Lastly, copy the asset bundles from the "PutThisUnderStreamingAssets" folder to AnotherCrabsTreasure_Data/SteamingAssets. Not the folder itself, just the contents. This contains the shaders for overlaying and rendering the collision.

That should be it! Toggle collision overlays with PgDn. It does break some UI effects, as I had to steal a layer to use for the collision camera.
