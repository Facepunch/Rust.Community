using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using System.IO;
using MaskableGraphic = UnityEngine.UI.MaskableGraphic;

#if CLIENT

public partial class CommunityEntity
{
    private static Dictionary<uint, List<ImageRequest>> requestingTextureImages = new Dictionary<uint, List<ImageRequest>>();
    private static Dictionary<uint, CachedTexture> _textureCache = new Dictionary<uint, CachedTexture>();

    private class CachedTexture
    {
        public Texture2D Texture;
        public Sprite Sprite;

        public void Destroy()
        {
            if ( Texture != null )
            {
                UnityEngine.Object.Destroy( Texture );
                Texture = null;
            }

            if ( Sprite != null )
            {
                UnityEngine.Object.Destroy( Sprite );
                Sprite = null;
            }
        }
    }

    [RPC_Client]
    public void CL_ReceiveFilePng( BaseEntity.RPCMessage msg )
    {
        var textureID = msg.read.UInt32();
        var bytes = msg.read.BytesWithSize();

        if ( bytes == null ) return;

        if ( FileStorage.client.Store( bytes, FileStorage.Type.png, net.ID ) != textureID )
        {
            Log( "Client/Server FileStorage CRC differs" );
        }

        var texture = StoreCachedTexture( textureID, bytes );

        List<UnityEngine.UI.MaskableGraphic> components;
        if ( requestingTextureImages.TryGetValue( textureID, out components ) )
        {
            requestingTextureImages.Remove( textureID );

            foreach ( var c in components )
            {
                ApplyCachedTextureToImage( c.graphic, texture, c.slice );
            }
        }
    }

    private static CachedTexture GetCachedTexture( uint textureId )
    {
        _textureCache.TryGetValue( textureId, out var texture );
        return texture;
    }

    private CachedTexture StoreCachedTexture( uint textureId, byte[] bytes )
    {
        var texture = new CachedTexture()
        {
            Texture = new Texture2D( 1, 1, TextureFormat.RGBA32, false ),
        };

        texture.Texture.LoadImage( bytes );

        // Check for duplicate textureId and unload old one ( dont think they will conflict but safety first)
        if ( _textureCache.TryGetValue( textureId, out var oldTexture ) )
        {
            oldTexture.Destroy(); 
        }

        _textureCache[ textureId ] = texture;

        return texture;
    }

    private void ApplyTextureToImage( UnityEngine.UI.MaskableGraphic component, uint textureID, Vector4? slice = null )
    {
        var texture = GetCachedTexture( textureID );

        if ( texture == null )
        {
            var bytes = FileStorage.client.Get( textureID, FileStorage.Type.png, net.ID );
            if ( bytes != null )
            {
                texture = StoreCachedTexture( textureID, bytes );
            }
            else
            {
                List<UnityEngine.UI.MaskableGraphic> components;
                if ( !requestingTextureImages.TryGetValue( textureID, out components ) )
                {
                    components = new List<UnityEngine.UI.MaskableGraphic>();
                    requestingTextureImages[ textureID ] = components;
                    RequestFileFromServer( textureID, FileStorage.Type.png, "CL_ReceiveFilePng" );
                }
                components.Add( new ImageRequest()
                {
                    graphic = component,
                    slice = slice
                });
                return;
            }
        }

        ApplyCachedTextureToImage( component, texture, slice );
    }

    private void ApplyCachedTextureToImage( UnityEngine.UI.MaskableGraphic component, CachedTexture texture, Vector4? slice = null )
    {
        var image = component as UnityEngine.UI.Image;
        if ( image )
        {
            if(slice != null){
                image.sprite = Sprite.Create( texture.Texture, new Rect( 0, 0, texture.Texture.width, texture.Texture.height ), new Vector2( 0.5f, 0.5f ), 100, 0, SpriteMeshType.Tight, slice.Value);
                return;
            }
            if ( texture.Sprite == null )
            {
                texture.Sprite = Sprite.Create( texture.Texture, new Rect( 0, 0, texture.Texture.width, texture.Texture.height ), new Vector2( 0.5f, 0.5f ) );
            }

            image.sprite = texture.Sprite;
            return;
        }

        var rawImage = component as UnityEngine.UI.RawImage;
        if ( rawImage )
        {
            rawImage.texture = texture.Texture;
        }
    }

    private static void UnloadTextureCache()
    {
        foreach( var texture in _textureCache.Values )
        {
            texture.Destroy();
        }

        _textureCache.Clear();
    }
}

#endif 