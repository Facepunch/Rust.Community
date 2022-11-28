
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

        public static List<IMouseReceiver> pendingListeners = new List<IMouseReceiver>();

        public Action onEnter;
        public Action onExit;
        public Action onClick;

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if(onClick != null) onClick();
            // Manually Bubble it up
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.pointerClickHandler);
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if(onEnter != null) onEnter();
            // Manually Bubble it up
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.pointerEnterHandler);
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if(onExit != null) onExit();
            // Manually Bubble it up
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.pointerExitHandler);
        }
    }


    public void ScheduleMouseListener(string name, IMouseReceiver receiver){
        receiver.mouseTarget = name;
        if(!MouseListener.pendingListeners.Contains(receiver)) MouseListener.pendingListeners.Add(receiver);
    }

    public void ApplyMouseListeners(){
        foreach(var receiver in MouseListener.pendingListeners){
            if(string.IsNullOrEmpty(receiver.mouseTarget)) continue;
            ApplyMouseListener(receiver.mouseTarget, receiver);
        }
        MouseListener.pendingListeners.Clear();
    }
    public void ApplyMouseListener(string name, IMouseReceiver receiver){
        GameObject hObj = FindPanel(name);
        if(!hObj) return;

        var c = hObj.GetComponent<MouseListener>();
        if(!c) c = hObj.AddComponent<MouseListener>();

        c.onEnter += receiver.OnHoverEnter;
        c.onExit += receiver.OnHoverExit;
        c.onClick += receiver.OnClick;

    }
    public void RemoveMouseListener(string name, IMouseReceiver receiver){
        GameObject hObj = FindPanel(name);
        if(!hObj) return;

        var c = hObj.GetComponent<MouseListener>();
        if(!c) return;

        c.onEnter -= receiver.OnHoverEnter;
        c.onExit -= receiver.OnHoverExit;
        c.onClick -= receiver.OnClick;

        receiver.mouseTarget = "";
    }
}
#endif
