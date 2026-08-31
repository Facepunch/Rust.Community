# The Basics of CUI
**[Back to the Start](/README.md)** | **[Next Topic](/docs/components/README.md) >**

There are a few basic Concepts that are needed to make your own CUI, below you will learn about what Panels are & how to create and destroy UI Elements

## The Schema

the JSON Schema to send UI to the Player consists of a List of Elements, where each Element creates a new Panel on the Client. Elements have One or more Components that control the Look & Functionality.

```json
[{
	"name": "AddUI CreatedPanel",
	"parent": "Overlay",
	"components": [...],
	"fadeOut": 0.0,
	"destroyUi": ""
}, ...]
```
> The values in these JSON examples represent the default values that are assigned if no property is specified.  
> … represent One or more collapsed Objects

| Key | Type     | Notes                |
| :-- | :------- | :------------------- |
| `name` | string | The identifier of your panel, needed when destroying UI or adding panels inside this one |
| `parent` | string | Tells the client which Panel or Layer to Parent to. **[Needs to be a valid Panel or Layer name](/docs/Bugs-Tips.md#addui-unknown-parent-for-name--parent)** |
| `components` |List of Components | One or more Components, without these there’s no point in sending a panel |
| `fadeOut` | float | Makes the Panel fade out instead of disappearing immediately.  _Currently doesn’t fade out any child panels._ |
| `destroyUi` | string | Destroys the Panel specified in the string before creating your Panel. Useful **[preventing flickering](/docs/Bugs-Tips.md#flickering-when-destroying--re-sending-ui-on-the-same-frame)** when updating UI. |


### About Layers
layers are used when creating your top most Panel. They differ from Panels because they are static GameObjects that cannot be destroyed via a DestroyUI call. Depending on the Layer you parent to, your UI will appear above or below Rust's own UI elements.

#### Available Layer values
- `Overall` the top most layer in front of all of Rust's UI 
- `Overlay`
- `Hud.Menu` the layer where rust positions menus like your inventory
- `Hud` the layer where Rust stores most HUD elements like your status bar
- `Under` the lowermost layer, your UI will appear behind all of Rust's UI


### About Naming

It’s recommended to always name your Panels  _something_, this is because the CUI System doesn’t support multiple Panels with the same name and may cause  **[Ghost panels](/docs/Bugs-Tips.md#orphaned-ui-panels-ui-that-cant-be-destroyed-or-ghost-panels)**  which can't be destroyed.

It’s also recommended to prefix the Name of your Panel with something unique to your Mod, which ensures there are no accidental name Conflicts with other Mods


## Sending & destroying UI

There are two RPC Calls you can use to send  & destroy UI respectively

Adding UI:
```c#
BasePlayer player = targetPlayer;
string json = "..."; // your UI in JSON form
var community = CommunityEntity.ServerInstance;
SendInfo sendInfo = new SendInfo(player.net.connection);
community.ClientRPCEx<string>(sendInfo, null, "AddUI", json);
```

Destroying UI:
```c#
BasePlayer player = targetPlayer;
string panel = "AddUi CreatedPanel"; // the name of the Panel you wish to destroy
var community = CommunityEntity.ServerInstance;
SendInfo sendInfo = new SendInfo(player.net.connection);
community.ClientRPCEx<string>(sendInfo, null, "DestroyUI", panel);
```
When destroying a Panel, all child panels will also get destroyed.
> Your Modding Framework may have helper Methods to simplify these steps.

----
The next Topic explains Components in detail

**[Back to the Start](/README.md)** | **[Next Topic](/docs/components/README.md) >**
