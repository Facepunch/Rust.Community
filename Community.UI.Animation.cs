using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using System.IO;

#if CLIENT

public partial class CommunityEntity
{
	private class Animation : FacepunchBehaviour
	{
		public List<AnimationProperty> properties = new List<AnimationProperty>();
		
		public List<UnityEngine.UI.Graphic> cachedGraphics = new List<UnityEngine.UI.Graphic>();
		
		public bool isHidden = false;
		
		public bool isKilled = false;
		
		public void Start()
		{
			CacheGraphics();
			foreach(var prop in properties)
			{
				prop.anim = this;
				prop.routine = StartCoroutine(prop.Animate());
			}
		}
		
		public void Kill()
		{
			isKilled = true;
			foreach(var prop in properties)
			{
				StopCoroutine(prop.routine);
			}
			properties.Clear();
		}
		private void OnDestroy(){
			if(!isKilled) Kill();
		}
		
		public void CacheGraphics(){
			cachedGraphics.Clear();
			gameObject.GetComponents<UnityEngine.UI.Graphic>(cachedGraphics);
		}
		public void ToggleTCulling(bool b){
			foreach(var c in cachedGraphics){
				c.cullTransparentMesh = b;
			}
		}
		public void WaitToggleHidden(float delay = 0f){
			if(delay <= 0f) ToggleTCulling(true);
			else Invoke(new Action(() => {
				ToggleTCulling(true);
				isHidden = true;
			}), delay);
		}
	}
	
	private class AnimationProperty
	{
		public float duration = 0f;
		public float delay = 0f;
		public int repeat = 0;
		public bool repeatDelay = 0f;
		public string type;
		public string from;
		public string to;
		
		public Animation anim;
		
		public Coroutine routine;
		
		public int completedRounds = 0;
		
		public IEnumerator Animate()
		{
			if(delay > 0f) yield return new WaitForSeconds(delay);
			
			if(repeatDelay <= 0f) repeatDelay = duration;
      
			do
			{
				AnimateProperty();
				completedRounds++;
				if(repeatDelay > 0f) yield return new WaitForSeconds(repeatDelay);
				else  yield return null;
			}
			while(repeat < 0 || (repeat > 0 && completedRounds <= repeat));
		}
		
		public void AnimateProperty()
		{
			switch(type){
				case "Opacity":
					{
						float fromOpacity;
						float toOpacity;
						
						// we need a valid toOpacity
						if(!float.TryParse(to, out toOpacity)) break;
						// unlike the toOpacity, a from value is optional. if we omit a from value the crossfade will instead use the current alpha
						float.TryParse(from, out fromOpacity);
						// disable TCulling if the toOpacity or FromOpacity are Higher than 0f, if the fromOpacity check was ommited this wouldnt trigger if its an animation from > 0 to 0
						if(anim.isHidden && toOpacity > 0f || (fromOpacity != null && fromOpacity > 0f)){
							anim.ToggleTCulling(false);
							anim.isHidden = false;
						}
						foreach(var c in anim.cachedGraphics){
							if(fromOpacity != null) c.canvasRenderer.SetAlpha(fromOpacity);
							c.CrossFadeAlpha(toOpacity, duration, true);
						}
						if(!anim.isHidden && toOpacity <= 0f) anim.WaitToggleHidden(duration);
						
						break;
					}
				case "Color":
					{
						Color fromColor = ColorEx.Parse(from);
						Color toColor = ColorEx.Parse(to);
						
						//dont use the from color if the from value was ommited
						bool useFrom = string.IsNullOrEmpty(from);
						
						foreach(var c in anim.cachedGraphics){
							if(useFrom && !anim.isHidden) c.canvasRenderer.SetColor(fromColor);
							c.CrossFadeColor(toColor, (anim.isHidden ? 0f : duration), true, false);
						}
						
						break;
					}
				case "ColorWithAlpha":
					{
						Color fromColor = ColorEx.Parse(from);
						Color toColor = ColorEx.Parse(to);
						
						//dont use the from color if the from value was ommited
						bool useFrom = string.IsNullOrEmpty(from);
						if(anim.isHidden && toColor.a > 0f || (useFrom && fromColor.a > 0f)){
							anim.ToggleTCulling(false);
							anim.isHidden = false;
						}
						bool doInstant = toColor.a <= 0f && (useFrom && fromColor.a <= 0f) || anim.isHidden;
						foreach(var c in anim.cachedGraphics){
							if(useFrom && !doInstant) c.canvasRenderer.SetColor(fromColor);
							c.CrossFadeColor(toColor, (doInstant ? 0f : duration), true, true);
						}
						
						if(!anim.isHidden && toColor.a <= 0f) anim.WaitToggleHidden(duration);
						
						break;
					}
			}
		}
	}
}

#endif
