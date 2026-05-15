using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class CommunityEntity
{
#if CLIENT
    public enum HoverPreviewContentAlignment
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    public struct HoverPreviewStyle
    {
        public Font TitleFont;
        public Font DescriptionFont;

        public int TitleFontSize;
        public int DescriptionFontSize;

        public Color TitleColor;
        public Color DescriptionColor;
        public Color ImageColor;
        public Color BackgroundColor;

        public float Spacing;
        public float ImageHeight;
        public HoverPreviewContentAlignment ContentAlignment;

        public bool UseFade;
        public float FadeInDuration;
        public float FadeOutDuration;
        public bool WaitFadeOutBeforeReplace;
    }

    public class HoverPreviewComponent : UIBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public RectTransform previewRoot;

        public string title = string.Empty;
        public string description = string.Empty;
        public Sprite image;

        public bool hidePreviewOnExit = false;
        public bool useFade = true;
        public float fadeInDuration = 0.12f;
        public float fadeOutDuration = 0.08f;
        public bool waitFadeOutBeforeReplace = false;

        public Font titleFont;
        public Font descriptionFont;
        public int titleFontSize = 22;
        public int descriptionFontSize = 16;
        public Color titleColor = Color.white;
        public Color descriptionColor = new Color( 1f, 1f, 1f, 0.75f );
        public Color imageColor = Color.white;
        public Color backgroundColor = new Color( 0f, 0f, 0f, 0f );

        public float spacing = 10f;
        public float imageHeight = 220f;
        public HoverPreviewContentAlignment contentAlignment = HoverPreviewContentAlignment.TopLeft;

        public bool highlightOnHover = false;
        public Color hoverColor = new Color( 1f, 1f, 1f, 0.12f );

        private Graphic targetGraphic;
        private Color originalColor;
        private bool hasOriginalColor;

        private HoverPreviewRoot previewController;
        private bool isHovered;
        private bool initialized;

        public void Init()
        {
            if ( initialized )
                return;

            initialized = true;

            targetGraphic = GetComponent<Graphic>();

            if ( targetGraphic != null )
            {
                originalColor = targetGraphic.color;
                hasOriginalColor = true;
                targetGraphic.raycastTarget = true;
            }

            if ( titleFont == null )
                titleFont = CommunityEntity.ClientInstance.LoadFont( "RobotoCondensed-Bold.ttf" );

            if ( descriptionFont == null )
                descriptionFont = CommunityEntity.ClientInstance.LoadFont( "RobotoCondensed-Regular.ttf" );
        }

        public void OnPointerEnter( PointerEventData eventData )
        {
            if ( !enabled )
                return;

            if ( isHovered )
                return;

            isHovered = true;

            ApplyHoverColor();

            HoverPreviewRoot controller = TryGetPreviewController();

            if ( controller == null )
                return;

            HoverPreviewStyle style = new HoverPreviewStyle
            {
                TitleFont = titleFont,
                DescriptionFont = descriptionFont,
                TitleFontSize = titleFontSize,
                DescriptionFontSize = descriptionFontSize,
                TitleColor = titleColor,
                DescriptionColor = descriptionColor,
                ImageColor = imageColor,
                BackgroundColor = backgroundColor,
                Spacing = spacing,
                ImageHeight = imageHeight,
                ContentAlignment = contentAlignment,
                UseFade = useFade,
                FadeInDuration = fadeInDuration,
                FadeOutDuration = fadeOutDuration,
                WaitFadeOutBeforeReplace = waitFadeOutBeforeReplace
            };

            controller.Show( title, description, image, style );
        }

        public void OnPointerExit( PointerEventData eventData )
        {
            if ( !isHovered )
                return;

            isHovered = false;

            RestoreColor();

            if ( !hidePreviewOnExit )
                return;

            HoverPreviewRoot controller = TryGetPreviewController();

            if ( controller != null )
                controller.Hide( useFade, fadeOutDuration );
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            isHovered = false;
            RestoreColor();

            if ( hidePreviewOnExit && previewController != null )
                previewController.HideInstant();
        }

        private HoverPreviewRoot TryGetPreviewController()
        {
            if ( previewRoot == null )
                return null;

            if ( previewController != null )
                return previewController;

            previewController = previewRoot.GetComponent<HoverPreviewRoot>();

            if ( previewController == null )
                previewController = previewRoot.gameObject.AddComponent<HoverPreviewRoot>();

            previewController.Initialize();

            return previewController;
        }

        private void ApplyHoverColor()
        {
            if ( !highlightOnHover || targetGraphic == null )
                return;

            if ( !hasOriginalColor )
            {
                originalColor = targetGraphic.color;
                hasOriginalColor = true;
            }

            targetGraphic.color = hoverColor;
        }

        private void RestoreColor()
        {
            if ( !highlightOnHover || targetGraphic == null || !hasOriginalColor )
                return;

            targetGraphic.color = originalColor;
        }
    }

    public class HoverPreviewRoot : MonoBehaviour
    {
        private RectTransform rootRect;

        private RectTransform container;
        private CanvasGroup containerCanvasGroup;
        private Image containerBackground;

        private RectTransform contentRoot;

        private Coroutine transitionCoroutine;
        private Coroutine fadeCoroutine;
        private bool initialized;
        private bool hasContent;

        public void Initialize()
        {
            if ( initialized )
                return;

            initialized = true;

            rootRect = GetComponent<RectTransform>();

            if ( rootRect == null )
                rootRect = gameObject.AddComponent<RectTransform>();

            CreateContainer();
            CreateContentRoot();
            DisablePreviewRaycasts();
        }

        public void Show( string title, string description, Sprite image, HoverPreviewStyle style )
        {
            Initialize();

            StopTransition();

            transitionCoroutine = StartCoroutine( ShowRoutine( title, description, image, style ) );
        }

        private IEnumerator ShowRoutine( string title, string description, Sprite image, HoverPreviewStyle style )
        {
            if ( !gameObject.activeSelf )
                gameObject.SetActive( true );

            if ( !container.gameObject.activeSelf )
                container.gameObject.SetActive( true );

            bool hadContentBeforeBuild = hasContent;

            bool shouldWaitOldFadeOut =
                style.WaitFadeOutBeforeReplace &&
                style.UseFade &&
                hasContent &&
                containerCanvasGroup.alpha > 0.01f &&
                style.FadeOutDuration > 0f;

            if ( shouldWaitOldFadeOut )
                yield return FadeTo( 0f, style.FadeOutDuration );

            ApplyRootStyle( style );
            ClearContent();
            BuildContent( title, description, image, style );
            DisablePreviewRaycasts();

            LayoutRebuilder.ForceRebuildLayoutImmediate( contentRoot );
            LayoutRebuilder.ForceRebuildLayoutImmediate( container );
            LayoutRebuilder.ForceRebuildLayoutImmediate( rootRect );

            hasContent = true;

            if ( !style.UseFade || style.FadeInDuration <= 0f )
            {
                containerCanvasGroup.alpha = 1f;
                transitionCoroutine = null;
                yield break;
            }

            if ( !hadContentBeforeBuild || shouldWaitOldFadeOut || containerCanvasGroup.alpha <= 0.01f )
                containerCanvasGroup.alpha = 0f;

            yield return FadeTo( 1f, style.FadeInDuration );

            transitionCoroutine = null;
        }

        public void Hide( bool useFade, float duration )
        {
            Initialize();

            StopTransition();

            if ( !container.gameObject.activeSelf )
                return;

            if ( !useFade || duration <= 0f )
            {
                HideInstant();
                return;
            }

            transitionCoroutine = StartCoroutine( HideRoutine( duration ) );
        }

        private IEnumerator HideRoutine( float duration )
        {
            yield return FadeTo( 0f, duration );

            container.gameObject.SetActive( false );
            hasContent = false;
            transitionCoroutine = null;
        }

        public void HideInstant()
        {
            Initialize();

            StopTransition();

            containerCanvasGroup.alpha = 0f;
            container.gameObject.SetActive( false );
            hasContent = false;
        }

        public void ClearContent()
        {
            Initialize();

            if ( contentRoot == null )
                return;

            for ( int i = contentRoot.childCount - 1; i >= 0; i-- )
            {
                Transform child = contentRoot.GetChild( i );

                if ( Application.isPlaying )
                    Destroy( child.gameObject );
                else
                    DestroyImmediate( child.gameObject );
            }

            hasContent = false;
        }

        private void CreateContainer()
        {
            Transform existing = transform.Find( "__HoverPreviewContainer" );

            if ( existing != null )
                container = existing as RectTransform;

            if ( container == null )
            {
                GameObject containerObject = new GameObject(
                    "__HoverPreviewContainer",
                    typeof( RectTransform ),
                    typeof( CanvasGroup ),
                    typeof( Image )
                );

                containerObject.transform.SetParent( transform, false );
                container = containerObject.GetComponent<RectTransform>();
            }

            container.anchorMin = Vector2.zero;
            container.anchorMax = Vector2.one;
            container.pivot = new Vector2( 0.5f, 0.5f );
            container.offsetMin = Vector2.zero;
            container.offsetMax = Vector2.zero;

            containerCanvasGroup = container.GetComponent<CanvasGroup>();

            if ( containerCanvasGroup == null )
                containerCanvasGroup = container.gameObject.AddComponent<CanvasGroup>();

            containerCanvasGroup.interactable = false;
            containerCanvasGroup.blocksRaycasts = false;
            containerCanvasGroup.alpha = 0f;

            containerBackground = container.GetComponent<Image>();

            if ( containerBackground == null )
                containerBackground = container.gameObject.AddComponent<Image>();

            containerBackground.raycastTarget = false;
        }

        private void CreateContentRoot()
        {
            Transform existing = container.Find( "__HoverPreviewContent" );

            if ( existing != null )
                contentRoot = existing as RectTransform;

            if ( contentRoot == null )
            {
                GameObject contentObject = new GameObject(
                    "__HoverPreviewContent",
                    typeof( RectTransform ),
                    typeof( VerticalLayoutGroup )
                );

                contentObject.transform.SetParent( container, false );
                contentRoot = contentObject.GetComponent<RectTransform>();
            }

            contentRoot.anchorMin = Vector2.zero;
            contentRoot.anchorMax = Vector2.one;
            contentRoot.pivot = new Vector2( 0.5f, 0.5f );
            contentRoot.offsetMin = Vector2.zero;
            contentRoot.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = contentRoot.GetComponent<VerticalLayoutGroup>();

            if ( layout == null )
                layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.padding = new RectOffset( 0, 0, 0, 0 );
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private void ApplyRootStyle( HoverPreviewStyle style )
        {
            if ( style.BackgroundColor.a > 0f )
            {
                containerBackground.color = style.BackgroundColor;
                containerBackground.enabled = true;
                containerBackground.raycastTarget = false;
            }
            else
            {
                containerBackground.enabled = false;
                containerBackground.raycastTarget = false;
            }

            VerticalLayoutGroup layout = contentRoot.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset( 0, 0, 0, 0 );
            layout.spacing = style.Spacing;
            layout.childAlignment = ToLayoutAlignment( style.ContentAlignment );
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private void BuildContent( string title, string description, Sprite image, HoverPreviewStyle style )
        {
            bool hasTitle = !string.IsNullOrEmpty( title );
            bool hasDescription = !string.IsNullOrEmpty( description );
            bool hasImage = image != null;

            TextAnchor textAlignment = ToTextAlignment( style.ContentAlignment );

            if ( hasTitle )
            {
                CreateText(
                    "Title",
                    title,
                    style.TitleFont,
                    style.TitleFontSize,
                    FontStyle.Bold,
                    style.TitleColor,
                    textAlignment
                );
            }

            if ( hasDescription )
            {
                CreateText(
                    "Description",
                    description,
                    style.DescriptionFont,
                    style.DescriptionFontSize,
                    FontStyle.Normal,
                    style.DescriptionColor,
                    textAlignment
                );
            }

            if ( hasImage )
            {
                CreateImage(
                    "Image",
                    image,
                    style.ImageColor,
                    style.ImageHeight
                );
            }
        }

        private Text CreateText(
            string objectName,
            string value,
            Font font,
            int fontSize,
            FontStyle fontStyle,
            Color color,
            TextAnchor alignment
        )
        {
            GameObject go = new GameObject(
                objectName,
                typeof( RectTransform ),
                typeof( CanvasRenderer ),
                typeof( Text ),
                typeof( ContentSizeFitter ),
                typeof( LayoutElement )
            );

            go.transform.SetParent( contentRoot, false );

            float width = GetAvailableContentWidth();

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2( 0f, 1f );
            rect.anchorMax = new Vector2( 0f, 1f );
            rect.pivot = GetChildPivot( alignment );
            rect.sizeDelta = new Vector2( width, 0f );

            Text text = go.GetComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            ContentSizeFitter fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement layoutElement = go.GetComponent<LayoutElement>();
            layoutElement.minHeight = fontSize + 4f;
            layoutElement.preferredWidth = width;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            return text;
        }

        private Image CreateImage(
            string objectName,
            Sprite sprite,
            Color color,
            float height
        )
        {
            GameObject go = new GameObject(
                objectName,
                typeof( RectTransform ),
                typeof( CanvasRenderer ),
                typeof( Image ),
                typeof( LayoutElement )
            );

            go.transform.SetParent( contentRoot, false );

            float width = height;

            if ( sprite != null && sprite.rect.height > 0f )
                width = height * ( sprite.rect.width / sprite.rect.height );

            width = Mathf.Min( width, GetAvailableContentWidth() );

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2( 0f, 1f );
            rect.anchorMax = new Vector2( 0f, 1f );
            rect.pivot = new Vector2( 0.5f, 1f );
            rect.sizeDelta = new Vector2( width, height );

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;

            LayoutElement layoutElement = go.GetComponent<LayoutElement>();
            layoutElement.minWidth = width;
            layoutElement.preferredWidth = width;
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            return img;
        }

        private float GetAvailableContentWidth()
        {
            if ( rootRect == null )
                return 0f;

            return Mathf.Max( 0f, rootRect.rect.width );
        }

        private IEnumerator FadeTo( float targetAlpha, float duration )
        {
            if ( fadeCoroutine != null )
            {
                StopCoroutine( fadeCoroutine );
                fadeCoroutine = null;
            }

            fadeCoroutine = StartCoroutine( Fade( targetAlpha, duration ) );

            yield return fadeCoroutine;

            fadeCoroutine = null;
        }

        private IEnumerator Fade( float targetAlpha, float duration )
        {
            float startAlpha = containerCanvasGroup.alpha;
            float time = 0f;
            float safeDuration = Mathf.Max( 0.01f, duration );

            while ( time < safeDuration )
            {
                time += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01( time / safeDuration );
                t = t * t * ( 3f - 2f * t );

                containerCanvasGroup.alpha = Mathf.Lerp( startAlpha, targetAlpha, t );

                yield return null;
            }

            containerCanvasGroup.alpha = targetAlpha;
        }

        private void StopTransition()
        {
            if ( transitionCoroutine != null )
            {
                StopCoroutine( transitionCoroutine );
                transitionCoroutine = null;
            }

            if ( fadeCoroutine != null )
            {
                StopCoroutine( fadeCoroutine );
                fadeCoroutine = null;
            }
        }

        private void DisablePreviewRaycasts()
        {
            if ( containerCanvasGroup != null )
            {
                containerCanvasGroup.interactable = false;
                containerCanvasGroup.blocksRaycasts = false;
            }

            if ( containerBackground != null )
                containerBackground.raycastTarget = false;

            if ( contentRoot == null )
                return;

            Graphic[] graphics = contentRoot.GetComponentsInChildren<Graphic>( true );

            for ( int i = 0; i < graphics.Length; i++ )
                graphics[i].raycastTarget = false;
        }

        private static TextAnchor ToLayoutAlignment( HoverPreviewContentAlignment alignment )
        {
            switch ( alignment )
            {
                case HoverPreviewContentAlignment.TopLeft:
                    return TextAnchor.UpperLeft;

                case HoverPreviewContentAlignment.TopCenter:
                    return TextAnchor.UpperCenter;

                case HoverPreviewContentAlignment.TopRight:
                    return TextAnchor.UpperRight;

                case HoverPreviewContentAlignment.MiddleLeft:
                    return TextAnchor.MiddleLeft;

                case HoverPreviewContentAlignment.MiddleCenter:
                    return TextAnchor.MiddleCenter;

                case HoverPreviewContentAlignment.MiddleRight:
                    return TextAnchor.MiddleRight;

                case HoverPreviewContentAlignment.BottomLeft:
                    return TextAnchor.LowerLeft;

                case HoverPreviewContentAlignment.BottomCenter:
                    return TextAnchor.LowerCenter;

                case HoverPreviewContentAlignment.BottomRight:
                    return TextAnchor.LowerRight;

                default:
                    return TextAnchor.UpperLeft;
            }
        }

        private static TextAnchor ToTextAlignment( HoverPreviewContentAlignment alignment )
        {
            switch ( alignment )
            {
                case HoverPreviewContentAlignment.TopCenter:
                case HoverPreviewContentAlignment.MiddleCenter:
                case HoverPreviewContentAlignment.BottomCenter:
                    return TextAnchor.UpperCenter;

                case HoverPreviewContentAlignment.TopRight:
                case HoverPreviewContentAlignment.MiddleRight:
                case HoverPreviewContentAlignment.BottomRight:
                    return TextAnchor.UpperRight;

                default:
                    return TextAnchor.UpperLeft;
            }
        }

        private static Vector2 GetChildPivot( TextAnchor alignment )
        {
            switch ( alignment )
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    return new Vector2( 0.5f, 1f );

                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    return new Vector2( 1f, 1f );

                default:
                    return new Vector2( 0f, 1f );
            }
        }
    }
#endif
}