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
        var str = msg.read.StringRaw();

        if ( string.IsNullOrEmpty( str ) ) return;

        var jsonArray = JSON.Array.Parse( str );

        if ( jsonArray == null ) return;

        foreach ( var value in jsonArray )
        {
            var json = value.Obj;

            var parentPanel = FindPanel( json.GetString( "parent", "Overlay" ) );
            if ( parentPanel == null )
            {
                Debug.LogWarning( "AddUI: Unknown Parent for \""+ json.GetString( "name", "AddUI CreatedPanel" ) + "\": " + json.GetString( "parent", "Overlay" ) );
                return;
            }

            var allowUpdate = json.GetBoolean("update", false);
            var gameObjectName = json.GetString("name", "AddUI CreatedPanel");
            GameObject go = null;

            if (allowUpdate && json.ContainsKey("name"))
            {
                go = AllUi.Find(p => p != null && p.name == gameObjectName);
            }

            if(allowUpdate && go == null)
                return;
                
            
            if (go == null)
            {
                go = new GameObject(gameObjectName, typeof(RectTransform));
                go.transform.SetParent(parentPanel.transform, false);
            RegisterUi( go );

            var rt = go.GetComponent<RectTransform>();
            if ( rt )
            {
                rt.anchorMin = new Vector2( 0, 0 );
                rt.anchorMax = new Vector2( 1, 1 );
                rt.offsetMin = new Vector2( 0, 0 );
                rt.offsetMax = new Vector2( 1, 1 );
            }
            }

            foreach ( var component in json.GetArray( "components" ) )
            {
                CreateComponents( go, component.Obj, allowUpdate );
            }

            if ( json.ContainsKey( "fadeOut" ) )
            {
                go.AddComponent<FadeOut>().duration = json.GetFloat( "fadeOut", 0 );
            }
        }
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

    

    private void CreateComponents( GameObject go, JSON.Object obj, bool allowUpdate )
    {
        bool ShouldUpdateField(string fieldName)
        {
            return !allowUpdate || obj.ContainsKey(fieldName);
        }
        
        T GetOrAddComponent<T>() where T : Component
        {
            if (allowUpdate && go.TryGetComponent(out T component))
            {
                return component;
            }

            return go.AddComponent<T>();
        }
        
        //
        // This is the 'stupid' but 'safe & predictable way of doing this.
        //
        switch ( obj.GetString( "type", "UnityEngine.UI.Text" ) )
        {
            case "UnityEngine.UI.Text":
                {
                    var c = GetOrAddComponent<UnityEngine.UI.Text>();
                    if(ShouldUpdateField("text"))
                        c.text = obj.GetString( "text",  "Text" );
                    if(ShouldUpdateField("fontSize"))
                        c.fontSize = obj.GetInt( "fontSize", 14 );
                    if(ShouldUpdateField("font"))
                        c.font = FileSystem.Load<Font>( "Assets/Content/UI/Fonts/" + obj.GetString( "font", "RobotoCondensed-Bold.ttf" ) );
                    if(ShouldUpdateField("align"))
                        c.alignment = ParseEnum( obj.GetString( "align" ), TextAnchor.UpperLeft );
                    if(ShouldUpdateField("color"))
                        c.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );
                    GraphicComponentCreated( c, obj );
                    break;
                }

            case "UnityEngine.UI.Image":
                {
                    var c = GetOrAddComponent<UnityEngine.UI.Image>();
                    if(ShouldUpdateField("sprite"))
                        c.sprite = FileSystem.Load<Sprite>( obj.GetString( "sprite", "Assets/Content/UI/UI.Background.Tile.psd" ) );
                    if(ShouldUpdateField("material"))
                        c.material = FileSystem.Load<Material>( obj.GetString( "material", "Assets/Icons/IconMaterial.mat" ) );
                    if(ShouldUpdateField("color"))
                        c.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );
                    if(ShouldUpdateField("imagetype"))
                        c.type = ParseEnum( obj.GetString( "imagetype", "Simple" ), UnityEngine.UI.Image.Type.Simple );

                    if ( obj.ContainsKey( "png" ) && uint.TryParse( obj.GetString( "png" ), out var id ) )
                    {
                        SetImageFromServer( c, id );
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
                                var requestedSkin = obj.GetInt( "skinid" );
                                var skin = itemdef.skins.FirstOrDefault( x => x.id == requestedSkin );
                                if ( skin.id == requestedSkin )
                                {
                                    c.sprite = skin.invItem.icon;
                                }
                                else
                                {
                                    var workshopSprite = WorkshopIconLoader.Find( (ulong)requestedSkin );
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
                    var c = GetOrAddComponent<UnityEngine.UI.RawImage>();
                    if(ShouldUpdateField("sprite"))
                        c.texture = FileSystem.Load<Texture>( obj.GetString( "sprite", "Assets/Icons/rust.png" ) );
                    if(ShouldUpdateField("color"))
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
                        SetImageFromServer( c, id );
                    }

                    GraphicComponentCreated( c, obj );

                    break;
                }

            case "UnityEngine.UI.Button":
                {
                    var c = GetOrAddComponent<UnityEngine.UI.Button>();

                    if ( obj.ContainsKey( "command" ) )
                    {
                        var cmd = obj.GetString( "command" );
                        if(allowUpdate)
                            c.onClick.RemoveAllListeners();
                        c.onClick.AddListener( () => { ConsoleNetwork.ClientRunOnServer( cmd ); } );
                    }

                    if ( obj.ContainsKey( "close" ) )
                    {
                        var pnlName = obj.GetString( "close" );
                        if(allowUpdate)
                            c.onClick.RemoveAllListeners();
                        c.onClick.AddListener( () => { DestroyPanel( pnlName ); } );
                    }

                    // bg image
                    var img = GetOrAddComponent<UnityEngine.UI.Image>();
                    if(ShouldUpdateField("sprite"))
                        img.sprite = FileSystem.Load<Sprite>( obj.GetString( "sprite", "Assets/Content/UI/UI.Background.Tile.psd" ) );
                    if(ShouldUpdateField("material"))
                        img.material = FileSystem.Load<Material>( obj.GetString( "material", "Assets/Icons/IconMaterial.mat" ) );
                    if(ShouldUpdateField("color"))
                        img.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );
                    if(ShouldUpdateField("imagetype"))
                        img.type = ParseEnum( obj.GetString( "imagetype", "Simple" ), UnityEngine.UI.Image.Type.Simple );

                    c.image = img;

                    GraphicComponentCreated( img, obj );

                    break;
                }

            case "UnityEngine.UI.Outline":
                {
                    var c = GetOrAddComponent<UnityEngine.UI.Outline>();
                    if(ShouldUpdateField("color"))
                        c.effectColor = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );
                    if(ShouldUpdateField("distance"))
                        c.effectDistance = Vector2Ex.Parse( obj.GetString( "distance", "1.0 -1.0" ) );
                    c.useGraphicAlpha = obj.ContainsKey( "useGraphicAlpha" );
                    break;
                }

            case "UnityEngine.UI.InputField":
                {
                    var t = GetOrAddComponent<UnityEngine.UI.Text>();
                    if(ShouldUpdateField("fontSize"))
                        t.fontSize = obj.GetInt( "fontSize", t.fontSize );
                    if(ShouldUpdateField("font"))
                        t.font = FileSystem.Load<Font>( "Assets/Content/UI/Fonts/" + obj.GetString( "font", "RobotoCondensed-Bold.ttf" ) );
                    if(ShouldUpdateField("align"))
                        t.alignment = ParseEnum( obj.GetString( "align" ), TextAnchor.UpperLeft );
                    if(ShouldUpdateField("color"))
                        t.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );

                    var c = GetOrAddComponent<UnityEngine.UI.InputField>();
                    c.textComponent = t;
                    if(ShouldUpdateField("characterLimit"))
                        c.characterLimit = obj.GetInt( "characterLimit", allowUpdate ? c.characterLimit : 0 );

                    if ( obj.ContainsKey( "command" ) )
                    {
                        var cmd = obj.GetString( "command" );
                        if(allowUpdate)
                            c.onEndEdit.RemoveAllListeners();
                        c.onEndEdit.AddListener( ( value ) => { ConsoleNetwork.ClientRunOnServer( cmd + " " + value ); } );
                    }
                    if(ShouldUpdateField("text"))
                        c.text = obj.GetString("text", "Text");
                    if(ShouldUpdateField("readOnly"))
			            c.readOnly = obj.GetBoolean("readOnly", false);
                    if(ShouldUpdateField("lineType"))
                        c.lineType = ParseEnum(obj.GetString("lineType", "SingleLine"), InputField.LineType.SingleLine);
                    if ( obj.ContainsKey( "password" ) )
                    {
                        c.inputType = UnityEngine.UI.InputField.InputType.Password;
                    }

                    if (obj.ContainsKey("needsKeyboard"))
                    {
                        go.AddComponent<NeedsKeyboardInputField>();
                    }

                    GraphicComponentCreated( t, obj );

                    break;
                }

            case "NeedsCursor":
                {
                    GetOrAddComponent<NeedsCursor>();
                    break;
                }

            case "RectTransform":
                {
                    var rt = go.GetComponent<RectTransform>();
                    if ( rt )
                    {
                        if(ShouldUpdateField("anchormin"))
                            rt.anchorMin = Vector2Ex.Parse( obj.GetString( "anchormin", "0.0 0.0" ) );
                        if(ShouldUpdateField("anchormax"))
                            rt.anchorMax = Vector2Ex.Parse( obj.GetString( "anchormax", "1.0 1.0" ) );
                        if(ShouldUpdateField("offsetmin"))
                            rt.offsetMin = Vector2Ex.Parse( obj.GetString( "offsetmin", "0.0 0.0" ) );
                        if(ShouldUpdateField("offsetmax"))
                            rt.offsetMax = Vector2Ex.Parse( obj.GetString( "offsetmax", "1.0 1.0" ) );
                    }
                    break;
                }

            case "Countdown":
                {
                    var c = GetOrAddComponent<Countdown>();
                    if(ShouldUpdateField("endTime"))
                        c.endTime = obj.GetInt( "endTime", allowUpdate ? c.endTime : 0 );
                    if(ShouldUpdateField("startTime"))
                        c.startTime = obj.GetInt( "startTime", allowUpdate ? c.startTime : 0 );
                    if(ShouldUpdateField("step"))
                        c.step = obj.GetInt( "step", allowUpdate ? c.step : 1 );

                    if ( obj.ContainsKey( "command" ) )
                    {
                        c.command = obj.GetString( "command" );
                    }

                    break;
                }
            case "NeedsKeyboard":
                {
                    GetOrAddComponent<NeedsKeyboard>();
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


        var texture = www.texture;
        if ( texture == null || c == null )
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

        var fadeOut = panel.GetComponent<FadeOut>();
        if ( fadeOut )
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
