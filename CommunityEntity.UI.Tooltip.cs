using UnityEngine;

public partial class CommunityEntity
{
    public GameObject TooltipRef;
    public GameObject TooltipAlwaysOnTopRef;
    public GameObject TooltipAlwaysOnTopEmojiRef;

    public enum TooltipType
    {
        Default = 0,
        AlwaysOnTop = 1,
        AlwaysOnTopEmoji = 2
    }
}
