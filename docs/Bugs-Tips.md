# Bugs & Tips

**< [Previous Topic](/docs/components/README.md)** | **[Back to the Start](/README.md)**

When working with the CUI System, you will often encounter weird Bugs & Situations. This document lists some ways to prevent them & shows you additional tricks to enhance your UIs.

## Table of Contents
- [Common Error Messages](#common-error-messages)
- [Bugs](#bugs)
- [Tricks](#tricks)


# Common Error Messages
> these errors will appear in your client console when testing the UI, make sure to test for these before releasing your mod

### AddUI: Unknown Parent for *name*: *parent*
this happens when the parent you supplied in your panel Payload doesn't exist. Double-check for spelling mistakes, and formatting issues & ensure your parent appears in your JSON list before the Panel you get this error on.



---

### Error downloading image: *URL* ( *error* )
the Client couldn't download your Image, double-check your URL & look at the error part within the brackets for what could cause it.



---

### A GameObject can only contain one 'Graphic' component.

This error indicates that you're trying to add multiple Unity Components that derive from `UnityEngine.UI.Graphic`. The **[Components](/docs/components/README.md)** topic has a list of allowed Components and lists the categories they are in. any Component of the **Visual** Category is a Component that derives from or adds a `UnityEngine.UI.Graphic` to the Panel. To get around this Limitation, you can put other Visual Components on a child panel instead.

# Bugs


### Orphaned UI Panels, UI that Can't be Destroyed, or Ghost Panels.

**Symptom:** you have 1 or more panels that are stuck on your Screen and can't be removed by Calling **DestroyUI** with their Name.

**Cause:** you have 2 or more panels that shared the same name & were active at the same time. If these are parented directly to a Layer and one is destroyed, the other Panel Can't be destroyed anymore. The only way to remove the UI Panel from your Screen is by reconnecting. Double-check your UI and ensure any Panel directly parented to a Layer has a unique Name. Also, ensure that you don't accidentally send the same UI twice.



---


### Flickering when destroying & re-sending UI on the same frame.

**Symptom:** when updating Part of your UI. the Player gets a noticeable Delay between the old UI being destroyed & the new UI being created.

**Cause:** the presumed cause of this issue is the Server Delaying your AddUI Packet to the next Network Cycle. This happens very unpredictably, but is more likely with larger UI Updates. The solution is to use the `destroyUi` field on your panel Payload, it works the same way a DestroyUI call works, but is combined with your panel Payload to prevent network scheduling from creating the flickering issue.



---

### Very Inconsistent Alpha handling for images

**Symptom:**  when using images with somewhat transparent pixels, the images get rendered more Opaque than they are, resulting in heavy artifacts.  
**Linked Issue:**  **[#42 - Rust has an Opacity Issue](https://github.com/Facepunch/Rust.Community/issues/42)**

**Cause:**  no known Cause or Workaround found. Vote on the Linked issue if you encounter this bug or have more info as to why this might happen.



---

### Panels with a blur Material & Transparency cause underlying Panels to be invisible.
**Symptom:** when using a blurry Material on a Color with Transparency, any underlying Image Components with blur get cut out.
**Visual Example:**
![image](https://user-images.githubusercontent.com/33698270/215882128-d1f0798c-d7ed-4986-9675-4fba48632ad7.png)
> an image of the following UI setup
> parent: black with 98% alpha
>
> square 1: child of Parent, gray with 20% alpha
>
> square 2: child of Parent, gray with 100% alpha
>
> square 3: child of Parent, gray with 20% alpha and a text element behind it.


**Cause:** no exact Cause is known, but it seems to be related to the blurry Materials themselves. This bug only occurs when all underlying Images are using a blurry material with transparency. If at least 1 element is opaque, the bug does not occur. This can be a text or image element.



# Tricks

### Use Outlines like an advanced User
Outlines can go way beyond the basic use case of putting them on your square Panels. Applying Outlines to Text can be a great way to make them stand out, for example when you can't control the background behind it.
![image](https://user-images.githubusercontent.com/33698270/215885917-4916ee09-a891-4609-82a4-51bb07881bde.png)

> Can you read this?

![image](https://user-images.githubusercontent.com/33698270/215886796-346be279-2d8e-43c4-a28f-f740bcf50ff5.png)

> Adding a 1px outline with a darker color makes it much clearer.

Another advanced trick is stacking Outlines with lowered Opacity to create a staggered outline or glow/shadow effect. Unlike **Visual** Components, the Outline Component doesn't derive from `UnityEngine.UI.Graphic`, meaning there's no limit on how many Outlines a Panel can have.

![image](https://user-images.githubusercontent.com/33698270/215899049-e0aa0cd7-b607-466e-a0b7-0cfafa62bdae.png)

> right has no outlines. left is simulating a 3D effect using 3 stacked Outlines with low opacity. looks great in certain scenarios.


---


### Positioning your CUI alongside rust UI

for features that augment Rust's existing UI, like when you're working with Furnaces & other storage inventories, it's important to make sure your CUI doesn't interfere with the UI you augment.

Rust UI uses the same Placement system CUI does. It primarily uses offsets for sizing & positioning and anchors to a specific Point depending on the UI. by doing the same, you can ensure that your CUI is positioned properly, regardless of Screen size & Aspect Ratio.

UIs like the inventory, belt bar & storage containers are anchored to the bottom of the screen, but many other UIs don't follow that convention.

A great way to discover what a UI element is anchored to is to use the windowed mode and stretch it to extreme aspect ratios. This clearly reveals anchoring thanks to rust's offset scaling
![image](https://user-images.githubusercontent.com/33698270/216077347-5461623c-8ff4-4890-8633-062519c4e371.png)
> Stretching our window show that the Crafting & Contacts buttons are anchored to the top middle, while the status bars are anchored to the bottom right corner

a RectTransform Example that covers the Belt-bar
```json
{
	"type": "RectTransform",
	"anchormin": "0.5 0.0",
	"anchormax": "0.5 0.0",
	"offsetmin": "-200 18",
	"offsetmax": "180 78"
}
```
![image](https://user-images.githubusercontent.com/33698270/215901408-c152fecb-8453-4597-8cd6-61038b2b976d.png)

**< [Previous Topic](/docs/components/README.md)** | **[Back to the Start](/README.md)**
