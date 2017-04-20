using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using System.IO;

#if CLIENT

public partial class CommunityEntity
{
    private static Dictionary<uint, List<UnityEngine.UI.MaskableGraphic>> requestingTextureImages = new Dictionary<uint, List<UnityEngine.UI.MaskableGraphic>>();

    [RPC_Client]
    public void CL_ReceiveFilePng( BaseEntity.RPCMessage msg )
    {
        var textureID = msg.read.UInt32();
        var bytes = msg.read.BytesWithSize();

        using ( var ms = new MemoryStream( bytes, 0, bytes.Length, true, true ) )
        {
            if ( FileStorage.client.Store( ms, FileStorage.Type.png, net.ID ) != textureID )
            {
                Log( "Client/Server FileStorage CRC differs" );
            }
        }

        List<UnityEngine.UI.MaskableGraphic> components;
        if ( !requestingTextureImages.TryGetValue( textureID, out components ) ) return;

        requestingTextureImages.Remove( textureID );

        foreach ( var c in components )
        {
            LoadPngIntoGraphic( c, bytes );
        }
    }

    private void SetImageFromServer( UnityEngine.UI.MaskableGraphic component, uint textureID )
    {
        var bytes = FileStorage.client.Get( textureID, FileStorage.Type.png, net.ID );
        if ( bytes == null )
        {
            List<UnityEngine.UI.MaskableGraphic> components;
            if ( !requestingTextureImages.TryGetValue( textureID, out components ) )
            {
                components = new List<UnityEngine.UI.MaskableGraphic>();
                requestingTextureImages[textureID] = components;
                RequestFileFromServer( textureID, FileStorage.Type.png, "CL_ReceiveFilePng" );
            }
            components.Add( component );
        }
        else
        {
            LoadPngIntoGraphic( component, bytes );
        }
    }

    private void LoadPngIntoGraphic( UnityEngine.UI.MaskableGraphic component, byte[] bytes )
    {
        var texture = new Texture2D( 1, 1, TextureFormat.RGBA32, false );
        texture.LoadImage( bytes );

        var image = component as UnityEngine.UI.Image;
        if ( image )
        {
            image.sprite = Sprite.Create( texture, new Rect( 0, 0, texture.width, texture.height ), new Vector2( 0.5f, 0.5f ) );
            return;
        }

        var rawImage = component as UnityEngine.UI.RawImage;
        if ( rawImage ) rawImage.texture = texture;
    }

}

#endif