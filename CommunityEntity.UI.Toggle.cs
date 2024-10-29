#if CLIENT

using UnityEngine;


// Toggle UI panels based on what the client is showing (instead of parenting them to GUI prefabs that could be destroyed)
public partial class CommunityEntity
{
    public void OnGuiChanged( Panel panelType, bool open )
    {
        var panel = transform.Find( panelType.ToString() );

        if (panel == null)
        {
            Debug.LogWarning( $"CommunityEntity Panel {panelType} not found" );
            return;
        }

        panel.gameObject.SetActive( open );
    }

    public enum Panel
    {
        Crafting,
        Contacts,
        TechTree,
        Inventory,
        Clans,
        Map,
    }
}

#endif