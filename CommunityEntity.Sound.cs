using ConVar;
using JSON;
using Object = JSON.Object;
using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using System.IO;

#if CLIENT

public partial class CommunityEntity
{
    private static List<Sound> AllSounds = new List<Sound>();
    private static Dictionary<string, SoundDefinition> ServerDefinitions = new Dictionary<string, SoundDefinition>();


    private static Dictionary<string, Sound> SoundDict = new Dictionary<string, Sound>();
    private static Dictionary<string, AudioClip> WebSoundCache = new Dictionary<string, AudioClip>();

    public static void DestroyServerCreatedSounds()
    {
        foreach ( var sound in AllSounds )
        {
            SoundManager.RecycleSound(sound);
        }

        foreach(var def in ServerDefinitions){
            UnityEngine.Object.Destroy(def.Value);
        }

        AllSounds.Clear();
        ServerDefinitions.Clear();
    }

    private static void RegisterDefinition(string name, SoundDefinition def )
    {
        if(ServerDefinitions.ContainsKey(name)){
            // unsure if we should kill the old definition, since it may affect currently playing sounds that still use the old definition
            // when in doubt, Leave it to the Facepunch Devs :smug:
            ServerDefinitions[name] = def;
            return;
        }
        ServerDefinitions.Add(name, def);
    }

    private static void RegisterSound(string name, Sound sound )
    {
        AllSounds.Add( sound );
        if(SoundDict.ContainsKey(name)){
            // kill old sound since we overwrite the ability to control the old sound source
            KillSound( SoundDict[name], 0f );
            SoundDict[name] = sound;
            return;
        }
        SoundDict.Add(name, sound);

    }

    [RPC_Client]
    public void AddSoundDefinition( RPCMessage msg )
    {
        AddSoundDefinition(msg.read.StringRaw());
    }
    public void AddSoundDefinition( string msg )
    {
        var str = msg;

        if ( string.IsNullOrEmpty( str ) ) return;

        var jsonArray = JSON.Array.Parse( str );

        if ( jsonArray == null ) return;

        foreach ( var value in jsonArray )
        {
            var json = value.Obj;
            // in my test case i used Resources.Load to load a template SoundDefinition, this should be changed to use something from the asset bundle
            SoundDefinition def = Instantiate<SoundDefinition>((SoundDefinition)Resources.Load("TestDefinition"));

            string name = json.GetString("name", "ServerCreated_SoundDefinition");
            string url = json.GetString("url", null);
            if(string.IsNullOrEmpty(url)) continue;

            def.loop = json.GetBoolean("loop", false);
            var cat = ParseEnum<VolumeCategory>(json.GetString("category", "master"), VolumeCategory.master);
            def.volume = GetVolumeForCategory(cat);

            Rust.Global.Runner.StartCoroutine( LoadSoundFromWWW(def, url) );

            if(json.ContainsKey("falloffCurve")){
                def.falloffCurve = CreateCurveFromArray(json.GetArray("falloffCurve"));
                def.useCustomFalloffCurve = true;
            }

            if(json.ContainsKey("spatialBlendCurve")){
                def.spatialBlendCurve = CreateCurveFromArray(json.GetArray("spatialBlendCurve"));
                def.useCustomSpatialBlendCurve = true;
            }

            if(json.ContainsKey("spreadCurve")){
                def.spreadCurve = CreateCurveFromArray(json.GetArray("spreadCurve"));
                def.useCustomSpreadCurve = true;
            }
            def.volumeVariation = json.GetFloat("volumeDiff", 0f);
            def.pitch = json.GetFloat("pitch", 1f);
            def.pitchVariation = json.GetFloat("pitchDiff", 0f);


            RegisterDefinition(name, def);
        }
    }
    public AnimationCurve CreateCurveFromArray(JSON.Array array){
        Keyframe[] keyframes = new Keyframe[array.Length];
        int i = 0;
        foreach(var value in array){
            Color val = ColorEx.Parse(value.Str);
            keyframes[i++] = new Keyframe(val.r, val.g, val.b, val.a);
        }
        return new AnimationCurve(keyframes);
    }

    [RPC_Client]
    public void PlaySound( RPCMessage msg )
    {
        PlaySound(msg.read.StringRaw());
    }
    public void PlaySound( string msg )
    {

        var str = msg;

        if ( string.IsNullOrEmpty( str ) ) return;

        var jsonArray = JSON.Array.Parse( str );

        if ( jsonArray == null ) return;

        foreach ( var value in jsonArray )
        {
            Sound sound;
            var json = value.Obj;

            string instanceName = json.GetString("instanceName", "ServerCreated_SoundInstance");
            float fadeIn = json.GetFloat("fadeIn", 0f);


            sound = FindSound(instanceName);
            if(sound != null){
                sound.Stop();
                if(fadeIn > 0f){
                    sound.FadeInAndPlay(fadeIn);
                }else{
                    sound.Play();
                }
                continue;
            }


            string type = json.GetString("definitionType", "Rust");
            string definition = json.GetString("definition", null);
            uint parentID = (uint)json.GetNumber("parent", 0);
            Vector3 position = Vector3Ex.Parse(json.GetString("offset", "0 0 0"));
            if(string.IsNullOrEmpty(definition)) continue;

            SoundDefinition def = FindDefinition(definition);
            BaseEntity parent = null;
            if(parentID != 0)
            parent = (BaseNetworkable.clientEntities.Find(parentID) as BaseEntity);
            if(type == "Rust"){
                if(def == null){
                    def = FileSystem.Load<SoundDefinition>(definition);
                    if(def == null){
                        continue;
                    }else{
                        RegisterDefinition(definition, def);
                    }
                }
            }
            else if(def == null) continue;
            bool firstPerson = (parent == null && position == Vector3.zero);
            sound = SoundManager.RequestSoundInstance(def, parent?.gameObject, position, firstPerson);
            if(!sound) continue;

            // Experiencing a bug here i'm unsure if its only occuring on my test scenario, it seems like the max distance of the source gets set to 500 every frame
            // ideally i would like to have this & dopplerScale be a setting when defining the sound definition instead, but i couldnt find a way to make that happen with the way new Sounds get instantiated
            float maxDistance = json.GetFloat("maxDistance", 1f);
            if(maxDistance > 0f){
                sound.audioSources[0].maxDistance = maxDistance;
            }
            sound.audioSources[0].dopplerLevel = json.GetFloat("dopplerScale", 1f);
            RegisterSound(instanceName, sound);
            if(fadeIn > 0f){
                sound.FadeInAndPlay(fadeIn);
            }else{
                sound.Play();
            }
        }
    }

    [RPC_Client]
    public void StopSound( RPCMessage msg )
    {
        StopSound(msg.read.StringRaw());
    }
    public void StopSound( string msg )
    {
        var str = msg;

        if ( string.IsNullOrEmpty( str ) ) return;

        var jsonArray = JSON.Array.Parse( str );

        if ( jsonArray == null ) return;

        foreach ( var value in jsonArray )
        {
            var json = value.Obj;

            string instanceName = json.GetString("instanceName", "ServerCreated_SoundInstance");
            float fadeOut = json.GetFloat("fadeOut", 0f);
            bool kill = json.GetBoolean("kill", false);
            Sound sound = FindSound(instanceName);
            if(!sound) continue;
            // we dont recycle the sound so it can be played again in future RPC calls
            if(fadeOut > 0f){
                sound.fade.FadeOut(fadeOut);
                if(kill) Invoke(new Action(() => KillSound(sound)), fadeOut);
                else Invoke(new Action(sound.Stop), fadeOut);
            }else{
                sound.Stop();
            }

        }
    }

    private SoundDefinition FindDefinition( string name )
    {

        SoundDefinition def;
        if ( ServerDefinitions.TryGetValue( name, out def ) )
        {
            return def;
        }

        return null;
    }

    private Sound FindSound( string name )
    {

        Sound sound;
        if ( SoundDict.TryGetValue( name, out sound ) )
        {
            return sound;
        }

        return null;
    }
    private IEnumerator LoadSoundFromWWW( SoundDefinition def, string source)
    {
        if(WebSoundCache.ContainsKey(source)){
            // todo: assign audio
            if(def != null)
                def.weightedAudioClips.Add(new WeightedAudioClip{
                    audioClip = WebSoundCache[source]
                });
            yield break;
        }

        var www = new WWW( source.Trim() );

        while ( !www.isDone )
        {
            yield return null;
        }

        if ( !string.IsNullOrEmpty( www.error ) )
        {
            Debug.Log( "Error downloading sound: " + source + " (" + www.error + ")" );
            www.Dispose();
            yield break;
        }


        AudioClip clip = www.GetAudioClip();
        if ( clip == null )
        {
            Debug.Log( "Error downloading sound: " + source + " (not a sound file)" );
            www.Dispose();
            yield break;
        }

        if(def != null) def.weightedAudioClips.Add(new WeightedAudioClip{
            audioClip = clip
        });

        if(!WebSoundCache.ContainsKey(source)) WebSoundCache.Add(source, clip);

        www.Dispose();
    }


    public void DestroySound( RPCMessage msg )
    {
        DestroySound( msg.read.StringRaw() );
    }
    public static void DestroySound( string str )
    {

        Sound sound;

        if ( string.IsNullOrEmpty( str ) ) return;

        var jsonArray = JSON.Array.Parse( str );
        if ( jsonArray == null ) return;

        foreach ( var value in jsonArray ){
            var json = value.Obj;
            if (SoundDict.TryGetValue( json.GetString("instance", "Invalid_Instance"), out sound )) continue;

            KillSound( sound, json.GetFloat("fadeOut", 0f));
        }

    }

    public float GetVolumeForCategory(VolumeCategory category){
        switch(category){
            case VolumeCategory.master: return Audio.master;
            case VolumeCategory.music: return Audio.musicvolume;
            case VolumeCategory.game: return Audio.game;
            case VolumeCategory.voice: return Audio.voices;
            case VolumeCategory.instruments: return Audio.instruments;
            case VolumeCategory.voiceProps: return Audio.voiceProps;
            default: return Audio.master;
        }
        return Audio.master;
    }

    public enum VolumeCategory{
        master,
        music,
        game,
        voice,
        instruments,
        voiceProps
    }

    private static void KillSound( Sound sound, float fadeOut = 0f )
    {
        if(AllSounds.Contains(sound)) AllSounds.Remove(sound);

        if ( fadeOut > 0f)
        {
            sound.FadeOutAndRecycle(fadeOut);
        }
        else
        {
            SoundManager.RecycleSound(sound);
        }
    }
}

#endif
