# Components: Button
**< [Previous Component](/docs/components/UnityEngine.UI.Outline.md)** | **[Back to Components](/docs/components/README.md)** | **[Next Component](/docs/components/UnityEngine.UI.InputField.md) >**

- Identifier: `UnityEngine.UI.Button`
- Category: **Interactive, Visual**
- Unity Documentation: **[Button @ docs.unity3d.com](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Button.html)**

The Button Component is an Interactive Component that lets you execute Commands & Destroy UI when it gets pressed. It automatically adds a  `UnityEngine.UI.Image`  to the panel, allowing you to change the  `sprite`,  `material`  &  `color`  on it.
```json
{
	"type": "UnityEngine.UI.Button",
	"command": "",
	"close": "",
	"sprite": "Assets/Icons/rust.png",
	"color": "1.0 1.0 1.0 1.0",
	"material": "",
	"imagetype": "Simple",
    "fadeIn": 0.0
}
```
Button-specific Fields:
| Key         | Type   | Notes                |
| :---------- | :----- | :------------------- |
| `command`   | string | The command that should get sent to the Server on Click |
| `close`     | string | The Name of the Panel that should get closed on Click |
| `sprite`    | string | The asset Path to the sprite |
| `color`     | string | The normalized RGBA values of your color |
| `material`  | string | The asset Path to the Material |
| `imagetype` | string (enum `Image.Type`) | Sets the display mode of the Image* |
| `fadeIn`    | float  | The Duration the Panel should take to fade in |
\*  Currently non-functioning for anything other than Rust's built-in Sprites

### Button as a Parent
click events bubble up, meaning that they will get triggered on every Panel until a Component consumes them. This means it’s possible to use the Button as a Parent and still get notified when the Player clicks a child panel.

**< [Previous Component](/docs/components/UnityEngine.UI.Outline.md)** | **[Back to Components](/docs/components/README.md)** | **[Next Component](/docs/components/UnityEngine.UI.InputField.md) >**
