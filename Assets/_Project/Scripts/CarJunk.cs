using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class CarJunk : MonoBehaviour
{
	void OnTriggerEnter(Collider collider)
	{
		var pv = this.GetComponent<PhotonView>();
		if(pv.IsMine)
		{
			PhotonNetwork.Destroy(this.gameObject);
		}

	}
}
