
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

        public static List<Animation> pendingListeners = new List<Animation>();

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
    public void ScheduleMouseListener(string name, Animation anim){
        anim.mouseTarget = name;
        if(!MouseListener.pendingListeners.Contains(anim)) MouseListener.pendingListeners.Add(anim);
    }

    public void ApplyMouseListeners(){
        foreach(var anim in MouseListener.pendingListeners){
            if(string.IsNullOrEmpty(anim.mouseTarget)) continue;
            ApplyMouseListener(anim.mouseTarget, anim);
        }
        MouseListener.pendingListeners.Clear();
    }
    public void ApplyMouseListener(string name, Animation anim){
        GameObject hObj = FindPanel(name);
        if(!hObj) return;

        var c = hObj.GetComponent<MouseListener>();
        if(!c) c = hObj.AddComponent<MouseListener>();

        c.onEnter += new Action(anim.OnHoverEnter);
        c.onExit += new Action(anim.OnHoverExit);
        c.onClick += new Action(anim.OnClick);

    }
    public void RemoveMouseListener(string name, Animation anim){
        GameObject hObj = FindPanel(name);
        if(!hObj) return;

        var c = hObj.GetComponent<MouseListener>();
        if(!c) return;

        c.onEnter -= new Action(anim.OnHoverEnter);
        c.onExit -= new Action(anim.OnHoverExit);
        c.onClick -= new Action(anim.OnClick);

        anim.mouseTarget = "";
    }
}
#endif
