using Object = UnityEngine.Object;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using System.IO;

#if CLIENT


public partial class CommunityEntity
{

    public class MouseListener : UIBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {

        public string name;
        public Action<string> onEnter;
        public Action<string> onExit;
        public Action<string> onClick;

        void Awake(){
            name = gameObject.name;
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if(onClick != null) onClick(name);
            // Manually Bubble it up
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.pointerClickHandler);
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if(onEnter != null) onEnter(name);
            // Manually Bubble it up
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.pointerEnterHandler);
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if(onExit != null) onExit(name);
            // Manually Bubble it up
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.pointerExitHandler);
        }
    }
}
#endif
