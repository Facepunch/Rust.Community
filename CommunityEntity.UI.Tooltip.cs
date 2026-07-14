public partial class CommunityEntity
{
    public GameObjectRef TooltipRef;
    public GameObjectRef TooltipAlwaysOnTopRef;
    public GameObjectRef TooltipAlwaysOnTopEmojiRef;

    public enum TooltipType
    {
        Default = 0,
        AlwaysOnTop = 1,
        AlwaysOnTopEmoji = 2
    }
}
