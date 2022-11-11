
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
            if(onEnter != null) onExit();
            // Manually Bubble it up
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.pointerExitHandler);
        }
    }

    public interface IMouseReceiver{

        string mouseTarget {
            get;
            set;
        }

        void OnHoverEnter();

        void OnHoverExit();

        void OnClick();
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

        c.onEnter += new Action(receiver.OnHoverEnter);
        c.onExit += new Action(receiver.OnHoverExit);
        c.onClick += new Action(receiver.OnClick);

    }
    public void RemoveMouseListener(string name, IMouseReceiver receiver){
        GameObject hObj = FindPanel(name);
        if(!hObj) return;

        var c = hObj.GetComponent<MouseListener>();
        if(!c) return;

        c.onEnter -= new Action(receiver.OnHoverEnter);
        c.onExit -= new Action(receiver.OnHoverExit);
        c.onClick -= new Action(receiver.OnClick);

        receiver.mouseTarget = "";
    }
}
#endif
