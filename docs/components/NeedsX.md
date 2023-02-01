# Components: NeedsCursor & NeedsKeyboard
**< [Previous Component](/docs/components/UnityEngine.UI.InputField.md)** | **[Back to Components](/docs/components/README.md)** | **[Next Component](/docs/components/Countdown.md) >**

- Identifier: `NeedsCursor` or `NeedsKeyboard`
- Category: **Misc**
- Unity Documentation: **N/A**

The NeedsCursor & NeedsKeyboard Components are Components with no additional Fields. Their only purpose is to tell Rust's input Controller if mouse & keyboard Behavior should only be focused on your UI.
```json
{
	"type": "NeedsCursor"
	// or
	"type": "NeedsKeyboard"
}
```

unlike the `needsKeyboard` and  `hudMenuInput` fields on an InputField Component, these Components Prevent default Behavior until your Panel is Destroyed

**< [Previous Component](/docs/components/UnityEngine.UI.InputField.md)** | **[Back to Components](/docs/components/README.md)** | **[Next Component](/docs/components/Countdown.md) >**
