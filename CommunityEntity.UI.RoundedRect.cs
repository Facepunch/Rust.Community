using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if CLIENT

[RequireComponent( typeof( RectTransform ) )]
public class RoundedRectGraphic : MaskableGraphic
{
    private const int MaxSegments = 64;
    private const float MaxRadius = 64f;

    [SerializeField] private float radius = 16f;
    [SerializeField] private int segments = 8;
    [SerializeField] private float softness = 1.5f;
    [SerializeField] private Sprite sprite;
    [SerializeField] private bool preserveAspect;

    public float Radius
    {
        get => radius;
        set
        {
            radius = Mathf.Clamp( value, 0f, MaxRadius );
            SetVerticesDirty();
        }
    }

    public int Segments
    {
        get => segments;
        set
        {
            segments = Mathf.Clamp( value, 1, MaxSegments );
            SetVerticesDirty();
        }
    }

    public float Softness
    {
        get => softness;
        set
        {
            softness = Mathf.Max( 0f, value );
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

    public override Texture mainTexture => sprite == null ? s_WhiteTexture : sprite.texture;

    protected override void OnValidate()
    {
        base.OnValidate();

        radius = Mathf.Clamp( radius, 0f, MaxRadius );
        segments = Mathf.Clamp( segments, 1, MaxSegments );
        softness = Mathf.Max( 0f, softness );

        SetMaterialDirty();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh( VertexHelper vh )
    {
        vh.Clear();

        Rect rect = rectTransform.rect;

        if ( rect.width <= 0f || rect.height <= 0f )
            return;

        Rect drawingRect = preserveAspect && sprite != null
            ? GetPreservedAspectRect( rect, sprite )
            : rect;

        float r = Mathf.Min( radius, drawingRect.width * 0.5f, drawingRect.height * 0.5f );
        int seg = Mathf.Clamp( segments, 1, MaxSegments );
        float soft = Mathf.Min( softness, drawingRect.width * 0.5f, drawingRect.height * 0.5f );

        Vector4 uv = GetSpriteOuterUV( sprite );

        var innerPoints = BuildRoundedRectPoints( drawingRect, r, seg );
        Color32 innerColor = color;

        if ( soft <= 0f )
        {
            AddFilledShape( vh, drawingRect.center, innerPoints, innerColor, uv, drawingRect );
            return;
        }

        Rect outerRect = new Rect(
            drawingRect.xMin - soft,
            drawingRect.yMin - soft,
            drawingRect.width + soft * 2f,
            drawingRect.height + soft * 2f
        );

        float outerRadius = r + soft;
        var outerPoints = BuildRoundedRectPoints( outerRect, outerRadius, seg );

        Color32 outerColor = innerColor;
        outerColor.a = 0;

        int centerIndex = 0;
        vh.AddVert(
            drawingRect.center,
            innerColor,
            GetUv( drawingRect.center, uv, drawingRect )
        );

        int innerStart = 1;
        for ( int i = 0; i < innerPoints.Count; i++ )
        {
            Vector2 point = innerPoints[i];
            vh.AddVert( point, innerColor, GetUv( point, uv, drawingRect ) );
        }

        int outerStart = innerStart + innerPoints.Count;
        for ( int i = 0; i < outerPoints.Count; i++ )
        {
            Vector2 point = outerPoints[i];
            vh.AddVert( point, outerColor, GetUv( point, uv, drawingRect ) );
        }

        int count = innerPoints.Count;

        for ( int i = 0; i < count; i++ )
        {
            int currentInner = innerStart + i;
            int nextInner = innerStart + ( ( i + 1 ) % count );

            vh.AddTriangle( centerIndex, currentInner, nextInner );
        }

        for ( int i = 0; i < count; i++ )
        {
            int currentInner = innerStart + i;
            int nextInner = innerStart + ( ( i + 1 ) % count );
            int currentOuter = outerStart + i;
            int nextOuter = outerStart + ( ( i + 1 ) % count );

            vh.AddTriangle( currentInner, currentOuter, nextOuter );
            vh.AddTriangle( currentInner, nextOuter, nextInner );
        }
    }

    private static void AddFilledShape( VertexHelper vh, Vector2 center, List<Vector2> points, Color32 color, Vector4 uv, Rect uvRect )
    {
        int centerIndex = 0;
        vh.AddVert( center, color, GetUv( center, uv, uvRect ) );

        for ( int i = 0; i < points.Count; i++ )
        {
            Vector2 point = points[i];
            vh.AddVert( point, color, GetUv( point, uv, uvRect ) );
        }

        for ( int i = 0; i < points.Count; i++ )
        {
            int current = i + 1;
            int next = i == points.Count - 1 ? 1 : current + 1;

            vh.AddTriangle( centerIndex, current, next );
        }
    }

    private static List<Vector2> BuildRoundedRectPoints( Rect rect, float radius, int segments )
    {
        float left = rect.xMin;
        float right = rect.xMax;
        float bottom = rect.yMin;
        float top = rect.yMax;

        float r = Mathf.Min( radius, rect.width * 0.5f, rect.height * 0.5f );

        var points = new List<Vector2>( ( segments + 1 ) * 4 );

        AddCorner( points, new Vector2( right - r, top - r ), r, 0f, 90f, segments, true );
        AddCorner( points, new Vector2( left + r, top - r ), r, 90f, 180f, segments, false );
        AddCorner( points, new Vector2( left + r, bottom + r ), r, 180f, 270f, segments, false );
        AddCorner( points, new Vector2( right - r, bottom + r ), r, 270f, 360f, segments, false );

        return points;
    }

    private static Vector2 GetUv( Vector2 point, Vector4 uv, Rect rect )
    {
        float normalizedX = Mathf.InverseLerp( rect.xMin, rect.xMax, point.x );
        float normalizedY = Mathf.InverseLerp( rect.yMin, rect.yMax, point.y );

        return new Vector2(
            Mathf.Lerp( uv.x, uv.z, normalizedX ),
            Mathf.Lerp( uv.y, uv.w, normalizedY )
        );
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

        float width = rect.height * spriteRatio;
        float x = rect.x + ( rect.width - width ) * 0.5f;
        return new Rect( x, rect.y, width, rect.height );
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
        int segments,
        bool includeFirstPoint
    )
    {
        int start = includeFirstPoint ? 0 : 1;

        for ( int i = start; i <= segments; i++ )
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