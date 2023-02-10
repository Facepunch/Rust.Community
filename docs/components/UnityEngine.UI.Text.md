# Components: Text
**< [Previous Component](/docs/components/UnityEngine.UI.Image.md)** | **[Back to Components](/docs/components/README.md)** | **[Next Component](/docs/components/UnityEngine.UI.Outline.md) >**

- Identifier: `UnityEngine.UI.Text`
- Category: **Visual**
- Unity Documentation: **[Text @ docs.unity3d.com](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Text.html)**

The Text Component is a Visual Component that allows you to display any Text you want. It has Options for Alignment, Color, and overflow Options & Supports RichText.
```json
{
	"type": "UnityEngine.UI.Text",
	"text": "Text",
	"fontSize": 14,
	"font": "RobotoCondensed-Bold.ttf",
	"align": "UpperLeft",
	"color": "1.0 1.0 1.0 1.0",
	"verticalOverflow": "Truncate",
	"fadeIn": 0.0
}
```
Text-specific Fields:
| Key         | Type   | Notes                |
| :---------- | :----- | :------------------- |
| `text`      | string | The Content of your Text Component |
| `fontSize`  | int    | The default font size of your Text |
| `font`      | string | The Asset name of the Font you wish to use |
| `align`     | string (enum `TextAnchor`) | The way your Text should be aligned |
| `color`     | string | The default Color of your Text |
| `verticalOverflow` | string (enum `VerticalWrapMode`) | How Text Overflowing vertically should be handled |
| `fadeIn`    | float  | The Duration the Panel should take to fade in |

### Available Fonts:
-   `DroidSansMono.ttf`
-   `PermanentMarker.ttf`
-   `RobotoCondensed-Bold.ttf`
-   `RobotoCondensed-Regular.ttf`

### Text Alignment Options:
The TextAnchor enum has the following options available
| `UpperLeft`  | `UpperCenter`  | `UpperRight`  |
| ------------ | -------------- | ------------- |
| `MiddleLeft` | `MiddleCenter` | `MiddleRight` |
| `LowerLeft`  | `LowerCenter`  | `LowerRight`  |

### VerticalWrapMode Options:
The VerticalWrapMode enum has the following options available
| Option | Notes |
| :----- | :---- |
| 'Truncate'| Text will be clipped when reaching the bottom. |
| 'Overflow'| Text will continue to generate past the bottom edge of the panel. |

## Using Unity RichText
Rust Allows you to use Unity  **[Rich Text](https://docs.unity3d.com/2021.3/Documentation/Manual/StyledText.html)**  to give you more control over text styling. This lets you empathize Paragraphs, Sentences, Words & individual Letters.

To modify a selection of text, simply wrap it in XML-style tags:  `<b>i am bold</b>`  
RichText tags can be Nested to combine Effects:  `<b>bold and <color=red>in red</color></b>`

Supported Tags:
| **_Tag_** |  **_Description_** | **_Example_** |
| :-------: | :----------------- | :------------ |
| **b** | Renders the Text  as bold | `I am  <b>bold</b>` |
| **i** | Renders the Text as italic | `I am <i>italic</i>` |
| **size** | Sets the fontSize to the Value supplied | `I am <size=10>small.</size> i am not.` |
| **color** | Changes the Color of the Text | `I am <color=#c0ffeeff>Coffee</color> coloured` |
For colors, Unity Supports an assortment of named Colors you can find in  **[this table](https://docs.unity3d.com/2021.3/Documentation/Manual/StyledText.html#ColorNames).**



**< [Previous Component](/docs/components/UnityEngine.UI.Image.md)** | **[Back to Components](/docs/components/README.md)** | **[Next Component](/docs/components/UnityEngine.UI.Outline.md) >**
