using UnityEngine;


public partial class CommunityEntity
{
    private class FadeOut : MonoBehaviour
    {
        public float duration;
#if CLIENT

        public void FadeOutAndDestroy()
        {
            Invoke( "Kill", duration + .1f );
            foreach ( var c in gameObject.GetComponents<UnityEngine.UI.Graphic>() )
            {
                c.CrossFadeAlpha( 0f, duration, false );
            }
        }

        public void Kill()
        {
            Destroy( gameObject );
        }
#endif
    }
}

