# Generic_ModelReplacementAPI

Notes
-
I have gone through the mod and made most things game agnostic, or at least provided explanations when I couldn't. This is in no way a working mod, and doesn't even compile. It is just a starting point for porting to another game. Lethal Company had lots of game specific code, and whatever game is being ported to will have just as much that needs to be considered. 

Known Challenges to Porting This mod
-
Reliance on `PlayerControllerB`. Generally we only use `PlayerControllerB` to get things like suit names, player names, and the base game gameobject, but given how entrenched it is in the original code it would be difficult to work without an analog. So long as an equivalent exists this can probably be directly replaced without issue. 

Registry Methods also rely on `PlayerControllerB`, and honestly I'm not sure that the registry system I have in place is the best option. You will probably need to devise some new method for whatever game is being ported to. The only requirement is that you have a consistent means of putting a `BodyReplacement` somewhere on the character. In Lethal Company I just ran a loop on `Update` that checks if somebody's suit name has changed, and if so it put on the `BodyReplacement` registered to that suit. 


Overview of Workflow
-
Implementing this mod requires a few monobehaviors to be placed at some point on the character GameObject tree. 
It would also be useful to have an analog for the Lethal Company `PlayerControllerB` monobehavior, which keeps most player related info in one spot. ModelReplacementAPI is highly dependent on `PlayerControllerB`, and would probably require a fair bit of refactoring if no analog exists. 

## Monobehaviors

### `BodyReplacementBase`
The way this mod functions is via an abstract `BodyReplacementBase` class that is derived from for some specific `BodyReplacement` class. This means the only effort required by the user of `BodyReplacementBase` is to fill in the required abstract methods, and optional overridable methods, which are used by the abstract base class to handle everything else. The `BodyReplacement` is then instantiated as a component somewhere on the character GameObject. 

This class outsources rendering logic to `ViewStateManager`, replacement model rig update logic to `AvatarUpdater`, and replacement viewmodel rig update logic to `ViewModelUpdater`

### `ViewStateManager` : `ManagerBase`
Handling all first/third person and rendering logic was outsourced to the `ViewStateManager` instead of managing it in `BodyReplacementBase`. In LethalCompany this was useful given the frequency with which models were changed, as some info needed to persist in between `BodyReplacements`, which were destroyed and reinstantiated with model change. 

`ViewStateManager` also handled everything to do with layers. In Lethal Company the general crustiness and visual quality required a No PostProcessing layer to be implemented, which would make things such as anime models function. This may not be necessary in general, and so the majority of the logic in `ViewStateManager` can be removed

### `ManagerBase`
This abstract class is derived by `ViewStateManager`, as well as other MonoBehaviors that need a reference to the current `BodyReplacement`. It splits its update logic into `UpdatePlayer()` and `UpdateModelReplacement()` methods to differentiate whether a model replacement is active. When a `BodyReplacement` calls `Awake()`, it reports to every `ManagerBase` its existence, which changes the reference in the `ManagerBase` classes. 

## Scripts

### `AvatarUpdater`
This script handles everything to do with rigs, and is both game specific and sometimes model specific.
Generally, it maps replacement model bones to the HumanoidAvatarDefinition, and then from the HumanoidAvatarDefinition to the base game rigs. So long as the replacement model is Humanoid, the only thing necessary is to define the map from the base game rig to the humanoid one. 

### `ViewModelUpdater`
The exact same thing as above, but for first person viewmodels. Note that we currently have the ability to generate first person viewmodels from the replacement model, given that the replacement model has a humanoid avatar. This code is in the MeshHelper class, and is probably the only thing that should translate without issue to any game. 



