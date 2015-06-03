using UnityEngine;
using System.Collections;

public partial class CommunityEntity : PointEntity 
{
	public static CommunityEntity ServerInstance = null;
	public static CommunityEntity ClientInstance = null;

	public override void InitShared()
	{
		if ( isServer ) ServerInstance = this;
		else ClientInstance = this;

		Log( "ServerInstance = " + ServerInstance );

		base.InitShared();
	}

	public override void DestroyShared()
	{
		base.DestroyShared();

		if ( isServer ) ServerInstance = null;
		else ClientInstance = null;
	}

}
