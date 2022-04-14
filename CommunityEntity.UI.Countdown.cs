using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using System.IO;

public partial class CommunityEntity
{
#if CLIENT
    private class Countdown : MonoBehaviour
    {
        public string command = "";
        public int endTime = 0;
        public int startTime = 0;
        public int step = 1;
        private int sign = 1;
        private string tempText = "";
        private UnityEngine.UI.Text textComponent;

        void Start()
        {
            textComponent = GetComponent<UnityEngine.UI.Text>();
            if ( textComponent )
            {
				tempText = textComponent.text;
                textComponent.text = tempText.Replace( "%TIME_LEFT%", startTime.ToString() );
            }
            if ( startTime == endTime )
            {
                End();
            }
            if ( step == 0 )
            {
                step = 1;
            }
            if ( startTime > endTime && step > 0 )
            {
                sign = -1;
            }

            InvokeRepeating( "UpdateCountdown", step, step );
        }

        void UpdateCountdown()
        {
            startTime = startTime + step * sign;

            if ( textComponent )
            {
                textComponent.text = tempText.Replace( "%TIME_LEFT%", startTime.ToString() );
            }

            if ( startTime == endTime )
            {
                if ( !string.IsNullOrEmpty( command ) )
                {
                    ConsoleNetwork.ClientRunOnServer( command );
                }

                End();
            }
        }

        void End()
        {
            CancelInvoke( "UpdateCountdown" );

            var fadeOut = GetComponent<FadeOut>();
            if ( fadeOut )
            {
                fadeOut.FadeOutAndDestroy();
            }
            else
            {
                Object.Destroy( gameObject );
            }
        }
    }

#endif
}