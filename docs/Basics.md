# The Basics of CUI
**< [Previous Topic](/README.md)** | **[Back to the Start](/README.md)** | **[Next Topic](/docs/components/README.md) >**

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
> the values in these JSON examples represent the default Values that are assigned if no property is specified.
> … represent One or more collapsed Objects

| Key | Type     | Notes                |
| :-- | :------- | :------------------- |
| `name` | string | the identifier of your panel, needed when destroying UI or adding panels inside this one |
| `parent` | string | tells the client which Panel or Layer to Parent to |
| `components` | List of Components | One or more Components, without these there's no point in sending a panel |
| `fadeOut` | float | makes the Panel fade out instead of disappearing immediately. *Currently doesn't fade out any child Panels* |
| `destroyUi` | string | destroys the Panel specified in the string before creating your Panel. useful for updating UI |

### About Naming
It's recommended to always name your Panels *something*, this is because the CUI System doesn't support multiple Panels with the same name and may cause *Ghost Panels* which cant be destroyed.

It's also recommended to prefix the Name of your Panel with something unique to your Mod, which ensures there are no accidental name Conflicts with other Mods


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
When destroying a Panel, all child Panels will also get destroyed.
> your Modding Framework may have helper Methods to simplify these steps.

----
the next Topic explains Components in detail

**< [Previous Topic](/README.md)** | **[Back to the Start](/README.md)** | **[Next Topic](/docs/components/README.md) >**
