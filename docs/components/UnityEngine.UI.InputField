# Components: InputField
**< [Previous Component](/docs/components/UnityEngine.UI.Button.md)** | **[Back to Components](/docs/components/README.md)** | **[Next Component](/docs/components/NeedsX.md) >**

- Identifier: `UnityEngine.UI.InputField`
- Category: **Interactive, Visual**
- Unity Documentation: **[InputField @ docs.unity3d.com](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-InputField.html)**

The InputField Component is an Interactive Component that allows Players to enter arbitrary text that gets sent back as a Command. it automatically adds a `UnityEngine.UI.Text` to the panel, allowing you to  change the `fontSize`, `font`, `align`ment & text`color` of it
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
}
```
> `password`, `needsKeyboard`, `hudMenuInput`,  and `autofocus` are key presence Fields, key presence Fields don't have a specific type and act as a Boolean.
> if the key is present it equals true, if absent it equals false.

InputField specific Fields:
| Key         | Type   | Notes                |
| :---------- | :----- | :------------------- |
| `command`   | string | the Command that should get sent to the Server alongside the Player's Input. The Input will get appended to the Command after a Space. |
| `text`      | string | the default Text of the InputField can be combined with `readOnly` |
| `readOnly`  | bool   | prevents the Content from being edited |
| `lineType`  | string (enum `InputField.LineType`) | dictates if the Field should allow multiple Lines & how to handle when the Player presses `enter` |
| `password`  | key presence Field | if the input should be obscured |
| `needsKeyboard`  | key presence Field | prevents default Keyboard behavior (movement, item switching etc) while the field is Focused |
| `hudMenuInput`  | key presence Field | same as above but for Rust UI (Inventory, Crafting, etc.) |
| `autofocus`  | key presence Field | selects the field upon creation |

### Receiving Input & the lineType Setting
to receive the Player's input Text, listen for the Command you specify in the `command` field. the Input will get sent as soon as the Player unfocuses the InputField, for example by clicking out of it.

depending on the `lineType` Setting, if it's set to SingleLine or MultiLineSubmit pressing `enter` will also cause the Input to get sent to the Server. Pressing `enter` with the MultiLineNewline Setting inserts a Newline instead.

**< [Previous Component](/docs/components/UnityEngine.UI.Button.md)** | **[Back to Components](/docs/components/README.md)** | **[Next Component](/docs/components/NeedsX.md) >**
