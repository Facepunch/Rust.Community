#if CLIENT


public partial class CommunityEntity
{

    public interface IMouseReceiver{

        string mouseTarget {
            get;
            set;
        }

        void OnHoverEnter();

        void OnHoverExit();

        void OnClick();
    }
}

#endif
