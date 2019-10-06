using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardEntryUi : MonoBehaviour
{
	[SerializeField] Text nameText;
	[SerializeField] Text scoreText;


	public void UpdateWithCar(PlayerManagerCarPhoton car)
	{
		if(car == null)
		{
			this.gameObject.SetActive(false);
		}
		else
		{
			this.gameObject.SetActive(true);
			this.nameText.text = car.photonView.Owner.NickName;
			this.scoreText.text = car.TotalJunkStored.ToString();
		}
	}
}
