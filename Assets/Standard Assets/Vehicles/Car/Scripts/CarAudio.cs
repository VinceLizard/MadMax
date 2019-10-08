using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;
using Photon.Pun;
using UnityStandardAssets.Vehicles.Car;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof (CarController))]
    public class CarAudio : MonoBehaviourPunCallbacks
    {
        // This script reads some of the car's current properties and plays sounds accordingly.
        // The engine sound can be a simple single clip which is looped and pitched, or it
        // can be a crossfaded blend of four clips which represent the timbre of the engine
        // at different RPM and Throttle state.

        // the engine clips should all be a steady pitch, not rising or falling.

        // when using four channel engine crossfading, the four clips should be:
        // lowAccelClip : The engine at low revs, with throttle open (i.e. begining acceleration at very low speed)
        // highAccelClip : Thenengine at high revs, with throttle open (i.e. accelerating, but almost at max speed)
        // lowDecelClip : The engine at low revs, with throttle at minimum (i.e. idling or engine-braking at very low speed)
        // highDecelClip : Thenengine at high revs, with throttle at minimum (i.e. engine-braking at very high speed)

        // For proper crossfading, the clips pitches should all match, with an octave offset between low and high.


        public enum EngineAudioOptions // Options for the engine audio
        {
            Simple, // Simple style audio
            FourChannel // four Channel audio
        }

        public EngineAudioOptions engineSoundStyle = EngineAudioOptions.FourChannel;// Set the default audio options to be four channel
        public AudioClip lowAccelClip;                                              // Audio clip for low acceleration
        public AudioClip lowDecelClip;                                              // Audio clip for low deceleration
        public AudioClip highAccelClip;                                             // Audio clip for high acceleration
        public AudioClip highDecelClip;                                             // Audio clip for high deceleration
        public AudioClip gravelClip;                                               // Audio clip for gravel sound when moving
        public AudioClip[] crashAudioClips;
        public AudioClip[] junkPickupClips;
        public float pitchMultiplier = 1f;                                          // Used for altering the pitch of audio clips
        public float lowPitchMin = 1f;                                              // The lowest possible pitch for the low sounds
        public float lowPitchMax = 6f;                                              // The highest possible pitch for the low sounds
        public float highPitchMultiplier = 0.25f;                                   // Used for altering the pitch of high sounds
        public float maxRolloffDistance = 500;                                      // The maximum distance where rollof starts to take place
        public float dopplerLevel = 1;                                              // The mount of doppler effect used in the audio
        public bool useDoppler = true;                                              // Toggle for using doppler

        private AudioSource m_LowAccel; // Source for the low acceleration sounds
        private AudioSource m_LowDecel; // Source for the low deceleration sounds
        private AudioSource m_HighAccel; // Source for the high acceleration sounds
        private AudioSource m_HighDecel; // Source for the high deceleration sounds
        private AudioSource gravelAudioSource; // Source for gravel sound when moving
        private AudioSource crashAudioSource;
        private AudioSource junkPickupAudioSource;
        private AudioMixer mixer;

        private bool m_StartedSound; // flag for knowing if we have started sounds
        private bool gravelSoundStarted = false;
        private CarController m_CarController; // Reference to car we are controlling
        float currentSpeed;
        float newCurrentSpeed;
        public float boostMultiplier = 1;

        private void Awake()
        {
            mixer = Resources.Load("MMMixer") as AudioMixer;

            //Setup gravel audiosource
            if (gravelClip != null)
            {
                gravelAudioSource = gameObject.AddComponent<AudioSource>();
                gravelAudioSource.outputAudioMixerGroup = mixer.FindMatchingGroups("Car Engine")[0];
                gravelAudioSource.playOnAwake = false;
                gravelAudioSource.clip = gravelClip;
                gravelAudioSource.volume = .2f;
                gravelAudioSource.loop = true;
                gravelAudioSource.minDistance = 5;
                gravelAudioSource.maxDistance = maxRolloffDistance;
                gravelAudioSource.dopplerLevel = 0;
            }

            //Setup gravel audiosource
            if (crashAudioClips[0] != null)
            {
                crashAudioSource = gameObject.AddComponent<AudioSource>();
                crashAudioSource.outputAudioMixerGroup = mixer.FindMatchingGroups("Crash")[0];
                crashAudioSource.playOnAwake = false;
                crashAudioSource.clip = crashAudioClips[Random.Range(0, crashAudioClips.Length)];
                crashAudioSource.volume = 1f;
                crashAudioSource.loop = false;
                crashAudioSource.minDistance = 5;
                crashAudioSource.maxDistance = maxRolloffDistance;
                crashAudioSource.dopplerLevel = 0;
                
            }

            //Setup junk pickup audiosource
            if (junkPickupClips[0] != null)
            {
                junkPickupAudioSource = gameObject.AddComponent<AudioSource>();
                junkPickupAudioSource.outputAudioMixerGroup = mixer.FindMatchingGroups("Pick Up Junk")[0];
                junkPickupAudioSource.playOnAwake = false;
                junkPickupAudioSource.clip = junkPickupClips[Random.Range(0, junkPickupClips.Length)];
                junkPickupAudioSource.volume = 1f;
                junkPickupAudioSource.loop = false;
                junkPickupAudioSource.minDistance = 5;
                junkPickupAudioSource.maxDistance = maxRolloffDistance;
                junkPickupAudioSource.dopplerLevel = 0;
            }
        }

        private void StartSound()
        {
            // get the carcontroller ( this will not be null as we have require component)
            m_CarController = GetComponent<CarController>();

            // setup the simple audio source
            m_HighAccel = SetUpEngineAudioSource(highAccelClip);

            // if we have four channel audio setup the four audio sources
            if (engineSoundStyle == EngineAudioOptions.FourChannel)
            {
                m_LowAccel = SetUpEngineAudioSource(lowAccelClip);
                m_LowDecel = SetUpEngineAudioSource(lowDecelClip);
                m_HighDecel = SetUpEngineAudioSource(highDecelClip);                
            }

            StartCoroutine("CrashingSounds");

            // flag that we have started the sounds playing
            m_StartedSound = true;
        }

        IEnumerator CrashingSounds()
        {
            while(true)
            {
                if (crashAudioSource != null && crashAudioClips != null)
                {
                    currentSpeed = m_CarController.CurrentSpeed;
                    yield return new WaitForSeconds(.1f);
                    newCurrentSpeed = m_CarController.CurrentSpeed;
                    if (newCurrentSpeed - currentSpeed < -.01f && !(m_CarController.BrakeInput > 0))
                    {
                        crashAudioSource.Play();
                        crashAudioSource.clip = crashAudioClips[Random.Range(0, crashAudioClips.Length)];
                        yield return new WaitForSeconds(1f);
                    }
                }          
            }
        }


        private void StopSound()
        {
            //Destroy all audio sources on this object:
            foreach (var source in GetComponents<AudioSource>())
            {
                Destroy(source);
            }

            m_StartedSound = false;
        }


        // Update is called once per frame
        private void Update()
        {
            if (!photonView.IsMine)
                return;

            // get the distance to main camera
            float camDist = (Camera.main.transform.position - transform.position).sqrMagnitude;

            // stop sound if the object is beyond the maximum roll off distance
            if (m_StartedSound && camDist > maxRolloffDistance * maxRolloffDistance)
            {
                StopSound();
            }

            // start the sound if not playing and it is nearer than the maximum distance
            if (!m_StartedSound && camDist < maxRolloffDistance * maxRolloffDistance)
            {
                StartSound();
            }

            if (m_StartedSound)
            {
                //Gravel sound

                if (gravelAudioSource != null)
                {
                    if (m_CarController.CurrentSpeed >= 5f && !gravelSoundStarted)
                    {
                        gravelAudioSource.Play();
                        gravelSoundStarted = true;
                    }
                    else if (gravelSoundStarted && m_CarController.CurrentSpeed < 5f)
                    {
                        gravelAudioSource.Stop();
                        gravelSoundStarted = false;
                    }
                }
                // The pitch is interpolated between the min and max values, according to the car's revs.
                float pitch = ULerp(lowPitchMin, lowPitchMax * boostMultiplier, m_CarController.Revs);

                // clamp to minimum pitch (note, not clamped to max for high revs while burning out)
                pitch = Mathf.Min(lowPitchMax * boostMultiplier, pitch);

                if (engineSoundStyle == EngineAudioOptions.Simple)
                {
                    // for 1 channel engine sound, it's oh so simple:
                    m_HighAccel.pitch = pitch*pitchMultiplier*highPitchMultiplier;
                    m_HighAccel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    m_HighAccel.volume = 1;
                }
                else
                {
                    // for 4 channel engine sound, it's a little more complex:

                    // adjust the pitches based on the multipliers
                    m_LowAccel.pitch = pitch*pitchMultiplier;
                    m_LowDecel.pitch = pitch*pitchMultiplier;
                    m_HighAccel.pitch = pitch*highPitchMultiplier*pitchMultiplier;
                    m_HighDecel.pitch = pitch*highPitchMultiplier*pitchMultiplier;

                    // get values for fading the sounds based on the acceleration
                    float accFade = Mathf.Abs(m_CarController.AccelInput);
                    float decFade = 1 - accFade;

                    // get the high fade value based on the cars revs
                    float highFade = Mathf.InverseLerp(0.2f, 0.8f, m_CarController.Revs);
                    float lowFade = 1 - highFade;

                    // adjust the values to be more realistic
                    highFade = 1 - ((1 - highFade)*(1 - highFade));
                    lowFade = 1 - ((1 - lowFade)*(1 - lowFade));
                    accFade = 1 - ((1 - accFade)*(1 - accFade));
                    decFade = 1 - ((1 - decFade)*(1 - decFade));

                    // adjust the source volumes based on the fade values
                    m_LowAccel.volume = lowFade*accFade;
                    m_LowDecel.volume = lowFade*decFade;
                    m_HighAccel.volume = highFade*accFade;
                    m_HighDecel.volume = highFade*decFade;

                    // adjust the doppler levels
                    m_HighAccel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    m_LowAccel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    m_HighDecel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    m_LowDecel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                }
            }
        }


        // sets up and adds new audio source to the game object
        private AudioSource SetUpEngineAudioSource(AudioClip clip)
        {
            // create the new audio source component on the game object and set up its properties
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = mixer.FindMatchingGroups("Car Engine")[0];
            source.clip = clip;
            source.volume = 0;
            source.loop = true;
            
            // start the clip from a random point
            source.time = Random.Range(0f, clip.length);
            source.Play();
            source.minDistance = 5;
            source.maxDistance = maxRolloffDistance;
            source.dopplerLevel = 0;
            return source;
        }

        public void PlayPickupSound()
        {
            junkPickupAudioSource.clip = junkPickupClips[Random.Range(0, junkPickupClips.Length)];
            this.junkPickupAudioSource.Play();
        }

        // unclamped versions of Lerp and Inverse Lerp, to allow value to exceed the from-to range
        private static float ULerp(float from, float to, float value)
        {
            return (1.0f - value)*from + value*to;
        }
    }
}
