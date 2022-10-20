using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using System.IO;

#if CLIENT

public partial class CommunityEntity
{
	private class Animation : MonoBehaviour
	{
		public List<AnimationProperty> properties = new List<AnimationProperty>();
		
		public void Start()
		{
			foreach(var prop in properties)
			{
				prop.routine = StartCoroutine(prop.Animate());
			}
		}
		
		public void Kill()
		{
			foreach(var prop in properties)
			{
				StopCoroutine(prop.routine);
			}
			properties.Clear();
		}
	}
	
	private class AnimationProperty
	{
		public float duration = 0f;
		public float delay = 0f;
		public bool repeat = false;
		public bool repeatDelay = 0f;
		public string type;
		public string from;
		public string to;
		
		public Coroutine routine;
		
		public IEnumerator Animate()
		{
			if(delay > 0f) yield return new WaitForSeconds(delay);
			
			if(repeatDelay <= 0f) repeatDelay = duration;
      
			do
			{
				AnimateProperty();
				if(repeatDelay > 0f) yield return new WaitForSeconds(repeatDelay);
			}
			while(repeat);
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
						
						foreach(var c in gameObject.GetComponents<UnityEngine.UI.Graphic>()){
							if(fromOpacity != null) c.canvasRenderer.SetAlpha(fromOpacity);
							c.CrossFadeAlpha(toOpacity, duration, true);
						}
						
						break;
					}
				case "Color":
					{
						Color fromColor = ColorEx.Parse(from);
						Color toColor = ColorEx.Parse(to);
						
						//dont use the from color if the from value was ommited
						bool useFrom = string.IsNullOrEmpty(from);
						
						foreach(var c in gameObject.GetComponents<UnityEngine.UI.Graphic>()){
							if(useFrom) c.canvasRenderer.SetColor(fromColor);
							c.CrossFadeColor(toColor, duration, true, false);
						}
						
						break;
					}
				case "ColorWithAlpha":
					{
						Color fromColor = ColorEx.Parse(from);
						Color toColor = ColorEx.Parse(to);
						
						//dont use the from color if the from value was ommited
						bool useFrom = string.IsNullOrEmpty(from);
						
						foreach(var c in gameObject.GetComponents<UnityEngine.UI.Graphic>()){
							if(useFrom) c.canvasRenderer.SetColor(fromColor);
							c.CrossFadeColor(toColor, duration, true, true);
						}
						
						break;
					}
			}
		}
	}
}

#endif
