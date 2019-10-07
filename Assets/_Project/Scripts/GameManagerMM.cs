// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Launcher.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Networking Demos
// </copyright>
// <summary>
//  Used in "PUN Basic tutorial" to handle typical game management requirements
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable 649

/// <summary>
/// Game manager.
/// Connects and watch Photon Status, Instantiate Player
/// Deals with quiting the room and the game
/// Deals with level loading (outside the in room synchronization)
/// </summary>
public class GameManagerMM : MonoBehaviourPunCallbacks
{
    
    

    #region Public Fields

    static public GameManagerMM Instance;

	public List<Transform> SpawnPoints;
    public bool EndGame = false;
    [HideInInspector]
    public GameObject ThisIsMe;
	#endregion

	#region Private Fields

	private GameObject instance;

	[Tooltip("The prefab to use for representing the player")]
	[SerializeField]
	private GameObject playerPrefab;
    [SerializeField]
    AudioClip music;
    [SerializeField]
    AudioClip intenseMusic;
    [SerializeField]
    AudioClip loseMusic;
    [SerializeField]
    AudioClip winMusic;
    [SerializeField]
    GameObject endGameOrchestralHit;
    [SerializeField]
    GameObject endGameVO;
    [SerializeField]
    GameObject startVO;
    [SerializeField]
    GameObject winVO;
    [SerializeField]
    GameObject loseVO;
    [SerializeField]
    GameObject revertVO;
    [SerializeField]
    float musicFadeTimeInSeconds = 2f;
    AudioSource thisAudio;

    bool triggerEndGame = false;
    bool audioTransitioning = false;
    GameObject junkeParent;

    // Set these in Inspector
	public int RequiredToDepot = 50;
	public float JunkSpawnRadius = 50f;
	public int MaxNumberOfJunkSpawns = 5000;
    public int MaxNumToSpawnPerJunkGroup = 50;
    public float CooldownBeforeNextJunkSpawn = 3.0f;

    #endregion

    #region MonoBehaviour CallBacks

    void Awake()
    {
        junkeParent = GameObject.Find("JunkParent");
    }

	/// <summary>
	/// MonoBehaviour method called on GameObject by Unity during initialization phase.
	/// </summary>
	void Start()
	{
		thisAudio = GetComponent<AudioSource>();

        Instance = this;

		// in case we started this demo with the wrong scene being active, simply load the menu scene
		if (!PhotonNetwork.IsConnected)
		{
			SceneManager.LoadScene("CarLauncher");

			return;
		}

		if (playerPrefab == null)
		{ // #Tip Never assume public properties of Components are filled up properly, always check and inform the developer of it.

			Debug.LogError("<Color=Red><b>Missing</b></Color> playerPrefab Reference. Please set it up in GameObject 'Game Manager'", this);
		}
		else
		{
			if (PlayerManagerCarPhoton.LocalPlayerInstance == null)
			{
				Debug.LogFormat("We are Instantiating LocalPlayer from {0}", SceneManagerHelper.ActiveSceneName);


				Transform spawn = null;
				int offsetSpawn = Random.Range(0, SpawnPoints.Count);
				for (int i=0;  i< SpawnPoints.Count; i++)
				{
					var sp = this.SpawnPoints[(i + offsetSpawn) % SpawnPoints.Count];
					var ray = new Ray(sp.transform.position + new Vector3(0, 100, 0), Vector3.down);
					if( Physics.SphereCast(ray, 5.0f, 1000.0f, LayerMask.GetMask(new string[1] { "Cars" })) )
					{
						spawn = sp;
					}
				}

				if(spawn == null)
				{
					spawn = SpawnPoints[offsetSpawn];
				}
				
				// we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
				ThisIsMe = PhotonNetwork.Instantiate(this.playerPrefab.name, spawn.position, spawn.rotation, 0);
                Instantiate(startVO, transform.position, transform.rotation);
                thisAudio.clip = music;
                thisAudio.Play();
			}
			else
			{

				Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
			}
		}

		StartCoroutine(SpawnCoro());
	}

	/// <summary>
	/// MonoBehaviour method called on GameObject by Unity on every frame.
	/// </summary>
	void Update()
	{
		// "back" button of phone equals "Escape". quit app if that's pressed
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			QuitApplication();
		}

        if (EndGame != triggerEndGame) 
        {
            triggerEndGame = EndGame;
            if (triggerEndGame) 
            {
                if (!audioTransitioning) {
                    StartCoroutine(StartEndGame());
                }
            } else {
                if (!audioTransitioning) {
                    StartCoroutine(RevertEndGame());
                }
            }
        }
	}

    // switch to endgame music
    IEnumerator StartEndGame() {
        audioTransitioning = true;
        Instantiate(endGameOrchestralHit, transform.position, transform.rotation);
        Instantiate(endGameVO, transform.position, transform.rotation);
        var t = 0f;
        while (t < musicFadeTimeInSeconds) {
            t += .1f;
            thisAudio.volume = 1-(t/musicFadeTimeInSeconds);
            yield return new WaitForSeconds(.1f);
        }
        thisAudio.Stop();
        thisAudio.volume = 1;
        thisAudio.clip = intenseMusic;
        thisAudio.Play();
        audioTransitioning = false;
    }

    // switch to normal music
    IEnumerator RevertEndGame() {
        audioTransitioning = true;
        Instantiate(revertVO, transform.position, transform.rotation);
        var t = 0f;
        while (t < musicFadeTimeInSeconds) {
            t += .1f;
            thisAudio.volume = 1 - (t / musicFadeTimeInSeconds);
            yield return new WaitForSeconds(.1f);
        }
        thisAudio.Stop();
        thisAudio.volume = 1;
        thisAudio.clip = music;
        thisAudio.Play();
        audioTransitioning = false;
        yield return null;
    }

    IEnumerator YouWin() {
        Instantiate(winVO, transform.position, transform.rotation);
        var t = 0f;
        while (t < musicFadeTimeInSeconds) {
            t += .1f;
            thisAudio.volume = 1 - (t / musicFadeTimeInSeconds);
            yield return new WaitForSeconds(.1f);
        }
        thisAudio.Stop();
        thisAudio.volume = 1;
        thisAudio.clip = winMusic;
        thisAudio.Play();
        yield return null;
    }

    IEnumerator YouLose() {
        thisAudio.Stop();
        Instantiate(loseVO, transform.position, transform.rotation);
        var t = 0f;
        while (t < musicFadeTimeInSeconds) {
            t += .1f;
            thisAudio.volume = 1 - (t / musicFadeTimeInSeconds);
            yield return new WaitForSeconds(.1f);
        }
        thisAudio.Stop();
        thisAudio.volume = 1;
        thisAudio.clip = loseMusic;
        thisAudio.Play();
        yield return null;
    }

    // Spawn junk
    IEnumerator SpawnCoro()
    {
        int amountToSpawn;
        while (true)
        {
            if (PhotonNetwork.IsMasterClient)
            {

                int numberspawed = GameObject.FindGameObjectsWithTag("Junk").Length;
                amountToSpawn = Mathf.Min(MaxNumToSpawnPerJunkGroup, (MaxNumberOfJunkSpawns - GameObject.FindGameObjectsWithTag("Junk").Length));
                Debug.Log("Starting to spawn a group of" + amountToSpawn + " junk.");
                while (amountToSpawn > 0)
                {
                    var v = Random.insideUnitCircle * JunkSpawnRadius;
                    var ray = new Ray(new Vector3(v.x, 500.0f, v.y), Vector3.down);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 2000.0f, LayerMask.GetMask(new string[1] { "Terrain" })))
                    {
                        Debug.Log("Spawning");
                        GameObject go = PhotonNetwork.InstantiateSceneObject("Junk", new Vector3(hit.point.x, hit.point.y + 1, hit.point.z), Quaternion.identity, 0);
                        go.transform.SetParent(junkeParent.transform);
                        amountToSpawn--;
                    }

                    yield return new WaitForSeconds(.1f);
                }

            }
            yield return new WaitForSeconds(CooldownBeforeNextJunkSpawn);
        }
    }

    #endregion

    #region Photon Callbacks
    /*
		/// <summary>
		/// Called when a Photon Player got connected. We need to then load a bigger scene.
		/// </summary>
		/// <param name="other">Other.</param>
		public override void OnPlayerEnteredRoom(Player other)
		{
			Debug.Log("OnPlayerEnteredRoom() " + other.NickName); // not seen if you're the player connecting

			if (PhotonNetwork.IsMasterClient)
			{
				Debug.LogFormat("OnPlayerEnteredRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom

				LoadArena();
			}
		}

		/// <summary>
		/// Called when a Photon Player got disconnected. We need to load a smaller scene.
		/// </summary>
		/// <param name="other">Other.</param>
		public override void OnPlayerLeftRoom(Player other)
		{
			Debug.Log("OnPlayerLeftRoom() " + other.NickName); // seen when other disconnects

			if (PhotonNetwork.IsMasterClient)
			{
				Debug.LogFormat("OnPlayerEnteredRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom

				LoadArena();
			}
		}
        */
    /// <summary>
    /// Called when the local player left the room. We need to load the launcher scene.
    /// </summary>
    public override void OnLeftRoom()
	{
            SceneManager.LoadScene("CarLauncher");
	}

	#endregion

	#region Public Methods

    public void StartTheWin()
    {
        StartCoroutine(YouWin());
    }

    public void StarTheLose() {
        StartCoroutine(YouLose());
    }

    public void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
	}

	public void QuitApplication()
	{
		Application.Quit();
	}

	#endregion

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0.0f, 0.0f, 1.0f, 0.5f);
		foreach (var sp in SpawnPoints)
			Gizmos.DrawSphere(sp.transform.position, 5.0f);

		Gizmos.DrawWireSphere(this.transform.position, JunkSpawnRadius);
	}
    /*
		#region Private Methods

		void LoadArena()
		{
			if (!PhotonNetwork.IsMasterClient)
			{
				Debug.LogError("PhotonNetwork : Trying to Load a level but we are not the master Client");
			}

			Debug.LogFormat("PhotonNetwork : Loading Level : {0}", PhotonNetwork.CurrentRoom.PlayerCount);

			PhotonNetwork.LoadLevel("PunBasics-Room for " + PhotonNetwork.CurrentRoom.PlayerCount);
		}

		#endregion
    */

}



