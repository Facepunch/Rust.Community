using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using System.IO;
using Rust.Workshop;

#if CLIENT

public partial class CommunityEntity
{
    private static List<GameObject> AllUi = new List<GameObject>();
    private static Dictionary<string, GameObject> UiDict = new Dictionary<string, GameObject>();

    public static void DestroyServerCreatedUI()
    {
        foreach ( var go in AllUi )
        {
            if ( !go ) continue;
            GameObject.Destroy( go );
        }

        AllUi.Clear();
        UiDict.Clear();
        requestingTextureImages.Clear();
        UnloadTextureCache();
    }

    public void SetVisible( bool b )
    {
        foreach ( var c in GetComponentsInChildren<Canvas>( true ) )
        {
            c.gameObject.SetActive( b );
        }
    }

    private static void RegisterUi( GameObject go )
    {
        AllUi.Add( go );
        UiDict[go.name] = go;
    }

    [RPC_Client]
    public void AddUI( RPCMessage msg )
    {
        if (Client.IsPlayingDemo && !ConVar.Demo.showCommunityUI)
            return;
        var str = msg.read.StringRaw();

        if ( string.IsNullOrEmpty( str ) ) return;

        var jsonArray = JSON.Array.Parse( str );

        if ( jsonArray == null ) return;

        foreach ( var value in jsonArray )
        {
            var json = value.Obj;
            if ( json.ContainsKey( "destroyUi" ) )
            {
                DestroyPanel( json.GetString( "destroyUi", "AddUI CreatedPanel" ) );
            }
            var parentPanel = FindPanel( json.GetString( "parent", "Overlay" ) );
            if ( parentPanel == null )
            {
                Debug.LogWarning( "AddUI: Unknown Parent for \""+ json.GetString( "name", "AddUI CreatedPanel" ) + "\": " + json.GetString( "parent", "Overlay" ) );
                return;
            }

            var go = new GameObject( json.GetString( "name", "AddUI CreatedPanel" ) , typeof(RectTransform) );
            go.transform.SetParent( parentPanel.transform, false );
            RegisterUi( go );

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

            Animation[] animations = go.GetComponents<Animation>();
			if(animations.Length > 0){
                for(var i = 0; i < animations.Length; i++){
                    animations[i].StartAnimation();
                }
			}
            if ( json.ContainsKey( "addCanvas" ) )
            {
                go.AddComponent<Canvas>();
                go.AddComponent<GraphicRaycaster>();
            }

            if ( json.ContainsKey( "fadeOut" ) )
            {
                go.AddComponent<FadeOut>().duration = json.GetFloat( "fadeOut", 0 );
            }
        }
        ApplyMouseListeners();
    }

    private GameObject FindPanel( string name )
    {
        GameObject panel;
        if ( UiDict.TryGetValue( name, out panel ) )
        {
            return panel;
        }

        var tx = transform.FindChildRecursive( name );
        if ( tx ) return tx.gameObject;

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
                    c.alignment = ParseEnum( obj.GetString( "align" ), TextAnchor.UpperLeft );
                    c.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );
                    c.verticalOverflow = ParseEnum( obj.GetString( "verticalOverflow", "Truncate" ), VerticalWrapMode.Truncate );
                    GraphicComponentCreated( c, obj );
                    break;
                }

            case "UnityEngine.UI.Image":
                {
                    var c = go.AddComponent<UnityEngine.UI.Image>();
                    c.sprite = FileSystem.Load<Sprite>( obj.GetString( "sprite", "Assets/Content/UI/UI.Background.Tile.psd" ) );
                    c.material = FileSystem.Load<Material>( obj.GetString( "material", "Assets/Icons/IconMaterial.mat" ) );
                    c.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );
                    c.type = ParseEnum( obj.GetString( "imagetype", "Simple" ), UnityEngine.UI.Image.Type.Simple );

                    if ( obj.ContainsKey( "png" ) && uint.TryParse( obj.GetString( "png" ), out var id ) )
                    {
                        ApplyTextureToImage( c, id );
                    }

                    if ( obj.ContainsKey( "itemid" ) )
                    {
                        var itemdef = ItemManager.FindItemDefinition( obj.GetInt( "itemid" ) );
                        if ( itemdef != null )
                        {
                            c.material = null;
                            c.sprite = itemdef.iconSprite;

                            if ( obj.ContainsKey( "skinid" ) )
                            {
                                var requestedSkin = (ulong)obj.GetNumber("skinid" );
                                var skin = itemdef.skins.FirstOrDefault( x => x.id == (int)requestedSkin );
                                if ( skin.id == (int)requestedSkin )
                                {
                                    c.sprite = skin.invItem.icon;
                                }
                                else
                                {
                                    var workshopSprite = WorkshopIconLoader.Find(requestedSkin, null, () =>
                                    {
                                        if (c != null)
                                            c.sprite = WorkshopIconLoader.Find(requestedSkin);
                                    });
                                    if ( workshopSprite != null )
                                    {
                                        c.sprite = workshopSprite;
                                    }
                                }
                            }
                        }
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
                        Rust.Global.Runner.StartCoroutine( LoadTextureFromWWW( c, obj.GetString( "url" ) ) );
                    }

                    if ( obj.ContainsKey( "png" ) && uint.TryParse( obj.GetString( "png" ), out var id ) )
                    {
                        ApplyTextureToImage( c, id );
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
                        c.onClick.AddListener( () => { ConsoleNetwork.ClientRunOnServer( cmd ); } );
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
                    img.type = ParseEnum( obj.GetString( "imagetype", "Simple" ), UnityEngine.UI.Image.Type.Simple );

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

            case "UnityEngine.UI.InputField":
                {
                    var t = go.AddComponent<UnityEngine.UI.Text>();
                    t.fontSize = obj.GetInt( "fontSize", 14 );
                    t.font = FileSystem.Load<Font>( "Assets/Content/UI/Fonts/" + obj.GetString( "font", "RobotoCondensed-Bold.ttf" ) );
                    t.alignment = ParseEnum( obj.GetString( "align" ), TextAnchor.UpperLeft );
                    t.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );

                    var c = go.AddComponent<UnityEngine.UI.InputField>();
                    c.textComponent = t;
                    c.characterLimit = obj.GetInt( "characterLimit", 0 );

                    if ( obj.ContainsKey( "command" ) )
                    {
                        var cmd = obj.GetString( "command" );
                        c.onEndEdit.AddListener( ( value ) => { ConsoleNetwork.ClientRunOnServer( cmd + " " + value ); } );
                    }
                    c.text = obj.GetString("text", string.Empty);
			        c.readOnly = obj.GetBoolean("readOnly", false);
                    c.lineType = ParseEnum(obj.GetString("lineType", "SingleLine"), InputField.LineType.SingleLine);
                    if ( obj.ContainsKey( "password" ) )
                    {
                        c.inputType = UnityEngine.UI.InputField.InputType.Password;
                    }

                    if (obj.ContainsKey("needsKeyboard"))
                    {
                        go.AddComponent<NeedsKeyboardInputField>();
                    }

                    //blocks keyboard input the same as NeedsKeyboard, but is used for UI in the inventory/crafting
                    if (obj.ContainsKey("hudMenuInput"))
                    {
                        go.AddComponent<HudMenuInput>();
                    }

                    if (obj.ContainsKey("autofocus"))
                    {
                        c.Select();
                    }

                    GraphicComponentCreated( t, obj );

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

            case "Countdown":
                {
                    var c = go.AddComponent<Countdown>();
                    c.endTime = obj.GetInt( "endTime", 0 );
                    c.startTime = obj.GetInt( "startTime", 0 );
                    c.step = obj.GetInt( "step", 1 );

                    if ( obj.ContainsKey( "command" ) )
                    {
                        c.command = obj.GetString( "command" );
                    }

                    break;
                }
            case "NeedsKeyboard":
                {
                    go.AddComponent<NeedsKeyboard>();
                    break;
                }
            case "UnityEngine.UI.RectMask2D":
                {
                    go.AddComponent<RectMask2D>();
                    break;
                }
            case "UnityEngine.UI.Mask":
                {
                    var c = go.AddComponent<Mask>();
                    c.showMaskGraphic = obj.GetBoolean("showMaskGraphic", true);
                    break;
                }
			case "Animation":
                {
                    Animation anim = null;
                    Animation[] animations = go.GetComponents<Animation>();
                    string mouseTarget = obj.GetString("mouseTarget", "");
                    if(animations.Length == 0){
                        // do nothing
                    } else if(!string.IsNullOrEmpty(mouseTarget)){
                        // find an existing animation component with the same mouse target, if not create one
        				anim = animations.FirstOrDefault((animation) => animation.mouseTarget == mouseTarget);
                    }else{
                        anim = animations[0];
                    }
                    
                    if(anim == null){
                        anim = go.AddComponent<Animation>();
                        if(!string.IsNullOrEmpty(mouseTarget)) ScheduleMouseListener(mouseTarget, anim);
                    }

					foreach(var prop in obj.GetArray("properties"))
					{
                        var propobj = prop.Obj;
                        var trigger = propobj.GetString("trigger", "OnCreate");
                        
                        if(!anim.ValidTrigger(trigger)) trigger = "OnCreate";
                        anim.properties[trigger].Add(new AnimationProperty{
							duration = propobj.GetFloat("duration", 0f),
							delay = propobj.GetFloat("delay", 0f),
							repeat = propobj.GetInt("repeat", 0),
							repeatDelay = propobj.GetFloat("repeatDelay", 0f),
							easing = propobj.GetString("easing", "Linear"),
							type = propobj.GetString("type", null),
							from = propobj.GetString("from", ""),
							to = propobj.GetString("to", null),
        					trigger = trigger
						});
					}
					break;
				}
        }
    }
    
    private static T ParseEnum<T>(string value, T defaultValue)
        where T : struct, System.Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        return System.Enum.TryParse<T>(value, true, out var parsedValue) ? parsedValue : defaultValue;
    }

    private void GraphicComponentCreated( UnityEngine.UI.Graphic c, JSON.Object obj )
    {
        if ( obj.ContainsKey( "fadeIn" ) )
        {
            c.canvasRenderer.SetAlpha( 0f );
            c.CrossFadeAlpha( 1f, obj.GetFloat( "fadeIn", 0 ), true );
        }
    }

    private IEnumerator LoadTextureFromWWW( UnityEngine.UI.RawImage c, string p )
    {
        var www = new WWW( p.Trim() );

        while ( !www.isDone )
        {
            yield return null;
        }

        if ( !string.IsNullOrEmpty( www.error ) )
        {
            Debug.Log( "Error downloading image: " + p + " (" + www.error + ")" );
            www.Dispose();
            yield break;
        }


        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        www.LoadImageIntoTexture(texture);
        if ( c == null )
        {
            Debug.Log( "Error downloading image: " + p + " (not an image)" );
            www.Dispose();
            yield break;
        }

        c.texture = texture;
        www.Dispose();
    }


    [RPC_Client]
    public void DestroyUI( RPCMessage msg )
    {
        DestroyPanel( msg.read.StringRaw() );
    }

    private void DestroyPanel( string pnlName )
    {
        GameObject panel;
        if ( !UiDict.TryGetValue( pnlName, out panel ) ) return;

        UiDict.Remove( pnlName );

        if ( !panel )
            return;

        Animation[] animations = panel.GetComponents<Animation>();
        var fadeOut = panel.GetComponent<FadeOut>();
        if(animations.Length > 0)
        {
            for(var i = 0; i < animations.Length; i++){
                if(animations[i].HasForTrigger("OnDestroy")) animations[i].Kill();
            }
        }
        else if ( fadeOut )
        {
            fadeOut.FadeOutAndDestroy();
        }
        else
        {
            Object.Destroy( panel );
        }
    }
}

#endif
