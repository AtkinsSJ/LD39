
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct Consequence
{
	public string field;
	public int minChange, maxChange;
}

[System.Serializable]
public struct EventChoice
{
	public string description;
	public Consequence[] consequences;
}

[System.Serializable]
public class Event
{
	public string character;
	public string description;
	public EventChoice[] choices;

	public int daysWaited;
}

[System.Serializable]
public struct EventArray
{
	public Event[] events;
}

public struct Status
{
	public int day;
	public int money;
	public int love;
	public int respect;

	public Status(int initialMoney)
	{
		day = 0;
		love = 0;
		respect = 0;
		money = initialMoney;
	}

	public override string ToString()
	{
		return string.Format("Status {{ Day: {3}, Money: {0}, Love: {1}, Respect: {2} }}", money, love, respect, day);
	}
}

public class Controller : MonoBehaviour
{
	public int initialMoney = 100;
	private Status status;

	public List<Event> allEvents = new List<Event>();

	public List<Event> unseenEvents = new List<Event>();
	public List<Event> seenEvents = new List<Event>();
	public List<Event> courtEvents = new List<Event>(); // events currently available as options

	public Event currentEvent;

	// Configuration
	public int eventsToShowPerDay = 5;
	public int daysPeopleWillWait = 2;
	public List<Consequence> consequencesForIgnoredPetition = new List<Consequence>();

	// Status UI
	public Text dayText;
	public Text moneyText;
	public Slider loveSlider;
	public Slider respectSlider;

	// Court UI
	public GameObject courtPanel;
	public Text courtDescriptionText;
	public PetitionButton petitionButtonPrefab;
	public GameObject petitionButtonsGroup;

	// Event UI
	public GameObject eventPanel;
	public Text characterText;
	public Text eventText;
	public ChoiceButtonScript choiceButtonPrefab;
	public GameObject choiceButtonsGroup;

	// Game Over UI
	public GameObject gameOverPanel;
	public Text gameOverText;
	string gameOverTextTemplate;

	void ShowPanel(GameObject panelToShow)
	{
		courtPanel.SetActive(false);
		eventPanel.SetActive(false);
		gameOverPanel.SetActive(false);

		if (panelToShow) panelToShow.SetActive(true);
	}

	// Use this for initialization
	void Start()
	{
		var eventGuids = AssetDatabase.FindAssets("t:TextAsset", new string[] { "Assets/events" });
		foreach (var guid in eventGuids)
		{
			TextAsset eventAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guid));
			var loadedEvents = JsonUtility.FromJson<EventArray>(eventAsset.text);
			allEvents.AddRange(loadedEvents.events);
		}

		gameOverTextTemplate = gameOverText.text;

		BeginGame();
	}

	public void BeginGame()
	{
		status = new Status(initialMoney);

		ShowPanel(null);

		unseenEvents.Clear();
		seenEvents.Clear();
		courtEvents.Clear();

		unseenEvents.AddRange(allEvents);

		StartDay();
		UpdateUI();
	}

	public void StartDay()
	{
		status.day++;
		dayText.text = string.Format("Day {0}", status.day);

		List<Event> toDelete = new List<Event>(courtEvents.Count);

		for (int i = 0; i < courtEvents.Count; i++)
		{
			courtEvents[i].daysWaited++;
			if (courtEvents[i].daysWaited > daysPeopleWillWait)
			{
				toDelete.Add(courtEvents[i]);
			}
		}

		foreach (var e in toDelete)
		{
			courtEvents.Remove(e);
			seenEvents.Add(e);

			// apply ignored-petition consequences
			foreach (var c in consequencesForIgnoredPetition)
			{
				SufferTheConsequence(c);
			}
		}

		// ensure there are X events in the court today
		while (courtEvents.Count < eventsToShowPerDay)
		{
			if (unseenEvents.Count > 0)
			{
				//move random item to court
				Event e = unseenEvents[Random.Range(0, unseenEvents.Count)];
				e.daysWaited = 0;
				unseenEvents.Remove(e);
				courtEvents.Add(e);
			}
			else if (seenEvents.Count > 0)
			{
				// move seen events back to unseen
				unseenEvents.AddRange(seenEvents);
				seenEvents.Clear();
			}
			else
			{
				// We're out of events! We have to stop looking.
				break;
			}
		}

		ShowCourt();
		UpdateUI();
	}

	public void ShowCourt()
	{
		// 

		// Now, init the court display
		courtDescriptionText.text = string.Format("There are {0} people waiting in your court.", courtEvents.Count);

		foreach (Transform oldButton in petitionButtonsGroup.transform)
		{
			GameObject.Destroy(oldButton.gameObject);
		}

		foreach (var e in courtEvents)
		{
			var button = Instantiate(petitionButtonPrefab, petitionButtonsGroup.transform) as PetitionButton;
			button.Init(this, e);
		}

		ShowPanel(courtPanel);
	}

	public void OnEventSelected(Event e)
	{
		ShowEvent(e);
	}

	public void ShowEvent(Event e)
	{
		currentEvent = e;
		characterText.text = currentEvent.character;
		eventText.text = currentEvent.description;

		foreach (Transform oldButton in choiceButtonsGroup.transform)
		{
			GameObject.Destroy(oldButton.gameObject);
		}

		for (int choiceIndex = 0; choiceIndex < currentEvent.choices.Length; choiceIndex++)
		{
			var choice = currentEvent.choices[choiceIndex];
			var button = Instantiate(choiceButtonPrefab, choiceButtonsGroup.transform) as ChoiceButtonScript;
			button.Init(this, choice, choiceIndex);
		}

		ShowPanel(eventPanel);
	}

	public void HideEvent()
	{
		ShowCourt();
	}

	int Adjust(int startValue, Consequence consequence)
	{
		int change = Random.Range(consequence.minChange, consequence.maxChange);
		int result = startValue + change;

		/*
		Debug.Log(string.Format("Random range between {0} and {1} produced {2}. Original value {3}, now {4}",
			consequence.minChange, consequence.maxChange, change, startValue, result));
		*/	
		return result;
	}

	public void OnChoiceSelected(int choiceIndex)
	{
		Debug.Log("Selected choice " + choiceIndex + "!");

		courtEvents.Remove(currentEvent);

		var choice = currentEvent.choices[choiceIndex];

		foreach (var consequence in choice.consequences)
		{
			SufferTheConsequence(consequence);
		}

		seenEvents.Add(currentEvent);
		ShowCourt();

		UpdateUI();
	}

	void SufferTheConsequence(Consequence consequence)
	{
		switch (consequence.field)
		{
			case "money":
			case "gold":
				status.money = Adjust(status.money, consequence);
				break;

			case "love":
				status.love = Adjust(status.love, consequence);
				break;

			case "respect":
				status.respect = Adjust(status.respect, consequence);
				break;

			default:
				Debug.LogError("Unrecognised consequence field: " + consequence.field);
				break;
		}
	}

	void CheckIfWeLost()
	{
		bool weLost = false;

		if (status.love <= -100) weLost = true;
		if (status.respect <= -100) weLost = true;
		if (status.money <= 0) weLost = true;

		if (weLost)
		{
			gameOverText.text = string.Format(gameOverTextTemplate, status.day);
			ShowPanel(gameOverPanel);
		}
	}

	public void OnRestartClicked()
	{
		BeginGame();
	}

	void UpdateUI()
	{
		loveSlider.value = status.love;
		respectSlider.value = status.respect;
		moneyText.text = string.Format("Gold: {0}", status.money);

		CheckIfWeLost();
	}
}
