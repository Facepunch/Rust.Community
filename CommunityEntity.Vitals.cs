using ProtoBuf;

public partial class CommunityEntity
{
#if CLIENT
    [RPC_Client]
    public void RPC_UpdateVitals(RPCMessage message)
    {
        
        // Deserialize list of vitals and update UI
        var vitals = message.read.Proto<CustomVitals>();

        NoticeArea.Instance?.UpdateCustomVitalsFromServer(vitals);
    }
#endif

#if SERVER
    public void SendCustomVitals(BasePlayer player, CustomVitals vitals)
    {
        ClientRPC(RpcTarget.Player("RPC_UpdateVitals", player), vitals);
    }
#endif
}
