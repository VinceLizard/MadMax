using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class CarGameUi : MonoBehaviour
{
	[SerializeField] GameObject leaderboardAnchor;
	[SerializeField] LeaderboardEntryUi leaderboardPrefab;
	[SerializeField] int NumberOfEntries = 5;
	[SerializeField] Text winnerText;
	[SerializeField] Text instructionText;

	List<LeaderboardEntryUi> leaderboardEntries = new List<LeaderboardEntryUi>();
	List<PlayerManagerCarPhoton> cars = new List<PlayerManagerCarPhoton>();

	private void Awake()
	{
		winnerText.gameObject.SetActive(false);
		leaderboardEntries.Clear();
		for (int i = 0; i < NumberOfEntries; i++)
		{
			leaderboardEntries.Add(GameObject.Instantiate<LeaderboardEntryUi>(leaderboardPrefab, leaderboardAnchor.transform));
			leaderboardEntries[i].UpdateWithCar(null);
		}
	}

	private void Start()
	{
		StartCoroutine(UpdateLeaderboard());
		instructionText.text = string.Format("The Monolith Requests {0} Blocks!", GameManagerMM.Instance.RequiredToDepot);
	}

	IEnumerator UpdateLeaderboard()
    {
        while(true)
		{
			cars.Clear();
			var gos = GameObject.FindGameObjectsWithTag("Player");
			foreach(var go in gos)
			{
				var car = go.GetComponent<PlayerManagerCarPhoton>();
				if(car != null)
					cars.Add(car);
			}

			var winner = cars.Find((c) => c.IsWinner);
			if (winner)
			{
				leaderboardAnchor.SetActive(false);
				winnerText.gameObject.SetActive(true);
				winnerText.text = winner.photonView.Owner.NickName + " Has Served the Monolith!\nEveryone else is a disappointment!";
				break;
			}
			else
			{
				leaderboardAnchor.SetActive(true);
				for (int i = 0; i < leaderboardEntries.Count; i++)
				{
					leaderboardEntries[i].UpdateWithCar(cars.Count > i ? cars[i] : null);
				}
			}
			yield return new WaitForSeconds(2.0f);
		}
    }
}
