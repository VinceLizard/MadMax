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
    [SerializeField] Text endGameText;
    [SerializeField] Text instructionText;
    
    GameObject thisIsMe;

    List<LeaderboardEntryUi> leaderboardEntries = new List<LeaderboardEntryUi>();
	List<PlayerManagerCarPhoton> cars = new List<PlayerManagerCarPhoton>();

	private void Awake()
	{
		winnerText.gameObject.SetActive(false);
        endGameText.gameObject.SetActive(false);
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
        Debug.Log("Setting instruction text");
		instructionText.text = string.Format("Jonny Junk needs {0} Junks. Once you got it, bring it to the center beacon.", GameManagerMM.Instance.RequiredToDepot);
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

            var loaded = cars.Find((c) => c.IsLoaded);
            if (loaded) 
            {
                GameManagerMM.Instance.EndGame = true;
                instructionText.gameObject.SetActive(false);
                endGameText.gameObject.SetActive(true);
                endGameText.text = loaded.photonView.Owner.NickName + " has a lot of junk! Hurry up!";
            } else {
                GameManagerMM.Instance.EndGame = false;
                endGameText.gameObject.SetActive(false);
            }

            var winner = cars.Find((c) => c.IsWinner);
			if (winner)
			{
				leaderboardAnchor.SetActive(false);
                endGameText.gameObject.SetActive(false);
                winnerText.gameObject.SetActive(true);
				winnerText.text = winner.photonView.Owner.NickName + " dumped their junk!\nEveryone else is a disappointment!";
                thisIsMe = GameManagerMM.Instance.ThisIsMe;
                if (winner.gameObject == thisIsMe) {
                    GameManagerMM.Instance.StartTheWin();
                } else {
                    GameManagerMM.Instance.StarTheLose();
                }
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
