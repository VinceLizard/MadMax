using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CarGameUi : MonoBehaviour
{
	[SerializeField] GameObject leaderboardAnchor;
	[SerializeField] LeaderboardEntryUi leaderboardPrefab;
	[SerializeField] int NumberOfEntries = 5;

	List<LeaderboardEntryUi> leaderboardEntries = new List<LeaderboardEntryUi>();
	List<PlayerManagerCarPhoton> cars = new List<PlayerManagerCarPhoton>();

	private void Awake()
	{
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

			cars.Sort((a, b) => a.TotalJunkStored.CompareTo(b.TotalJunkStored));

			for (int i = 0; i < leaderboardEntries.Count; i++)
			{
				leaderboardEntries[i].UpdateWithCar(cars.Count > i ? cars[i] : null);
			}
			yield return new WaitForSeconds(2.0f);
		}
    }
}
