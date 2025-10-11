using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager instance;

    [SerializeField] private AudioSource soundFXObject;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        // spawn in AudioSource prefab at the position
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);

        // assign the Audio Clip
        audioSource.clip = audioClip;

        // assign volume
        audioSource.volume = volume;

        // Play sound
        audioSource.Play();

        // get length of sound FX Clip
        float clipLength = audioSource.clip.length;

        // destroy the clip after it is done playing 
        Destroy(audioSource.gameObject, clipLength);
    }

    public void PlayRandomSoundFXClip(AudioClip[] audioClip, Transform spawnTransform, float volume)
    {

        int rand = Random.Range(0, audioClip.Length);

        // spawn in AudioSource prefab at the position
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);

        // assign the Audio Clip
        audioSource.clip = audioClip[rand];

        // assign volume
        audioSource.volume = volume;

        // Play sound
        audioSource.Play();

        // get length of sound FX Clip
        float clipLength = audioSource.clip.length;

        // destroy the clip after it is done playing 
        Destroy(audioSource.gameObject, clipLength);
    }
}
