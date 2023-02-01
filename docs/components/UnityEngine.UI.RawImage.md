# Components: RawImage
**< [Previous Component](/docs/components/RectTransform.md)** | **[Back to Components](/docs/components/README.md)** | **[Next Component](/docs/components/UnityEngine.UI.Image.md) >**

- Identifier: `UnityEngine.UI.RawImage`
- Category: **Visual**
- Unity Documentation: **[RawImage @ docs.unity3d.com](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-RawImage.html)**

The RawImage is a Visual Component that allows you to display Images from your Server & the Web or Rust Sprites. it can also be used with a single color to act as a Background for your panel
```json
{
	"type": "UnityEngine.UI.RawImage",
	"sprite": "Assets/Icons/rust.png",
	"color": "1.0 1.0 1.0 1.0",
	"material": "",
	"url": "",
	"png": ""
}
```
RawImage specific Fields:
| Key         | Type   | Notes                |
| :---------- | :----- | :------------------- |
| `sprite`    | string | the asset Path to the sprite |
| `color`     | string | the normalized RGBA values of your color |
| `material`  | string | the asset Path to the Material |
| `url`       | string | the URL of the Image you want to show |
| `png`       | string | the CRC Checksum of the Image hosted on the Server |

## Images can be your Backgrounds
the most common use for RawImage is its Ability to display any* Color you give it.  therefore it's often used to give Panels a Background Color. Colors are sent as a `string` of 4 normalized `floats`

Tip: if you're used to using hexadecimal you can use this function to convert your colors on the fly:
```c#
static public string NormalizeHex(string hex, byte alpha = 255){
	if (string.IsNullOrEmpty(hex) || hex.Length < 7)
		hex = "#FFFFFF";
	var str = hex.Trim('#');

	var r = byte.Parse(str.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
	var g = byte.Parse(str.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
	var b = byte.Parse(str.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
	var a = alpha;
	// if the hex has alpha encoded, use it instead
	if (str.Length >= 8)
		a = byte.Parse(str.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);

	return $"{(double)r / 255:f3} {(double)g / 255:f3} {(double)b / 255:f3} {(double)a / 255:f3}";
}
```

Images can also be combined with a Subset of Materials to enhance your UIs, some Materials can be combined with a Color, while others will enforce their own Color. some Materials won't have any visual Effect.
> \* Colors are limited to a Precision of 0.01 per Channel, giving you only 1.000.000 Colors to choose from (excluding the alpha Channel)

## Using Rust's Sprites
You can re-use any of Rust's Sprites by specifying an asset path in the `sprite` field.

## Loading your own Images
the RawImage Component has two Ways of displaying your own Images, the first is by downloading the image onto the game Server & supplying the CRC Checksum in the `png` field, and the second is by hosting the image online and supplying a direct link to it in the `url` field

Loading Images from your Server requires a bit of setup but has the benefit of the Client caching the image once it's loaded from your server, resulting in faster loading times on subsequent UI sends.

### Loading Images from your Server
to load Images from your own Server you have to download & store the image in Rust's `FileStorage`.  

```c#

private IEnumerator SaveImageFromURL(string URL){
	using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(URL)){
		// Download your image from the web
		yield return request.SendWebRequest();

		if (uwr.result != UnityWebRequest.Result.Success)
			yield break;

		var texture = DownloadHandlerTexture.GetContent(request);
		byte[] data = texture.EncodeToPNG();
		
		// this CRC is what you send to the client. save it somewhere for later
		uint crc = FileStorage.server.Store(data, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID);
	}
}
```
the rust Client will then automatically fetch the Image from the Server. You will have to re-download the Image every Wipe as the CommunityEntity's netID will differ, which will cause the Client to not find your Image.

## Combining Images & Color
when using an image you can use the `color` property to modify the image's Color on the fly, this is great when using Icons that are White or Grayscale, as it allows you to communicate Information by changing the Color of the Icon instead of having to load multiple versions.

---
before using RawImage, take a look at the Image Component to see if it's better suited for your usecase.
**< [Previous Component](/docs/components/README.md)** | **[Back to Components](/docs/components/README.md)** | **[Next Component](/docs/components/UnityEngine.UI.RawImage.md) >**
