using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using System.IO;

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
        var str = msg.read.String();

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

            var go = new GameObject( json.GetString( "name", "AddUI CreatedPanel" ) );
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
            if (json.ContainsKey( "rotate" ) )
			{
				go.transform.Rotate(Vector3Ex.Parse(json.GetString( "rotate", "0.0 0.0 0.0" ) ) );
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
                        SetImageFromServer( c, uint.Parse( obj.GetString( "png" ) ) );
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

                    if ( obj.ContainsKey( "png" ) )
                    {
                        SetImageFromServer( c, uint.Parse( obj.GetString( "png" ) ) );
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

            case "UnityEngine.UI.InputField":
                {
                    var t = go.AddComponent<UnityEngine.UI.Text>();
                    t.text = obj.GetString( "text", "Text" );
                    t.fontSize = obj.GetInt( "fontSize", 14 );
                    t.font = FileSystem.Load<Font>( "Assets/Content/UI/Fonts/" + obj.GetString( "font", "RobotoCondensed-Bold.ttf" ) );
                    t.alignment = (TextAnchor)System.Enum.Parse( typeof( TextAnchor ), obj.GetString( "align", "UpperLeft" ) );
                    t.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );

                    var c = go.AddComponent<UnityEngine.UI.InputField>();
                    c.textComponent = t;
                    c.characterLimit = obj.GetInt( "characterLimit", 0 );

                    if ( obj.ContainsKey( "command" ) )
                    {
                        var cmd = obj.GetString( "command" );
                        c.onEndEdit.AddListener( ( value ) => { ConsoleNetwork.ClientRunOnServer( cmd + " " + value ); } );
                    }

                    if ( obj.ContainsKey( "password" ) )
                    {
                        c.inputType = UnityEngine.UI.InputField.InputType.Password;
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
        DestroyPanel( msg.read.String() );
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
