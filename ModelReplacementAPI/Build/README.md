# ModelReplacementAPI v2.4.17

## Instructions
For more info on making model replacements see the wiki at https://github.com/BunyaPineTree/LethalCompany_ModelReplacementAPI

## Viewmodel Support
Viewmodels are now supported and can be generated automatically from your character's model, but because not all models have high quality hands, or are mapped to the in game hands well, viewmodels will be implemented in an opt-in basis. 
After custom viewmodel support is finished and guides are made, the SDK will be updated which will allow people to opt-in their models to use custom or automatic viewmodels. 
In the meantime, I have added a config option to ModelReplacementAPI that will automatically generate viewmodels for every model replacement. This can be used as a debug tool to see if your model's arms are mapped well enough or would be better with a custom viewmodel.

### Instructions on Enabling Viewmodels
To enable, launch the game once with v2.4.0 to generate the config file. You can then change the config with a mod manager like r2modman, manually at `\BepInEx\config\meow.ModelReplacementAPI.cfg`, or in game via LethalConfig.
Note that if you enable this config option while in a game, you will have to put on a new model for the viewmodel to generate. 

## Masked Support 
Masked enemies will now take on the model replacements of players. 
If they are mimicking a player then they will take on that player's model, otherwise they will randomly take on the model replacement (or lack of) of any player. 

## Known Issues
- More Company cosmetics behave strangely on ragdolls
- Due to the implementation of post processing removal, some circumstances will prevent a noPostProcessing enabled model from appearing on cameras. An example of this is MirrorDecor, which will not immediately show your model. As of this moment this can be fixed by taking off your model replacement, and putting it back on. 
- At this moment, some cameras do not display the base player model
- Supporting no post process layers has required finding two layers that aren't used freqently, or ever, to render models without post process on. I have performed a thorough search this time to prevent purple rails, but just because nothing was on the layers I searched in doesn't mean nothing ever will be. If you find objects that don't look correct, please let me know.
- Viewmodels rubberband
- Items held in viewmodels may face a different direction, be in a different location, and drop to a place other than directly under you. 
- Dropping dead players with a viewmodel can result in unexpected results

## Changelog
	- v2.4.17
		- Thanks to Wheatly126 for improvements that should fix belt bag
	- V2.4.16
		- Thanks to Wheatly126 for performance improvements.  
	- V2.4.15
		- Huge thanks to Wheatly126 for fixing bugs related to holding items, weapons, jetpacks, as well as resolving the rubber banding of first person viewmodels.
		- Thanks to TheSlowlyy for resolving some errors in the console.
	- v2.4.14
		- Handles some errors logged in console.
	- v2.4.13
		- Thanks to 1A3Dev for fixing the disconnected player bug.
	- v2.4.12
		- Thanks to 1A3Dev for fixing MoreCompany cosmetic visability issues.
	- v2.4.11
		- Rebuilt with latest version of MoreCompany
	- v2.4.10
		- Your honour, I plead oopsie daisies.
	- v2.4.9
		- Fixed ragdoll collision issues. 
		- Fixed the chance of company cruisers inexplicably exploding when dead bodies are placed inside. 
		- Fixed weather effects disappearing when in third person. 
		- Removed some error messages from console.
	- v2.4.7
		- Fixed vanishing trees
	- v2.4.6
		- Thanks to DiFFoZ for finding a bug that reduced performance.
	- v2.4.5
		- LOD issues should be resolved. 
		- Thanks to 1A3 for fixing the masked related error messages in console.
	- v2.4.1
		- Removed Masked nametags
		- Fixed incompatability with other mods that remove the hud
		- Fixed bug that prevented MoreCompany cosmetics from being seen in the first person camera
	- v2.4.0
		- ViewModel support added
		- Masked support added
		- Implemented shadows
	- v2.3.9
		- Resolved Incompatability that appears when LCVR and TooManyEmotes are both present. 
	- v2.3.8
		- Removed MoreCompany Cosmetic errors from console.
		- Removed debug line that occasionally caused masked errors.
		- Fixed character sizing issue when in the middle of an emote. 
		- Fixed rubberbanding(?)
	- v2.3.7
		- Invisible scav dead bodies made visible. Huge thanks to SylviBlossom for locating the problem and providing the solution. 
	- v2.3.6
		- Players should be visible further than a meter. 
	- v2.3.5
		- LODS no longer show in third person
		- First person arms no longer appear with TooManyEmotes emotes
	- v2.3.4
		- Fixed no post processing for models that set it via script instead of SDK
		- Fixed TooManyEmotes camera smearing
	- v2.3.3
		- Can actually see players now
	- v2.3.2
		- Fixed the looking down bug this time? 
		- Fixed the purple rails
		- Fixed nametags missing
	- v2.3.1
		- Works without MoreCompany now
	- v2.3.0
		- Modders can now exclude their model replacements from Lethal Company's post processing to improve custom shader quality. 
		- Minor performance improvements
	- v2.2.1
		- More rendering changes that should fix items not being held at the correct location.
		- MirrorDecor, LCThirdPerson, and emote mods should no longer have conflicts.
		- There should no longer be models visible in first person when you look down
		- You should be able to see the base game suit now after removing a model replacement. 
		- Thanks to Naelstrof for changing the execution order of UnityJigglePhysics LateUpdate, which should remove jitter in the ship. 
	- v2.2.0
		- Rendering changes to make models more robust to cameras. 
		- Recording camera support, and third person mods should no longer have conflicts. 
		- Models with converted standard materials will no longer have the crusty base game normal map
	- v2.1.2
		- Fixed ragdolls and death commands not activating
	- v2.1.0
		- Native UnityJigglePhysics support for modders. 
		- Item rotation refactor to fix items not being held correctly. 
		- MirrorDecor now supported with 3rdPerson and LCThirdPerson, thanks to Adamasnaldo for the fix. 
	- v2.0.4
		- Uncommented debug
	- v2.0.3
		- TooManyEmotes OnEmote command fix
	- v2.0.2
		- Known issues section
		- Fix for renderers disappearing for some models
		- OnEmote commands now only trigger once per emote
	- v2.0.1
		- MoreCompany cosmetic fixes
	- v2.0.0
		- Overhauled BoneMap system, moved to unity-editor workflow with .unitypackage
		- Added the ability to set rotation offsets to items, 
		- Added extra registry options. See the wiki for more info. 
		- Changed how the model replacement is rendered. No more skeletons separating from bodies. 
		- Thanks to Linkoid for refactoring and improving materials and adding support for materials that already utilize a setup HDRP shader. 
		- Added OnEmote commands, and will provide support for other events (like taking damage) down the line 
		- Note that mods that target v1.x.x will not be compatible with further updates of ModelReplacementAPI as a consequence of moving to the unity workflow, but this shouldn't happen again. 
	- v1.4.1
		- Added RegisterModelReplacementOverride(Type bodyReplacement) to the API to prevent body replacements from constantly being removed and put back on when you want every player to have the same body replacement. 
	- v1.4.0
		- Added support for LCThirdPerson
		- Fixed bug that caused incompatibility with the More Suits mod. Thanks to Linkoid for the fix. 
	- v1.3.2
		- Fixed models appearing in first person for users without the 3rdperson mod.
	- v1.3.0
		- Added support for 3rdperson.
	- v1.2.4
		- Fixed an issue where the bone offset tool was not setting the held item offset
	- v1.2.3
		- Fixed the same bug a second time, hopefully for real this time. 
		- Fixed MoreCompany cosmetics not parenting correctly. 
		- Possibly fixed players being invisible after death. 
	- v1.2.2
		- Fixed a bug related to held items. Thanks to Mina for reporting a fix. 
	- v1.2.0
		- Added support for ModelReplacementTool, removed a bug that spawned gameobjects periodically. 
	- v1.1.0
		- Fixed some errors with bone mapping and biblically accurate angels. 
	- v1.0.0
		- Release