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
    private static Dictionary<uint, ImageRequest> requestingTextureImages = new Dictionary<uint, ImageRequest>();
    private static Dictionary<uint, CachedTexture> _textureCache = new Dictionary<uint, CachedTexture>();

    private class ImageRequest
    {
        public List<Entry> Entries = new List<Entry>();

        public struct Entry
        {
            public UnityEngine.UI.MaskableGraphic graphic;
            public Vector4? slice;
        }
    }

    private class CachedTexture
    {
        public Texture2D Texture;
        // Default non-sliced sprite
        public Sprite Sprite;
        // Cache sprites created with slicing settings
        public Dictionary<Vector4, Sprite> SlicedSprites;

        public Sprite GetOrCreateSprite(Vector4? slice = null)
        {
            // Create sliced sprite if Vector4 settings provided
            if(slice != null)
            {
                if (SlicedSprites != null && SlicedSprites.TryGetValue(slice.Value, out var slicedSprite))
                    return slicedSprite;
                
                SlicedSprites ??= new Dictionary<Vector4, Sprite>();

                // 100 pixels per unit is the same as the constructor below uses for non-sliced sprites
                slicedSprite = Sprite.Create( Texture, new Rect( 0, 0, Texture.width, Texture.height ), new Vector2( 0.5f, 0.5f ), 100, 0, SpriteMeshType.FullRect, slice.Value);
                SlicedSprites.Add(slice.Value, slicedSprite);

                return slicedSprite;
            }
            
            // Otherwise create a normal sprite
            if (Sprite == null)
            {
                Sprite = Sprite.Create(Texture, new Rect(0, 0, Texture.width, Texture.height), new Vector2(0.5f, 0.5f));
            }

            return Sprite;
        }

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

            if (SlicedSprites != null)
            {
                foreach (var slicedSprite in SlicedSprites)
                {
                    UnityEngine.Object.Destroy( slicedSprite.Value );
                }
                SlicedSprites = null;
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

        // Get existing image request
        var request = RequestImage(textureID, false);

        if (request != null)
        {
            foreach (var c in request.Entries)
            {
                ApplyCachedTextureToImage(c, texture);
            }
            
            // Remove request
            requestingTextureImages.Remove(textureID);
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

    // Request an image or get the existing request
    private ImageRequest RequestImage(uint textureID, bool createRequest)
    {
        if (!requestingTextureImages.TryGetValue(textureID, out var request) && createRequest)
        {
            request = new ImageRequest();
            requestingTextureImages[textureID] = request;
            RequestFileFromServer(textureID, FileStorage.Type.png, "CL_ReceiveFilePng");
        }
        return request;
    }

    public Sprite GetOrRequestSprite(uint id, Vector4? slice = null)
    {
        var cachedImage = GetCachedTexture(id);

        // Cached in memory
        if (cachedImage != null)
        {
            return cachedImage.GetOrCreateSprite(slice);
        }

        var bytes = FileStorage.client.Get(id, FileStorage.Type.png, net.ID);

        // Cached on disk
        if (bytes != null)
        {
            cachedImage = StoreCachedTexture(id, bytes);
            return cachedImage.GetOrCreateSprite(slice);
        }

        // Create request for texture
        // NOTE: don't worry about callback since ItemIcon should be polling every frame
        RequestImage(id, true);

        return null;
    }

    public void ApplyTextureToImage( UnityEngine.UI.MaskableGraphic component, uint textureID, Vector4? slice = null )
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
                var request = RequestImage(textureID, true);
                request.Entries.Add( new ImageRequest.Entry()
                {
                    graphic = component, 
                    slice = slice
                });
                return;
            }
        }

        ApplyCachedTextureToImage( new ImageRequest.Entry()
        {
            graphic = component,
            slice = slice
        }, texture );
    }

    private void ApplyCachedTextureToImage( ImageRequest.Entry entry, CachedTexture texture )
    {
        var image = entry.graphic as UnityEngine.UI.Image;
        if ( image )
        {
            image.sprite = texture.GetOrCreateSprite(entry.slice);
            return;
        }

        var rawImage = entry.graphic as UnityEngine.UI.RawImage;
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

        // Clear image requests as well
        requestingTextureImages.Clear();
    }
}

#endif 
