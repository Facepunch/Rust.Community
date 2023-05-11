using Object = UnityEngine.Object;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch;
using Facepunch.Extend;
using System.IO;

#if CLIENT

public partial class CommunityEntity
{

    public class Draggable : UIBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IDropHandler {

        //reusable for world corners
        public static Vector3[] corners = new Vector3[4];

        #region Config

        // if the draggable should be allowed to be dragged out of the parent's bounds
        public bool limitToParent = false;
        // how far the draggable should be allowed to be dragged, -1 to disable
        public float maxDistance = -1f;
        public float scaledMaxDistance => maxDistance * rt.lossyScale.x;
        // if the Draggable should be allowed to swap places with other draggables
        public bool allowSwapping = false;
        // if 2 Draggables get swapped, should their anchors get swapped aswell?
        public bool swapAnchors = false;
        // if false, the draggable will return to its anchor when dropped unless swapped with another draggable or parented to a slot
        public bool dropAnywhere = true;
        // the alpha the group should have while being dragged
        public float dragAlpha = 1f;
        // the filter tag to use when interacting with slots
        public string filter = null;
        // this setting allows us to somewhat customize what parent will be used for the limiting parent
        public int parentLimitIndex = 1;
        // used to add additional padding to the parent bounds check
        public Vector2 parentPadding = Vector2.zero;
        // what type of position should be sent back
        public PositionSendType positionRPC;
        // if true, the draggable will not return to its orignal position in the hirarchy, and will instead stay at the front of the dragParent
        public bool keepOnTop = false;

        public Vector2 anchorOffset = Vector2.zero;

        #endregion

        #region Values

        // references to components
        public CanvasGroup canvasGroup;
        public RectTransform rt;

        // transform references
        public Transform dragParent => (limitToParent ? limitParent : canvas);
        public RectTransform limitParent; // for the keeping it within bounds
        public Transform realParent; // the parent when its not being dragged
        public Transform canvas; // the first canvas this is in, gets parented to it while getting dragged
        public int index; // the original sibling index. used to re-insert the draggable in the correct place in its hirarchy

        // the world rect of the limitParent
        public Rect parentWorldRect;
        private Vector3 _scaleAtLastCache; // the scale when this was last cached, to check if it needs to be re-cached

        // a shadow object used to hold the draggable's initial position, this object's RectTransform matches the anchormin/max & offsetmin/max of the draggable
        // this keeps the initial position aligned to the parent after resizing occurs, regardless of if offsets or anchors are used
        public GameObject anchorObj;
        public Vector2 anchor => (Vector2)anchorObj.transform.position;

        // use to return the draggable if dropAnywhere is false, other scripts may set this value in their OnDrop calls
        public Vector2 lastDropPosition;
        public Vector2 offset; // distance the panel has been dragged from its anchor

        // a reference to the parent slot if inside of one
        public Slot slot;

        // set by other Scripts if the position was set, in those cases the script is responsible for sending the appropriate RPC
        public bool wasSnapped = false;

        private bool _initialized;

        #endregion

        #region Core

        // call to initialize the Draggable, marking it as ready
        public void Init(){
            // setup values
            rt = (transform as RectTransform);
            canvasGroup = GetComponent<CanvasGroup>();
            canvas = GetComponentInParent<Canvas>().transform;
            realParent = rt.parent;
            index = rt.GetSiblingIndex();

            // bounds setup
            FindParentLimit();

            // anchor setup
            if(anchorObj == null)
                CreateAnchor();
            lastDropPosition = anchor;

            _initialized = true;
        }

        public void OnDestroy(){
            if(anchorObj != null)
                UnityEngine.Object.Destroy(anchorObj);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if(!_initialized)
                return;

            if(ShouldDie())
                return;

            TryRefreshParentBounds();

            // set this again incase the game resizes since the last time this has been dropped
            lastDropPosition = rt.position;
            // center the draggable onto the mouse
            var mousePos = eventData.pointerCurrentRaycast.screenPosition;
            if(limitToParent){
                // ensure parent limits arent breached
                LimitToParent(mousePos - lastDropPosition);
            } else {
                rt.position = mousePos;
            }
            offset = mousePos - anchor;


            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = dragAlpha;
            realParent = rt.parent;
            rt.SetParent(dragParent);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if(!_initialized)
                return;
            //  set the offset after scaling
            offset += eventData.delta;

            // use distance constraint
            if(maxDistance > 0f){
                LimitToRange();
                return;
            }

            // use parent constraint
            if(limitToParent){
                LimitToParent(eventData.delta);
                return;
            }

            // no constraints
            rt.position = anchor + offset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if(!_initialized)
                return;

            if(ShouldDie())
                return;

            if(!wasSnapped)
                SendDragRPC();

            wasSnapped = false;

            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
            if(!keepOnTop){
                rt.SetParent(realParent);
                rt.SetSiblingIndex(index);
            }

            if(!dropAnywhere){
                rt.position = lastDropPosition;
                offset = lastDropPosition - anchor;
                return;
            }

            lastDropPosition = rt.position;
        }

        // to support swapping
        public void OnDrop(PointerEventData eventData)
        {
            if(!_initialized)
                return;

            if(ShouldDie())
                return;

            if(!allowSwapping)
                return;

            // if this panel is in a slot, let the slot handle the matching & potential swapping
            if(slot != null){
                // resends the event to the parent and up
                ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.dropHandler);
                return;
            }

            var draggedObj = eventData.pointerDrag.GetComponent<Draggable>();
            if(draggedObj == null || !draggedObj.allowSwapping)
                return;

            // if the 2 objects are on seperate canvases, dont swap
            if(draggedObj.canvas != canvas)
                return;

            // cant swap because the draggable's position is too far away
            if(scaledMaxDistance > 0f && Vector2.Distance(draggedObj.lastDropPosition, anchor) > scaledMaxDistance)
                return;

            // if swapping would violate the draggable's constraints
            if(draggedObj.limitToParent && !draggedObj.parentWorldRect.Contains(draggedObj.lastDropPosition))
                return;

            // incase a resize occured
            TryRefreshParentBounds();

            // if swapping would violate our own's constraints
            if(limitToParent && !parentWorldRect.Contains(draggedObj.lastDropPosition))
                return;

            Draggable.Swap(draggedObj, this);
        }

        #endregion

        #region helpers

        // checks if the distance dragged is larger than maxDistance & limits the position if so
        private void LimitToRange(){
            if(maxDistance < 0f)
                return;

            if(Vector2.Distance(offset, Vector2.zero) <= scaledMaxDistance)
                rt.position = anchor + offset;
            else
                rt.position = anchor + (offset.normalized * scaledMaxDistance);
        }
        // compares world rects to ensure the object stays within the parent
        private void LimitToParent(Vector2 delta){
            if(!limitToParent)
                return;

            var p = parentWorldRect;
            var c = GetWorldRect(rt);
            // check if applying the vector would put it out of pounds on any side
            bool inLeft = p.xMin <= c.xMin + delta.x - parentPadding.x;
            bool inTop = p.yMax >= c.yMax + delta.y + parentPadding.y;
            bool inRight = p.xMax >= c.xMax + delta.x + parentPadding.x;
            bool inBottom = p.yMin <= c.yMin + delta.y - parentPadding.y;

            Vector2 pos = rt.position;
            var mousePos = anchor + offset;
            float paddingX = (c.size.x/2);
            float paddingY = (c.size.y/2);

            if(inLeft && delta.x < 0f && (mousePos.x + paddingX) + parentPadding.x <= p.xMax)
                pos.x += delta.x; // if mouse isnt past the right edge
            else if(inRight && delta.x > 0f && (mousePos.x - paddingX) - parentPadding.x >= p.xMin)
                pos.x += delta.x; // if mouse isnt past the left edge
            if(inTop && delta.y > 0f && (mousePos.y - paddingY) - parentPadding.y >= p.yMin)
                pos.y += delta.y; // if mouse isnt past the bottom edge
            else if(inBottom && delta.y < 0f && (mousePos.y + paddingY) + parentPadding.y <= p.yMax)
                pos.y += delta.y; // if mouse isnt past the top edge

            rt.position = pos;

            /* for debugging
            c = GetWorldRect(rt);
            inLeft = p.xMin <= c.xMin;
            inTop = p.yMax >= c.yMax;
            inRight = p.xMax >= c.xMax;
            inBottom = p.yMin <= c.yMin;
            if(!inLeft || !inTop || !inRight || !inBottom){
                //rt.position = last;
                Debug.Log($"supposedly safe position is out of bounds! \n {inLeft} {inTop} {inRight} {inBottom} - {delta}");
            }
            */
        }

        // add a gameobject to reference as the anchor position, this makes the anchor position resizing proof
        private void CreateAnchor(){
            anchorObj = new GameObject("Shadow Anchor", typeof(RectTransform));
            var anchorRT = (anchorObj.transform as RectTransform);
            anchorRT.SetParent(rt.parent);
            anchorRT.anchorMin = rt.anchorMin;
            anchorRT.anchorMax = rt.offsetMax;
            anchorRT.offsetMin = rt.offsetMin;
            anchorRT.offsetMax = rt.offsetMax;
            anchorRT.localPosition = rt.localPosition;
            if(anchorOffset != Vector2.zero){
                anchorRT.offsetMin = new Vector2(anchorRT.offsetMin.x + anchorOffset.x, anchorRT.offsetMin.y + anchorOffset.y);
                anchorRT.offsetMax = new Vector2(anchorRT.offsetMax.x + anchorOffset.x, anchorRT.offsetMax.y + anchorOffset.y);
            }
        }

        // finds the parent to use as a limit based on the parentLimitIndex setting
        private void FindParentLimit(){
            limitParent = rt;
            for(int i = 0; i < parentLimitIndex; i++){
                limitParent = (limitParent.parent as RectTransform);
                Slot potentialSlot = limitParent.GetComponent<Slot>();
                if(potentialSlot){
                    // only set our parent as the slot if its actually the first one we encounter
                    if(slot == null){
                        slot = potentialSlot;
                        slot.content = this;
                    }
                    // always skip slots when looking for the limitParent
                    limitParent = (limitParent.parent as RectTransform);
                }

            }
            // force a refresh
            _scaleAtLastCache = Vector3.zero;
            TryRefreshParentBounds();
        }

        // check if this draggable should die, this covers if the real parent gets destroyed while this object is detached from it
        public bool ShouldDie(){
            if(realParent.gameObject != null)
                return false;

            UnityEngine.Object.Destroy(gameObject);

            return true;
        }

        // sets the appropiate transform index
        public void SetIndex(int index){
            this.index = index;
            rt.SetSiblingIndex(index);
        }
        // re-caches the parent's world rect
        public void TryRefreshParentBounds(){
            if(rt.lossyScale == _scaleAtLastCache)
                return;

            if(limitToParent)
                parentWorldRect = GetWorldRect(limitParent);

            _scaleAtLastCache = rt.lossyScale;
        }

        // used via the json API
        public void MoveToAnchor(){
            rt.position = anchor;
            lastDropPosition = rt.position;
            offset = Vector2.zero;
        }

        // used via the json API
        public void RebuildAnchor(){
            UnityEngine.Object.Destroy(anchorObj);
            CreateAnchor();
        }

        public Vector2 PositionForRPC(){
            Vector2 pos = rt.position;
            Rect parent = parentWorldRect;
            return positionRPC switch{
                PositionSendType.NormalizedScreen => new Vector2(pos.x / Screen.width, 1 - (pos.y / Screen.height)),
                PositionSendType.NormalizedParent => new Vector2((pos.x - parent.xMin) / parent.width, (pos.y - parent.yMin) / parent.height),
                PositionSendType.Relative => (pos - lastDropPosition) / rt.lossyScale,
                PositionSendType.RelativeAnchor => (pos - anchor) / rt.lossyScale,
            };
        }
        // packetsize go brrrr
        public void SendDragRPC(){
            ClientInstance.ServerRPC<string, Vector2, byte>("DragRPC", gameObject.name, PositionForRPC(), (byte)positionRPC);
        }

        // the same as the extension method, but without the allocation
        public Rect GetWorldRect(RectTransform transform){
            transform.GetWorldCorners(corners);
            return new Rect(corners[0], corners[2] - corners[0]);
        }

        // packetsize go brrrr
        public static void SendDropRPC(string draggedName, string draggedSlot, string swappedName, string swappedSlot){
            ClientInstance.ServerRPC<string, string, string, string>("DropRPC", draggedName, draggedSlot, swappedName, swappedSlot);
        }

        public static void Swap(Draggable from, Draggable to){
            // set this incase a resize occured since the last time this got dragged
            to.lastDropPosition = to.rt.position;

            // get variables of draggable
            var oldParent = from.realParent;
            var oldIndex = from.index;
            var oldPosition = from.lastDropPosition;
            var oldAnchor = from.anchor;
            var oldAnchorObj = from.anchorObj;
            var oldAnchorParent = from.anchorObj.transform.parent;
            var oldSlot = from.slot;

            // update the draggable
            from.lastDropPosition = to.lastDropPosition;
            from.rt.position = to.lastDropPosition;
            from.realParent = to.realParent;
            //from.rt.SetParent(to.realParent); // probably not needed because the from object should always be in drag mode
            from.SetIndex(to.index);
            if(from.swapAnchors){
                from.anchorObj = to.anchorObj;
                to.anchorObj.transform.SetParent(from.anchorObj.transform.parent);
            }
            from.offset = from.lastDropPosition - from.anchor;
            if(from.slot){
                from.slot.content = null;
                from.slot = to.slot;
                if(from.slot)
                    from.slot.content = from;
            }

            // update this panel
            to.lastDropPosition = oldPosition;
            to.rt.position = oldPosition;
            to.rt.SetParent(oldParent);
            to.realParent = oldParent;
            to.SetIndex(oldIndex);
            if(to.swapAnchors){
                to.anchorObj = oldAnchorObj;
                to.anchorObj.transform.SetParent(oldAnchorParent);
            }
            to.offset = to.lastDropPosition - to.anchor;
            if(oldSlot){
                to.slot = oldSlot;
                oldSlot.content = to;
            }
            from.wasSnapped = true;
            SendDropRPC(from.gameObject.name, from.slot?.gameObject.name, to.gameObject.name, to.slot?.gameObject.name);
        }

        #endregion

        public enum PositionSendType : byte{
            NormalizedScreen,
            NormalizedParent,
            Relative,
            RelativeAnchor
        }
    }
}
#endif
