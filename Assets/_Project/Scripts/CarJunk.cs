using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class CarJunk : MonoBehaviour
{
	public GameObject model;
	public float scaleSpeed = 1.0f;
	public float scaleAmount;
	public bool IsCollected { get; set; }


	private void Update()
	{
		model.transform.localScale = Vector3.one * (1f + (scaleAmount * Mathf.Abs(Mathf.Sin(Time.time * scaleSpeed))));
	}

	void OnEnable()
	{
		IsCollected = false;
	}

	public void Collect()
	{
		var pv = this.GetComponent<PhotonView>();
		if (pv.IsMine)
		{
			PhotonNetwork.Destroy(this.gameObject);
		}
		else
		{
			pv.RPC("CollectRpc", RpcTarget.MasterClient); 
		}
	}

	[PunRPC]
	public void CollectRpc()
	{
		PhotonNetwork.Destroy(this.gameObject);
	}

	void OnTriggerEnter(Collider collider)
	{
		/*var pv = this.GetComponent<PhotonView>();
		if(pv.IsMine)
		{
			var carPhoton = collider.gameObject.GetComponentInParent<PlayerManagerCarPhoton>();
			if (carPhoton)
			{
				Debug.Log("Car IsMine: " + carPhoton.GetComponent<PhotonView>().IsMine.ToString());
				carPhoton.Junk += 1;
				PhotonNetwork.Destroy(this.gameObject);
			}
		}*/

	}
}
