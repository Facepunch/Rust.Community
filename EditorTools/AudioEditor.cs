using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class AudioEditor : MonoBehaviour
{

    [Header("Load SoundDefinition")]

    [Tooltip("Required, the name of the SoundDefinition")]
    public string definitionName = "";

    [Tooltip("Required, the url of the sound file")]
    public string soundUrl = "";

    [Tooltip("Optional, the Volume Setting the sound should use, defaults to Master")]
    public VolumeCategory audioCategory = VolumeCategory.master;

    [Tooltip("Whether or not the sound should loop")]
    public bool shouldLoop = false;

    [Tooltip("Optional, sets range for volume variation")]
    [Range(0f, 1f)]
    public float volumeDiff = 0f;

    [Tooltip("Optional, sets pitch offset")]
    [Range(-3f, 3f)]
    public float pitch = 1f;

    [Tooltip("Optional, sets range for random pitch")]
    [Range(0f, 1f)]
    public float pitchDiff = 0f;

    [Tooltip("Optional, Serializes the Animation's keyframes into a list of strings to reconstruct later")]
    public AnimationCurve falloffCurve = new AnimationCurve();

    [Tooltip("Optional, Serializes the Animation's keyframes into a list of strings to reconstruct later")]
    public AnimationCurve spatialBlendCurve = new AnimationCurve();

    [Tooltip("Optional, Serializes the Animation's keyframes into a list of strings to reconstruct later")]
    public AnimationCurve spreadCurve = new AnimationCurve();

    [CustomButtonAttribute("ApplyDefinition")]
    public bool ApplyingDefinition;

    [Space(10)]
    [Header("Load Sound")]

    [Tooltip("Required, the name of the sound instance, allows you to kill it later")]
    public string instanceName = "";

    [Tooltip("Required, the path/name of the definition to use")]
    public string definition = "";

    [Tooltip("Optional, if higher than 0 the sound will linearly fade in instead of playing at full volume immediately")]
    public float fadeIn = 0f;

    [Tooltip("Optional, attempts to find an Entity to parent the sound to")]
    public uint parentNetID = 0;

    [Tooltip("Optional, Positions the sound in the world instead of it being first person")]
    public Vector3 offset;

    [Tooltip("Optional, Sets a Custom Max Distance")]
    [Range(0f, 500f)]
    public float maxDistance = 0f;

    [Tooltip("Optional, sets a custom doppler value to modify how relative velocity affects the pitch of the sound")]
    [Range(0f, 1f)]
    public float dopplerScale = 0f;

    [CustomButtonAttribute("ApplySound")]
    public bool ApplyingSound;


    [Space(10)]
    [Header("Stop Sound")]


    [Tooltip("Required, finds the active sound instance to kill")]
    public string instanceToKill = "";

    [Tooltip("Optional, fades out the sound before killing it")]
    public float fadeOut = 0f;

    [Tooltip("Whether or not to Kill the Sound")]
    public bool killSound = false;

    [CustomButtonAttribute("StopSound")]
    public bool ApplyingEndSound;

    public void ApplyDefinition(){
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("[{\"name\": \"" + definitionName + "\", \"url\": \"" + soundUrl + "\", \"loop\": " + (shouldLoop ? "true" : "false") + ", \"category\": \"" + audioCategory + "\", \"volumeDiff\": " + volumeDiff + ", \"pitch\": " + pitch + ", \"pitchDiff\": " + pitchDiff + "");

        if (falloffCurve.length > 0){
            stringBuilder.Append(", \"falloffCurve\": [");
            foreach(var frame in falloffCurve.keys){
                stringBuilder.Append($"\"{frame.time} {frame.@value} {frame.inTangent} {frame.outTangent}\", ");
            }
            stringBuilder.Remove(stringBuilder.Length - 2, 2);
            stringBuilder.Append("]");
        }

        if (spatialBlendCurve.length > 0){
            stringBuilder.Append(", \"spatialBlendCurve\": [");
            foreach(var frame in spatialBlendCurve.keys){
                stringBuilder.Append($"\"{frame.time} {frame.@value} {frame.inTangent} {frame.outTangent}\", ");
            }
            stringBuilder.Remove(stringBuilder.Length - 2, 2);
            stringBuilder.Append("]");
        }

        if (spreadCurve.length > 0){
            stringBuilder.Append(", \"spreadCurve\": [");
            foreach(var frame in spreadCurve.keys){
                stringBuilder.Append($"\"{frame.time} {frame.@value} {frame.inTangent} {frame.outTangent}\", ");
            }
            stringBuilder.Remove(stringBuilder.Length - 2, 2);
            stringBuilder.Append("]");
        }

        stringBuilder.Append("}]");
        NuCommunityEntity.ClientInstance?.AddSoundDefinition( stringBuilder.ToString());
    }

    public void ApplySound(){
        string LoadSound = "[{\"instanceName\": \"" + instanceName + "\", \"definition\": \"" + definition + "\", \"fadeIn\": " + fadeIn + ",  \"parent\": " + parentNetID + ",  \"offset\": \"" + $"{offset.x} {offset.y} {offset.z}" + "\", \"maxDistance\": " + maxDistance + ", \"dopplerScale\": " + dopplerScale + "}]";
        NuCommunityEntity.ClientInstance?.PlaySound( LoadSound );
    }

    public void StopSound(){
        string EndSound = "[{\"instanceName\": \"" + instanceToKill + "\", \"fadeOut\": " + fadeOut + ", \"kill\": " + (killSound ? "true" : "false") + ",}]";
        NuCommunityEntity.ClientInstance?.StopSound( EndSound );

    }

    public enum VolumeCategory{
        master,
        music,
        game,
        voice,
        instruments,
        voiceProps
    }
}
