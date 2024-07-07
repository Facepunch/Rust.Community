using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using System.IO;

public partial class CommunityEntity
{
#if CLIENT
    private class Countdown : FacepunchBehaviour
    {
        public string command = "";
        public float endTime = 0f;
        public float startTime = 0f;
        public float step = 1f;
        public float interval = 1f;
        public TimerFormat timerFormat = TimerFormat.None;
        public string numberFormat = "0.####";
        public bool destroyIfDone = true;

        private string sign = "";
        private string tempText = "";
        private UnityEngine.UI.Text textComponent;

        void Start()
        {
            // dont let the timer update more than 50 times per second, though even that is excessive
            interval = Mathf.Max(interval, 0.02f);

            textComponent = GetComponent<UnityEngine.UI.Text>();
            if ( textComponent )
            {
				tempText = textComponent.text;
                UpdateDisplay(startTime);
            }
            if ( startTime == endTime )
            {
                End();
            }
            if ( step == 0f )
            {
                step = 1f;
            }
            if ( startTime > endTime && step > 0f )
            {
                sign = "-";
                step = 0 - step;
            }

            InvokeRepeating(UpdateCountdown, interval, interval );
        }

        void UpdateCountdown()
        {
            startTime = startTime + step;

            if ( textComponent )
            {
                UpdateDisplay(startTime);
            }

            if ( (sign == "-" && startTime <= endTime) || (sign == "" && startTime >= endTime) )
            {
                if ( textComponent )
                    UpdateDisplay(endTime);

                if ( !string.IsNullOrEmpty( command ) )
                {
                    ConsoleNetwork.ClientRunOnServer( command );
                }

                End();
            }
        }

        void End()
        {
            CancelInvoke( UpdateCountdown );

            if(!destroyIfDone) return;

            CommunityEntity.ClientInstance.DestroyPanel(gameObject.name);
        }

		public void Reset(){
            CancelInvoke( UpdateCountdown );
            InvokeRepeating( UpdateCountdown, interval, interval );
		}
		
        void UpdateDisplay(float time){
            TimeSpan t = TimeSpan.FromSeconds( time );
            string formattedTime = timerFormat switch{
                TimerFormat.SecondsHundreth => t.ToString("ss\\.ff"),
                TimerFormat.MinutesSeconds => t.ToString("mm\\:ss"),
                TimerFormat.MinutesSecondsHundreth => t.ToString("mm\\:ss\\.ff"),
                TimerFormat.HoursMinutes => t.ToString("hh\\:mm"),
                TimerFormat.HoursMinutesSeconds => t.ToString("hh\\:mm\\:ss"),
                _ => time.ToString(numberFormat)
            };
            textComponent.text = tempText.Replace( "%TIME_LEFT%", formattedTime );
        }

        public enum TimerFormat {
            None,
            SecondsHundreth,
            MinutesSeconds,
            MinutesSecondsHundreth,
            HoursMinutes,
            HoursMinutesSeconds
        }
    }

#endif
}
