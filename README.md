# LethalCompany_ModelReplacementAPI

Helps the user create model replacement mods in Lethal Company. [See the wiki](https://github.com/BunyaPineTree/LethalCompany_ModelReplacementAPI/wiki) for more info. Developers [see the SDK repo(in progress)](https://github.com/BunyaPineTree/LethalCompany_ModelReplacementSDK) if you want to contribute to its development.    

Features
-
- A unity package manager based workflow to simplify model replacement and assetbundle creation.
- A skin registry that allows the user to set specific skin names to specific models, default models replacements, or override that entirely and make all players the same model. It is also possible to except a model replacement from the registry entirely, and implement your own logic.
- Native support for [UnityJigglePhysics](https://github.com/naelstrof/UnityJigglePhysics) as a free and highly functional method to implement bone physics into your model without needing to struggle with Assembly Definitions. 
- Seemingly client side
- More Company cosmetic support
- 3rdPerson and LCThirdPerson support
- TooManyEmotes support(?)
- MirrorDecor support
- Recording Camera support

How it works
-
This mod maps the bone rotations from the base game character model to the unity Humanoid Avatar Definition, and then from your model's humanoid avatar to the underlying bones. A result of this is that you do not need to make custom animations, but the result will not be as high quality as if you did and made your model replacement mod from scratch. 
Players with an active model replacement are given a `BodyReplacement` component derived from `BodyReplacementBase`, which on each call of `Update()` sets your model replacement's bone rotations via the above method. The mod maker can set which skins activate any given `BodyReplacement` with the skin registry.
* See the Miku example project for a demonstration on how your registered model replacement can make use of bepinex configs. 

Known issues
-
* Ragdoll behaves strangely at times
* Blood decals are not currently visible on the ragdoll replacement.
* Dying at the company may make the dead individual respawn without a model, but it may also return at a later point in time. 

Unknown issues
-
* Many.

And a special thanks to mina, linkoid, notnotnotswipez, naelstrof and everybody else who has been a huge help. 

