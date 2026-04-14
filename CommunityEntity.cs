using UnityEngine;
using System.Collections;
using ProtoBuf;

public partial class CommunityEntity : PointEntity 
{
	public static CommunityEntity ServerInstance = null;
	public static CommunityEntity ClientInstance = null;

	// Inside shared class so it stays serialized when switching defines
    public GameObject[] OverallPanels;
    public Canvas[] AllCanvases;

    public override void InitShared()
	{
		if ( isServer ) ServerInstance = this;
		else ClientInstance = this;

		base.InitShared();
	}

	public override void DestroyShared()
	{
		base.DestroyShared();

		if ( isServer ) ServerInstance = null;
		else ClientInstance = null;
	}

	#if CLIENT
	protected override void ClientInit(Entity info)
	{
		base.ClientInit(info);
		UpdateCanvasesVisibility();
	}
	#endif
}
