using UnityEngine;
using System;

public partial class CommunityEntity
{
    private class Countdown : MonoBehaviour
    {
        public string command = "";
        public float endTime = 0f;
        public float startTime = 0f;
        public float step = 1f;
        public float interval = 1f;
        public TimerFormat timerFormat = TimerFormat.None;
        public string numberFormat = "0.####";
        public bool destroyIfDone = true;

        public enum TimerFormat 
        {
            None,
            SecondsHundreth,
            MinutesSeconds,
            MinutesSecondsHundreth,
            HoursMinutes,
            HoursMinutesSeconds,
            HoursMinutesSecondsMilliseconds,
            HoursMinutesSecondsTenths,
            DaysHoursMinutes,
            DaysHoursMinutesSeconds,
            Custom
        }
        
#if CLIENT

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

            InvokeRepeating( "UpdateCountdown", interval, interval );
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
            CancelInvoke( "UpdateCountdown" );

            if(!destroyIfDone) return;

            CommunityEntity.ClientInstance.DestroyPanel(gameObject.name);
        }

	    public void Reset()
        {
            CancelInvoke( "UpdateCountdown" );
            InvokeRepeating( "UpdateCountdown", interval, interval );
	    }

        void UpdateDisplay(float time)
        {
            TimeSpan t = TimeSpan.FromSeconds( time );
            
            string formattedTime = timerFormat switch
            {
                TimerFormat.SecondsHundreth => t.ToString("ss\\.ff"),
                TimerFormat.MinutesSeconds => t.ToString("mm\\:ss"),
                TimerFormat.MinutesSecondsHundreth => t.ToString("mm\\:ss\\.ff"),
                TimerFormat.HoursMinutes => $"{(int)t.TotalHours:0}:{t:mm}",
                TimerFormat.HoursMinutesSeconds => $"{(int)t.TotalHours:0}:{t:mm\\:ss}",
                TimerFormat.HoursMinutesSecondsMilliseconds => $"{(int)t.TotalHours:0}:{t:mm\\:ss\\:fff}",
                TimerFormat.HoursMinutesSecondsTenths => $"{(int)t.TotalHours:0}:{t:mm\\:ss\\.f}",
                TimerFormat.DaysHoursMinutes => $"{t:%d}.{t:hh\\:mm}",
                TimerFormat.DaysHoursMinutesSeconds => $"{t:%d}.{t:hh\\:mm\\:ss}",
                TimerFormat.Custom => t.ToString(numberFormat),
                _ => time.ToString(numberFormat)
            };
            textComponent.text = tempText.Replace( "%TIME_LEFT%", formattedTime );
        }
#endif

    }

}
