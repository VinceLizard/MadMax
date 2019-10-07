using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroySelf : MonoBehaviour
{
    AudioSource thisAudio;
    // Start is called before the first frame update
    void Start()
    {
       thisAudio = GetComponent<AudioSource>();
        StartCoroutine(DieAfterAudio());

    }

    IEnumerator DieAfterAudio() {
        yield return new WaitForSeconds(1f);
        while (thisAudio.isPlaying) {
            yield return new WaitForSeconds(.1f);
        }
        Destroy(gameObject);
    }

}
