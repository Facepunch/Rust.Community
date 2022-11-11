
using Object = UnityEngine.Object;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


#if CLIENT


public partial class CommunityEntity
{

    public class SoundTrigger : MonoBehaviour, IMouseReceiver {

        public List<SoundEvent> OnHoverEvents = new List<SoundEvent>();
        public List<SoundEvent> OnClickEvents = new List<SoundEvent>();

        public List<SoundEvent> ActiveSounds = new List<SoundEvent>();

		private string _mouseTarget = "";

		public string mouseTarget{
			get => _mouseTarget;
			set => _mouseTarget = value;
		}

        public bool isHovered = false;
        private float lastUnhovered = 0f;

		public void OnHoverEnter(){
            if(isHovered || lastUnhovered + 0.1f > Time.time) return;
            isHovered = true;
            foreach(var ev in OnHoverEvents){
                PlayEvent(ev);
            }
		}
		public void OnClick(){
            foreach(var ev in OnClickEvents){
                PlayEvent(ev);
            }
		}
		public void OnHoverExit(){
            foreach(var ev in ActiveSounds){
                if(ev == null || ev.ActiveSound == null || !ev.killOnExit) continue;
                if(ev.fadeOut > 0f){
                    ev.ActiveSound.FadeOutAndRecycle(ev.fadeOut);
                }else{
                    SoundManager.RecycleSound(ev.ActiveSound);
                }
            }
            ActiveSounds.Clear();
            isHovered = false;
            lastUnhovered = Time.time;
		}

        public void PlayEvent(SoundEvent ev){
            if(ev == null || ev.definitionToPlay == null) return;
            if(ev.killOnExit){
                ev.ActiveSound = PlayActive(ev);
                ActiveSounds.Add(ev);
            }else{
                PlayOneshot(ev);
            }
        }

        public void PlayOneshot(SoundEvent ev){
            SoundDefinition def = NuCommunityEntity.ClientInstance.FindDefinition(ev.definitionToPlay);
            if(def == null) return;
            SoundManager.PlayOneshot(def, null, true, Vector3.zero);
        }
        public Sound PlayActive(SoundEvent ev){
            SoundDefinition def = NuCommunityEntity.ClientInstance.FindDefinition(ev.definitionToPlay);
            if(def == null) return null;

            Sound sound = SoundManager.RequestSoundInstance(def, null, Vector3.zero, true);
            if(!sound) return null;

            if(ev.fadeIn > 0f){
                sound.FadeInAndPlay(ev.fadeIn);
            }else{
                sound.Play();
            }
            if(!def.loop) sound.RecycleAfterPlaying();
            return sound;
        }

        public class SoundEvent {
            public string definitionToPlay = "";
            public float fadeIn = 0f;
            public bool killOnExit = true;
            public float fadeOut = 0f;

            public Sound ActiveSound;
        }
    }

}

#endif
