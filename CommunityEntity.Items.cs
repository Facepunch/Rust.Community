using System.Collections.Generic;
using UnityEngine;

public partial class CommunityEntity
{
#if SERVER
    public void SendCreateCustomItems(BasePlayer player, string json)
    {
        ClientRPC(RpcTarget.Player("RPC_CreateCustomItems", player), json);
    }

    public void SendRemoveCustomItems(BasePlayer player, string json)
    {
        ClientRPC(RpcTarget.Player("RPC_RemoveCustomItems", player), json);
    }
#endif

#if CLIENT
    private static readonly List<ItemDefinition> CustomItems = new List<ItemDefinition>();
    private static readonly Dictionary<string, ItemDefinition> CustomItemsDict = new Dictionary<string, ItemDefinition>();

    public static ItemDefinition CreateItem(JSON.Object obj) => CreateItem(obj, refresh: true);

    public static ItemDefinition CreateItem(JSON.Object obj, bool refresh)
    {
        var shortname = obj.GetString("shortname", null);
        if (string.IsNullOrEmpty(shortname))
            return null;

        ItemManager.Initialize();

        int itemid = obj.GetInt("itemid", shortname.GetHashCode());
        if (ItemManager.itemDictionary.ContainsKey(itemid))
        {
            Debug.LogError($"[CommunityEntity] Custom item shortname '{shortname}' itemid {itemid} already in use; not registering.");
            return null;
        }
        if (ItemManager.itemDictionaryByName.ContainsKey(shortname))
        {
            Debug.LogError($"[CommunityEntity] Custom item shortname '{shortname}' already exists; not registering.");
            return null;
        }

        List<ItemAmount> ingredients = null;
        var ingredientsArray = obj.GetArray("ingredients");
        if (ingredientsArray != null)
        {
            ingredients = new List<ItemAmount>(ingredientsArray.Length);
            for (int i = 0; i < ingredientsArray.Length; i++)
            {
                var ingredient = ingredientsArray[i].Obj;
                if (ingredient == null)
                    continue;

                var shortname = ingredient.GetString("shortname", null);
                if (string.IsNullOrEmpty(shortname))
                    continue;

                var definition = ItemManager.FindItemDefinition(shortname);
                if (definition == null)
                    continue;

                ingredients.Add(new ItemAmount(definition, ingredient.GetFloat("amount", 1f)));
            }
            if (ingredients.Count == 0)
                ingredients = null;
        }

        var go = new GameObject("CustomItem_" + shortname);
        var definition = go.AddComponent<ItemDefinition>();
        definition.itemid = itemid;
        definition.shortname = shortname;
        definition.displayName = new Translate.Phrase(shortname, obj.GetString("displayName", shortname));
        definition.displayDescription = new Translate.Phrase(shortname + ".description", obj.GetString("displayDescription", ""));
        definition.category = ParseEnum(obj.GetString("category", "Misc"), ItemCategory.Misc);
        definition.stackable = obj.GetInt("stackable", 1);
        definition.rarity = ParseEnum(obj.GetString("rarity", "Common"), Rarity.Common);
        definition.amountType = ParseEnum(obj.GetString("amountType", "Count"), ItemDefinition.AmountType.Count);
        definition.Initialize(ItemManager.itemList);

        ItemManager.itemList.Add(definition);
        ItemManager.itemDictionary.Add(itemid, definition);
        ItemManager.itemDictionaryByName.Add(shortname, definition);

        CustomItems.Add(definition);
        CustomItemsDict[shortname] = definition;

        uint.TryParse(obj.GetString("iconSpriteCrc"), out var iconSpriteCrc);
        if (iconSpriteCrc != 0 && ClientInstance != null)
            ClientInstance.ApplyTextureToItem(definition, iconSpriteCrc);

        if (ingredients != null)
        {
            var bp = go.AddComponent<ItemBlueprint>();
            bp.ingredients = ingredients;
            bp.time = obj.GetFloat("craftTime", 1f);
            bp.workbenchLevelRequired = obj.GetInt("workbenchLevelRequired", 0);
            bp.amountToCreate = obj.GetInt("amountToCreate", 1);
            bp.defaultBlueprint = obj.GetBoolean("defaultBlueprint", false);
            bp.userCraftable = obj.GetBoolean("userCraftable", true);
            bp.isResearchable = obj.GetBoolean("isResearchable", true);
            bp.scrapRequired = obj.GetInt("scrapRequired", 0);

            ItemManager.bpList.Add(bp);
            ItemManager.itemToBlueprint.Add(definition, bp);

            for (int i = 0; i < ingredients.Count; i++)
            {
                var ingredient = ingredients[i];
                if (ingredient.itemDef == null)
                    continue;

                if (!ItemManager.ingredientToBlueprints.TryGetValue(ingredient.itemDef, out var list))
                {
                    list = new List<ItemBlueprint>();
                    ItemManager.ingredientToBlueprints.Add(ingredient.itemDef, list);
                }
                list.Add(bp);
            }

            if (bp.defaultBlueprint)
                AddDefaultBlueprint(definition.itemid);
        }

        if (refresh)
        {
            LocalPlayer.OnInventoryChanged();
            UIBlueprints.Refresh();
        }

        return definition;
    }

    private static void AddDefaultBlueprint(int itemid)
    {
        var current = ItemManager.defaultBlueprints;
        for (int i = 0; i < current.Length; i++)
        {
            if (current[i] == itemid)
                return;
        }

        var result = new int[current.Length + 1];
        current.CopyTo(result, 0);
        result[current.Length] = itemid;
        ItemManager.defaultBlueprints = result;
    }

    private static void RemoveDefaultBlueprint(int itemid)
    {
        var current = ItemManager.defaultBlueprints;
        int index = -1;
        for (int i = 0; i < current.Length; i++)
        {
            if (current[i] == itemid)
            {
                index = i;
                break;
            }
        }
        if (index < 0)
            return;

        var result = new int[current.Length - 1];
        for (int i = 0, j = 0; i < current.Length; i++)
        {
            if (i == index)
                continue;
            result[j++] = current[i];
        }
        ItemManager.defaultBlueprints = result;
    }

    [RPC_Client]
    public void RPC_CreateCustomItems(RPCMessage rpc)
    {
        var json = rpc.read.StringRaw();
        if (string.IsNullOrEmpty(json))
            return;

        var array = JSON.Array.Parse(json);
        if (array == null)
            return;

        bool created = false;
        for (int i = 0; i < array.Length; i++)
        {
            var obj = array[i].Obj;
            if (obj == null)
                continue;

            if (CreateItem(obj, refresh: false) != null)
                created = true;
        }

        if (created)
        {
            LocalPlayer.OnInventoryChanged();
            UIBlueprints.Refresh();
        }
    }

    [RPC_Client]
    public void RPC_RemoveCustomItems(RPCMessage rpc)
    {
        var json = rpc.read.StringRaw();
        if (string.IsNullOrEmpty(json))
            return;

        var array = JSON.Array.Parse(json);
        if (array == null)
            return;

        bool removed = false;
        for (int i = 0; i < array.Length; i++)
        {
            var shortname = array[i].Str;
            if (string.IsNullOrEmpty(shortname))
                continue;

            if (!CustomItemsDict.TryGetValue(shortname, out var definition))
                continue;

            CustomItemsDict.Remove(shortname);
            RemoveCustomItem(definition);
            removed = true;
        }

        if (removed)
        {
            CustomItems.Clear();
            CustomItems.AddRange(CustomItemsDict.Values);
            LocalPlayer.OnInventoryChanged();
            UIBlueprints.Refresh();
        }
    }

    public static void RemoveCustomItems()
    {
        for (int i = 0; i < CustomItems.Count; i++)
            RemoveCustomItem(CustomItems[i]);

        CustomItems.Clear();
        CustomItemsDict.Clear();
    }

    private static void RemoveCustomItem(ItemDefinition definition)
    {
        ItemManager.itemList.Remove(definition);
        ItemManager.itemDictionary.Remove(definition.itemid);
        if (!string.IsNullOrEmpty(definition.shortname))
            ItemManager.itemDictionaryByName.Remove(definition.shortname);

        var bp = definition.GetComponent<ItemBlueprint>();
        if (bp != null)
        {
            ItemManager.bpList.Remove(bp);
            ItemManager.itemToBlueprint.Remove(definition);

            for (int j = 0; j < bp.ingredients.Count; j++)
            {
                var ingredient = bp.ingredients[j];
                if (ingredient.itemDef == null)
                    continue;

                if (ItemManager.ingredientToBlueprints.TryGetValue(ingredient.itemDef, out var list))
                {
                    list.Remove(bp);
                    if (list.Count == 0)
                        ItemManager.ingredientToBlueprints.Remove(ingredient.itemDef);
                }
            }

            if (bp.defaultBlueprint)
                RemoveDefaultBlueprint(definition.itemid);
        }

        if (definition.gameObject != null)
            Object.Destroy(definition.gameObject);
    }
#endif
}