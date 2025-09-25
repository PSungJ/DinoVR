using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DinoSound : MonoBehaviour
{
    public AudioSource audioSource;
    public float volume = 1f;
    [Header("¿Àµð¿À Å¬¸³")]
    public AudioClip[] growlClips;  // À¸¸£··
    public AudioClip[] yelpClips;   // ÇÇ°Ý
    public AudioClip[] barkClips;   // Â¢±â
    public AudioClip[] roarClips;   // Æ÷È¿
    public AudioClip[] deathClips;  // Á×À½

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void Growl()
    {
        int Index = Random.Range(0, growlClips.Length);

        AudioClip clip = growlClips[Index];
        audioSource.PlayOneShot(clip);
    }

    //Yelp Sounds (Random)

    public void Yelp()
    {
        int Index = Random.Range(0, yelpClips.Length);

        AudioClip clip = yelpClips[Index];
        audioSource.PlayOneShot(clip);
    }

    //Bark Sounds (Random)

    public void Bark()
    {
        int Index = Random.Range(0, barkClips.Length);

        AudioClip clip = barkClips[Index];
        audioSource.PlayOneShot(clip);
    }

    //Roar Sounds (Random)

    public void Roar()
    {
        int Index = Random.Range(0, roarClips.Length);

        AudioClip clip = roarClips[Index];
        audioSource.PlayOneShot(clip);
    }


    //Death Sounds (Random)

    public void Death()
    {
        int Index = Random.Range(0, deathClips.Length);

        AudioClip clip = deathClips[Index];
        audioSource.PlayOneShot(clip);
    }
}
