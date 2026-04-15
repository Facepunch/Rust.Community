using UnityEngine;
using UnityEngine.EventSystems;
using Application = Rust.Application;

public partial class CommunityEntity
{
    private enum CUiEventType
    {
        OnDestroy,
        OnPointerUp,
        OnPointerDown,
        OnPointerEnter,
        OnPointerExit,
        OnPointerClick
    }
    
#if SERVER
    [RPC_Server]
    public void CUIEventRPC(RPCMessage rpc)
    {
        CUiEventType type = (CUiEventType)rpc.read.Int32();
        string name = rpc.read.String();
        switch (type)
        {
            case CUiEventType.OnDestroy:
                Hook_OnDestroy(rpc.player, name);
                break;
            case CUiEventType.OnPointerEnter:
                Hook_OnPointerEnter(rpc.player, name);
                break;
            case CUiEventType.OnPointerExit:
                Hook_OnPointerExit(rpc.player, name);
                break;
            case CUiEventType.OnPointerUp:
            {
                PointerEventData.InputButton button = (PointerEventData.InputButton)rpc.read.Int32();
                Vector2 position = rpc.read.Vector3();
                Hook_OnPointerUp(rpc.player, name, button, position);
                break;
            }
            case CUiEventType.OnPointerDown:
            {
                PointerEventData.InputButton button = (PointerEventData.InputButton)rpc.read.Int32();
                Vector2 position = rpc.read.Vector3();
                Hook_OnPointerDown(rpc.player, name, button, position);
                break;
            }
            case CUiEventType.OnPointerClick:
            {
                PointerEventData.InputButton button = (PointerEventData.InputButton)rpc.read.Int32();
                Vector2 position = rpc.read.Vector3();
                Hook_OnPointerClick(rpc.player, name, button, position);
                break;
            }
        }
    }

    private void Hook_OnDestroy(BasePlayer player, string name)
    {
        
    }

    private void Hook_OnPointerEnter(BasePlayer player, string name)
    {
        
    }
    
    private void Hook_OnPointerExit(BasePlayer player, string name)
    {
        
    }
    
    private void Hook_OnPointerUp(BasePlayer player, string name, PointerEventData.InputButton button, Vector2 position)
    {
        
    }
    
    private void Hook_OnPointerDown(BasePlayer player, string name, PointerEventData.InputButton button, Vector2 position)
    {
        
    }
    
    private void Hook_OnPointerClick(BasePlayer player, string name, PointerEventData.InputButton button, Vector2 position)
    {
        
    }
#endif
    
#if CLIENT
    
    private abstract class BaseEventHandler : MonoBehaviour
    {
        private RectTransform _rectTransform;
        public bool allowChildren;
        protected const string RPC = "CUIEventRPC";
        
        private void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
        }
        
        public void SetAllowChildren(bool allowChildren) => this.allowChildren = allowChildren;
        protected bool CanSendEvent(GameObject go) => allowChildren || go == gameObject;

        /// <summary>
        /// Returns the local position of the event relative to the bottom left corner of the element
        /// </summary>
        protected Vector2 GetLocalPosition(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            
            // Subtract the rect's x and y (which are relative to the pivot)
            // to get the position starting from (0,0) at the bottom-left.
            float bottomLeftX = localPoint.x - _rectTransform.rect.x;
            float bottomLeftY = localPoint.y - _rectTransform.rect.y;

            Vector2 finalPos = new Vector2(bottomLeftX, bottomLeftY);
            return finalPos;
        }
    }
    
    private class OnDestroyEventHandler : BaseEventHandler
    {
        private void OnDestroy()
        {
            //Only fire the event if we are in-game and not quitting
            if (Client.IsIngame && !Application.isQuitting)
            {
                ClientInstance?.ServerRPC(RPC, (int)CUiEventType.OnDestroy, gameObject.name);
            }
        }
    }
    
    private class PointerUpEventHandler : BaseEventHandler, IPointerUpHandler
    {
        public void OnPointerUp(PointerEventData eventData)
        {
            if (CanSendEvent(eventData.rawPointerPress))
            {
                ClientInstance.ServerRPC(RPC, (int)CUiEventType.OnPointerUp, gameObject.name, (int)eventData.button, (Vector3)GetLocalPosition(eventData));
            }
        }
    }
    
    private class PointerDownEventHandler : BaseEventHandler, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            if (CanSendEvent(eventData.pointerPressRaycast.gameObject))
            {
                ClientInstance.ServerRPC(RPC, (int)CUiEventType.OnPointerDown, gameObject.name, (int)eventData.button, (Vector3)GetLocalPosition(eventData));
            }
        }
    }
    
    private class PointerEnterEventHandler : BaseEventHandler, IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (CanSendEvent(eventData.pointerEnter))
            {
                ClientInstance.ServerRPC(RPC, (int)CUiEventType.OnPointerEnter, gameObject.name);
            }
        }
    }
    
    private class PointerExitEventHandler : BaseEventHandler, IPointerExitHandler
    {
        public void OnPointerExit(PointerEventData eventData)
        {
            if (CanSendEvent(eventData.pointerEnter))
            {
                ClientInstance.ServerRPC(RPC, (int)CUiEventType.OnPointerExit, gameObject.name);
            }
        }
    }
    
    private class PointerClickEventHandler : BaseEventHandler, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (CanSendEvent(eventData.rawPointerPress))
            {
                ClientInstance.ServerRPC(RPC, (int)CUiEventType.OnPointerClick, gameObject.name, (int)eventData.button, (Vector3)GetLocalPosition(eventData));
            }
        }
    }
#endif
}