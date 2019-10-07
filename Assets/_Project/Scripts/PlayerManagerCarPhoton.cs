// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayerManager.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Networking Demos
// </copyright>
// <summary>
//  Used in PUN Basics Tutorial to deal with the networked player instance
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;
using UnityStandardAssets.Vehicles.Car;

#pragma warning disable 649

/// <summary>
/// Player manager.
/// Handles fire Input and Beams.
/// </summary>
public class PlayerManagerCarPhoton : MonoBehaviourPunCallbacks, IPunObservable
{

    private static float JUNK_SPAWN_EJECT_HEIGHT_OFFSET = 7;
    private static int JUNK_EJECT_COOLDOWN = 5;
    private static int AMOUNT_OF_JUNK_TO_EJECT = 10;

    #region Public Fields

    [Tooltip("The current Health of our player")]
	public float Health = 1f;

	[Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
	public static GameObject LocalPlayerInstance;

	[Tooltip("The current Junk of our player")]
	public int Junk = 0;

	[Tooltip("The total junk stored")]
	public bool IsWinner = false;

    [Tooltip("The player has enough junk to win")]
    public bool IsLoaded = false;

    public Renderer CarUiRenderer;
	#endregion

	#region Private Fields

	[Tooltip("The Player's UI GameObject Prefab")]
	[SerializeField]
	private GameObject playerUiPrefab;

	[Tooltip("The Lasers GameObject to control")]
	[SerializeField]
	private GameObject lasers;

    [SerializeField]
    private float boostPower = 100;

    [SerializeField]
    private float boostCoolDownTime = 10;

    //True, when the user is firing
    bool IsFiring;
    Rigidbody rb;
    CarController cc;

    // True when player will eject junk again
    bool junkEjectCooled = true;

    //use this when changin Boost status in UI
    public bool BoostCooled { get; set; } = true;

    #endregion

    #region MonoBehaviour CallBacks

    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
    /// </summary>
    public void Awake()
	{
        
		if (this.lasers == null)
		{
			Debug.LogError("<Color=Red><b>Missing</b></Color> Lasers Reference.", this);
		}
		else
		{
			this.lasers.SetActive(false);
		}

		// #Important
		// used in GameManager.cs: we keep track of the localPlayer instance to prevent instanciation when levels are synchronized
		if (photonView.IsMine)
		{
			LocalPlayerInstance = gameObject;
            rb = gameObject.GetComponent<Rigidbody>();
            cc = gameObject.GetComponent<CarController>();
		}

		// #Critical
		// we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
		DontDestroyOnLoad(gameObject);
	}

	/// <summary>
	/// MonoBehaviour method called on GameObject by Unity during initialization phase.
	/// </summary>
	public void Start()
	{
		CameraWorkCar _cameraWork = gameObject.GetComponent<CameraWorkCar>();

		if (_cameraWork != null)
		{
			if (photonView.IsMine)
			{
				_cameraWork.OnStartFollowing();
			}
		}
		else
		{
			Debug.LogError("<Color=Red><b>Missing</b></Color> CameraWork Component on player Prefab.", this);
		}

		// Create the UI
		if (this.playerUiPrefab != null)
		{
			GameObject _uiGo = Instantiate(this.playerUiPrefab, GameObject.Find("Canvas").transform );
			if (_uiGo == null)
			{
				Debug.Log("NULL PLAYER UI!!!!");
			}
			else
			{
				Debug.Log("PLAYER UI: " + _uiGo.name + " vs " + this.playerUiPrefab.name);
				_uiGo.GetComponent<CarPlayerUI>().SetTarget(this);
			}
		}
		else
		{
			Debug.LogWarning("<Color=Red><b>Missing</b></Color> PlayerUiPrefab reference on player Prefab.", this);
		}


#if UNITY_5_4_OR_NEWER
		// Unity 5.4 has a new scene management. register a method to call CalledOnLevelWasLoaded.
		UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
#endif
	}


	public override void OnDisable()
	{
		// Always call the base to remove callbacks
		base.OnDisable();

#if UNITY_5_4_OR_NEWER
		UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
#endif
	}


	/// <summary>
	/// MonoBehaviour method called on GameObject by Unity on every frame.
	/// Process Inputs if local player.
	/// Show and hide the beams
	/// Watch for end of game, when local player health is 0.
	/// </summary>
	public void Update()
	{
		// we only process Inputs and check health if we are the local player
		if (photonView.IsMine)
		{
			this.ProcessInputs();

			if (this.Health <= 0f)
			{

				GameManagerMM.Instance.LeaveRoom();
			}
		}

		if (this.lasers != null && this.IsFiring != this.lasers.activeInHierarchy)
		{
			this.lasers.SetActive(this.IsFiring);
		}
	}

	/// <summary>
	/// MonoBehaviour method called when the Collider 'other' enters the trigger.
	/// Affect Health of the Player if the collider is a beam
	/// Note: when jumping and firing at the same, you'll find that the player's own beam intersects with itself
	/// One could move the collider further away to prevent this or check if the beam belongs to the player.
	/// </summary>
	public void OnTriggerEnter(Collider other)
	{
		if (!photonView.IsMine)
		{
			return;
		}

		var junk = other.GetComponentInParent<CarJunk>();
		if (junk != null && !junk.IsCollected)
		{
			junk.IsCollected = true;
			junk.Collect();
			this.Junk += 1;

            if(GameManagerMM.Instance.RequiredToDepot <= this.Junk)
            {
                this.IsLoaded = true;
            }
		}

		var depot = other.GetComponentInParent<CarJunkDepot>();
		if(depot != null)
		{
			if (GameManagerMM.Instance.RequiredToDepot <= this.Junk)
			{
				this.IsWinner = true;
			}
		}

		// We are only interested in Beamers
		// we should be using tags but for the sake of distribution, let's simply check by name.
		if (other.name.Contains("Laser"))
		{
            if (Junk > 0)
            {
                EjectJunk();
            }  
            this.Health -= 0.1f;
        }
	}

    void OnCollisionEnter(Collision other)
    {
        if(other.transform.tag == "Player")
        {
            if (Junk > 0)
            {
                EjectJunk();
            }
        }
    }

    void EjectJunk()
    {
        if (junkEjectCooled)
        {
            int amount = Mathf.Min(Junk, AMOUNT_OF_JUNK_TO_EJECT);
            junkEjectCooled = false;

            for (int i = 0; i < amount; i++)
            {
                this.Junk--;
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.InstantiateSceneObject("Junk", new Vector3(transform.position.x, transform.position.y + JUNK_SPAWN_EJECT_HEIGHT_OFFSET, transform.position.z), Quaternion.identity, 0);
                }
                else
                {
                    photonView.RPC("SpawnJunk", RpcTarget.MasterClient);
                }
            }
            
            Invoke("CoolJunk", JUNK_EJECT_COOLDOWN);
        }
    }

    [PunRPC]
    public void SpawnJunk()
    {
        PhotonNetwork.InstantiateSceneObject("Junk", new Vector3(transform.position.x, transform.position.y + JUNK_SPAWN_EJECT_HEIGHT_OFFSET, transform.position.z), Quaternion.identity, 0);
    }

    void CoolJunk()
    {
        junkEjectCooled = true;
    }

	/// <summary>
	/// MonoBehaviour method called once per frame for every Collider 'other' that is touching the trigger.
	/// We're going to affect health while the beams are interesting the player
	/// </summary>
	/// <param name="other">Other.</param>
	public void OnTriggerStay(Collider other)
	{
		// we dont' do anything if we are not the local player.
		if (!photonView.IsMine)
		{
			return;
		}

		// We are only interested in Beamers
		// we should be using tags but for the sake of distribution, let's simply check by name.
		if (!other.name.Contains("Laser"))
		{
			return;
		}

		// we slowly affect health when beam is constantly hitting us, so player has to move to prevent death.
		this.Health -= 0.1f * Time.deltaTime;
	}


#if !UNITY_5_4_OR_NEWER
        /// <summary>See CalledOnLevelWasLoaded. Outdated in Unity 5.4.</summary>
        void OnLevelWasLoaded(int level)
        {
            this.CalledOnLevelWasLoaded(level);
        }
#endif

	/// <summary>
	/// MonoBehaviour method called after a new level of index 'level' was loaded.
	/// We recreate the Player UI because it was destroy when we switched level.
	/// Also reposition the player if outside the current arena.
	/// </summary>
	/// <param name="level">Level index loaded</param>
	void CalledOnLevelWasLoaded(int level)
	{
		// check if we are outside the Arena and if it's the case, spawn around the center of the arena in a safe zone
		if (!Physics.Raycast(transform.position, -Vector3.up, 5f))
		{
			transform.position = new Vector3(0f, 5f, 0f);
		}

		GameObject _uiGo = Instantiate(this.playerUiPrefab);
		_uiGo.GetComponent<CarPlayerUI>().SetTarget(this);
	}

	#endregion

	#region Private Methods


#if UNITY_5_4_OR_NEWER
	void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode loadingMode)
	{
		this.CalledOnLevelWasLoaded(scene.buildIndex);
	}
#endif

	/// <summary>
	/// Processes the inputs. This MUST ONLY BE USED when the player has authority over this Networked GameObject (photonView.isMine == true)
	/// </summary>
	void ProcessInputs()
	{
		if (Input.GetButtonDown("Fire1"))
		{
			// we don't want to fire when we interact with UI buttons for example. IsPointerOverGameObject really means IsPointerOver*UI*GameObject
			// notice we don't use on on GetbuttonUp() few lines down, because one can mouse down, move over a UI element and release, which would lead to not lower the isFiring Flag.
			if (EventSystem.current.IsPointerOverGameObject())
			{
				//return;
			}

			if (!this.IsFiring)
			{
				this.IsFiring = true;
			}
		}

        if(Input.GetKeyDown("space"))
        {
            Debug.Log("BOOST");
            if (BoostCooled)
            {
                BoostCooled = false;
                rb.AddForce(transform.forward * boostPower, ForceMode.Impulse);
                Invoke("BoostCooler", boostCoolDownTime);
            }
        }

		if (Input.GetButtonUp("Fire1"))
		{
			if (this.IsFiring)
			{
				this.IsFiring = false;
			}
		}
	}

    void BoostCooler()
    {
        BoostCooled = true;
    }

	#endregion

	#region IPunObservable implementation

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			// We own this player: send the others our data
			stream.SendNext(this.IsFiring);
			stream.SendNext(this.Health);
			stream.SendNext(this.Junk);
			stream.SendNext(this.IsWinner);
            stream.SendNext(this.IsLoaded);
        }
		else
		{
			// Network player, receive data
			this.IsFiring = (bool)stream.ReceiveNext();
			this.Health = (float)stream.ReceiveNext();
			this.Junk = (int)stream.ReceiveNext();
			this.IsWinner = (bool)stream.ReceiveNext();
            this.IsLoaded = (bool)stream.ReceiveNext();
        }
	}


	#endregion
}
