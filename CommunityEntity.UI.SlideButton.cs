using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if CLIENT

public partial class CommunityEntity
{
    private class SlideButton : MonoBehaviour
    {
        public enum SlideDirection
        {
            Left,
            Right,
            Up,
            Down
        }

        public bool isOn;

        public string offText = "Off";
        public string offCommand = "";
        public Color offColor = new Color( 0.35f, 0.35f, 0.35f, 1f );
        public Color offTextColor = Color.white;
        public SlideDirection offSlideDirection = SlideDirection.Right;

        public string onText = "On";
        public string onCommand = "";
        public Color onColor = new Color( 0.2f, 0.75f, 0.3f, 1f );
        public Color onTextColor = Color.white;
        public SlideDirection onSlideDirection = SlideDirection.Left;

        public float duration = 0.15f;
        public float slidePixels = 30f;

        private Button button;
        private Image image;
        private Text label;

        private Vector3 labelStartPosition;
        private bool initialized;
        private bool animating;

        private void Awake()
        {
            Init();
        }

        private void Start()
        {
            Init();
            ApplyInstant();
        }

        private void OnEnable()
        {
            Init();
            ApplyInstant();
        }

        private void LateUpdate()
        {
            if ( !initialized )
            {
                ApplyInstant();
            }
        }

        private void OnDestroy()
        {
            if ( button != null )
            {
                button.onClick.RemoveListener( OnClick );
            }
        }

        public void Init()
        {
            if ( initialized )
            {
                return;
            }

            button = GetComponent<Button>();
            image = GetComponent<Image>();
            label = GetComponentInChildren<Text>( true );

            if ( button == null || image == null || label == null )
            {
                return;
            }

            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;

            button.onClick.RemoveListener( OnClick );
            button.onClick.AddListener( OnClick );

            label.raycastTarget = false;
            labelStartPosition = label.rectTransform.localPosition;

            initialized = true;
        }

        public void ApplyInstant()
        {
            Init();

            if ( !initialized )
            {
                return;
            }

            StopAllCoroutines();
            animating = false;

            image.color = CurrentColor();

            label.text = CurrentText();
            label.color = CurrentTextColor();
            label.rectTransform.localPosition = labelStartPosition;
        }

        private void OnClick()
        {
            if ( animating )
            {
                return;
            }

            isOn = !isOn;

            var command = CurrentCommand();

            if ( !string.IsNullOrEmpty( command ) )
            {
                ConsoleNetwork.ClientRunOnServer( command );
            }

            StopAllCoroutines();
            StartCoroutine( Animate() );
        }

        private IEnumerator Animate()
        {
            if ( !initialized )
            {
                yield break;
            }

            animating = true;

            var imageFrom = image.color;
            var imageTo = CurrentColor();

            var textFrom = label.color;
            var textTo = CurrentTextColor();

            var nextText = CurrentText();

            var center = labelStartPosition;
            var direction = CurrentSlideDirection();

            var outPosition = center + GetOutOffset( direction );
            var inPosition = center + GetInOffset( direction );

            var half = Mathf.Max( 0.01f, duration * 0.5f );
            var time = 0f;

            while ( time < half )
            {
                time += Time.unscaledDeltaTime;

                var t = Mathf.Clamp01( time / half );

                label.rectTransform.localPosition = Vector3.Lerp( center, outPosition, t );
                label.color = Color.Lerp( textFrom, Transparent( textFrom ), t );
                image.color = Color.Lerp( imageFrom, imageTo, t * 0.5f );

                yield return null;
            }

            label.text = nextText;
            label.rectTransform.localPosition = inPosition;

            time = 0f;

            while ( time < half )
            {
                time += Time.unscaledDeltaTime;

                var t = Mathf.Clamp01( time / half );

                label.rectTransform.localPosition = Vector3.Lerp( inPosition, center, t );
                label.color = Color.Lerp( Transparent( textTo ), textTo, t );
                image.color = Color.Lerp( imageFrom, imageTo, 0.5f + t * 0.5f );

                yield return null;
            }

            image.color = imageTo;
            label.text = nextText;
            label.color = textTo;
            label.rectTransform.localPosition = center;

            animating = false;
        }

        private Vector3 GetOutOffset( SlideDirection direction )
        {
            switch ( direction )
            {
                case SlideDirection.Left:
                    return new Vector3( -slidePixels, 0f, 0f );

                case SlideDirection.Right:
                    return new Vector3( slidePixels, 0f, 0f );

                case SlideDirection.Up:
                    return new Vector3( 0f, slidePixels, 0f );

                case SlideDirection.Down:
                    return new Vector3( 0f, -slidePixels, 0f );
            }

            return Vector3.zero;
        }

        private Vector3 GetInOffset( SlideDirection direction )
        {
            return -GetOutOffset( direction );
        }

        private SlideDirection CurrentSlideDirection()
        {
            return isOn ? onSlideDirection : offSlideDirection;
        }

        private static Color Transparent( Color color )
        {
            return new Color( color.r, color.g, color.b, 0f );
        }

        private string CurrentText()
        {
            return isOn ? onText : offText;
        }

        private string CurrentCommand()
        {
            return isOn ? onCommand : offCommand;
        }

        private Color CurrentColor()
        {
            return isOn ? onColor : offColor;
        }

        private Color CurrentTextColor()
        {
            return isOn ? onTextColor : offTextColor;
        }
    }
}

#endif