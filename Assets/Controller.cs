
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct Consequence
{
	public string field;
	public float minChange, maxChange;
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
	public float money;
	public float love;
	public float respect;

	Status(float initialMoney = 100)
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

	public Status status = new Status();

	public List<Event> allEvents = new List<Event>();

	public List<Event> unseenEvents = new List<Event>();
	public List<Event> seenEvents = new List<Event>();
	public List<Event> courtEvents = new List<Event>(); // events currently available as options

	public Event currentEvent;

	// Configuration
	public int eventsToShowPerDay = 5;
	public int daysPeopleWillWait = 2;

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

		BeginGame();
	}

	public void BeginGame()
	{
		courtPanel.SetActive(false);
		eventPanel.SetActive(false);

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

		courtPanel.SetActive(true);
		eventPanel.SetActive(false);
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

		courtPanel.SetActive(false);
		eventPanel.SetActive(true);
	}

	public void HideEvent()
	{
		ShowCourt();
	}

	float Adjust(float startValue, Consequence consequence)
	{
		float result = startValue;

		float adjustment = Random.Range(consequence.minChange, consequence.maxChange);

		result += adjustment;

		return result;
	}

	public void OnChoiceSelected(int choiceIndex)
	{
		Debug.Log("Selected choice " + choiceIndex + "!");

		courtEvents.Remove(currentEvent);

		var choice = currentEvent.choices[choiceIndex];

		foreach (var consequence in choice.consequences)
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

		seenEvents.Add(currentEvent);

		UpdateUI();

		// TODO: check if we lost!

		ShowCourt();
	}

	void UpdateUI()
	{
		loveSlider.value = status.love;
		respectSlider.value = status.respect;
		moneyText.text = string.Format("Gold: {0}", status.money);
	}
}
