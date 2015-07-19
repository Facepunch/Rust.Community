using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if SERVER
[Factory( "cui" )]
public class cui : ConsoleSystem
{
	[User]
	public static void test( Arg args )
	{
		var json = @"[	
						{
							""name"": ""TestPanel7766"",
							""parent"": ""Overlay"",

							""components"":
							[
								{
									""type"":""UnityEngine.UI.RawImage"",
									""color"": ""1.0 1.0 1.0 1.0"",
									""url"": ""http://files.facepunch.com/garry/2015/June/03/2015-06-03_12-19-17.jpg"",
								},

								{
									""type"":""RectTransform"",
									""anchormin"": ""0 0"",
									""anchormax"": ""1 1""
								},

								{
									""type"":""NeedsCursor""
								}
							]
						},

						{
							""parent"": ""TestPanel7766"",

							""components"":
							[
								{
									""type"":""UnityEngine.UI.Text"",
									""text"":""Do you want to press a button?"",
									""fontSize"":32,
									""align"": ""MiddleCenter"",
								},

								{
									""type"":""RectTransform"",
									""anchormin"": ""0 0.5"",
									""anchormax"": ""1 0.9""
								}
							]
						},

						{
							""name"": ""Button88"",
							""parent"": ""TestPanel7766"",

							""components"":
							[
								{
									""type"":""UnityEngine.UI.Button"",
									""close"":""TestPanel7766"",
									""command"":""status"",
									""color"": ""0.9 0.8 0.3 0.8"",
									""imagetype"": ""Tiled""
								},

								{
									""type"":""RectTransform"",
									""anchormin"": ""0.3 0.15"",
									""anchormax"": ""0.7 0.2""
								}
							]
						},

						{
							""parent"": ""Button88"",

							""components"":
							[
								{
									""type"":""UnityEngine.UI.Text"",
									""text"":""YES"",
									""fontSize"":20,
									""align"": ""MiddleCenter""
								}
							]
						}

					]
					";

		CommunityEntity.ServerInstance.ClientRPCEx( new Network.SendInfo() { connection = args.connection }, null, "AddUI", json );
	}
}
#endif

public partial class CommunityEntity : PointEntity 
{
#if CLIENT

	private static Dictionary<string, List<GameObject>> serverCreatedUI;
	private static Dictionary<uint, List<UnityEngine.UI.MaskableGraphic>> requestingTextureImages;

	static CommunityEntity()
	{
		serverCreatedUI = new Dictionary<string, List<GameObject>>();
		requestingTextureImages = new Dictionary<uint, List<UnityEngine.UI.MaskableGraphic>>();
	}

	public static void DestroyServerCreatedUI()
	{
		foreach ( var gameObjects in serverCreatedUI.Values )
		{
			foreach ( var go in gameObjects )
			{
				if ( go ) GameObject.Destroy( go );
			}
		}

		serverCreatedUI.Clear();
		requestingTextureImages.Clear();
	}

	public static void ServerUICreated( GameObject go )
	{
		List<GameObject> gameObjects;
		if ( !serverCreatedUI.TryGetValue( go.name, out gameObjects ) )
		{
			gameObjects = new List<GameObject>();
			serverCreatedUI[go.name] = gameObjects;
		}
		gameObjects.Add( go );
	}

	[RPC_Client]
	public void AddUI( RPCMessage msg )
	{
		var str = msg.read.String();
		var jsonArray = JSON.Array.Parse( str );

		foreach ( var value in jsonArray )
		{
			var json = value.Obj;

			var parentPanel = FindPanel( json.GetString( "parent", "Overlay" ) );
			if ( parentPanel == null )
			{
				Debug.LogWarning( "AddUI: Unknown Parent: " + json.GetString( "parent", "Overlay" ) );
				return;
			}

			var go = new GameObject( json.GetString( "name", "AddUI CreatedPanel" ) );
			go.transform.SetParent( parentPanel.transform, false );
			ServerUICreated( go );

			var rt = go.GetComponent<RectTransform>();
			if ( rt )
			{
				rt.anchorMin = new Vector2( 0, 0 );
				rt.anchorMax = new Vector2( 1, 1 );
				rt.offsetMin = new Vector2( 0, 0 );
				rt.offsetMax = new Vector2( 1, 1 );
			}

			foreach ( var component in json.GetArray( "components" ) )
			{
				CreateComponents( go, component.Obj );
			}

			if ( json.ContainsKey( "fadeOut" ) )
			{
				go.AddComponent<FadeOut>().duration = json.GetFloat( "fadeOut", 0 );
			}
		}
	}

	private GameObject FindPanel( string name )
	{
		List<GameObject> panels;
		if ( serverCreatedUI.TryGetValue( name, out panels ) )
		{
			return panels.FirstOrDefault();
		}
		var transform = UIHUD.Instance.transform.FindChildRecursive( name );
		if ( transform ) return transform.gameObject;
		return null;
	}

	private void CreateComponents( GameObject go, JSON.Object obj )
	{
		//
		// This is the 'stupid' but 'safe & predictable way of doing this.
		//
		switch ( obj.GetString( "type", "UnityEngine.UI.Text" ) )
		{
			case "UnityEngine.UI.Text":
				{
					var c = go.AddComponent<UnityEngine.UI.Text>();
					c.text = obj.GetString( "text", "Text" );
					c.fontSize = obj.GetInt( "fontSize", 14 );
					c.font = FileSystem.Load<Font>( "Assets/Content/UI/Fonts/" + obj.GetString( "font", "RobotoCondensed-Bold.ttf" ) );
					c.alignment = (TextAnchor)System.Enum.Parse( typeof( TextAnchor ), obj.GetString( "align", "UpperLeft" ) );
					c.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );
					GraphicComponentCreated( c, obj );
					break;
				}


			case "UnityEngine.UI.Image":
				{
					var c = go.AddComponent<UnityEngine.UI.Image>();
					c.sprite = FileSystem.Load<Sprite>( obj.GetString( "sprite", "Assets/Content/UI/UI.Background.Tile.psd" ) );
					c.material = FileSystem.Load<Material>( obj.GetString( "material", "Assets/Icons/IconMaterial.mat" ) );
					c.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );
					c.type = (UnityEngine.UI.Image.Type)System.Enum.Parse( typeof( UnityEngine.UI.Image.Type ), obj.GetString( "imagetype", "Simple" ) );

					if ( obj.ContainsKey( "png" ) )
					{
						PngGraphicLoadRequested( c, uint.Parse( obj.GetString( "png" ) ) );
					}

					GraphicComponentCreated( c, obj );

					break;
				}

			case "UnityEngine.UI.RawImage":
				{
					var c = go.AddComponent<UnityEngine.UI.RawImage>();
					c.texture = FileSystem.Load<Texture>( obj.GetString( "sprite", "Assets/Icons/rust.png" ) );
					c.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );

					if ( obj.ContainsKey( "material" ) )
					{
						c.material = FileSystem.Load<Material>( obj.GetString( "material" ) );
					}

					if ( obj.ContainsKey( "url" ) )
					{
						StartCoroutine( LoadTextureFromWWW( c, obj.GetString( "url" ) ) );
					}

					if ( obj.ContainsKey( "png" ) )
					{
						PngGraphicLoadRequested( c, uint.Parse( obj.GetString( "png" ) ) );
					}

					GraphicComponentCreated( c, obj );

					break;
				}

			case "UnityEngine.UI.Button":
				{
					var c = go.AddComponent<UnityEngine.UI.Button>();

					if ( obj.ContainsKey( "command" ) )
					{
						var cmd = obj.GetString( "command" );
						c.onClick.AddListener( () => { ConsoleSystem.ClientRunOnServer( cmd ); } );
					}

					if ( obj.ContainsKey( "close" ) )
					{
						var pnlName = obj.GetString( "close" );
						c.onClick.AddListener( () => { DestroyPanel( pnlName ); } );
					}

					// bg image
					var img = go.AddComponent<UnityEngine.UI.Image>();
					img.sprite = FileSystem.Load<Sprite>( obj.GetString( "sprite", "Assets/Content/UI/UI.Background.Tile.psd" ) );
					img.material = FileSystem.Load<Material>( obj.GetString( "material", "Assets/Icons/IconMaterial.mat" ) );
					img.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );
					img.type = (UnityEngine.UI.Image.Type)System.Enum.Parse( typeof( UnityEngine.UI.Image.Type ), obj.GetString( "imagetype", "Simple" ) );

					c.image = img;

					GraphicComponentCreated( img, obj );

					break;
				}

			case "UnityEngine.UI.Outline":
				{
					var c = go.AddComponent<UnityEngine.UI.Outline>();
					c.effectColor = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );
					c.effectDistance = Vector2Ex.Parse( obj.GetString( "distance", "1.0 -1.0" ) );
					c.useGraphicAlpha = obj.ContainsKey( "useGraphicAlpha" );
					break;
				}
			case "NeedsCursor":
				{
					go.AddComponent<NeedsCursor>();
					break;
				}

			case "RectTransform":
				{
					var rt = go.GetComponent<RectTransform>();
					if ( rt )
					{
						rt.anchorMin = Vector2Ex.Parse( obj.GetString( "anchormin", "0.0 0.0" ) );
						rt.anchorMax = Vector2Ex.Parse( obj.GetString( "anchormax", "1.0 1.0" ) );
						rt.offsetMin = Vector2Ex.Parse( obj.GetString( "offsetmin", "0.0 0.0" ) );
						rt.offsetMax = Vector2Ex.Parse( obj.GetString( "offsetmax", "1.0 1.0" ) );
					}
					break;
				}
		}
	}

	private void GraphicComponentCreated( UnityEngine.UI.Graphic c, JSON.Object obj )
	{
		if ( obj.ContainsKey( "fadeIn" ) )
		{
			c.canvasRenderer.SetAlpha( 0f );
			c.CrossFadeAlpha( 1f, obj.GetFloat( "fadeIn", 0 ), true );
		}
	}

	private void PngGraphicLoadRequested( UnityEngine.UI.MaskableGraphic component, uint textureID )
	{
		var bytes = FileStorage.client.Get( textureID, FileStorage.Type.png );
		if ( bytes == null )
		{
			List<UnityEngine.UI.MaskableGraphic> components;
			if ( !requestingTextureImages.TryGetValue( textureID, out components ) )
			{
				components = new List<UnityEngine.UI.MaskableGraphic>();
				requestingTextureImages[textureID] = components;
				RequestFileFromServer( textureID, FileStorage.Type.png, "CL_ReceiveFilePng" );
			}
			components.Add( component );
		}
		else
		{
			LoadPngIntoGraphic( component, bytes );
		}
	}

	private void LoadPngIntoGraphic( UnityEngine.UI.MaskableGraphic component, byte[] bytes )
	{
		var texture = new Texture2D( 1, 1, TextureFormat.RGBA32, false );
		texture.LoadImage( bytes );

		var image = component as UnityEngine.UI.Image;
		if ( image )
		{
			image.sprite = Sprite.Create( texture, new Rect( 0, 0, texture.width, texture.height ), new Vector2( 0.5f, 0.5f ) );
			return;
		}

		var rawImage = component as UnityEngine.UI.RawImage;
		if ( rawImage ) rawImage.texture = texture;
	}

	private IEnumerator LoadTextureFromWWW( UnityEngine.UI.RawImage c, string p )
	{
		var www = new WWW( p );

		yield return www;

		if ( !string.IsNullOrEmpty( www.error ) )
		{
			www.Dispose();
			yield break;
		}


		var texture = www.texture;
		if ( texture == null || c == null )
		{
			www.Dispose();
			yield break;
		}

		c.texture = texture;
		www.Dispose();
	}
	
	[RPC_Client]
	public void CL_ReceiveFilePng( BaseEntity.RPCMessage msg )
	{
		var textureID = msg.read.UInt32();
		var bytes = msg.read.BytesWithSize();
		
		if ( FileStorage.client.Store( bytes, FileStorage.Type.png ) != textureID )
		{
			Log( "Client/Server FileStorage CRC differs" );
		}

		List<UnityEngine.UI.MaskableGraphic> components;
		if ( !requestingTextureImages.TryGetValue( textureID, out components ) ) return;

		requestingTextureImages.Remove( textureID );

		foreach ( var c in components )
		{
			LoadPngIntoGraphic( c, bytes );
		}
	}

	[RPC_Client]
	public void DestroyUI( RPCMessage msg )
	{
		DestroyPanel( msg.read.String() );
	}

	private void DestroyPanel( string pnlName )
	{
		List<GameObject> gameObjects;
		if ( !serverCreatedUI.TryGetValue( pnlName, out gameObjects ) ) return;

		serverCreatedUI.Remove( pnlName );

		foreach ( var go in gameObjects )
		{
			if ( !go ) continue;

			var fadeOut = go.GetComponent<FadeOut>();
			if ( fadeOut )
			{
				fadeOut.FadeOutAndDestroy();
			}
			else
			{
				Object.Destroy( go );
			}
		}
	}

	private class FadeOut : MonoBehaviour
	{
		public float duration;

		public void FadeOutAndDestroy()
		{
			Invoke( "Kill", duration + .1f );
			foreach ( var c in gameObject.GetComponents<UnityEngine.UI.Graphic>() )
			{
				c.CrossFadeAlpha( 0f, duration, false );
			}
		}

		public void Kill()
		{
			Destroy( gameObject );
		}
	}

#endif
}