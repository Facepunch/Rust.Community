using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using System.IO;
using Rust.Workshop;
using TMPro;

#if CLIENT

public partial class CommunityEntity
{
    private static List<GameObject> AllUi = new List<GameObject>();
    private static Dictionary<string, GameObject> UiDict = new Dictionary<string, GameObject>();

    private static List<string> ScrollViews = new List<string>();

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
		ScrollViews.Clear();
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
        UiDict[ go.name ] = go;
    }

    [RPC_Client]
    public void AddUI( RPCMessage msg )
    {
        if ( Client.IsPlayingDemo && !ConVar.Demo.showCommunityUI )
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
            var gameObjectName = json.GetString( "name", "AddUI CreatedPanel" );

            // ensuring that unnamed panels are given a unique name
            if ( parentPanel == null )
            {
                Debug.LogWarning( "AddUI: Unknown Parent for \"" + gameObjectName + "\": " + json.GetString( "parent", "Overlay" ) );
                return;
            }

            var allowUpdate = json.GetBoolean( "update", false );
            GameObject go = null;

            if ( allowUpdate && json.ContainsKey( "name" ) )
            {
                go = FindPanel( gameObjectName, false );
            }

            if ( allowUpdate && go == null )
            {
                Debug.LogWarning( $"[AddUI] Unable to update object '{gameObjectName}': can't be found" );
                return;
            }


            if ( go == null )
            {
                go = new GameObject( gameObjectName, typeof( RectTransform ) );
                go.transform.SetParent( parentPanel.transform, false );
                RegisterUi( go );

                var rt = go.GetComponent<RectTransform>();
                if ( rt )
                    FitParent(rt);
            }

            foreach ( var component in json.GetArray( "components" ) )
            {
                CreateComponents( go, component.Obj, allowUpdate );
            }

            if ( json.ContainsKey( "fadeOut" ) )
                Animation.AddFadeOut(go, json.GetFloat( "fadeOut", 0 ), json.GetBoolean( "fadeAsGroup", false ));

            var anim = go.GetComponent<Animation>();
            if(anim != null)
                Animation.AddPendingAnim(anim);

            if ( json.ContainsKey( "addCanvas" ) )
            {
                go.AddComponent<Canvas>();
                go.AddComponent<GraphicRaycaster>();
            }
        }
        Animation.InitPendingAnims();
    }

    private GameObject FindPanel( string name, bool allowScrollviews = true )
    {
        // if its a scrollview we're looking for, return the child if it exists
        if(allowScrollviews && ScrollViews.Contains(name)){
			var scrollContent = FindPanel(name + "___Content");
			if(scrollContent != null)
				return scrollContent;
		}

        GameObject panel;
        if ( UiDict.TryGetValue( name, out panel ) )
        {
            return panel;
        }

        var tx = transform.FindChildRecursive( name );
        if ( tx ) return tx.gameObject;

        return null;
    }

    // Move this local function outside CreateComponents()
    private static void HandleEnableState( JSON.Object json, Behaviour component )
    {
        if ( json.TryGetBoolean( "enabled", out var result ) )
        {
            component.enabled = result;
        }
    }

    private void CreateComponents( GameObject go, JSON.Object obj, bool allowUpdate )
    {
        // Unsure if local functions allocate like lambdas do, this is just for modding so not a big deal & can double check once it builds
        bool ShouldUpdateField( string fieldName )
        {
            return !allowUpdate || obj.ContainsKey( fieldName );
        }

        T GetOrAddComponent<T>() where T : Component
        {
            if ( allowUpdate && go.TryGetComponent( out T component ) )
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
                    HandleEnableState( obj, c );
                    if ( ShouldUpdateField( "text" ) )
                        c.text = obj.GetString( "text", "Text" );
                    if ( ShouldUpdateField( "fontSize" ) )
                        c.fontSize = obj.GetInt( "fontSize", 14 );
                    if ( ShouldUpdateField( "font" ) )
                    {
                        c.font = LoadFont(obj.GetString("font", strDEFAULT: "RobotoCondensed-Bold.ttf"));
                    }
                    if ( ShouldUpdateField( "align" ) )
                        c.alignment = ParseEnum( obj.GetString( "align" ), TextAnchor.UpperLeft );
                    if ( ShouldUpdateField( "color" ) )
                        c.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );
                    if ( ShouldUpdateField( "verticalOverflow" ) )
                        c.verticalOverflow = ParseEnum( obj.GetString( "verticalOverflow", "Truncate" ), VerticalWrapMode.Truncate );

                    GraphicComponentCreated( c, obj );
                    break;
                }

            case "UnityEngine.UI.Image":
                {
                    var c = GetOrAddComponent<UnityEngine.UI.Image>();
                    HandleEnableState( obj, c );
                    if ( ShouldUpdateField( "sprite" ) )
                        c.sprite = FileSystem.Load<Sprite>( obj.GetString( "sprite", "Assets/Content/UI/UI.Background.Tile.psd" ) );
                    if ( ShouldUpdateField( "material" ) )
                        c.material = FileSystem.Load<Material>( obj.GetString( "material", "Assets/Icons/IconMaterial.mat" ) );
                    if ( ShouldUpdateField( "color" ) )
                        c.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );
                    if ( ShouldUpdateField( "imagetype" ) )
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
                                var requestedSkin = (ulong)obj.GetNumber( "skinid" );
                                var skin = itemdef.skins.FirstOrDefault( x => x.id == (int)requestedSkin );
                                if ( skin.id == (int)requestedSkin )
                                {
                                    c.sprite = skin.invItem.icon;
                                }
                                else
                                {
                                    var workshopSprite = WorkshopIconLoader.Find( requestedSkin, null, () =>
                                    {
                                        if ( c != null )
                                            c.sprite = WorkshopIconLoader.Find( requestedSkin );
                                    } );
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
                    HandleEnableState( obj, c );
                    if ( ShouldUpdateField( "sprite" ) )
                        c.texture = FileSystem.Load<Texture>( obj.GetString( "sprite", "Assets/Icons/rust.png" ) );
                    if ( ShouldUpdateField( "color" ) )
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

                    if ( obj.ContainsKey( "steamid" ) )
                    {
                        var steamidString = obj.GetString( "steamid" );
                        if(ulong.TryParse(steamidString, out var steamId))
                            c.texture = SingletonComponent<SteamClientWrapper>.Instance.GetAvatarTexture(steamId);
                    }

                    GraphicComponentCreated( c, obj );

                    break;
                }

            case "UnityEngine.UI.Button":
                {
                    var c = GetOrAddComponent<UnityEngine.UI.Button>();
                    HandleEnableState( obj, c );

                    if ( obj.ContainsKey( "command" ) )
                    {
                        var cmd = obj.GetString( "command" );
                        if ( allowUpdate )
                            c.onClick.RemoveAllListeners();
                        c.onClick.AddListener( () => { ConsoleNetwork.ClientRunOnServer( cmd ); } );
                    }

                    if ( obj.ContainsKey( "close" ) )
                    {
                        var pnlName = obj.GetString( "close" );
                        if ( allowUpdate )
                            c.onClick.RemoveAllListeners();
                        c.onClick.AddListener( () => { DestroyPanel( pnlName ); } );
                    }

                    // bg image
                    var img = GetOrAddComponent<UnityEngine.UI.Image>();
                    if ( ShouldUpdateField( "sprite" ) )
                        img.sprite = FileSystem.Load<Sprite>( obj.GetString( "sprite", "Assets/Content/UI/UI.Background.Tile.psd" ) );
                    if ( ShouldUpdateField( "material" ) )
                        img.material = FileSystem.Load<Material>( obj.GetString( "material", "Assets/Icons/IconMaterial.mat" ) );
                    if ( ShouldUpdateField( "color" ) )
                        img.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );
                    if ( ShouldUpdateField( "imagetype" ) )
                        img.type = ParseEnum( obj.GetString( "imagetype", "Simple" ), UnityEngine.UI.Image.Type.Simple );

                    c.image = img;

                    GraphicComponentCreated( img, obj );

                    break;
                }

            case "UnityEngine.UI.Outline":
                {
                    var c = GetOrAddComponent<UnityEngine.UI.Outline>();
                    HandleEnableState( obj, c );
                    if ( ShouldUpdateField( "color" ) )
                        c.effectColor = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );
                    if ( ShouldUpdateField( "distance" ) )
                        c.effectDistance = Vector2Ex.Parse( obj.GetString( "distance", "1.0 -1.0" ) );
                    c.useGraphicAlpha = obj.ContainsKey( "useGraphicAlpha" );
                    break;
                }

            case "UnityEngine.UI.InputField":
                {
                    var t = GetOrAddComponent<UnityEngine.UI.Text>();
                    HandleEnableState( obj, t );
                    if ( ShouldUpdateField( "fontSize" ) )
                        t.fontSize = obj.GetInt( "fontSize", allowUpdate ? t.fontSize : 14 );
                    if ( ShouldUpdateField( "font" ) )
                    {
                        t.font = LoadFont(obj.GetString("font", strDEFAULT: "RobotoCondensed-Bold.ttf"));
                    }
                    if ( ShouldUpdateField( "align" ) )
                        t.alignment = ParseEnum( obj.GetString( "align" ), TextAnchor.UpperLeft );
                    if ( ShouldUpdateField( "color" ) )
                        t.color = ColorEx.Parse( obj.GetString( "color", "1.0 1.0 1.0 1.0" ) );

                    var c = GetOrAddComponent<UnityEngine.UI.InputField>();
                    HandleEnableState( obj, c );
                    c.textComponent = t;
                    if ( ShouldUpdateField( "characterLimit" ) )
                        c.characterLimit = obj.GetInt( "characterLimit", allowUpdate ? c.characterLimit : 0 );

                    if ( obj.ContainsKey( "command" ) )
                    {
                        var cmd = obj.GetString( "command" );
                        if ( allowUpdate )
                            c.onEndEdit.RemoveAllListeners();
                        c.onEndEdit.AddListener( ( value ) => { ConsoleNetwork.ClientRunOnServer( cmd + " " + value ); } );
                    }

                    if ( ShouldUpdateField( "text" ) )
                        c.text = obj.GetString( "text", "Text" );
                    if ( ShouldUpdateField( "readOnly" ) )
                        c.readOnly = obj.GetBoolean( "readOnly", false );
                    if ( ShouldUpdateField( "lineType" ) )
                        c.lineType = ParseEnum( obj.GetString( "lineType", "SingleLine" ), InputField.LineType.SingleLine );

                    if ( obj.TryGetBoolean( "password", out var password ) )
                    {
                        c.inputType = password ? InputField.InputType.Password : InputField.InputType.Standard;
                    }

                    if ( obj.TryGetBoolean( "needsKeyboard", out var needsKeyboard ) )
                    {
                        var comp = GetOrAddComponent<NeedsKeyboardInputField>();
                        comp.enabled = needsKeyboard;
                    }

                    //blocks keyboard input the same as NeedsKeyboard, but is used for UI in the inventory/crafting
                    if ( obj.TryGetBoolean( "hudMenuInput", out var hudMenuInput ) )
                    {
                        var comp = GetOrAddComponent<HudMenuInput>();
                        comp.enabled = hudMenuInput;
                    }

                    if ( obj.ContainsKey( "autofocus" ) )
                    {
                        c.Select();
                    }

                    GraphicComponentCreated( t, obj );

                    break;
                }

            case "NeedsCursor":
                {
                    var c = GetOrAddComponent<NeedsCursor>();
                    HandleEnableState( obj, c );
                    break;
                }

            case "RectTransform":
                {
                    var rt = go.GetComponent<RectTransform>();
                    if ( rt )
                    {
                        if ( ShouldUpdateField( "anchormin" ) )
                            rt.anchorMin = Vector2Ex.Parse( obj.GetString( "anchormin", "0.0 0.0" ) );
                        if ( ShouldUpdateField( "anchormax" ) )
                            rt.anchorMax = Vector2Ex.Parse( obj.GetString( "anchormax", "1.0 1.0" ) );
                        if ( ShouldUpdateField( "offsetmin" ) )
                            rt.offsetMin = Vector2Ex.Parse( obj.GetString( "offsetmin", "0.0 0.0" ) );
                        if ( ShouldUpdateField( "offsetmax" ) )
                            rt.offsetMax = Vector2Ex.Parse( obj.GetString( "offsetmax", "1.0 1.0" ) );
                    }
                    break;
                }

            case "Countdown":
                {
                    var c = GetOrAddComponent<Countdown>();
                    HandleEnableState( obj, c );
                    if ( ShouldUpdateField( "endTime" ) )
                        c.endTime = obj.GetFloat( "endTime", allowUpdate ? c.endTime : 0f );
                    if ( ShouldUpdateField( "startTime" ) )
                        c.startTime = obj.GetFloat( "startTime", allowUpdate ? c.startTime : 0f );
                    if ( ShouldUpdateField( "step" ) )
                        c.step = obj.GetFloat( "step", allowUpdate ? c.step : 1f );
                    if ( ShouldUpdateField( "interval" ) ) {
                        c.interval = obj.GetFloat( "interval", allowUpdate ? c.interval : c.step );
                        if(allowUpdate)
                            c.Reset();
                    }
                    if ( ShouldUpdateField( "timerFormat" ) )
                        c.timerFormat = ParseEnum<Countdown.TimerFormat>(obj.GetString("timerFormat", "None"), allowUpdate ? c.timerFormat : Countdown.TimerFormat.None);
                    if ( ShouldUpdateField( "numberFormat" ) )
                        c.numberFormat = obj.GetString( "numberFormat", allowUpdate ? c.numberFormat : "0.####" );
                    if ( ShouldUpdateField( "destroyIfDone" ) )
                        c.destroyIfDone = obj.GetBoolean( "destroyIfDone", allowUpdate ? c.destroyIfDone : true);

                    if ( obj.ContainsKey( "command" ) )
                    {
                        c.command = obj.GetString( "command" );
                    }

                    break;
                }
            case "NeedsKeyboard":
                {
                    var c = GetOrAddComponent<NeedsKeyboard>();
                    HandleEnableState( obj, c );
                    break;
                }
            case "UnityEngine.UI.RectMask2D":
                {
                    var c = GetOrAddComponent<RectMask2D>();
                    if( ShouldUpdateField("maskSoftness") )
                        c.softness = Vector2Int.RoundToInt(Vector2Ex.Parse( obj.GetString( "maskSoftness", "0.0 0.0" )));

                    HandleEnableState( obj, c );
                    break;
                }
            case "UnityEngine.UI.Mask":
                {
                    var c = GetOrAddComponent<Mask>();
                    if( ShouldUpdateField("showMaskGraphic") )
                        c.showMaskGraphic = obj.GetBoolean("showMaskGraphic", true);

                    HandleEnableState( obj, c );
                    break;
                }
            case "Animation":
                {
                    // Moved Setup to its own function in CommunityEntity.UI.Animation.cs
                    Animation.ParseAnimation(obj, go, allowUpdate);
                    break;
                }
            case "UnityEngine.UI.ScrollView":
                {
                    var scrollRect = GetOrAddComponent<ScrollRect>();
                    HandleEnableState(obj, scrollRect);

                    if(!ScrollViews.Contains(go.name)) ScrollViews.Add(go.name);
                    // Adding a Canvas allows unity to isolate any changes inside the scrollrect, improving performance as the outer canvas wont need an update on scroll
                    var canvas = go.GetComponent<Canvas>();
                    if(!canvas){
                        canvas = go.AddComponent<Canvas>();
                        go.AddComponent<GraphicRaycaster>();
                    }

                    // already present if its being updated
                    if(!allowUpdate){
                        // add viewport as child component, dont register it as a ui panel though. this allows scrollbars to resize the viewport if autoHide is set to true
                        var viewportGO = new GameObject(go.name + "___Viewport");
                        var viewportRT = viewportGO.AddComponent<RectTransform>();
                        FitParent(viewportRT);
                        // this is required if the scrollbar shrinks the viewport, it ensures the viewport is pushed to the top left corner
                        // the default pivot would center it, meaning the scrollbar would partially cover it instead of being beside it
                        viewportRT.pivot = new Vector2(0f, 1f);
                        viewportRT.SetParent(go.transform, false);
                        var mask = viewportGO.AddComponent<RectMask2D>();
                    	scrollRect.viewport = viewportRT;

                        // if(obj.ContainsKey("maskSoftness"))
                        //     mask.softness = Vector2Int.RoundToInt(Vector2Ex.Parse( obj.GetString( "maskSoftness", "0.0 0.0" )));

                        // create & register content panel
                        var childGO = new GameObject(go.name + "___Content");
                        childGO.transform.SetParent(viewportGO.transform, false);
                        RegisterUi(childGO);
                        scrollRect.content = childGO.AddComponent<RectTransform>();
                    }

                    // initialize from the json object
                    if(ShouldUpdateField("contentTransform")){
                        var contentObj = obj.GetObject("contentTransform");
                        scrollRect.content.anchorMin = Vector2Ex.Parse( contentObj.GetString( "anchormin", "0.0 0.0" ) );
                        scrollRect.content.anchorMax = Vector2Ex.Parse( contentObj.GetString( "anchormax", "1.0 1.0" ) );
                        scrollRect.content.offsetMin = Vector2Ex.Parse( contentObj.GetString( "offsetmin", "0.0 0.0" ) );
			// we dont have to apply the shoddy offsetmax default here because no existing implementations rely on it
                        scrollRect.content.offsetMax = Vector2Ex.Parse( contentObj.GetString( "offsetmax", "0.0 0.0" ) );
                    }
                    if(ShouldUpdateField("horizontal"))
                        scrollRect.horizontal = obj.GetBoolean("horizontal", false);
                    if(ShouldUpdateField("vertical"))
                        scrollRect.vertical = obj.GetBoolean("vertical", false);

                    if(ShouldUpdateField("movementType"))
                        scrollRect.movementType = ParseEnum<ScrollRect.MovementType>(obj.GetString("movementType", "Clamped"), ScrollRect.MovementType.Clamped);

                    if(ShouldUpdateField("elasticity"))
                        scrollRect.elasticity = obj.GetFloat("elasticity", 0.1f);
                    if(ShouldUpdateField("inertia"))
                        scrollRect.inertia = obj.GetBoolean("inertia", false);
                    if(ShouldUpdateField("decelerationRate"))
                        scrollRect.decelerationRate = obj.GetFloat("decelerationRate", 0.135f);
                    if(ShouldUpdateField("scrollSensitivity"))
                        scrollRect.scrollSensitivity = obj.GetFloat("scrollSensitivity", 1f);

                    // add scrollbars if objects are present
                    GameObject barGO;
                    JSON.Object scrollObj;
                    bool invert;
                    bool hideUnlessNeeded;
                    Scrollbar scrollbar;
                    // dont need ShouldUpdateField here either
                    if(scrollRect.horizontal && obj.ContainsKey("horizontalScrollbar")){
                        barGO = new GameObject("Horizontal Scrollbar");
                        scrollObj = obj.GetObject("horizontalScrollbar");
                        invert = scrollObj.GetBoolean("invert", false);
                        hideUnlessNeeded = scrollObj.GetBoolean("autoHide", false);
                        scrollbar = barGO.AddComponent<Scrollbar>();
                        HandleEnableState(scrollObj, scrollbar);

                        barGO.transform.SetParent(go.transform, false);
                        scrollbar.direction = (invert ? Scrollbar.Direction.LeftToRight : Scrollbar.Direction.RightToLeft);
                        scrollRect.horizontalScrollbar = scrollbar;
                        if(hideUnlessNeeded)
                            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

                        BuildScrollbar(scrollbar, scrollObj, false);
                    }
                    // dont need ShouldUpdateField here either
                    if(scrollRect.vertical && obj.ContainsKey("verticalScrollbar")){
                        barGO = new GameObject("Vertical Scrollbar");
                        scrollObj = obj.GetObject("verticalScrollbar");
                        invert = scrollObj.GetBoolean("invert", false);
                        hideUnlessNeeded = scrollObj.GetBoolean("autoHide", false);
                        scrollbar = barGO.AddComponent<Scrollbar>();
                        HandleEnableState(scrollObj, scrollbar);

                        barGO.transform.SetParent(go.transform, false);
                        scrollbar.direction = (invert ? Scrollbar.Direction.TopToBottom : Scrollbar.Direction.BottomToTop);
                        scrollRect.verticalScrollbar = scrollbar;
                        if(hideUnlessNeeded)
                            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

                        BuildScrollbar(scrollbar, scrollObj, true);
                    }
					break;
                }
        }
    }

    private void BuildScrollbar(Scrollbar scrollbar, JSON.Object obj, bool vertical){
        // build the scrollbar handle
        var handle = new GameObject("Scrollbar Handle");
        var handleRT = handle.AddComponent<RectTransform>();
        FitParent(handleRT);
        handle.transform.SetParent(scrollbar.transform, false);//.SetParent(track.transform, false);
        var handleImage = handle.AddComponent<Image>();
        handleImage.sprite = FileSystem.Load<Sprite>( obj.GetString( "handleSprite", "assets/content/ui/ui.rounded.tga" ) );
        handleImage.type = Image.Type.Sliced;
        // target for color changes
        scrollbar.image = handleImage;
        scrollbar.handleRect = handleRT;

        // style the scollbar
        float size = obj.GetFloat("size", 20f);
        var block = scrollbar.colors;
        block.normalColor = ColorEx.Parse( obj.GetString( "handleColor", "0.15 0.15 0.15 1" ) ); // main color
        block.highlightedColor = ColorEx.Parse( obj.GetString( "highlightColor", "0.17 0.17 0.17 1" ) ); // hover
        block.pressedColor = ColorEx.Parse( obj.GetString( "pressedColor", "0.2 0.2 0.2 1" ) ); // press
        block.selectedColor = block.pressedColor; // never really used, but can still show up sometimes
        scrollbar.colors = block;

        // style the background track
        var image = scrollbar.gameObject.AddComponent<Image>();
        image.sprite = FileSystem.Load<Sprite>( obj.GetString( "trackSprite", "assets/content/ui/ui.background.tile.psd" ) );
        image.type = Image.Type.Sliced;
        image.color = ColorEx.Parse( obj.GetString( "trackColor", "0.09 0.09 0.09 1" ) ); // background

        // position the scrollbar
        var rt = scrollbar.GetComponent<RectTransform>();
        if(rt == null)
            return;
        if(vertical){
            // positions it along the right side
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(-size, 0f);
            rt.offsetMax = Vector2.zero;
        } else {
            // positions it along the bottom
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(0f, -size);
            rt.offsetMax = Vector2.zero;
        }
    }

    // sets the transform to a sensible default
    private void FitParent(RectTransform transform){
        transform.anchorMin = Vector2.zero;
        transform.anchorMax = Vector2.one;
        transform.offsetMin = Vector2.zero;
        transform.offsetMax = Vector2.one; // to preserve the shoddy offsetmax default that alot of existing UIs rely on
    }

    private static T ParseEnum<T>(string value, T defaultValue)
        where T : struct, System.Enum
    {
        if ( string.IsNullOrWhiteSpace( value ) ) return defaultValue;
        return System.Enum.TryParse<T>( value, true, out var parsedValue ) ? parsedValue : defaultValue;
    }

    private void GraphicComponentCreated( UnityEngine.UI.Graphic c, JSON.Object obj )
    {
        if ( obj.ContainsKey( "fadeIn" ) )
            Animation.AddFadeIn(c.gameObject, obj.GetFloat( "fadeIn", 0 ), obj.GetBoolean( "fadeAsGroup", false ));

        if ( obj.ContainsKey( "shouldRaycast" ) )
            c.raycastTarget = obj.GetBoolean("shouldRaycast", true);
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


        Texture2D texture = new Texture2D( 2, 2, TextureFormat.RGBA32, false );
        www.LoadImageIntoTexture( texture );
        if ( c == null )
        {
            Debug.Log( "Error downloading image: " + p + " (not an image)" );
            www.Dispose();
            yield break;
        }

        c.texture = texture;
        www.Dispose();
    }

    private Font LoadFont(string fontName)
    {
        var font = FileSystem.Load<Font>( "Assets/Content/UI/Fonts/" + fontName );
        if (font == null)
        {
            // Fallback to TMP default font if the loading failed
            font = TMP_Settings.defaultFontAsset.sourceFontFile;

            Debug.LogWarning($"Failed loading {fontName}, using RobotoCondensed-Bold as a fallback");
        }

        return font;
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
        //Remove it from the scrollviews if its present
        ScrollViews.Remove(pnlName);


        Animation animation = panel.GetComponent<Animation>();

        if(animation != null && animation.HasForTrigger("OnDestroy"))
            animation.Kill();
        else
        {
            Destroy( panel );
        }
    }
}

#endif
