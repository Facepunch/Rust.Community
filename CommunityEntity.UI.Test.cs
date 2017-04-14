using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if SERVER
public class cui
{
	[ServerUserVar]
	public static void test( ConsoleSystem.Arg args )
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
						}

					]
					";

	    CommunityEntity.ServerInstance.ClientRPCEx( new Network.SendInfo() { connection = args.Connection }, null, "AddUI", new Facepunch.ObjectList( json ) );
	}

    [ServerUserVar]
    public static void endtest( ConsoleSystem.Arg args )
    {
        args.ReplyWith( "Ending Test!" );
        CommunityEntity.ServerInstance.ClientRPCEx( new Network.SendInfo() { connection = args.Connection }, null, "DestroyUI", new Facepunch.ObjectList( "TestPanel7766" ) );
    }
}
#endif