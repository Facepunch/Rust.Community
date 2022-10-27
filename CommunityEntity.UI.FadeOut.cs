using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using System.IO;

#if CLIENT

public partial class CommunityEntity
{

      private class FadeOut : MonoBehaviour
    {
        public float duration;

        public void FadeOutAndDestroy()
        {
            Invoke( "Kill", duration + .1f );
            foreach ( var c in gameObject.GetComponentsInChildren<UnityEngine.UI.Graphic>() )
            {
                c.CrossFadeAlpha( 0f, duration, false );
            }
        }

        public void Kill()
        {
            Destroy( gameObject );
        }
    }

}

#endif
