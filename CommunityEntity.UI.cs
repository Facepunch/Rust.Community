using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
		var json = JSON.Object.Parse( str );

		var parentPanel = FindPanel( json.GetString( "parent", "Overlay" ) );
		if ( parentPanel == null )
		{
			Debug.LogWarning( "AddUI: Unknown Parent: " + json.GetString( "parent", "Overlay" ) );
			return;
		}

		var go = new GameObject( json.GetString( "name", "AddUI CreatedPanel" ) );
		go.transform.SetParent( parentPanel.transform, false );
		serverCreatedUI.Add( go );

		foreach ( var component in json.GetArray( "components" ) )
		{
			CreateComponents( go, component.Obj );
		}
	}

	private GameObject FindPanel( string name )
	{
		var found = UIHUD.Instance.transform.FindChildRecursive( name );
		if ( found ) return found.gameObject;

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
					break;
				}
		}
		
	}

#endif
}
