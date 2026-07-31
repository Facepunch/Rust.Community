using ProtoBuf;
using UnityEngine;

public partial class CommunityEntity
{
#if SERVER
    [ServerVar]
    public static void pietest(ConsoleSystem.Arg arg)
    {
        using var pie = Facepunch.Pool.Get<CustomPie>();
        pie.menus = Facepunch.Pool.Get<System.Collections.Generic.List<CustomPieMenu>>();

        AddPieMenu(pie, "Switch Night", "Switch to night mode", "env.time 0", "assets/icons/device_add.png", false, false, "", "");
        AddPieMenu(pie, "Switch Day", "Switch to day mode", "env.time 12", "assets/icons/device_add.png", false, false, "", "");
        AddPieMenu(pie, "Rain", "Make it rain on the server", "sv weather.load storm", "assets/icons/embrella.png", false, false, "", "");
        AddPieMenu(pie, "Be Malicious", "This attempts to open your player inventory.", "inventory.toggle", "assets/icons/explosion_sprite.png", false, false, "", "");
        AddPieMenu(pie, "Left/Right Test", "Pick one or the other", "pietest_prev", "assets/icons/facepunch.png", false, false, "pietest_prev", "pietest_next");
        AddPieMenu(pie, "Exit", "Close this context menu", "", "assets/icons/close.png", false, false, "", "");

        ServerInstance.SendPie(arg.Player(), pie);
    
    }

    [ServerVar]
    public static void pietest_prev(ConsoleSystem.Arg arg)
    {
        using var pie = Facepunch.Pool.Get<CustomPie>();
        pie.menus = Facepunch.Pool.Get<System.Collections.Generic.List<CustomPieMenu>>();

        AddPieMenu(pie, "Go Back", null, "pietest", "assets/icons/fun.png", false, false, "", "");
        AddPieMenu(pie, "Or Don't", "You went previous", "pietest", "assets/icons/fun.png", true, false, "", "");

        ServerInstance.SendPie(arg.Player(), pie);
    }


    [ServerVar]
    public static void pietest_next(ConsoleSystem.Arg arg)
    {
        using var pie = Facepunch.Pool.Get<CustomPie>();
        pie.menus = Facepunch.Pool.Get<System.Collections.Generic.List<CustomPieMenu>>();

        AddPieMenu(pie, "Go Back", null, "pietest", "assets/icons/fun.png", false, false, "", "");
        AddPieMenu(pie, "Or Don't", "You went next", "pietest", "assets/icons/fun.png", true, false, "", "");


        ServerInstance.SendPie(arg.Player(), pie);
    }
    private static void AddPieMenu(CustomPie pie, string name, string description, string command, string sprite, bool disabled, bool selected, string next, string prev)
    {
        var pieMenu = Facepunch.Pool.Get<CustomPieMenu>();
        pieMenu.name = name;
        pieMenu.description = description;
        pieMenu.command = command;
        pieMenu.sprite = sprite;
        pieMenu.disabled = disabled;
        pieMenu.selected = selected;
        pieMenu.nextCommand = next;
        pieMenu.prevCommand = prev;
        pie.menus.Add(pieMenu);
    }

    public void SendPie(BasePlayer player, CustomPie pie)
    {
        ClientRPC(RpcTarget.Player("OpenPie", player), pie);
    }
#endif

#if CLIENT
    [RPC_Client]
    public void OpenPie(RPCMessage rpc)
    {
        using var pie = rpc.read.Proto<CustomPie>();

        if (UIInventory.isOpen || UICrafting.isOpen || UIContacts.isOpen || UIClans.IsOpen || UIDialog.isOpen)
        {
            if (!string.IsNullOrEmpty(pie.closeCommand))
            {
                ConsoleSystem.Run(ConsoleSystem.Option.Client.FromServer(), pie.closeCommand);
            }
            return;
        }
        ContextMenuUI.Start(ContextMenuUI.MenuType.Custom, string.IsNullOrEmpty(pie.closeCommand) ? null : () => 
        {
            ConsoleSystem.Run(ConsoleSystem.Option.Client.FromServer(), pie.closeCommand);
        });
        for (int i = 0; i < pie.menus.Count; i++)
        {
            var menu = pie.menus[i];
            var sprite = string.IsNullOrEmpty(menu.sprite) ? GetOrRequestSprite(menu.imageId) : FileSystem.Load<Sprite>(menu.sprite);
            var menuCommand = menu.command;
            var menuDisabledCommand = menu.disabledCommand;
            var menuNextCommand = menu.nextCommand;
            var menuPrevCommand = menu.prevCommand;
            ContextMenuUI.AddOptionPhrase(
                namePhrase: menu.name,
                descPhrase: menu.description,
                icon: sprite,
                action: string.IsNullOrEmpty(menuCommand) ? null : (ply) =>
                {
                    ConsoleSystem.Run(ConsoleSystem.Option.Client.FromServer(), menuCommand);
                },
                actionDisabled: string.IsNullOrEmpty(menuDisabledCommand) ? null : ((ply) =>
                {
                    ConsoleSystem.Run(ConsoleSystem.Option.Client.FromServer(), menuDisabledCommand);
                }),
                actionNext: string.IsNullOrEmpty(menuNextCommand) ? null : ((ply) =>
                {
                    ConsoleSystem.Run(ConsoleSystem.Option.Client.FromServer(), menuNextCommand);
                    PieMenu.Instance.Close(true);
                }),
                actionPrev: string.IsNullOrEmpty(menuPrevCommand) ? null : ((ply) =>
                {
                    ConsoleSystem.Run(ConsoleSystem.Option.Client.FromServer(), menuPrevCommand);
                    PieMenu.Instance.Close(true);
                }),
                order: menu.order,
                disabled: menu.disabled,
                selected: menu.selected,
                colorMode: menu.color != default ? new PieMenu.MenuOption.ColorMode()
                {
                    Mode = (PieMenu.MenuOption.ColorMode.PieMenuSpriteColorOption)menu.colorMode,
                    CustomColor = menu.color
                } : null);
        }
        ContextMenuUI.End();
    }
#endif
}

