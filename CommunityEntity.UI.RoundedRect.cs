using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if CLIENT

[RequireComponent( typeof( RectTransform ) )]
public class RoundedRectGraphic : MaskableGraphic
{
    [SerializeField] private float radius = 16f;
    [SerializeField] private int segments = 8;
    [SerializeField] private Sprite sprite;
    [SerializeField] private bool preserveAspect;

    public float Radius
    {
        get => radius;
        set
        {
            radius = Mathf.Max( 0f, value );
            SetVerticesDirty();
        }
    }

    public int Segments
    {
        get => segments;
        set
        {
            segments = Mathf.Clamp( value, 1, 32 );
            SetVerticesDirty();
        }
    }

    public Sprite Sprite
    {
        get => sprite;
        set
        {
            sprite = value;
            SetMaterialDirty();
            SetVerticesDirty();
        }
    }

    public bool PreserveAspect
    {
        get => preserveAspect;
        set
        {
            preserveAspect = value;
            SetVerticesDirty();
        }
    }

    public override Texture mainTexture
    {
        get
        {
            if ( sprite == null )
                return s_WhiteTexture;

            return sprite.texture;
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        radius = Mathf.Max( 0f, radius );
        segments = Mathf.Clamp( segments, 1, 32 );

        SetMaterialDirty();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh( VertexHelper vh )
    {
        vh.Clear();

        Rect rect = rectTransform.rect;

        float width = rect.width;
        float height = rect.height;

        if ( width <= 0f || height <= 0f )
            return;

        Rect drawingRect = preserveAspect && sprite != null
            ? GetPreservedAspectRect( rect, sprite )
            : rect;

        float left = drawingRect.xMin;
        float right = drawingRect.xMax;
        float bottom = drawingRect.yMin;
        float top = drawingRect.yMax;

        float r = Mathf.Min( radius, drawingRect.width * 0.5f, drawingRect.height * 0.5f );
        int seg = Mathf.Max( 1, segments );

        Color32 color32 = color;

        var points = new List<Vector2>();

        AddCorner( points, new Vector2( right - r, top - r ), r, 0f, 90f, seg );
        AddCorner( points, new Vector2( left + r, top - r ), r, 90f, 180f, seg );
        AddCorner( points, new Vector2( left + r, bottom + r ), r, 180f, 270f, seg );
        AddCorner( points, new Vector2( right - r, bottom + r ), r, 270f, 360f, seg );

        Vector4 uv = GetSpriteOuterUV( sprite );

        int centerIndex = 0;
        vh.AddVert(
            drawingRect.center,
            color32,
            new Vector2(
                Mathf.Lerp( uv.x, uv.z, 0.5f ),
                Mathf.Lerp( uv.y, uv.w, 0.5f )
            )
        );

        for ( int i = 0; i < points.Count; i++ )
        {
            Vector2 point = points[i];

            float normalizedX = Mathf.InverseLerp( drawingRect.xMin, drawingRect.xMax, point.x );
            float normalizedY = Mathf.InverseLerp( drawingRect.yMin, drawingRect.yMax, point.y );

            Vector2 pointUv = new Vector2(
                Mathf.Lerp( uv.x, uv.z, normalizedX ),
                Mathf.Lerp( uv.y, uv.w, normalizedY )
            );

            vh.AddVert( point, color32, pointUv );
        }

        for ( int i = 0; i < points.Count; i++ )
        {
            int current = i + 1;
            int next = i == points.Count - 1 ? 1 : current + 1;

            vh.AddTriangle( centerIndex, current, next );
        }
    }

    private static Rect GetPreservedAspectRect( Rect rect, Sprite sprite )
    {
        float spriteWidth = sprite.rect.width;
        float spriteHeight = sprite.rect.height;

        if ( spriteWidth <= 0f || spriteHeight <= 0f )
            return rect;

        float spriteRatio = spriteWidth / spriteHeight;
        float rectRatio = rect.width / rect.height;

        if ( spriteRatio > rectRatio )
        {
            float height = rect.width / spriteRatio;
            float y = rect.y + ( rect.height - height ) * 0.5f;
            return new Rect( rect.x, y, rect.width, height );
        }
        else
        {
            float width = rect.height * spriteRatio;
            float x = rect.x + ( rect.width - width ) * 0.5f;
            return new Rect( x, rect.y, width, rect.height );
        }
    }

    private static Vector4 GetSpriteOuterUV( Sprite sprite )
    {
        if ( sprite == null )
            return new Vector4( 0f, 0f, 1f, 1f );

        Rect textureRect = sprite.textureRect;
        Texture texture = sprite.texture;

        return new Vector4(
            textureRect.xMin / texture.width,
            textureRect.yMin / texture.height,
            textureRect.xMax / texture.width,
            textureRect.yMax / texture.height
        );
    }

    private static void AddCorner(
        List<Vector2> points,
        Vector2 center,
        float radius,
        float startAngle,
        float endAngle,
        int segments
    )
    {
        for ( int i = 0; i <= segments; i++ )
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp( startAngle, endAngle, t ) * Mathf.Deg2Rad;

            points.Add( new Vector2(
                center.x + Mathf.Cos( angle ) * radius,
                center.y + Mathf.Sin( angle ) * radius
            ) );
        }
    }
}

#endif