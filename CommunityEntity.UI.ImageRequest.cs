using UnityEngine;
using UnityEngine.UI;

#if CLIENT

public partial class CommunityEntity
{
    public struct ImageRequest
    {
        public Vector4? slice;

        public MaskableGraphic graphic;
    }
}

#endif