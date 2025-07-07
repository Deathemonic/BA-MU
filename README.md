# Blue Archive - Mod Updater

A tool to re-dump AssetBundle for Blue Archive.

> [!WARNING]
> This is a `Work in Progress/Proof of Concept` 


## How this works?
This will export the dumps from your modded AssetBundle then import it to the original AssetBundle

*Technically this should work on some Unity Games, but it's not tested. The only game I tested is MiSide*


```shell
>> bamu --modded your_modded.bundle --patch game_asset.bundle --only "mesh"
```

This will load `your_modded.bundle` and `game_asset.bundle` then it will do a match if the modded asset `m_Name` and `PathID` matches
with the patch asset then it will export it as a `.json` using that JSON it will import it back to patch AssetBundle then save it on another folder.

To make it work, your modded AssetBundle should have the same name asset as the patch AssetBundle.


## TODO

- [ ] Handle Texture2D
- [ ] Handle Text Asset / Spine2D