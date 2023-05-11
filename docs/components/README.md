# Components
**< [Previous Topic](/docs/Basics.md)** | **[Back to the Start](/README.md)** | **[Next Topic](/docs/Bugs-Tips.md) >**

The CUI System comes with a Set of Components you can use to build your UI, some are visual, and others add Interactivity.

Components are added to a panel by sending them as a List in the Schema shown in  [The Basics](/docs/Basics.md). To identify the Type of Component sent, a  `type`  Field is added by every Component.
```json
[{
	"type": "UnityEngine.UI.Text",
	// More Component fields ...
},
...]
```

## Component List
- [RectTransform](/docs/components/RectTransform.md)
- [RawImage](/docs/components/UnityEngine.UI.RawImage.md)
- [Image](/docs/components/UnityEngine.UI.Image.md)
- [Text](/docs/components/UnityEngine.UI.Text.md)
- [Outline](/docs/components/UnityEngine.UI.Outline.md)
- [Button](/docs/components/UnityEngine.UI.Button.md)
- [InputField](/docs/components/UnityEngine.UI.InputField.md)
- [NeedsCursor & NeedsKeyboard](/docs/components/NeedsX.md)
- [Countdown](/docs/components/Countdown.md)

## About Component Categories
Each Component Page has a  _Category_  listed, which helps you identify what a Component does. Pay attention to the  **Visual**  Category, as it means the Component Adds a Unity Component deriving from  `UnityEngine.UI.Graphic`. This is important because  **each panel can only have 1 Graphic Component**.

---
The next Topic explains common Pitfalls & Things you need to look out for

**< [Previous Topic](/docs/Basics.md)** | **[Back to the Start](/README.md)** | **[Next Topic](/docs/Bugs-Tips.md) >**
