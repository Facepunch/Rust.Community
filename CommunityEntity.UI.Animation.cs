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
            foreach(var prop in properties[trigger]){
                StartProperty(prop);
            }
        }
        
        public void StartProperty(AnimationProperty prop){
            prop.anim = this;
            prop.routine = StartCoroutine(prop.Animate());
        }
        
        public void StopByTrigger(string trigger){
            if(!ValidTrigger(trigger)) return;
            foreach(var prop in properties[trigger]){
                if(prop.routine != null) StopCoroutine(prop.routine);
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
        public string from;
        public string to;
        public string trigger;
        
        public Animation anim;
        
        public Coroutine routine;
        
        public int completedRounds = 0;
        
        // Launches the animation, keeping track of loops if its set to repeat
        public IEnumerator Animate()
        {
            if(from == null) from = "";
            if(string.IsNullOrEmpty(to)){
                Debug.LogWarning($"Animation for {anim.gameObject.name} failed to execute - invalid \"to\" value");
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
        // adding new animations can be achieved by adding cases to the switch statement
        public IEnumerator AnimateProperty()
        {
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

            // remove this animation property of there is no valid case or the selected case fails
            Debug.LogWarning($"Animation for {anim.gameObject.name} failed to execute - invalid values for animation of type {type}");
            anim.RemoveProperty(this);
            repeat = 0; // ensure the animation wont repeat
            // Return an empty enumerator so the coroutine finishes
            return new System.Object[0].GetEnumerator();
        }

        // Changes the alpha of the current canvasRenderer
        // NOTE: this uses the canvasRenderer's SetAlpha Property which might cause some unexpected behaviour
        // this is because it acts as a multiplier multiplying the graphic's color with the canvasRenderer's Alpha
        // if we have an image with a color of 0.5 0.5 0.5 0.5, the opacity wont go above 0.5 even if we try to change it to 1 with an animation
        // when changing this to set the graphic's color directly it resulted in terrible performance
        // type: ABSOLUTE, meaning it cant be affected by other alpha or color changing animations and will get to its goal opacity
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

        // Changes the color of the current canvasRenderer, same Note Applies as with the ChangeAlpha Function, except each channel is multiplied seperately
        // type: ABSOLUTE, meaning it cant be affected by other alpha or color changing animations and will get to its goal opacity
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

        // type: ABSOLUTE, meaning it cant be affected by other Scale animations and will reach its target scale
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

        // type: ABSOLUTE, meaning it cant be affected by other Rotation animations and will reach its target scale
        // TIP: since we are using vector3s, rotating more than 180deg requires 2 (probably 3) seperate animations
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

        // type: RELATIVE, meaning other Translate and MoveTo Animations can affect the destination
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

        // type: RELATIVE, meaning other TranslatePX and MoveToPX Animations can affect the destination
        // NOTE: {Type} and {Type}PX Animations are independent of eachother
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

        // type: RELATIVE, meaning other Translate and MoveTo Animations may cause this to not arrive at its destination
        // NOTE: MoveTo may seem like it can achieve the same effect as Scale and Translate
        // but Translate is an easy way to do relative movement, while using a Scale animation has a different effect from changing anchor values
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

        // type: RELATIVE, meaning other TranslatePX and MoveToPX Animations may cause this to not arrive at its destination
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
            var obj = value.Obj;
            var panel = obj.GetString("name", "Overlay");

            GameObject go;
            if (string.IsNullOrEmpty(panel) || !UiDict.TryGetValue(panel, out go))
                return;

            Animation anim = null;
            Animation[] animations = go.GetComponents<Animation>();

            string mouseTarget = obj.GetString("mouseTarget", "");
            if(animations.Length == 0){
                // do nothing
            } else if(!string.IsNullOrEmpty(mouseTarget)){
                // find an existing animation component with the same mouse target, if not create one
                anim = animations.FirstOrDefault((animation) => animation.mouseTarget == mouseTarget);
            }else{
                anim = animations[0];
            }

            if(anim == null){
                anim = go.AddComponent<Animation>();
                anim.CacheGraphicComponent();
                if(!string.IsNullOrEmpty(mouseTarget)) ScheduleMouseListener(mouseTarget, anim);
            }


            foreach(var prop in obj.GetArray("properties"))
            {
                var propobj = prop.Obj;
                var trigger = propobj.GetString("trigger", "OnCreate");

                if(!anim.ValidTrigger(trigger)) trigger = "OnCreate";
                var animprop = new AnimationProperty{
                    duration = propobj.GetFloat("duration", 0f),
                    delay = propobj.GetFloat("delay", 0f),
                    repeat = propobj.GetInt("repeat", 0),
                    repeatDelay = propobj.GetFloat("repeatDelay", 0f),
                    easing = propobj.GetString("easing", "Linear"),
                    type = propobj.GetString("type", null),
                    from = propobj.GetString("from", null),
                    to = propobj.GetString("to", null),
                    trigger = trigger
                };
                anim.properties[trigger].Add(animprop);

                if(trigger == "OnCreate") anim.StartProperty(animprop);
            }
            break;
        }
        ApplyMouseListeners();
    }
}

#endif
