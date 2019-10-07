using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroySelf : MonoBehaviour
{
    AudioSource thisAudio;
    [SerializeField]
    AudioClip[] voiceOvers;
    // Start is called before the first frame update
    void Start()
    {
       thisAudio = GetComponent<AudioSource>();
        StartCoroutine(DieAfterAudio());

    }

    IEnumerator DieAfterAudio() {
        var whichClip = Random.Range(0, voiceOvers.Length-1);
        thisAudio.clip = voiceOvers[whichClip];
        thisAudio.Play();
        yield return new WaitForSeconds(.5f);
        while (thisAudio.isPlaying) {
            yield return new WaitForSeconds(.1f);
        }
        Destroy(gameObject);
    }

}
