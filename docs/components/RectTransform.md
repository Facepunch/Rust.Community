# Components: RectTransform
**[Back to Components](/docs/components/README.md)** | **[Next Component](/docs/components/UnityEngine.UI.RawImage.md) >**

- Identifier: `RectTransform`
- Category: **Position**
- Unity Documentation: **[RectTransform @ docs.unity3d.com](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/class-RectTransform.html)**

The RectTransform is a Component that specifies the Position & Size of the Panel it's attached to.
It allows you to position a Panel via [Anchors & Offsets](#anchors-and-offsets).
```json
{
	"type": "RectTransform",
	"anchormin": "0.0 0.0",
	"anchormax": "1.0 1.0",
	"offsetmin": "0.0 0.0",
	"offsetmax": "1.0 1.0",
}
```
RectTransform specific Fields:
| Key         | Type   | Notes                |
| :---------- | :----- | :------------------- |
| `anchormin` | string | Specifies the anchor for the bottom left Corner |
| `anchormax` | string | Specifies the anchor for the top right Corner |
| `offsetmin` | string | Specifies the Pixel offset for the bottom left Corner |
| `offsetmax` | string | Specifies the Pixel offset for the top right Corner |

both Anchors & Offsets come with a *-min* and *-max* Field, where the min holds x & y Values for the bottom left Corner & the max holds x & y Values for the top right Corner

## Positioning in Rust
a Panel's Position is always relative to its Parent, even when parenting directly to a Layer.
Unity's Coordinate System in a 2D Space goes from Left to Right, Bottom to Top.
| `x 0.0 y 1.0` | `x 1.0 y 1.0` |
| ------------- | ------------- |
| `x 0.0 y 0.0` | `x 1.0 y 0.0` |
> this might be confusing to people coming from the web-design space, where the x coordinate commonly goes down the screen instead of up

## Anchors and Offsets
Anchors Specify where your Panel starts & ends. Offsets let you move the start & end points with pixel values.

Anchors use a normalized Percentage of the Parent, where  `0.0`  is the Start and  `1.0`  is the End.  [The “Anchors” Section](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/UIBasicLayout.html#anchors)  in the Unity Documentation has some visual examples which also apply here.

Offsets use  [scaled](#offsets-and-scaling)  pixel values to move the corners relative to the Anchors you provide.

Positioning using only anchors is great for when you want your Panel to cover a Percentage of the Screen or Parent, but for any other cases, a mixture of Anchors & Offsets should be used to position your UI.

A common Technique is to use anchors to specify what part of the Parent the UI should be anchored to (Top Left Corner or Dead Center. Etc.) and to use Offsets for Size & Position.
Anchors Specify where your Panel starts & ends. Offsets let you move the start & end Points with pixel Values.

## Offsets and Scaling

Rust will automatically scale offsets depending on the Player’s Resolution, which ensures your Offsets are predictable.

By default, Rust handles Offsets based on a Resolution of  **1280**  by  **720**. This means that for smaller & larger  **16:9**  Resolutions, Rust will simply multiply Offset by the difference between the Player’s Resolution and 720p,

For non-16:9 Resolutions such as Ultrawide or windowed Mode, Rust will apply Letter boxing before calculating the scaling Factor

> NOTE: when scaling offsets rust also takes the player's `graphics.uiscale` into account.

https://user-images.githubusercontent.com/33698270/214582004-c695e73c-9125-4ccb-9cfb-abc883b5c0a0.mp4
> a video of a gray Rectangle positioned at the Center of the Screen (anchor min & max at  `0.5 0.5`) with an offset of 1280 x 720 px. see how Unity applies Letter boxing before scaling the Rectangle to ensure it never gets stretched. NOTE: the black Bars are only to illustrate in this Example, your in game UI won’t have any black Bars


**[Back to Components](/docs/components/README.md)** | **[Next Component](/docs/components/UnityEngine.UI.RawImage.md) >**
