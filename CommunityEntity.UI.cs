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
									""imagetype"": ""Tiled"",
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

	private static List<GameObject> serverCreatedUI = new List<GameObject>();

	public static void DestroyServerCreatedUI()
	{
		foreach ( var obj in serverCreatedUI )
		{
			GameObject.Destroy( obj );
		}

		serverCreatedUI.Clear();
	}

	
	[RPC_Client]
	public void AddUI( RPCMessage msg )
	{
		var str = msg.read.String();
		Log( str );
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
			go.transform.SetParent( parentPanel, false );
			serverCreatedUI.Add( go );

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
		}
	}

	private Transform FindPanel( string name )
	{
		return UIHUD.Instance.transform.FindChildRecursive( name );
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
					break;
				}


			case "UnityEngine.UI.Image":
				{
					var c = go.AddComponent<UnityEngine.UI.Image>();
					c.sprite = FileSystem.Load<Sprite>( obj.GetString( "sprite", "Assets/Content/UI/UI.Background.Tile.psd" ) );
					c.material = FileSystem.Load<Material>( obj.GetString( "material", "Assets/Icons/IconMaterial.mat" ) );
					c.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );
					c.type = (UnityEngine.UI.Image.Type)System.Enum.Parse( typeof( UnityEngine.UI.Image.Type ), obj.GetString( "imagetype", "Simple" ) );
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

	private void DestroyPanel( string pnlName )
	{
		foreach( var pnl in serverCreatedUI.Where( x => x.name == pnlName ).ToArray() )
		{
			Object.Destroy( pnl );
			serverCreatedUI.Remove( pnl );
		}
	}

#endif
}
