# Components: InputField
**< [Previous Component](/docs/components/UnityEngine.UI.Button.md)** | **[Back to Components](/docs/components/README.md)** | **[Next Component](/docs/components/NeedsX.md) >**

- Identifier: `UnityEngine.UI.InputField`
- Category: **Interactive, Visual**
- Unity Documentation: **[InputField @ docs.unity3d.com](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-InputField.html)**

The InputField Component is an Interactive Component that allows Players to enter arbitrary text that gets sent back as a Command. It automatically adds a  `UnityEngine.UI.Text`  to the panel, allowing you to change the  `fontSize`,  `font`,  `align`ment & text`color`  of it.
```json
{
	"type": "UnityEngine.UI.InputField",
	"fontSize": 14,
	"font": "RobotoCondensed-Bold.ttf",
	"align": "UpperLeft",
	"color": "1.0 1.0 1.0 1.0",
	"command": "",
	"text": "",
	"readOnly": false,
	"lineType": "SingleLine",
	"password": null,
	"needsKeyboard": null,
	"hudMenuInput": null,
	"autofocus": null,
    "fadeIn": 0.0
}
```
> `password`, `needsKeyboard`, `hudMenuInput`,  and `autofocus` are key presence Fields, key presence Fields don't have a specific type and act as a Boolean.
> If the key is present it equals true, if absent it equals false.

InputField specific Fields:
| Key         | Type   | Notes                |
| :---------- | :----- | :------------------- |
| `command`   | string | The command that should get sent to the Server alongside the Player's Input. The Input will get appended to the Command after a Space. |
| `text`      | string | The default Text of the InputField can be combined with `readOnly` |
| `readOnly`  | bool   | Prevents the Content from being edited |
| `lineType`  | string (enum `InputField.LineType`) | Dictates if the Field should allow multiple Lines & how to handle when the Player presses `enter` |
| `password`  | key presence Field | If the input should be obscured |
| `needsKeyboard`  | key presence Field | Prevents default Keyboard behavior (movement, item switching etc.) While the field is Focused |
| `hudMenuInput`  | key presence Field | Same as above but for Rust UI (Inventory, Crafting, etc.) |
| `autofocus`  | key presence Field | Selects the field upon creation |
| `fadeIn`    | float  | The Duration the Panel should take to fade in |

### needsKeyboard vs hudMenuInput
while both prevent Vanilla behavior, they have some key differences that are good to keep in mind.
needsKeyboard works well for normal use, but will close any Rust UI the player has open when the player selects an InputField with it enabled.
This is the primary reason why hudMenuInput was added. It won't close Rust's UI when selected, but won't prevent the player from moving & executing key binds.

### Selecting Text
an underutilized Power of the InputField is that you can select its contents. This is helpful when creating forms & editors, but can also be used for other features. Like using it for displaying links to your website or discord, allowing players to select and copy it instead of having to type it out.

It’s recommended to wrap your InputField in another panel, ensuring it's the only child of it’s parent, as it prevents the selected text from being covered by other children.

### Receiving Input & the lineType Setting
to receive the Player’s input text, listen for the command you specify in the  `command`  field. The Input will get sent as soon as the Player unfocuses the InputField, for example by clicking out of it.

Depending on the  `lineType`  Setting, if it’s set to SingleLine or MultiLineSubmit pressing  `enter`  will also cause the Input to get sent to the Server. Pressing  `enter`  with the MultiLineNewline Setting inserts a Newline instead.

**< [Previous Component](/docs/components/UnityEngine.UI.Button.md)** | **[Back to Components](/docs/components/README.md)** | **[Next Component](/docs/components/NeedsX.md) >**
