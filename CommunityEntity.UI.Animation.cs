using Object = UnityEngine.Object;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using System.IO;

#if CLIENT

public partial class CommunityEntity
{
	public class Animation : FacepunchBehaviour
	{
		public Dictionary<string, List<AnimationProperty>> properties = new Dictionary<string, List<AnimationProperty>>(){
			["Generic"] = new List<AnimationProperty>(),
			["OnDestroy"] = new List<AnimationProperty>(),
			["OnHoverEnter"] = new List<AnimationProperty>(),
			["OnHoverExit"] = new List<AnimationProperty>(),
			["OnClick"] = new List<AnimationProperty>(),
		};

		public string mouseTarget = "";

		public UnityEngine.UI.Graphic cachedGraphic;
		public RectTransform cachedRect;
		public bool shouldRaycast = false;

		public bool isHidden = false;
		public bool isKilled = false;

		public bool ValidCondition(string condition) => properties.ContainsKey(condition);

		public bool HasForCondition(string condition) => ValidCondition(condition) && properties[condition].Count > 0;

		public void StartAnimation(){
			CacheGraphicComponent();
			StartByCondition("Generic");
		}
		public void StartByCondition(string condition){
			if(!ValidCondition(condition)) return;
			foreach(var prop in properties[condition]){
				StartProperty(prop);
			}
		}
		public void StartProperty(AnimationProperty prop){
			prop.anim = this;
			prop.routine = StartCoroutine(prop.Animate());
		}

		public void StopByCondition(string condition){
			if(!ValidCondition(condition)) return;
			foreach(var prop in properties[condition]){
				if(prop.routine != null) StopCoroutine(prop.routine);
			}
		}

		public void Kill(bool destroyed = false)
		{
			isKilled = true;
			// stop all conditions except onDestroy
			StopByCondition("Generic");
			StopByCondition("OnHoverEnter");
			StopByCondition("OnHoverExit");
			float killDelay = 0f;
			// out early if the game object allready got destroyed
			if(destroyed) return;

			foreach(var prop in properties["OnDestroy"])
			{
				float totalDelay = prop.duration + prop.delay;
				if(killDelay < totalDelay) killDelay = totalDelay;
				StartProperty(prop);
			}

			Invoke(new Action(() => Object.Destroy(gameObject)), killDelay + 0.05f);
		}
		private void OnDestroy(){
			if(!isKilled) Kill(true);
		}

		public void CacheGraphicComponent(){
			cachedGraphic = gameObject.GetComponent<UnityEngine.UI.Graphic>();
			cachedRect = gameObject.GetComponent<RectTransform>();
			shouldRaycast = (cachedGraphic != null ? cachedGraphic.raycastTarget : false);
		}
		public void DisableGraphic(float delay = 0f){
			var a = new Action(() => {
				cachedGraphic.canvasRenderer.cullTransparentMesh = true;
				isHidden = true;
				cachedGraphic.raycastTarget = false;
			});
			if(delay <= 0f) a();
			else Invoke(a, delay);
		}
		public void EnableGraphic(float delay = 0f){
			var a = new Action(() => {
				cachedGraphic.canvasRenderer.cullTransparentMesh = false;
				isHidden = false;
				if(shouldRaycast) cachedGraphic.raycastTarget = true;
			});
			if(delay <= 0f) a();
			else Invoke(a, delay);
		}

		public void OnHoverEnter(){
			if(isKilled) return;
			StopByCondition("OnHoverExit");
			StartByCondition("OnHoverEnter");
		}
		public void OnHoverExit(){
			if(isKilled) return;

			StopByCondition("OnHoverEnter");
			StartByCondition("OnHoverExit");
		}
		public void OnClick(){
			if(isKilled) return;

			StopByCondition("OnClick");
			StartByCondition("OnClick");
		}
	}

	public class AnimationProperty : UnityEngine.Object
	{
		public float duration = 0f;
		public float delay = 0f;
		public int repeat = 0;
		public float repeatDelay = 0f;
		public string easing = "linear";
		public string type;
		public string from;
		public string to;

		public Animation anim;

		public Coroutine routine;

		public int completedRounds = 0;

		public IEnumerator Animate()
		{
			if(delay > 0f) yield return new WaitForSeconds(delay);

			do
			{
				yield return AnimateProperty();
				completedRounds++;
				if(repeatDelay > 0f) yield return new WaitForSeconds(repeatDelay);
				else  yield return null;
			}
			while(repeat < 0 || (repeat > 0 && completedRounds <= repeat));
		}

		public IEnumerator AnimateProperty()
		{
			if(from == null) from = "";
			if(!string.IsNullOrEmpty(to))
				switch(type){
					case "Opacity":
						{
							if(!anim.cachedGraphic) break;
							float fromOpacity;
							float toOpacity;

							// we need a valid toOpacity
							if(!float.TryParse(to, out toOpacity)) break;
							// unlike the toOpacity, a from value is optional. if we omit a from value the crossfade will instead use the current alpha
							float.TryParse(from, out fromOpacity);
							// disable TCulling if the toOpacity or FromOpacity are Higher than 0f, if the fromOpacity check was ommited this wouldnt trigger if its an animation from > 0 to 0
							if(anim.isHidden && toOpacity > 0f || (!string.IsNullOrEmpty(from) && fromOpacity > 0f))
								anim.EnableGraphic(0f);


							if(!string.IsNullOrEmpty(from)) anim.cachedGraphic.canvasRenderer.SetAlpha(fromOpacity);
							// calling this wont actually start a tween, but it will stop any running color or alpha tweens, this is vital for stopping any potential fadeIns & Outs (otherwise the the tween and our own Animation will intersect)
							anim.cachedGraphic.CrossFadeAlpha(anim.cachedGraphic.canvasRenderer.GetAlpha(), 0f, true);

							if(!anim.isHidden && toOpacity <= 0f) anim.DisableGraphic(duration);
							return ChangeAlpha(toOpacity, duration, easing);
						}
					case "Color":
						{
							if(!anim.cachedGraphic) break;
							Color fromColor = ColorEx.Parse(from);
							Color toColor = ColorEx.Parse(to);

							//dont use the from color if the from value was ommited
							bool useFrom = !string.IsNullOrEmpty(from);
							if(anim.isHidden && toColor.a > 0f || (useFrom && fromColor.a > 0f))
								anim.EnableGraphic(0f);

							if(useFrom) anim.cachedGraphic.canvasRenderer.SetColor(fromColor);

							// Same as with alpha, stopping any tweens
							anim.cachedGraphic.CrossFadeColor(anim.cachedGraphic.canvasRenderer.GetColor(), 0f, true, true);

							if(!anim.isHidden && toColor.a <= 0f) anim.DisableGraphic(duration);
							return ChangeColor(toColor, duration, easing);
						}
					case "Scale":
						{
							Vector2 fromVec = Vector2Ex.Parse(from);
							Vector2 toVec = Vector2Ex.Parse(to);

							//dont use the from Vector if the from value was ommited
							bool useFrom = !string.IsNullOrEmpty(from);

							if(useFrom && anim.cachedRect) anim.cachedRect.localScale = new Vector3(fromVec.x, fromVec.y, 1f);

							return Scale(toVec, duration, easing);
						}
					case "Translate":
						{
							Vector2 toVec = Vector2Ex.Parse(to);
							Vector2 fromVec = Vector2Ex.Parse(from);

							if(!string.IsNullOrEmpty(from)) TranslateInstantly(fromVec);

							return Translate(toVec, duration, easing);
						}
					case "TranslatePX":
						{
							Vector2 toVec = Vector2Ex.Parse(to);
							Vector2 fromVec = Vector2Ex.Parse(from);

							if(!string.IsNullOrEmpty(from)) TranslatePXInstantly(fromVec);

							return TranslatePX(toVec, duration, easing);
						}
					case "Rotate":
						{
							Vector3 fromVec = Vector3Ex.Parse(from);
							Vector3 toVec = Vector3Ex.Parse(to);

							//dont use the from Vector if the from value was ommited
							bool useFrom = !string.IsNullOrEmpty(from);

							if(useFrom && anim.cachedRect) anim.cachedRect.rotation = Quaternion.Euler(fromVec);
							return Rotate(toVec, duration, easing);
						}
					case "MoveTo":
						{
							// ColorEx is basically a Vector4 with extra steps
							Color toCol = ColorEx.Parse(to);
							Vector4 toVec = new Vector4(toCol.r, toCol.g, toCol.b, toCol.a);


							if(!string.IsNullOrEmpty(from)){
								Color fromCol = ColorEx.Parse(from);
								Vector4 fromVec = new Vector4(fromCol.r, fromCol.g, fromCol.b, fromCol.a);
								// immediately move to the from vector
								MoveToInstantly(fromVec);
							}

							return MoveTo(toVec, duration, easing);
						}
					case "MoveToPX":
						{
							// ColorEx is basically a Vector4 with extra steps
							Color toCol = ColorEx.Parse(to);
							Vector4 toVec = new Vector4(toCol.r, toCol.g, toCol.b, toCol.a);


							if(!string.IsNullOrEmpty(from)){
								Color fromCol = ColorEx.Parse(from);
								Vector4 fromVec = new Vector4(fromCol.r, fromCol.g, fromCol.b, fromCol.a);
								// immediately move to the from vector
								MoveToPXInstantly(fromVec);
							}

							return MoveToPX(toVec, duration, easing);
						}
				}
			// Return an empty enumerator so the coroutine continues normally
			return new System.Object[0].GetEnumerator();
		}

		// absolute
		public IEnumerator ChangeAlpha(float toOpacity, float duration, string easing){
			float time = 0f;
			float old = anim.cachedGraphic.canvasRenderer.GetAlpha();
			while(time < duration){
				float opacity = Mathf.LerpUnclamped(old, toOpacity, Ease(easing, time / duration));
				anim.cachedGraphic.canvasRenderer.SetAlpha(opacity);
				time += Time.deltaTime;
				yield return null;
			}
			anim.cachedGraphic.canvasRenderer.SetAlpha(toOpacity);
		}
		// absolute
		public IEnumerator ChangeColor(Color toColor, float duration, string easing){
			float time = 0f;
			Color old = anim.cachedGraphic.canvasRenderer.GetColor();
			while(time < duration){
				Color col = Color.LerpUnclamped(old, toColor, Ease(easing, time / duration));
				anim.cachedGraphic.canvasRenderer.SetColor(col);
				time += Time.deltaTime;
				yield return null;
			}
			anim.cachedGraphic.canvasRenderer.SetColor(toColor);
		}

		// absolute
		public IEnumerator Scale(Vector2 scale, float duration, string easing){
			float time = 0f;
			var rt = anim.cachedRect;
			if(!rt) yield break;
			Vector2 old = new Vector2(rt.localScale.x, rt.localScale.y);
			Vector2 goal = new Vector2(scale.x, scale.y);
			Vector2 current;
			while(time < duration){
				current = Vector2.LerpUnclamped(old, goal, Ease(easing, time / duration));
				rt.localScale = new Vector3(current.x, current.y, 0f);

				time += Time.deltaTime;
				yield return null;
			}
			rt.localScale = new Vector3(scale.x, scale.y, 0f);
		}
		// absolute
		public IEnumerator Rotate(Vector3 rotation, float duration, string easing){
			float time = 0f;
			var rt = anim.cachedRect;
			if(!rt) yield break;
			Quaternion old = rt.rotation;
			Quaternion goal = Quaternion.Euler(rotation);
			while(time < duration){
				rt.rotation = Quaternion.LerpUnclamped(old, goal, Ease(easing, time / duration));

				time += Time.deltaTime;
				yield return null;
			}
			rt.rotation = goal;
		}

		// relative
		public IEnumerator Translate(Vector2 to, float duration, string easing){
			float time = 0f;
			var rt = anim.cachedRect;
			if(!rt) yield break;
			Vector4 old = new Vector4(rt.anchorMin.x, rt.anchorMin.y, rt.anchorMax.x, rt.anchorMax.y);
			Vector4 goal = new Vector4(old.x + to.x, old.y + to.y, old.z + to.x, old.w + to.y);
			Vector4 last = old;
			Vector4 current;
			while(time < duration){
				current = Vector4.LerpUnclamped(old, goal, Ease(easing, time / duration));
				Vector4 diff = current - last;
				rt.anchorMin += new Vector2(diff.x, diff.y);
				rt.anchorMax += new Vector2(diff.z, diff.w);

				last = current;
				time += Time.deltaTime;
				yield return null;
			}
			current = goal - last;
			rt.anchorMin += new Vector2(current.x, current.y);
			rt.anchorMax += new Vector2(current.z, current.w);
		}
		// Because Translate with duration = 0 wouldnt work unless it was returned to the coroutine
		public void TranslateInstantly(Vector2 to){
			float time = 0f;
			var rt = anim.cachedRect;
			if(!rt) return;
			Vector4 old = new Vector4(rt.anchorMin.x, rt.anchorMin.y, rt.anchorMax.x, rt.anchorMax.y);
			Vector4 goal = new Vector4(old.x + to.x, old.y + to.y, old.z + to.x, old.w + to.y);
			Vector4 current = goal - old;
			rt.anchorMin += new Vector2(current.x, current.y);
			rt.anchorMax += new Vector2(current.z, current.w);
		}

		// relative
		public IEnumerator TranslatePX(Vector2 to, float duration, string easing){
			float time = 0f;
			var rt = anim.cachedRect;
			if(!rt) yield break;
			Vector4 old = new Vector4(rt.offsetMin.x, rt.offsetMin.y, rt.offsetMax.x, rt.offsetMax.y);
			Vector4 goal = new Vector4(old.x + to.x, old.y + to.y, old.z + to.x, old.w + to.y);
			Vector4 last = old;
			Vector4 current;
			while(time < duration){
				current = Vector4.LerpUnclamped(old, goal, Ease(easing, time / duration));
				Vector4 diff = current - last;
				rt.offsetMin += new Vector2(diff.x, diff.y);
				rt.offsetMax += new Vector2(diff.z, diff.w);

				last = current;
				time += Time.deltaTime;
				yield return null;
			}
			current = goal - last;
			rt.offsetMin += new Vector2(current.x, current.y);
			rt.offsetMax += new Vector2(current.z, current.w);
		}
		// Because TranslatePX with duration = 0 wouldnt work unless it was returned to the coroutine
		public void TranslatePXInstantly(Vector2 to){
			float time = 0f;
			var rt = anim.cachedRect;
			if(!rt) return;
			Vector4 old = new Vector4(rt.offsetMin.x, rt.offsetMin.y, rt.offsetMax.x, rt.offsetMax.y);
			Vector4 goal = new Vector4(old.x + to.x, old.y + to.y, old.z + to.x, old.w + to.y);
			Vector4 current = goal - old;
			rt.offsetMin += new Vector2(current.x, current.y);
			rt.offsetMax += new Vector2(current.z, current.w);
		}

		// relative
		public IEnumerator MoveTo(Vector4 to, float duration, string easing){
			float time = 0f;
			var rt = anim.cachedRect;
			if(!rt) yield break;
			Vector4 old = new Vector4(rt.anchorMin.x, rt.anchorMin.y, rt.anchorMax.x, rt.anchorMax.y);
			Vector4 goal = to;
			Vector4 last = old;
			Vector4 current;
			while(time < duration){
				current = Vector4.LerpUnclamped(old, goal, Ease(easing, time / duration));
				Vector4 diff = current - last;
				rt.anchorMin += new Vector2(diff.x, diff.y);
				rt.anchorMax += new Vector2(diff.z, diff.w);

				last = current;
				time += Time.deltaTime;
				yield return null;
			}
			current = goal - last;
			rt.anchorMin += new Vector2(current.x, current.y);
			rt.anchorMax += new Vector2(current.z, current.w);
		}
		// Because MoveTo with duration = 0 wouldnt work unless it was returned to the coroutine
		public void MoveToInstantly(Vector4 to){
			float time = 0f;
			var rt = anim.cachedRect;
			if(!rt) return;
			Vector4 old = new Vector4(rt.anchorMin.x, rt.anchorMin.y, rt.anchorMax.x, rt.anchorMax.y);
			Vector4 current = to - old;
			rt.anchorMin += new Vector2(current.x, current.y);
			rt.anchorMax += new Vector2(current.z, current.w);
		}

		// relative
		public IEnumerator MoveToPX(Vector4 to, float duration, string easing){
			float time = 0f;
			var rt = anim.cachedRect;
			if(!rt) yield break;
			Vector4 old = new Vector4(rt.offsetMin.x, rt.offsetMin.y, rt.offsetMax.x, rt.offsetMax.y);
			Vector4 goal = to;
			Vector4 last = old;
			Vector4 current;
			while(time < duration){
				current = Vector4.LerpUnclamped(old, goal, Ease(easing, time / duration));
				Vector4 diff = current - last;
				rt.offsetMin += new Vector2(diff.x, diff.y);
				rt.offsetMax += new Vector2(diff.z, diff.w);

				last = current;
				time += Time.deltaTime;
				yield return null;
			}
			current = goal - last;
			rt.offsetMin += new Vector2(current.x, current.y);
			rt.offsetMax += new Vector2(current.z, current.w);
		}
		// Because MoveToPX with duration = 0 wouldnt work unless it was returned to the coroutine
		public void MoveToPXInstantly(Vector4 to){
			float time = 0f;
			var rt = anim.cachedRect;
			if(!rt) return;
			Vector4 old = new Vector4(rt.offsetMin.x, rt.offsetMin.y, rt.offsetMax.x, rt.offsetMax.y);
			Vector4 current = to - old;
			rt.offsetMin += new Vector2(current.x, current.y);
			rt.offsetMax += new Vector2(current.z, current.w);
		}


		public float Ease(string type, float input){
			switch(type){
				case "Linear": return input;
				case "EaseIn": return input * input;
				case "EaseOut": return 1f - ((1f - input) * (1f - input));
				case "EaseInOut": return Mathf.Lerp(input * input, 1f - ((1f - input) * (1f - input)), input);
				default: // Custom Easing
					{
						var split = type.Split(' ');
						float X1, Y1, X2, Y2;
						if(split.Length < 4) return input;
						if(
							!float.TryParse(split[0], out X1) || !float.TryParse(split[1], out Y1) ||
							!float.TryParse(split[2], out X2) || !float.TryParse(split[3], out Y2)
						) return input;

						return BezierEasing.Ease(X1, Y1, X2, Y2, input);
					}
			}
		}
	}

	[RPC_Client]
    public void AddAnimation( RPCMessage msg )
    {
        string str = msg.read.StringRaw();

        if (string.IsNullOrEmpty(str)) return;

        var json = JSON.Array.Parse( str );
        if (json == null) return;

        foreach (var value in json){
            var obj = value.Obj;
            var panel = obj.GetString("name", "Overlay");

            GameObject go;
            if (string.IsNullOrEmpty(panel) || !UiDict.TryGetValue(panel, out go))
                return;

			Animation anim = go.GetComponent<Animation>();

			string mouseTarget = "";
			if(obj.ContainsKey("mouseTarget") && anim){
				mouseTarget = obj.GetString("mouseTarget", "");

				// Remove the existing animation's Enter/Exit Actions from the mouseListener, assuming its different from the current target
				if(anim && !string.IsNullOrEmpty(anim.mouseTarget) && !string.IsNullOrEmpty(mouseTarget) && anim.mouseTarget != mouseTarget)
					RemoveMouseListener(anim.mouseTarget, anim);
			}
			if(!anim){
				anim = go.AddComponent<Animation>();
				anim.CacheGraphicComponent();
			}

			// Apply new mouse target if its valid & different than the existing target
			if(!string.IsNullOrEmpty(mouseTarget) && anim.mouseTarget != mouseTarget)
				ScheduleMouseListener(mouseTarget, anim);

			foreach(var prop in obj.GetArray("properties"))
			{
				var propobj = prop.Obj;
				var condition = propobj.GetString("condition", "Generic");

				if(!anim.ValidCondition(condition)) condition = "Generic";
				var animprop = new AnimationProperty{
					duration = propobj.GetFloat("duration", 0f),
					delay = propobj.GetFloat("delay", 0f),
					repeat = propobj.GetInt("repeat", 0),
					repeatDelay = propobj.GetFloat("repeatDelay", 0f),
					easing = propobj.GetString("easing", "Linear"),
					type = propobj.GetString("type", null),
					from = propobj.GetString("from", null),
					to = propobj.GetString("to", null)
				};
				anim.properties[condition].Add(animprop);

				if(condition == "Generic") anim.StartProperty(animprop);
			}
			break;
        }
		ApplyMouseListeners();
    }
}

#endif
