using Facepunch;
using ProtoBuf;
using System.Collections.Generic;
using UnityEngine;

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

#if SERVER
    // This mainly exists for our ServerRPC overload generator so this specific overload can exist
    public void SendDestroyUIs(BasePlayer player, List<string> uiPanels)
    {
        using var destroyUi = Pool.Get<ProtoBuf.CommunityEntity_DestroyUIs>();
        destroyUi.list = Pool.Get<List<string>>();
        for(int i = 0; i < uiPanels.Count; i++)
        {
            destroyUi.list.Add(uiPanels[i]);
        }
        ClientRPC(RpcTarget.Player("DestroyUIs", player), destroyUi);
    }

    // Added alternative overload; plugins can have a static array with all UIs they want to destroy predefined
    public void SendDestroyUIs(BasePlayer player, string[] uiPanels)
    {
        using var destroyUi = Pool.Get<ProtoBuf.CommunityEntity_DestroyUIs>();
        destroyUi.list = Pool.Get<List<string>>();
        for (int i = 0; i < uiPanels.Length; i++)
        {
            destroyUi.list.Add(uiPanels[i]);
        }
        ClientRPC(RpcTarget.Player("DestroyUIs", player), destroyUi);
    }
#endif

}
