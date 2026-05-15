#if CLIENT
using UnityEngine;
using UnityEngine.UI;

public partial class CommunityEntity
{
    private class Pulse : MonoBehaviour
    {
        public float alphaMin = 0.4f;
        public float alphaMax = 1f;
        public float duration = 1.2f;
        public bool restoreOnDisable = true;
        public bool includeChildren;

        private Graphic[] graphics;
        private Color[] originalColors;
        private float previousAlpha = -1f;

        private void OnEnable()
        {
            CacheTargets();
            previousAlpha = -1f;
        }

        private void OnDisable()
        {
            previousAlpha = -1f;

            if ( restoreOnDisable )
                Restore();
        }

        private void OnDestroy()
        {
            Restore();
        }

        private void Update()
        {
            if ( graphics == null || originalColors == null || graphics.Length == 0 )
                return;

            var safeDuration = Mathf.Max( 0.01f, duration );
            var t = (Mathf.Sin( (Time.unscaledTime / safeDuration) * Mathf.PI * 2f ) + 1f) * 0.5f;
            var alpha = Mathf.Lerp( alphaMin, alphaMax, t );

            if ( Mathf.Abs( alpha - previousAlpha ) < 0.001f )
                return;

            previousAlpha = alpha;

            for ( var i = 0; i < graphics.Length; i++ )
            {
                var graphic = graphics[i];

                if ( graphic == null )
                    continue;

                var color = originalColors[i];
                color.a *= alpha;
                graphic.color = color;
            }
        }

        public void Refresh()
        {
            CacheTargets();
        }

        private void CacheTargets()
        {
            Restore();

            if ( includeChildren )
            {
                graphics = GetComponentsInChildren<Graphic>( true );
            }
            else
            {
                var graphic = GetComponent<Graphic>();
                graphics = graphic == null ? new Graphic[0] : new[] { graphic };
            }

            originalColors = new Color[graphics.Length];

            for ( var i = 0; i < graphics.Length; i++ )
            {
                if ( graphics[i] != null )
                    originalColors[i] = graphics[i].color;
            }

            previousAlpha = -1f;
        }

        private void Restore()
        {
            if ( graphics == null || originalColors == null )
                return;

            for ( var i = 0; i < graphics.Length; i++ )
            {
                if ( graphics[i] != null )
                    graphics[i].color = originalColors[i];
            }
        }
    }
}
#endif