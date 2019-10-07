using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class CarJunk : MonoBehaviour
{
    private static float JUNK_EJECTION_POWER = 10;
    
    public GameObject model;
	public float scaleSpeed = 1.0f;
	public float scaleAmount;
	public bool IsCollected { get; set; }

    private Rigidbody rb;
    private BoxCollider bc;

    private void Awake()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        bc = gameObject.GetComponent<BoxCollider>();
        EjectJunk();
    }

    void OnEnable()
    {
        IsCollected = false;
        StartCoroutine("Pulsate");
    }

    private void OnDisable()
    {
        StopCoroutine("Pulsate");
        model.transform.localScale = Vector3.one;
        rb.isKinematic = false;
        bc.isTrigger = false;
    }

    IEnumerator Pulsate()
    {
        //when junk first spawns it shoots into the air an roles downhill
        yield return new WaitForSeconds(.5f);
        while(rb.velocity.magnitude >= Mathf.Epsilon)
        {
            yield return null;
        }

        //once junk is still, turn it kinematic and turn collider off (into trigger) and hover/pulse
        bc.isTrigger = true;
        rb.isKinematic = true;
        Vector3 tp = transform.position;
        float yPlusOffset = transform.position.y + 2;
        transform.position = new Vector3(transform.position.x, yPlusOffset, transform.position.z);

        while(true)
        {
            model.transform.localScale = Vector3.one * (1f + (scaleAmount * Mathf.Abs(Mathf.Sin(Time.time * scaleSpeed))));
            yield return null;
        }
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

    public void EjectJunk()
    {
        var x = Random.Range(2, 3);
        var z = Random.Range(2, 3);
        Vector3 dir = new Vector3(x, 1, z).normalized;
        this.rb.AddForce(dir * JUNK_EJECTION_POWER, ForceMode.Impulse);
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
