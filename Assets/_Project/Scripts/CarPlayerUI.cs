﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayerUI.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Networking Demos
// </copyright>
// <summary>
//  Used in PUN Basics Tutorial to deal with the networked player instance UI display tha follows a given player to show its health and name
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;


#pragma warning disable 649

/// <summary>
/// Player UI. Constraint the UI to follow a PlayerManager GameObject in the world,
/// Affect a slider and text to display Player's name and health
/// </summary>
public class CarPlayerUI : MonoBehaviour
{
	#region Private Fields

	[Tooltip("Pixel offset from the player target")]
	[SerializeField]
	private Vector3 screenOffset = new Vector3(0f, 30f, 0f);

	[Tooltip("UI Text to display Player's Name")]
	[SerializeField]
	private Text playerNameText;

	/*[Tooltip("UI Slider to display Player's Health")]
	[SerializeField]
	private Slider playerHealthSlider;*/

	[SerializeField]
	private Text junkText;

	PlayerManagerCarPhoton target;
	float characterControllerHeight;

	Transform targetTransform;

	Renderer targetRenderer;

	CanvasGroup _canvasGroup;

	Vector3 targetPosition;

	int prevJunk = -666;
	#endregion

	#region MonoBehaviour Messages

	/// <summary>
	/// MonoBehaviour method called on GameObject by Unity during early initialization phase
	/// </summary>
	void Awake()
	{

		_canvasGroup = this.GetComponent<CanvasGroup>();

		this.transform.SetParent(GameObject.Find("Canvas").GetComponent<Transform>(), false);
	}

	/// <summary>
	/// MonoBehaviour method called on GameObject by Unity on every frame.
	/// update the health slider to reflect the Player's health
	/// </summary>
	void Update()
	{
		// Destroy itself if the target is null, It's a fail safe when Photon is destroying Instances of a Player over the network
		if (target == null)
		{
			Destroy(this.gameObject);
			return;
		}

        /*
		// Reflect the Player Health
		if (playerHealthSlider != null)
		{
			playerHealthSlider.value = target.Health;
		}*/

		if(junkText != null)
		{
			if(prevJunk != target.Junk)
			{
				prevJunk = target.Junk;
				junkText.text = prevJunk.ToString();
			}
		}
	}

	/// <summary>
	/// MonoBehaviour method called after all Update functions have been called. This is useful to order script execution.
	/// In our case since we are following a moving GameObject, we need to proceed after the player was moved during a particular frame.
	/// </summary>
	void LateUpdate()
	{

		// Do not show the UI if we are not visible to the camera, thus avoid potential bugs with seeing the UI, but not the player itself.
		if (targetRenderer != null)
		{
			this._canvasGroup.alpha = targetRenderer.isVisible ? 1f : 0f;
		}

		// #Critical
		// Follow the Target GameObject on screen.
		if (targetTransform != null)
		{
			targetPosition = targetTransform.position;
			targetPosition.y += characterControllerHeight;

			this.transform.position = Camera.main.WorldToScreenPoint(targetPosition) + screenOffset;
		}

	}




	#endregion

	#region Public Methods

	/// <summary>
	/// Assigns a Player Target to Follow and represent.
	/// </summary>
	/// <param name="target">Target.</param>
	public void SetTarget(PlayerManagerCarPhoton _target)
	{

		if (_target == null)
		{
			Debug.LogError("<Color=Red><b>Missing</b></Color> PlayMakerManager target for PlayerUI.SetTarget.", this);
			return;
		}

		// Cache references for efficiency because we are going to reuse them.
		this.target = _target;
		targetTransform = this.target.GetComponent<Transform>();
		targetRenderer = this.target.CarUiRenderer;


		BoxCollider boxCollider = this.target.GetComponent<BoxCollider>();

		// Get data from the Player that won't change during the lifetime of this Component
		if (boxCollider != null)
		{
			characterControllerHeight = boxCollider.center.y + (boxCollider.size.y * 2);
		}

		if (playerNameText != null)
		{
			playerNameText.text = this.target.photonView.Owner.NickName;
		}
	}

	#endregion

}
