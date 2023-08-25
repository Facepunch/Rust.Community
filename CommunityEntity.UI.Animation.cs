using Object = UnityEngine.Object;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Facepunch.Extend;
using System.IO;
using System.Globalization;

#if CLIENT

public partial class CommunityEntity
{
    public class Animation : FacepunchBehaviour
    {

        #region Fields

        public static List<Animation> reusableList = new List<Animation>();

        public static IEnumerator emptyEnumerator = new System.Object[0].GetEnumerator();
        
        public static List<AnimationProperty> reusablePropertyList = new List<AnimationProperty>();

        // properties by trigger
        public Dictionary<string, List<AnimationProperty>> properties = new Dictionary<string, List<AnimationProperty>>(){
            ["OnCreate"] = new List<AnimationProperty>(),
            ["OnDestroy"] = new List<AnimationProperty>(),
            ["OnHoverEnter"] = new List<AnimationProperty>(),
            ["OnHoverExit"] = new List<AnimationProperty>(),
            ["OnDrag"] = new List<AnimationProperty>(),
            ["OnDrop"] = new List<AnimationProperty>(),
            ["OnClick"] = new List<AnimationProperty>(),
        };

        // a record of all Triggers handled
        public List<string> targets = new List<string>();

        // cached relevant components
        public UnityEngine.UI.Graphic graphic;
        public RectTransform rt;
        public CanvasGroup group;

        // flags
        public bool isHidden = false;
        public bool isKilled = false;
        public bool initialized = false;

        #endregion

        #region Core

        // sets up the Animation component & start Animations
        public void Init(){
            if (initialized)
                return;
            
            CacheComponents();
            AttachTriggers(properties["OnHoverEnter"]);
            AttachTriggers(properties["OnHoverExit"]);
            AttachTriggers(properties["OnClick"]);
            AttachTriggers(properties["OnDrag"]);
            AttachTriggers(properties["OnDrop"]);

            StartByTrigger("OnCreate");
            initialized = true;
        }

        public void StartProperty(AnimationProperty prop){
            prop.anim = this;
            if(prop.routine == null)
                prop.routine = StartCoroutine(prop.Animate());
        }

        public void RemoveProperty(AnimationProperty property){
            if(string.IsNullOrEmpty(property.trigger) || !ValidTrigger(property.trigger))
                return;

            properties[property.trigger].Remove(property);
        }

        public void Reset()
        {
            foreach(var proplists in properties)
            {
                StopByTrigger(proplists.Key);
                proplists.Value.Clear();
            }
            initialized = false;
        }

        private void OnDestroy(){
            if(!isKilled)
                Kill(true);
        }

        public void Kill(bool destroyed = false)
        {
            // mark as killed & clean up
            isKilled = true;
            StopByTrigger("OnCreate");
            StopByTrigger("OnHoverEnter");
            StopByTrigger("OnHoverExit");
            StopByTrigger("OnDrag");
            StopByTrigger("OnDrop");

            if(destroyed)
                return;

            // determine duration of all OnDestroy durations to delay the actual destroying
            float killDelay = 0f;
            foreach(var prop in properties["OnDestroy"])
            {
                float totalDelay = prop.duration + prop.delay;
                if(killDelay < totalDelay) killDelay = totalDelay;
                StartProperty(prop);
            }
            Invoke(new Action(() => Object.Destroy(gameObject)), killDelay + 0.05f);
        }

        #endregion

        #region Trigger

        public bool ValidTrigger(string trigger) => properties.ContainsKey(trigger);

        public bool HasForTrigger(string trigger) => ValidTrigger(trigger) && properties[trigger].Count > 0;

        public void StartByTrigger(string trigger, string panel = null){
            if(!ValidTrigger(trigger))
                return;
            for(int i = 0; i < properties[trigger].Count; i++){
                if(panel == null || properties[trigger][i].target == panel)
                    StartProperty(properties[trigger][i]);
            }
        }

        public void StopByTrigger(string trigger, string panel = null){
            if(!ValidTrigger(trigger))
                return;

            for(int i = 0; i < properties[trigger].Count; i++){
                if(panel != null && properties[trigger][i].target != panel)
                    continue;

                if(properties[trigger][i].routine == null)
                    continue;

                StopCoroutine(properties[trigger][i].routine);
                properties[trigger][i].routine = null;
            }
        }

        // returns true if the trigger relies on a MouseListener component
        public bool TriggerNeedsListener(string trigger){
            return trigger switch{
                "OnHoverEnter" => true,
                "OnHoverExit" => true,
                "OnClick" => true,
                _ => false
            };
        }

        // Attaches trigger events to any panels needed by the properties in the list
        public void AttachTriggers(List<AnimationProperty> properties){
            if(properties.Count == 0)
                return;

            GameObject go;
            for(int i = 0; i < properties.Count; i++){
                string target = properties[i].target;
                if(target == null || targets.Contains(target))
                    continue;

                if(target == this.gameObject.name)
                    go = this.gameObject;
                else
                    go = CommunityEntity.ClientInstance.FindPanel(target);

                if(go == null)
                    continue;

                AttachTo(go, TriggerNeedsListener(properties[i].trigger));
                targets.Add(go.name);
            }
        }

        // attaches events to a Draggable & MouseListener if needed
        private void AttachTo(GameObject go, bool addListener = false){
            if(go == null)
                return;

            var listener = go.GetComponent<MouseListener>();
            if(listener == null && addListener)
                listener = go.AddComponent<MouseListener>();

            if(listener != null){
                listener.onEnter += this.OnHoverEnter;
                listener.onExit += this.OnHoverExit;
                listener.onClick += this.OnClick;
            }

            var drag = go.GetComponent<Draggable>();
            if(drag == null)
                return;

            drag.onDragCallback += this.OnDrag;
            drag.onDropCallback += this.OnDrop;
        }

        // Events
        public void OnDrag(string panel){
            if(isKilled)
                return;
            StopByTrigger("OnDrop", panel);
            StartByTrigger("OnDrag");
        }

        public void OnDrop(string panel){
            if(isKilled)
                return;

            StopByTrigger("OnDrag", panel);
            StartByTrigger("OnDrop", panel);
        }

        public void OnHoverEnter(string panel){
            if(isKilled)
                return;
            StopByTrigger("OnHoverExit", panel);
            StartByTrigger("OnHoverEnter", panel);
        }

        public void OnHoverExit(string panel){
            if(isKilled)
                return;

            StopByTrigger("OnHoverEnter", panel);
            StartByTrigger("OnHoverExit", panel);
        }

        public void OnClick(string panel){
            if(isKilled)
                return;

            StopByTrigger("OnClick", panel);
            StartByTrigger("OnClick", panel);
        }

        #endregion

        #region Helpers

        public void CacheComponents(){
            graphic = gameObject.GetComponent<UnityEngine.UI.Graphic>();
            rt = gameObject.GetComponent<RectTransform>();
            if(!group)
                group = gameObject.GetComponent<CanvasGroup>();
        }

        public void TryToggleGraphic(float delay = 0f){
            if(graphic == null) return;

            var a = new Action(() => {
                bool visible = GetAlpha() > 0f;
                if(group == null)
                    graphic.canvasRenderer.cullTransparentMesh = visible;
                isHidden = !visible;
                SetRaycasting(visible);
            });
            if(delay <= 0f) a();
            else Invoke(a, delay);
        }

        public float GetAlpha(){
            if(group != null)
                return group.alpha;

            return graphic.canvasRenderer.GetAlpha();
        }
        // uses the canvasGroup if found, otherwise the graphic
        public void SetAlpha(float alpha){
            if(group != null)
                group.alpha = alpha;
            else
                graphic.canvasRenderer.SetAlpha(alpha);
        }
        // uses the canvasGroup if found, otherwise the graphic
        public void SetRaycasting(bool wants){
            if(group != null)
                group.blocksRaycasts = wants;
            else
                graphic.raycastTarget = wants;
        }

        // Compatability for old FadeIn method
        public static void AddFadeIn(GameObject go, float duration, bool addCanvasGroup){
            if(duration == 0f)
                return;

            var anim = go.GetComponent<Animation>();
            if(anim == null)
                anim = go.AddComponent<Animation>();

            var prop = new AnimationProperty(){
                animValue = new AnimationProperty.AnimationValue(){
                    from = new AnimationProperty.DynamicVector(0f),
                    to = new AnimationProperty.DynamicVector(1f)
                },
                type = "Opacity",
                duration = duration
            };

            anim.properties["OnCreate"].Add(prop);
            
            if(addCanvasGroup && anim.group == null) {
                anim.group = go.GetComponent<CanvasGroup>();
                if(anim.group == null)
                    anim.group = go.AddComponent<CanvasGroup>();
            }
        }

        // Compatability for old FadeOut method
        public static void AddFadeOut(GameObject go, float duration, bool addCanvasGroup){
            if(duration == 0f)
                return;

            var anim = go.GetComponent<Animation>();
            if(anim == null)
                anim = go.AddComponent<Animation>();

            var prop = new AnimationProperty(){
                animValue = new AnimationProperty.AnimationValue(){
                    to = new AnimationProperty.DynamicVector(0f)
                },
                type = "Opacity",
                duration = duration
            };

            anim.properties["OnDestroy"].Add(prop);
            
            if(addCanvasGroup && anim.group == null) {
                anim.group = go.GetComponent<CanvasGroup>();
                if(anim.group == null)
                    anim.group = go.AddComponent<CanvasGroup>();
            }
        }

        public static void AddPendingAnim(Animation anim){
            reusableList.Add(anim);
        }

        public static void InitPendingAnims(){
            for(int i = 0; i < reusableList.Count; i++){
                reusableList[i]?.Init();
            }
            reusableList.Clear();
        }

    public static Animation ParseAnimation(JSON.Object obj, GameObject go = null, bool allowUpdate = false){

        Animation anim = go.GetComponent<Animation>();
        // create a new animation component if no Animation existed
        if(anim == null)
            anim = go.AddComponent<Animation>();

        if (allowUpdate && obj.ContainsKey("Reset"))
            anim.Reset();

        var arr = obj.GetArray("properties");
        for (int i = 0; i < arr.Length; i++)
        {
            var prop = AnimationProperty.ParseProperty(anim, arr[i].Obj);
            Animation.reusablePropertyList.Add(prop);
        }
        if (anim.initialized)
            anim.AttachTriggers(Animation.reusablePropertyList);
        Animation.reusablePropertyList.Clear();

        // ensures a canvasGroup is added if needed, regardless of if its a new animation or an existing one
        if(obj.GetBoolean("addCanvasGroup", false) && anim.group == null){
            anim.group = go.GetComponent<CanvasGroup>();
            if(anim.group == null)
                anim.group = go.AddComponent<CanvasGroup>();
        }

        return anim;
    }

        #endregion
    }

    // this could be a class if the allocation is insignificant
    public class AnimationProperty
    {

        #region Fields

        public float duration;
        public float delay;
        public int repeat;
        public float repeatDelay;
        public BezierEasing.BezierPoints easing;
        public string type;
        public AnimationProperty.AnimationValue animValue;
        public string target;
        public string trigger;

        public Animation anim;

        public Coroutine routine;

        public int completedRounds;

        #endregion

        #region Core

        // Launches the animation, keeping track of loops if its set to repeat
        public IEnumerator Animate()
        {
            completedRounds = 0; // reset completedRounds on restart
            if(animValue == null || animValue.to.Count == 0){
                Debug.LogWarning($"Animation of type {type} for {anim.gameObject.name} failed to execute - no from/to values provided");
                anim.RemoveProperty(this);
                yield break;
            }

            // initial delay
            if(delay > 0f) yield return new WaitForSeconds(delay);

            do
            {
                yield return AnimateProperty();
                completedRounds++;
                if(repeatDelay > 0f) yield return CoroutineEx.waitForSeconds(repeatDelay);
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
            // for use in lambdas, would otherwise trigger error CS1673
            var prop = this;
            switch(type){
                case "Opacity":
                    {
                        // needs a reference to the graphic & atleast 1 value in the to value
                        if((!anim.graphic && !anim.group) || animValue.to.Count < 1) break;

                        // try to enable the graphic after 0.1 seconds if:
                        //     - the from value is higher than 0 or
                        //   - the graphic is currently hidden but will go above 0 opacity during the animation
                        if((animValue.from.Count != 0 && animValue.from.TryGet(0) > 0f) || anim.isHidden && animValue.to.TryGet(0) > 0f)
                            anim.TryToggleGraphic(0.1f);

                        animValue.initial = new DynamicVector(anim.graphic.canvasRenderer.GetAlpha());

                        // force applies the value, meaning Opacity & Color animations may clash when multiple are setting the opacity
                        animValue.apply = (DynamicVector value) => {
                            prop.anim.SetAlpha(value.TryGet(0));
                        };

                        // disables the graphic at the end of the animation if the end opacity is 0
                        if(animValue.to.TryGet(0) <= 0f) anim.TryToggleGraphic(duration);

                        //Use Absolute mode for these Interpolations, as it wont need the initial color for any calculations
                        return InterpolateValue(animValue, duration, easing, true);
                    }
                case "Color":
                    {
                        // needs a reference to the graphic & atleast 4 values in the to value
                        if(!anim.graphic || animValue.to.Count < 4) break;

                        // enables the graphic if:
                        //     - the from color's alpha is higher than 0 or
                        //   - the graphic is currently hidden but will go above 0 opacity during the animation
                        if((animValue.from.Count != 0 && animValue.from.TryGet(3) > 0f) || anim.isHidden && animValue.to.TryGet(3) > 0f)
                            anim.TryToggleGraphic(0.1f);

                        animValue.initial = new DynamicVector(anim.graphic.canvasRenderer.GetColor());

                        // force applies the value, meaning Opacity & Color animations may clash when multiple are setting the opacity
                        animValue.apply = (DynamicVector value) => {
                            prop.anim.graphic.canvasRenderer.SetColor(value.ToColor());
                        };

                        if(animValue.to.TryGet(3) <= 0f) anim.TryToggleGraphic(duration);


                        //Use Absolute mode for these Interpolations, as it wont need the initial color for any calculations
                        return InterpolateValue(animValue, duration, easing);
                    }
                case "Scale":
                    {
                        // needs a reference to the rectTransform & atleast 2 values in the to value
                        if(!anim.rt || animValue.to.Count < 2) break;

                        if(!anim.rt) break;

                        animValue.initial = new DynamicVector(anim.rt.localScale);

                        // force applies the value, meaning multiple Scale animations running at the same time will clash
                        animValue.apply = (DynamicVector value) => {
                            // we convert to a Vector3 even though the DynamicVector only holds 2 floats
                            // this is fine because the z value will be set to 0f, which isnt used for the scale of rectTransforms
                            prop.anim.rt.localScale = value.ToVector3();
                        };

                        //Use Absolute mode for these Interpolations, as it wont need the initial scale for any calculations
                        return InterpolateValue(animValue, duration, easing);
                    }
                case "Translate":
                    {
                        // needs a reference to the rectTransform & atleast 2 values in the to value
                        if(!anim.rt || animValue.to.Count < 2) break;

                        animValue.initial = new DynamicVector();
                        animValue.last = new DynamicVector();
                        // incrementally applies the value, allowing multiple MoveTo/PX & Translate/PX values to affect the position
                        animValue.apply = (DynamicVector value) => {
                            DynamicVector diff = value - prop.animValue.last;
                            prop.anim.rt.anchorMin += diff.ToVector2();
                            prop.anim.rt.anchorMax += diff.ToVector2();
                            prop.animValue.last += diff;
                        };
                        // Use Relative mode for these Interpolations, as it will take the initial position into account for the translation
                        return InterpolateValue(animValue, duration, easing, false);
                    }
                case "TranslatePX":
                    {
                        // needs a reference to the rectTransform & atleast 4 values in the to value
                        if(!anim.rt || animValue.to.Count < 4) break;

                        animValue.initial = new DynamicVector();
                        animValue.last = new DynamicVector();
                        // incrementally applies the value, allowing multiple MoveTo/PX & Translate/PX values to affect the position
                        animValue.apply = (DynamicVector value) => {
                            DynamicVector diff = value - prop.animValue.last;
                            prop.anim.rt.offsetMin += diff.ToVector2(0);
                            prop.anim.rt.offsetMax += diff.ToVector2(0);
                            prop.animValue.last += diff;
                        };
                        // Use Relative mode for these Interpolations, as it will take the initial position into account for the translation
                        return InterpolateValue(animValue, duration, easing, false);
                    }
                case "Rotate":
                    {
                        // needs a reference to the rectTransform & atleast 3 values in the to value
                        if(!anim.rt || animValue.to.Count < 3) break;

                        animValue.initial = new DynamicVector(anim.rt.rotation.eulerAngles);
                        // force applies the value, meaning multiple Rotate animations running at the same time will clash
                        animValue.apply = (DynamicVector value) => {
                            prop.anim.rt.rotation = Quaternion.Euler(value.ToVector3());
                        };

                        //Use Absolute mode for these Interpolations, as it wont need the initial rotation for any calculations
                        return InterpolateValue(animValue, duration, easing, true);
                    }
                case "MoveTo":
                    {
                        if(!anim.rt) break;

                        animValue.initial = new DynamicVector(anim.rt.anchorMin);
                        animValue.initial.Add(anim.rt.anchorMax);
                        animValue.last = animValue.initial;
                        // incrementally applies the value, allowing multiple MoveTo/PX & Translate/PX values to affect the position
                        animValue.apply = (DynamicVector value) => {
                            DynamicVector diff = value - prop.animValue.last;
                            prop.anim.rt.anchorMin += diff.ToVector2(0);
                            prop.anim.rt.anchorMax += diff.ToVector2(2); // skip the first 2 values
                            prop.animValue.last += diff;
                        };
                        //Use Absolute mode for these Interpolations, as the from & to values supplied are absolute values
                        return InterpolateValue(animValue, duration, easing, true);
                    }
                case "MoveToPX":
                    {
                        if(!anim.rt) break;

                        animValue.initial = new DynamicVector(anim.rt.offsetMin);
                        animValue.initial.Add(anim.rt.offsetMax);
                        animValue.last = animValue.initial;
                        // incrementally applies the value, allowing multiple MoveTo/PX & Translate/PX values to affect the position
                        animValue.apply = (DynamicVector value) => {
                            DynamicVector diff = value - prop.animValue.last;
                            prop.anim.rt.offsetMin += diff.ToVector2(0);
                            prop.anim.rt.offsetMax += diff.ToVector2(2); // skip the first 2 values
                            prop.animValue.last += diff;
                        };
                        //Use Absolute mode for these Interpolations, as the from & to values supplied are absolute values
                        return InterpolateValue(animValue, duration, easing, true);
                    }
            }

            // remove this animation property of there is no valid case or the selected case fails
            Debug.LogWarning($"Animation has invalid values\ngameObject: {anim.gameObject.name}\nparent: {anim.transform.parent.gameObject.name}\ntype: {type}\nfrom value: \"{animValue.from}\"\nto value: \"{animValue.to}\"\ngraphic: {anim.graphic?.ToString() ?? "null"}\ncanvasGroup: {anim.group?.ToString() ?? "null"}");
            anim.RemoveProperty(this);
            repeat = 0; // ensure the animation wont repeat
            // Return an empty enumerator so the coroutine finishes
            return Animation.emptyEnumerator;
        }

        #endregion

        #region Helpers

        // manipulates the input based on a preset easing function or a custom Bezier curve
        // accepts a predefined easing type, or a string of 4 floats to represent a bezier curve
        // NOTE: the return value is unclamped as this allowes bezier curves with under- and overshoot to work
        public static float Ease(BezierEasing.BezierPoints easing, float input)
        {
            return easing switch
            {
                _ when easing == BezierEasing.BezierPoints.LINEAR => input,
                _ when easing == BezierEasing.BezierPoints.EASE_IN => input * input,
                _ when easing == BezierEasing.BezierPoints.EASE_OUT => 1f - ((1f - input) * (1f - input)),
                _ when easing == BezierEasing.BezierPoints.EASE_IN_OUT => Mathf.Lerp(input * input, 1f - ((1f - input) * (1f - input)), input),
                _ => BezierEasing.Ease(easing, input)
            };
        }

        // Interpolats an AnimationValue over the duration with the easing specified
        // the absolute arguement specifies if the animation should be handled as a relative animation or an absolute animation
        // absolute = false: the objects initial value gets used as a 0 point, with the from and to values being relative to the initial value
        // absolute = true: the object's initial value does not get factored in and the from and to values are seen as absolute
        public static IEnumerator InterpolateValue(AnimationValue value, float duration, BezierEasing.BezierPoints easing, bool absolute = true){
            float time = 0f;
            DynamicVector current;
            DynamicVector start = value.from.Count == 0 ? value.initial : (absolute ? value.from : value.initial + value.from);

            // Immediately apply the start value if present
            if(value.from.Count != 0){
                value.apply(start);
                value.initial = start;
            }
            DynamicVector end = (absolute ? value.to : value.initial + value.to);
            
            if(start == end){
                yield return CoroutineEx.waitForSeconds(duration);
                yield break;
            }

            while(time < duration){
                current = DynamicVector.LerpUnclamped(start, end, Ease(easing, time / duration));
                value.apply(current);
                time += Time.deltaTime;
                yield return null;
            }
            value.apply(end);
        }

        public static AnimationProperty ParseProperty(Animation anim, JSON.Object obj){
            var trigger = obj.GetString("trigger", "OnCreate");
    
            BezierEasing.BezierPoints easing = BezierEasing.BezierPoints.LINEAR;
            var easingString = obj.GetString("easing", "Linear");
            switch (easingString)
            {
                case "Linear": break;
                case "EaseIn": easing = BezierEasing.BezierPoints.EASE_IN; break;
                case "EaseOut": easing = BezierEasing.BezierPoints.EASE_OUT; break;
                case "EaseInOut": easing = BezierEasing.BezierPoints.EASE_IN_OUT; break;
                default:
                    {
                        var parsed = AnimationProperty.DynamicVector.FromString(easingString);
                        if (parsed.Count != 4)
                            break;
                        easing = new BezierEasing.BezierPoints(parsed.TryGet(0), parsed.TryGet(1), parsed.TryGet(2), parsed.TryGet(3));
                        break;
                    }
            }
    
            if (!anim.ValidTrigger(trigger))
                trigger = "OnCreate";
    
            string from = obj.GetString("from", null);
            string to = obj.GetString("to", null);
            var animprop = new AnimationProperty
            {
                duration = obj.GetFloat("duration", 0f),
                delay = obj.GetFloat("delay", 0f),
                repeat = obj.GetInt("repeat", 0),
                repeatDelay = obj.GetFloat("repeatDelay", 0f),
                easing = easing,
                target = obj.GetString("target", anim.gameObject.name),
                type = obj.GetString("type", null),
                animValue = new AnimationProperty.AnimationValue(to, from),
                trigger = trigger
            };
            anim.properties[trigger].Add(animprop);
    
            // if the animation has a graphic it means Start has allready been called on it
            // manually start the OnCreate Properties in this case
            if (anim.initialized && trigger == "OnCreate")
                anim.StartProperty(animprop);
    
            return animprop;
        }

        #endregion

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

            public AnimationValue(){

            }

            public AnimationValue(string sourceTo, string sourceFrom = null){
                this.from = DynamicVector.FromString(sourceFrom);
                this.to = DynamicVector.FromString(sourceTo);
            }
        }

        // a struct that mimics Vector2/3/4/n, previously used a list to hold values, but lists dont work as structs
        // turning this into a struct makes alot of sense, thanks for the insights @WhiteThunder
        public struct DynamicVector : IEquatable<DynamicVector> {

            #region Fields

            // need it to hold more than 4? add a _valueN and adjust the Capacity, indexer & Clear method
            private float _value0;
            private float _value1;
            private float _value2;
            private float _value3;

            public int Count;

            public int Capacity => 4;

            public float this[int i]{
                get {
                    switch(i){
                        case 0: return _value0; break;
                        case 1: return _value1; break;
                        case 2: return _value2; break;
                        case 3: return _value3; break;
                        default: throw new IndexOutOfRangeException();
                    }
                }
                set {
                    switch(i){
                        case 0: _value0 = value; break;
                        case 1: _value1 = value; break;
                        case 2: _value2 = value; break;
                        case 3: _value3 = value; break;
                        default: throw new IndexOutOfRangeException();
                    }
                }
            }

            #endregion

            #region Adding & Constructing

            public DynamicVector(Vector4 vec) : this() => Add(vec);
            public DynamicVector(Color col) : this() => Add(col);
            public DynamicVector(Vector3 vec) : this() => Add(vec);
            public DynamicVector(Vector2 vec) : this() => Add(vec);
            public DynamicVector(Vector2 vec1, Vector2 vec2) : this()
            {
                Add(vec1);
                Add(vec2);
            }
            public DynamicVector(float num) : this() => Add(num);

            public void Add(float num) => this[Count++] = num;

            public void Add(Color col){
                Add(col.r);
                Add(col.g);
                Add(col.b);
                Add(col.a);
            }

            public void Add(Vector4 vec){
                Add(vec.x);
                Add(vec.y);
                Add(vec.z);
                Add(vec.w);
            }

            public void Add(Vector3 vec){
                Add(vec.x);
                Add(vec.y);
                Add(vec.z);
            }

            public void Add(Vector2 vec){
                Add(vec.x);
                Add(vec.y);
            }

            #endregion

            #region Casting

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

            #endregion

            #region Helpers

            public static DynamicVector FromString(string source)
            {
                var values = new DynamicVector();
                if (string.IsNullOrEmpty(source)) return values;
                var split = source.Split(' ');
                if (split.Length == 0) return values;
                for (int i = 0; i < split.Length; i++)
                {
                    float temp;
                    if (!float.TryParse(split[i], NumberStyles.Any, CultureInfo.InvariantCulture, out temp))
                        continue;
                    values.Add(temp);
                    if (values.Count == values.Capacity) break;
                }
                return values;
            }

            public float TryGet(int index, float defaultValue = 0f){
                if(index < 0 || index >= this.Count)
                    return defaultValue;
                return this[index];
            }

            public void Clear(){
                _value0 = 0f;
                _value1 = 0f;
                _value2 = 0f;
                _value3 = 0f;
                Count = 0;
            }

            #endregion

            #region Operations

            public static DynamicVector Lerp(DynamicVector from, DynamicVector to, float t){
                t = Mathf.Clamp01(t);
                return LerpUnclamped(from, to, t);
            }

            public static DynamicVector LerpUnclamped(DynamicVector from, DynamicVector to, float t){
                DynamicVector result = new DynamicVector();
                int HigherCount = (from.Count > to.Count ? from.Count : to.Count);
                for(int i = 0; i < HigherCount; i++){
                    result.Add(from.TryGet(i) + (to.TryGet(i) - from.TryGet(i)) * t);
                }
                return result;
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

            public bool Equals(DynamicVector other)
            {
                if (Count != other.Count)
                    return false;

                for (int i = 0; i < Count; i++)
                {
                    if (TryGet(i) != other.TryGet(i))
                        return false;
                }

                return true;
            }

            public static bool operator == (DynamicVector lhs, DynamicVector rhs){
                return lhs.Equals(rhs);
            }

            public static bool operator != (DynamicVector lhs, DynamicVector rhs)
            {
                return !lhs.Equals(rhs);
            }

            public override string ToString()
            {
                var sb = new StringBuilder(32);
                for (int i = 0; i < Count; i++)
                {
                    sb.Append(TryGet(i));
                    sb.Append(' ');
                }
                return sb.ToString();
            }

            #endregion
        }
    }
}

#endif
