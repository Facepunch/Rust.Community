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
    public class Animation : FacepunchBehaviour, IMouseReceiver
    {
        // properties by trigger
        public Dictionary<string, List<AnimationProperty>> properties = new Dictionary<string, List<AnimationProperty>>(){
            ["OnCreate"] = new List<AnimationProperty>(),
            ["OnDestroy"] = new List<AnimationProperty>(),
            ["OnHoverEnter"] = new List<AnimationProperty>(),
            ["OnHoverExit"] = new List<AnimationProperty>(),
            ["OnClick"] = new List<AnimationProperty>()
        };
        
        public string _mouseTarget = "";
        
        public string mouseTarget{
            get => _mouseTarget;
            set => _mouseTarget = value;
        }
        
        public UnityEngine.UI.Graphic cachedGraphic;
        public RectTransform cachedRect;
        public bool shouldRaycast = false;

        public bool isHidden = false;
        public bool isKilled = false;
        
        public bool ValidTrigger(string trigger) => properties.ContainsKey(trigger);
        
        public bool HasForTrigger(string trigger) => ValidTrigger(trigger) && properties[trigger].Count > 0;
        
        public void StartAnimation(){
            CacheGraphicComponent();
            StartByTrigger("OnCreate");
        }
        
        public void StartByTrigger(string trigger){
            if(!ValidTrigger(trigger)) return;
            for(int i = 0; i < properties[trigger].Count; i++){
                StartProperty(properties[trigger][i]);
            }
        }
        
        public void StartProperty(AnimationProperty prop){
            prop.anim = this;
            prop.routine = StartCoroutine(prop.Animate());
        }
        
        public void StopByTrigger(string trigger){
            if(!ValidTrigger(trigger)) return;
            for(int i = 0; i < properties[trigger].Count; i++){
                if(properties[trigger][i].routine != null) StopCoroutine(properties[trigger][i].routine);
            }
        }
        
        // this method handles 2 things:
        // stop any currently running animations and
        // if the object isnt allready destroyed, trigger the OnDestroy animation and kill the gameobject afterwards
        public void Kill(bool destroyed = false)
        {
            isKilled = true;
            // stop all triggers except onDestroy
            StopByTrigger("OnCreate");
            StopByTrigger("OnHoverEnter");
            StopByTrigger("OnHoverExit");
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
        
        // used with opacity & color animations, this hides the graphic and prevents it from blocking mouse interactions
        public void DisableGraphic(float delay = 0f){
            var a = new Action(() => {
                cachedGraphic.canvasRenderer.cullTransparentMesh = true;
                isHidden = true;
                cachedGraphic.raycastTarget = false;
            });
            if(delay <= 0f) a();
            else Invoke(a, delay);
        }
        
        // does the oposite
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
            StopByTrigger("OnHoverExit");
            StartByTrigger("OnHoverEnter");
        }
        
        public void OnHoverExit(){
            if(isKilled) return;
            StopByTrigger("OnHoverEnter");
            StartByTrigger("OnHoverExit");
        }
        
        public void OnClick(){
            if(isKilled) return;
            StopByTrigger("OnClick");
            StartByTrigger("OnClick");
        }
        
        public void RemoveProperty(AnimationProperty property){
            if(string.IsNullOrEmpty(property.trigger) || !ValidTrigger(property.trigger))
                return;
            
            properties[property.trigger].Remove(property);
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
		public AnimationProperty.AnimationValue animValue;
        public string trigger;
        
        public Animation anim;
        
        public Coroutine routine;
        
        public int completedRounds = 0;
        
        // Launches the animation, keeping track of loops if its set to repeat
        public IEnumerator Animate()
        {
            if(animValue == null || animValue.to == null){
                Debug.LogWarning($"Animation of type {type} for {anim.gameObject.name} failed to execute - no to values provided");
                anim.RemoveProperty(this);
                yield break;
			}
            
            // initial delay
            if(delay > 0f) yield return new WaitForSeconds(delay);
            
            do
            {
                yield return AnimateProperty();
                completedRounds++;
                if(repeatDelay > 0f) yield return new WaitForSeconds(repeatDelay);
                else yield return null;
            }
            while(repeat < 0 || (repeat > 0 && completedRounds <= repeat));
            
            // this animation wont get triggered again, so remove it
            if(trigger == "OnCreate")
                anim.RemoveProperty(this);
        }
        
        // Parses the from & to values and Launches the individual animation
        // Adding new animations can be achieved by Adding cases to the switch statement
        public IEnumerator AnimateProperty()
        {
            switch(type){
                case "Opacity":
                    {
                        // needs a reference to the graphic & atleast 1 value in the to value
                        if(!anim.cachedGraphic || animValue.to.Count < 1) break;
                        
                        // enables the graphic if:
                        //	 - the from value is higher than 0 or
                        //   - the graphic is currently hidden but will go above 0 opacity during the animation
                        if((animValue.from != null && animValue.from.TryGet(0) > 0f) || anim.isHidden && animValue.to.TryGet(0) > 0f)
                            anim.EnableGraphic(0f);
                        
                        animValue.initial = new DynamicVector(anim.cachedGraphic.canvasRenderer.GetAlpha());
                        animValue.apply = (DynamicVector value) => {
                            anim.cachedGraphic.canvasRenderer.SetAlpha(value.TryGet(0));
                        };
                        
                        // calling this wont actually start a tween, but it will stop any running color or alpha tweens, this is vital for stopping any potential fadeIns & Outs (otherwise the the tween and our own Animation will intersect)
                        anim.cachedGraphic.CrossFadeAlpha(animValue.initial.TryGet(0), 0f, true);
                        
                        if(animValue.to.TryGet(0) <= 0f) anim.DisableGraphic(duration);
                        return InterpolateValue(animValue, duration, easing);
                    }
                case "Color":
                    {
                        // needs a reference to the graphic & atleast 4 values in the to value
                        if(!anim.cachedGraphic || animValue.to.Count < 4) break;
                        
                        // enables the graphic if:
                        //	 - the from color's alpha is higher than 0 or
                        //   - the graphic is currently hidden but will go above 0 opacity during the animation
                        if((animValue.from != null && animValue.from.TryGet(3) > 0f) || anim.isHidden && animValue.to.TryGet(3) > 0f)
                            anim.EnableGraphic(0f);
                        
                        animValue.initial = new DynamicVector(anim.cachedGraphic.canvasRenderer.GetColor());
                        animValue.apply = (DynamicVector value) => {
                            anim.cachedGraphic.canvasRenderer.SetColor(value.ToColor());
                        };
                        
                        // Same as with alpha, stopping any tweens
                        anim.cachedGraphic.CrossFadeColor(animValue.initial.ToColor(), 0f, true, true);
                        
                        if(animValue.to.TryGet(3) <= 0f) anim.DisableGraphic(duration);
                        return InterpolateValue(animValue, duration, easing);
                    }
                case "Scale":
                    {
                        // needs a reference to the rectTransform & atleast 2 values in the to value
                        if(!anim.cachedRect || animValue.to.Count < 2) break;
                        
                        if(!anim.cachedRect) break;
                        
                        animValue.initial = new DynamicVector(anim.cachedRect.localScale);
                        animValue.apply = (DynamicVector value) => {
                            // we convert to a Vector3 even though the DynamicVector only holds 2 floats
                            // this is fine because the z value will be set to 0f, which isnt used for the scale of rectTransforms
                            anim.cachedRect.localScale = value.ToVector3();
                        };
                        return InterpolateValue(animValue, duration, easing);
                    }
                case "Translate":
                    {
                        // needs a reference to the rectTransform & atleast 2 values in the to value
                        if(!anim.cachedRect || animValue.to.Count < 2) break;
                        
                        animValue.initial = new DynamicVector();
                        animValue.last = new DynamicVector();
                        animValue.apply = (DynamicVector value) => {
                            DynamicVector diff = value - animValue.last;
                            anim.cachedRect.anchorMin += diff.ToVector2();
                            anim.cachedRect.anchorMax += diff.ToVector2();
                            animValue.last += diff;
                        };
                        return InterpolateValue(animValue, duration, easing, false);
                    }
                case "TranslatePX":
                    {
                        // needs a reference to the rectTransform & atleast 4 values in the to value
                        if(!anim.cachedRect || animValue.to.Count < 4) break;
                        
                        animValue.initial = new DynamicVector();
                        animValue.last = new DynamicVector();
                        animValue.apply = (DynamicVector value) => {
                            DynamicVector diff = value - animValue.last;
                            anim.cachedRect.offsetMin += diff.ToVector2(0);
                            anim.cachedRect.offsetMax += diff.ToVector2(0);
                            animValue.last += diff;
                        };
                        return InterpolateValue(animValue, duration, easing, false);
                    }
                case "Rotate":
                    {
                        // needs a reference to the rectTransform & atleast 3 values in the to value
                        if(!anim.cachedRect || animValue.to.Count < 3) break;
                        
                        animValue.initial = new DynamicVector(anim.cachedRect.rotation.eulerAngles);
                        animValue.apply = (DynamicVector value) => {
                            anim.cachedRect.rotation = Quaternion.Euler(value.ToVector3());
                        };
                        return InterpolateValue(animValue, duration, easing);
                    }
                case "MoveTo":
                    {
                        if(!anim.cachedRect) break;
                        
                        animValue.initial = new DynamicVector();
                        animValue.initial.Add(anim.cachedRect.anchorMin);
                        animValue.initial.Add(anim.cachedRect.anchorMax);
                        animValue.apply = (DynamicVector value) => {
                            anim.cachedRect.anchorMin = value.ToVector2(0);
                            anim.cachedRect.anchorMax = value.ToVector2(2); // skip the first 2 values
                        };
                        return InterpolateValue(animValue, duration, easing);
                    }
                case "MoveToPX":
                    {
                        if(!anim.cachedRect) break;
                        
                        animValue.initial = new DynamicVector();
                        animValue.initial.Add(anim.cachedRect.offsetMin);
                        animValue.initial.Add(anim.cachedRect.offsetMax);
                        animValue.apply = (DynamicVector value) => {
                            anim.cachedRect.offsetMin = value.ToVector2(0);
                            anim.cachedRect.offsetMax = value.ToVector2(2); // skip the first 2 values
                        };
                        return InterpolateValue(animValue, duration, easing);
                    }
            }
            
            // remove this animation property of there is no valid case or the selected case fails
            Debug.LogWarning($"Animation for {anim.gameObject.name} failed to execute - invalid to value [{animValue.to}] for animation of type {type}");
            anim.RemoveProperty(this);
            repeat = 0; // ensure the animation wont repeat
            // Return an empty enumerator so the coroutine finishes
            return new System.Object[0].GetEnumerator();
		}
        
        // Interpolats an AnimationValue over the duration with the easing specified
        // the absolute arguement specifies if the animation should be handled as a relative animation or an absolute animation
        // absolute = false: the objects initial value gets used as a 0 point, with the from and to values being relative to the initial value
        // absolute = true: the object's initial value does not get factored in and the from and to values are seen as absolute
        public IEnumerator InterpolateValue(AnimationValue value, float duration, string easing, bool absolute = true){
            float time = 0f;
            DynamicVector current;
            DynamicVector start = value.from == null ? value.initial : (absolute ? value.from : value.initial + value.from);
            
            // Immediately apply the start value if present
            if(value.from != null){
                value.apply(start);
                value.initial = start;
            }
            DynamicVector end = (absolute ? value.to : value.initial + value.to);
            
            while(time < duration){
                current = DynamicVector.LerpUnclamped(start, end, Ease(easing, time / duration));
                value.apply(current);
                time += Time.deltaTime;
                yield return null;
            }
            value.apply(end);
        }
        
        

        // manipulates a the input based on a preset easing function or a custom Bezier curve
        // accepts a predefined easing type, or a string of 4 floats to represent a bezier curve
        // NOTE: the return value is unclamped as this allowes bezier curves with under- and overshoot to work
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
        
        // Generalizes the values for an AnimationProperty
        public class AnimationValue {
            // gets set just before InterpolateValue is called
            public DynamicVector initial;
            // used for relative animations
            public DynamicVector last;
            // the from value can be optional
            public DynamicVector from;
            public DynamicVector to;
            // gets called during interpolation with the arguement being the current value.
            public Action<DynamicVector> apply;
            
            public AnimationValue(string sourceTo, string sourceFrom = null){
                this.from = ParseFromString(sourceFrom);
                this.to = ParseFromString(sourceTo);
            }
            
            public DynamicVector ParseFromString(string source){
                if(string.IsNullOrEmpty(source)) return null;
                var split = source.Split(' ');
                if(split.Length == 0) return null;
                var values = new DynamicVector();
                for(int i = 0; i < split.Length; i++){
                    float temp;
                    float.TryParse(split[i], out temp);
                    values.Add(temp);
                }
                return values;
            }
        }
        
        // Wraps a List of floats, Adding overloads for easily Adding and extracting Vector 2/3/4s & Colors
        public class DynamicVector : List<float> {
            // using a single static instance because lerp results should get immediately applied to the property when ToVector/Color is called
            private static DynamicVector lerpTemp = new DynamicVector();
            
            public DynamicVector(){}
            
            public DynamicVector(Vector4 vec){
                this.Add(vec);
            }
            public DynamicVector(Color col){
                this.Add(col);
            }
            public DynamicVector(Vector3 vec){
                this.Add(vec);
            }
            public DynamicVector(Vector2 vec){
                this.Add(vec);
            }
            public DynamicVector(float num){
                this.Add(num);
            }
            
            public void Add(Color col){
                base.Add(col.r);
                base.Add(col.g);
                base.Add(col.b);
                base.Add(col.a);
            }
            
            public void Add(Vector4 vec){
                base.Add(vec.x);
                base.Add(vec.y);
                base.Add(vec.z);
                base.Add(vec.w);
            }
            
            public void Add(Vector3 vec){
                base.Add(vec.x);
                base.Add(vec.y);
                base.Add(vec.z);
            }
            
            public void Add(Vector2 vec){
                base.Add(vec.x);
                base.Add(vec.y);
            }
            // the ToVectorX & ToColor Functions have an optional offset arguement that shifts the starting point of the list when turning it into the vector
            public Vector4 ToVector4(int offset = 0){
                return new Vector4(
                    TryGet(offset),
                    TryGet(offset + 1),
                    TryGet(offset + 2),
                    TryGet(offset + 3)
                );
            }
            public Color ToColor(int offset = 0){
                return new Color(
                    TryGet(offset),
                    TryGet(offset + 1),
                    TryGet(offset + 2),
                    TryGet(offset + 3)
                );
            }
            public Vector3 ToVector3(int offset = 0){
                return new Vector3(
                    TryGet(offset),
                    TryGet(offset + 1),
                    TryGet(offset + 2)
                );
            }
            public Vector2 ToVector2(int offset = 0){
                return new Vector2(
                    TryGet(offset),
                    TryGet(offset + 1)
                );
            }
            public float TryGet(int index, float defaultValue = 0f){
                if(index < 0 || index >= this.Count)
                    return defaultValue;
                return this[index];
            }
            
            public static DynamicVector Lerp(DynamicVector from, DynamicVector to, float t){
                t = Mathf.Clamp01(t);
                return LerpUnclamped(from, to, t);
            }
            
            public static DynamicVector LerpUnclamped(DynamicVector from, DynamicVector to, float t){
                lerpTemp.Clear();
                int HigherCount = (from.Count > to.Count ? from.Count : to.Count);
                for(int i = 0; i < HigherCount; i++){
                    lerpTemp.Add(from.TryGet(i) + (to.TryGet(i) - from.TryGet(i)) * t);
                }
                return lerpTemp;
            }
            
            public static DynamicVector operator +(DynamicVector lhs, DynamicVector rhs){
                DynamicVector result = new DynamicVector();
                int HigherCount = (lhs.Count > rhs.Count ? lhs.Count : rhs.Count);
                for(int i = 0; i < HigherCount; i++){
                    result.Add(lhs.TryGet(i) + rhs.TryGet(i));
                }
                return result;
            }
            
            public static DynamicVector operator -(DynamicVector lhs, DynamicVector rhs){
                DynamicVector result = new DynamicVector();
                int HigherCount = (lhs.Count > rhs.Count ? lhs.Count : rhs.Count);
                for(int i = 0; i < HigherCount; i++){
                    result.Add(lhs.TryGet(i) - rhs.TryGet(i));
                }
                return result;
            }
            
            public override string ToString(){
                var sb = new StringBuilder(32);
                for(int i = 0; i < this.Count; i++){
                    sb.Append(this.TryGet(i));
                    sb.Append(' ');
                }
                return sb.ToString();
            }
        }
    }
    
    public Animation ParseAnimation(JSON.Object obj, GameObject go = null){
        // if no gameobject is given attempt to find a name property and find it that way
        if(go == null){
            var panel = obj.GetString("name", null);
            if (string.IsNullOrEmpty(panel) || !UiDict.TryGetValue(panel, out go))
                return null;
        }
        
        Animation anim = null;
        Animation[] animations = go.GetComponents<Animation>();
        
        string mouseTarget = obj.GetString("mouseTarget", "");
        if(animations.Length == 0){
            // do nothing
        } else if(!string.IsNullOrEmpty(mouseTarget)){
            anim = animations.FirstOrDefault((animation) => animation.mouseTarget == mouseTarget);
        }else{
            anim = animations[0];
        }
        
        // create a new animation component as no matching component existed
        if(anim == null){
            anim = go.AddComponent<Animation>();
            if(!string.IsNullOrEmpty(mouseTarget)) ScheduleMouseListener(mouseTarget, anim);
        }
        
        foreach(var prop in obj.GetArray("properties")){
            ParseProperty(anim, prop.Obj);
        }
        return anim;
    }

	public AnimationProperty ParseProperty(Animation anim, JSON.Object propobj){
        var trigger = propobj.GetString("trigger", "OnCreate");
        
        if(!anim.ValidTrigger(trigger)) trigger = "OnCreate";
        string from = propobj.GetString("from", null);
        string to = propobj.GetString("to", null);
        var animprop = new AnimationProperty{
            duration = propobj.GetFloat("duration", 0f),
            delay = propobj.GetFloat("delay", 0f),
            repeat = propobj.GetInt("repeat", 0),
            repeatDelay = propobj.GetFloat("repeatDelay", 0f),
            easing = propobj.GetString("easing", "Linear"),
            type = propobj.GetString("type", null),
            animValue = new AnimationProperty.AnimationValue(to, from),
            trigger = trigger
        };
        anim.properties[trigger].Add(animprop);
        // if the animation has a cachedGraphic it means StartAnimation has allready been called on it
        // manually start the OnCreate Properties in this case
        if(anim.cachedGraphic != null && trigger == "OnCreate") anim.StartProperty(animprop);
        return animprop;
	}
    
    // RPC function to Add Animations to existing objects
    // accepts the same json object that the CreateComponents function does
    [RPC_Client]
    public void AddAnimation( RPCMessage msg )
    {
        string str = msg.read.StringRaw();
        if (string.IsNullOrEmpty(str)) return;
        
        var json = JSON.Array.Parse( str );
        if (json == null) return;
        
        foreach (var value in json){
            Animation anim = ParseAnimation(value.Obj);
            // if it returns a valid animation that hasnt allready been started, start it
            if(anim == null || anim.cachedGraphic != null)
                continue;
            anim.StartAnimation();
        }
        ApplyMouseListeners();
    }
}

#endif
