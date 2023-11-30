# RMMBYHost
A MelonLoader mod to enable the use of the RMMBY Mod Manager in game.

This repository targets the game Idol Showdown. 

## Adding RMMBYHost To Other Games
RMMBY is designed to be as simple as possible to add to new games. To add a game to the RMMBY Mod Manager, make a pull request for the 3 files in RMMBYInstallers, or PM Makarew.

To add RMMBYHost to new games, start by replacing "GameName" in "Plugin.cs" with the new game's name.
Delete "ModButtonSetup.cs". This is specific to Idol Showdown.
In "Plugin.cs", delete everything below "Idol Showdown Specific Code". 
Change The "Assembly-CSharp" reference to your game's "Assembly-CSharp".
Finally, add a way to call "CallModManager()" from in game.
