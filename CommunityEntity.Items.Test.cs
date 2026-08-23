using UnityEngine;

#if SERVER
public class citem
{
    [ServerVar]
    public static void customitem_test( ConsoleSystem.Arg args )
    {
        var player = args.Player();
        if ( player == null ) return;

        var ingredients = new JSON.Array();
        ingredients.Add( new JSON.Object { ["shortname"] = "wood", ["amount"] = 100 } );
        ingredients.Add( new JSON.Object { ["shortname"] = "stones", ["amount"] = 50 } );

        var obj = new JSON.Object
        {
            ["shortname"] = "custom_rpc_item",
            ["displayName"] = "Custom RPC Item",
            ["displayDescription"] = "Created via server RPC",
            ["category"] = ItemCategory.Misc.ToString(),
            ["stackable"] = 100,
            ["rarity"] = Rarity.Uncommon.ToString(),
            ["amountType"] = ItemDefinition.AmountType.Count.ToString(),
            ["ingredients"] = ingredients,
            ["craftTime"] = 5,
            ["workbenchLevelRequired"] = 1,
            ["amountToCreate"] = 1,
            ["defaultBlueprint"] = true
        };

        var items = new JSON.Array();
        items.Add( obj );

        CommunityEntity.ServerInstance.SendCreateCustomItems( player, items.ToString() );
    }

    [ServerVar]
    public static void customitem_endtest( ConsoleSystem.Arg args )
    {
        var player = args.Player();
        if ( player == null ) return;

        var array = new JSON.Array();
        array.Add( "custom_rpc_item" );

        CommunityEntity.ServerInstance.SendRemoveCustomItems( player, array.ToString() );
    }
}
#endif