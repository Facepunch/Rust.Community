using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if SERVER
public class cui
{
    [ServerUserVar]
    public static void cui_test( ConsoleSystem.Arg args )
    {
        const string json = @"[	
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
							""name"": ""buttonText"",
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
									""command"":""cui.endtest"",
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
						},
						{
							""name"": ""ItemIcon"",
							""parent"": ""TestPanel7766"",
							""components"":
							[
								{
									""type"":""UnityEngine.UI.Image"",
									""color"": ""1.0 1.0 1.0 1.0"",
									""imagetype"": ""Simple"",
									""itemid"": -151838493,
								},
								{
									""type"":""RectTransform"",
									""anchormin"":""0.4 0.4"",
									""anchormax"":""0.4 0.4"",
									""offsetmin"": ""-32 -32"",
									""offsetmax"": ""32 32""
								}
							]
						},
						{
							""name"": ""ItemIconSkinTest"",
							""parent"": ""TestPanel7766"",
							""components"":
							[
								{
									""type"":""UnityEngine.UI.Image"",
									""color"": ""1.0 1.0 1.0 1.0"",
									""imagetype"": ""Simple"",
									""itemid"": -733625651,
									""skinid"": 13035
								},
								{
									""type"":""RectTransform"",
									""anchormin"":""0.6 0.6"",
									""anchormax"":""0.6 0.6"",
									""offsetmin"": ""-32 -32"",
									""offsetmax"": ""32 32""
								}
							]
						},
						{
							""name"": ""UpdateLabelTest"",
							""parent"": ""TestPanel7766"",
							""components"":
							[
								{
									""type"":""UnityEngine.UI.Text"",
									""text"":""This should go away once you update!"",
									""font"":""DroidSansMono.ttf"",
									""fontSize"":32,
									""align"": ""MiddleRight"",
								},
								{
									""type"":""RectTransform"",
									""anchormin"":""0.4 0.4"",
									""anchormax"":""0.6 0.6"",

								}
							]
						},
						{
							""name"": ""SteamAvatar"",
							""parent"": ""TestPanel7766"",
							""components"":
							[
								{
									""type"":""UnityEngine.UI.RawImage"",
									""color"": ""1.0 1.0 1.0 1.0"",
									""steamid"": ""76561197960279927"",
								},
								{
									""type"":""RectTransform"",
									""anchormin"":""0.8 0.8"",
									""anchormax"":""0.8 0.8"",
									""offsetmin"": ""-32 -32"",
									""offsetmax"": ""32 32""
								}
							]
						},

					]
					";

        CommunityEntity.ServerInstance.ClientRPC( RpcTarget.Player( "AddUI", args.Connection ), json );
    }

    [ServerUserVar]
    public static void cui_test_update( ConsoleSystem.Arg args )
    {
        //adding an allowUpdate field will try and update components based on the name
        //if a gameObject isn't named then update will not work
        //only fields that are included will be updated, so it's not necessary to pass every variable
        //for instance, the below update json will update the background image and text of the test command above
        const string json = @"[	
						{
							""name"": ""TestPanel7766"",
							""update"": true,
							""components"":
							[
								{
									""type"":""UnityEngine.UI.RawImage"",
									""url"": ""https://files.facepunch.com/paddy/20220405/zipline_01.jpg"",
								},
							]
						},
						{
							""name"": ""buttonText"",
							""update"": true,
							""components"":
							[
								{
									""type"":""UnityEngine.UI.Text"",
									""text"":""This text just got updated!"",
								},
							]
						},
						{
							""name"": ""ItemIcon"",
							""update"": true,
							""components"":
							[
								{
									""type"":""UnityEngine.UI.Image"",
									""itemid"": -2067472972,
								},
							]
						},
						{
							""name"": ""Button88"",
							""update"": true,
							""components"":
							[
								{
									""type"":""UnityEngine.UI.Button"",
									""color"": ""0.9 0.3 0.3 0.8"",
								},
							]
						},
						{
							""name"": ""UpdateLabelTest"",
							""update"": true,
							""components"":
							[
								{
									""type"":""UnityEngine.UI.Text"",
									""enabled"": false,
								},
							]
						},
					]
					";

        CommunityEntity.ServerInstance.ClientRPC( RpcTarget.Player( "AddUI", args.Connection), json );
    }

#if UNITY_EDITOR && CLIENT && SERVER
    [UnityEditor.MenuItem( "Debug/CUI/Load custom UI" )]
    public static void loadcustomjson()
    {
        if ( LocalPlayer.Entity == null || !Application.isPlaying )
            return;

        var toLoad = UnityEditor.EditorUtility.OpenFilePanel( "JSON to load", $"{Application.dataPath}/Third Party/Community/Tests", "json" );

        if ( !string.IsNullOrEmpty( toLoad ) )
        {
            CommunityEntity.ServerInstance.ClientRPC( RpcTarget.NetworkGroup( "AddUI" ), System.IO.File.ReadAllText( toLoad ) );
        }
    }
#endif

    [ServerUserVar]
    public static void endtest( ConsoleSystem.Arg args )
    {
        args.ReplyWith( "Ending Test!" );
        CommunityEntity.ServerInstance.ClientRPC( RpcTarget.Player( "DestroyUI", args.Connection ), "TestPanel7766" );
    }
}
#endif
