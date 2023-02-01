# Components
**< [Previous Topic](/docs/Basics.md)** | **[Back to the Start](/README.md)** | **[Next Topic](/docs/Tips-Pitfalls.md) >**

The CUI System comes with a Set of Components you can use to build your UI, some are visual, and others add Interactivity.

Components are added to a Panel by sending them as a List in the Schema shown in [The Basics](/docs/Basics.md). to identify the Type of Component sent, a `type` Field is added by every Component
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
Each Component Page has a *Category* listed, which helps you identify what a Component does.  pay attention to the **Visual** Category, as it means the Component Adds a Unity Component deriving from `UnityEngine.UI.Graphic`. This is important because **each Panel can only have 1 Graphic Component**.

---
The next Topic explains common Pitfalls & Things you need to look out for

**< [Previous Topic](/docs/Basics.md)** | **[Back to the Start](/README.md)** | **[Next Topic](/docs/Bugs-Tips.md) >**
