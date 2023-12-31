# Miku Model Replacement v1.4.2
### Replaces suits with Hatsune Miku. This is the example mod for ModelReplacementAPI

## Instructions
- Place contents in plugins folder. Ensure that ModelReplacementAPI is also installed. 
- Set your preferred replacement suits in the config, or enable miku for all suits. By default the starting suit is replaced. 
- For more info see https://github.com/BunyaPineTree/LethalCompany_ModelReplacementAPI

## Known Issues
- Cloth physics can be very laggy. ~~It is recommended in larger lobbies that you lower the dynamic bone update rate and disable physics at range via the config.~~ Physics configs temporarily do not function as a result of the switch away from Dynamic Bones.
- Miku is a flashbang as a result of the toon shader currently used. 
- Does not replace hands. You will only see Miku in multiplayer lobbies or with a third person mod. 

## Note
ModelReplacementAPI versison 2.3.0 required for post processing changes to work. 

## Changelog
	- v1.4.2
		- Additional graphics changes. Removal of post processing. 
	- v1.4.1
		- Switched away from Dynamic Bones.
		- Items should be held correctly with the latest version of ModelReplacementAPI
	- v1.4.0
		- Graphics overhaul. 
		- Added config option to set miku as default. Suits that have been registered to another model replacement will take priority. 
		- Emote expressions
	- v1.3.1
		- Fixed the enable miku for all suits setting constantly putting on and removing the miku body replacement, make sure to download the latest version of ModelReplacementAPI 
	- v1.3.0
		- Added config options to set model replacements for specific suit names, or all suits. 
		- The bug that prevented miku from disappearing after changing suits has been resolved, make sure to update to the latest ModelReplacementAPI version. 
	- v1.2.1
		- Fixed miku appearing in first person
	- v1.2.0
		- Added config options to improve dynamic bone performance. 
	- V1.1.0
		- Improved bone offsets, now actually only replaces the default suit, some script parameter changes
	- v1.0.0
		- Release