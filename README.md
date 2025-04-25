Diorama - Plugin Creation Engine

Use an in-game GUI and the Engine to create Skyrim ESPs and BOS INIs without the Creation Kit. Add new objects or update an existing objects position, rotation, base object, etc while in game and let Diorama work its magic.

## Installation
Install via a mod manager. Add the DioramaEngine.exe as an executable

## Usage
While playing Skyrim, open the GUI using the Page Down key (this is configurable in the ini). Create or select the profile you wish to edit. Profiles enable you to work on multiple mods simultaneously
Add an object to either by target (whatever the reticule is looking at), by console (whatever is selected in console) or by it's Form ID
This will store the objects current properties. Remember that if an object is moved after being added then the profile will need to be updated (re-add the object or click the Update from world button)
Save the changes to store the profile to a JSON file

Run the DioramaEngine.exe via the mod manager to open the Engine GUI. Select your profile and either save or update your ESP or BOS ini
At this stage it is also possible to select additional masters and add an author and description

## In-Game GUI
Profiles - Select a profile via the dropdown, add or remove profiles with the '+'/'-' buttons respectively

References - Add a new reference to the profile using any of the three buttons

Reference list - Select a reference in the list box and it's details will be displayed to the right

Reference controls
	- Enable/Disable button - toggle whether the reference is enabled/disabled in game and in the profile
	- Remove reference button - Remove the selected reference from the profile
	- Restore from profile button - Restore the references properties (position/rotation/base object/etc) based on the profile. If the object does not exist and is within a distance of 4096 it will be created automatically.
	- Move to reference/Move to cell button - Teleport the player to the references position (or if the player is too far away, coc to the references cell)
	- Swap base button - Swap the selected references base object. Recommended to use a mod like Modex which will allow you to search for and copy a Form ID which you can paste into Diorama
	- Update from world button - Update the profile with the selected objects current in-game properties
	- Tint reference checkbox - Add a colour filter over the object. By default a yellow tint indicates that the references in-game properties do not match those in the profile, otherwise the tint will be red. (This can be updated in the ini)

All References controls
	- Save all references button - Save the current profile to a JSON file. This needs to be done before shutting the game down in order for any changes to be picked up by the Engine
	- Restore all button - Restore the properties of all references (position/rotation/base object/etc) based on the profile. Any objects that do not exist and is within a distance of 4096 will be created automatically	
	- Update all button - Update the profile with the current in-game properties of all references
	- Tint all references button - Adds a colour filter over all the references in the profile