using UnityEngine;

public class DinoSound : MonoBehaviour
{
    public AudioSource audioSource;
    public float volume = 1f;
    [Header("오디오 클립")]
    public AudioClip[] stepClips;  // 발소리
    public AudioClip[] breathClips;  // 숨소리
    public AudioClip[] growlClips;  // 으르렁
    public AudioClip[] barkClips;   // 짖기
    public AudioClip[] roarClips;   // 포효
    public AudioClip[] yelpClips;   // 피격
    public AudioClip[] deathClips;  // 죽음

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayGrowl() // 으르렁
    {
        int Index = Random.Range(0, growlClips.Length);

        AudioClip clip = growlClips[Index];
        audioSource.PlayOneShot(clip);
    }

    public void PlayStep() // 발소리
    {
        int Index = Random.Range(0, stepClips.Length);

        AudioClip clip = stepClips[Index];
        audioSource.PlayOneShot(clip);
    }

    public void PlayBreath() // 경계
    {
        int Index = Random.Range(0, breathClips.Length);

        AudioClip clip = breathClips[Index];
        audioSource.PlayOneShot(clip);
    }


    public void PlayBark()  // 짖기
    {
        int Index = Random.Range(0, barkClips.Length);

        AudioClip clip = barkClips[Index];
        audioSource.PlayOneShot(clip);
    }


    public void PlayRoar()  // 포효
    {
        int Index = Random.Range(0, roarClips.Length);

        AudioClip clip = roarClips[Index];
        audioSource.PlayOneShot(clip);
    }

    public void PlayYelp()  // 피격
    {
        int Index = Random.Range(0, yelpClips.Length);

        AudioClip clip = yelpClips[Index];
        audioSource.PlayOneShot(clip);
    }


    public void PlayDeath() // 죽음
    {
        int Index = Random.Range(0, deathClips.Length);

        AudioClip clip = deathClips[Index];
        audioSource.PlayOneShot(clip);
    }
}
