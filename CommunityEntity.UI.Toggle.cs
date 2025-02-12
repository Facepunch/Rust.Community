#if CLIENT

using UnityEngine;


// Toggle UI panels based on what the client is showing (instead of parenting them to GUI prefabs that could be destroyed)
public partial class CommunityEntity
{
    public void OnGuiChanged( Panel panelType, bool open )
    {
        // Could occur if calling OnGuiChanged as the client is disconnecting
        if (this == null || transform == null) return;

        var panel = transform.Find( panelType.ToString() );

        // Also check gameObject in case the panel was destroyed
        if (panel == null || panel.gameObject == null)
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