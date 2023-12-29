# ModelReplacementAPI v2.2.0
### Simplifies character model replacement for modders

## Instructions
For more info on making model replacements see the wiki at https://github.com/BunyaPineTree/LethalCompany_ModelReplacementAPI , or message Bunya Pine Tree at the unofficial modding discord https://discord.gg/nYcQFEpXfU

## Known Issues
- More Company cosmetics behave strangely on ragdolls
- Some third person mods may not be natively supported
- Known issues with some emote mods. 

## Changelog
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