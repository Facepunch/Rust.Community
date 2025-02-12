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

    public class DraggableSlot : UIBehaviour, IDropHandler
    {


        #region Config

        // the filter tag for the slot. if both Draggable & slot have a filter tag that dont match the slot is denied
        public string filter = null;

        #endregion

        #region Values

        public Draggable content;

        public Transform canvas;

        private bool _initialized;

        #endregion

        #region Core

        // call to initialize the Draggable, marking it as ready
        public void Init()
        {
            content = GetComponentInChildren<Draggable>();
            canvas = GetComponentInParent<Canvas>().transform;

            _initialized = true;
        }

        // to support swapping
        public void OnDrop(PointerEventData eventData)
        {
            if (!_initialized)
                return;

            var draggedObj = eventData.pointerDrag.GetComponent<Draggable>();

            if (draggedObj.ShouldDie())
                return;

            if (draggedObj == content)
                return;

            // prevent sending the DragRPC regardless. because the player intended to attach it, not drag it.
            if (!draggedObj.dropAnywhere)
                draggedObj.wasSnapped = true;

            // if the dragging object is on seperate canvases, dont parent
            if (draggedObj.canvas != canvas)
                return;

            if (!FitsIntoSlot(draggedObj, this))
                return;

            // cant swap because the draggable's position is too far away
            if (draggedObj.scaledMaxDistance > 0f && Vector2.Distance(draggedObj.lastDropPosition, transform.position) > draggedObj.scaledMaxDistance)
                return;

            // if swapping would violate the draggable's constraints
            if (draggedObj.limitToParent && !draggedObj.parentWorldRect.Contains(eventData.pointerCurrentRaycast.screenPosition))
                return;

            if (content != null)
            {
                // cant swap because the draggable's position is too far away from the current content's anchor
                if (content.scaledMaxDistance > 0f && Vector2.Distance(draggedObj.lastDropPosition, content.anchor) > content.scaledMaxDistance)
                    return;

                // incase a resize occured
                content.TryRefreshParentBounds();

                // if swapping would violate the current content's constraints
                if (content.limitToParent && !content.parentWorldRect.Contains(draggedObj.lastDropPosition))
                    return;

                if (draggedObj.slot != null && !FitsIntoSlot(content, draggedObj.slot))
                    return;

                Draggable.Swap(draggedObj, content);
                return;
            }

            if (draggedObj.slot != null)
                draggedObj.slot.content = null;
            content = draggedObj;
            draggedObj.slot = this;
            draggedObj.realParent = this.transform;
            draggedObj.lastDropPosition = transform.position;
            if (draggedObj.dropAnywhere)
                draggedObj.rt.position = this.transform.position;
            draggedObj.offset = draggedObj.lastDropPosition - draggedObj.anchor;
            draggedObj.wasSnapped = true;
            Draggable.SendDropRPC(draggedObj.gameObject.name, draggedObj.slot?.gameObject.name, null, null);
        }

        #endregion

        #region Helpers

        // checks filters
        public static bool FitsIntoSlot(Draggable drag, DraggableSlot slot)
        {
            if (slot.filter == null)
                return true;

            return drag.filter == slot.filter;
        }

        #endregion
    }
}
#endif
