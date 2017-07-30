﻿
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
	public string name;
	public string title;

	public bool gameOver;
	public int day;
	public int actionsLeftToday;
	public int dailyTax;
	public int money;
	public int love;
	public int respect;

	public Status(string myName, string myTitle, int initialMoney)
	{
		title = myTitle;
		name = myName;
		gameOver = false;
		day = 0;
		love = 0;
		respect = 0;
		actionsLeftToday = 0;
		dailyTax = 15;
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
	public int actionsPerDay = 3;
	public int daysPeopleWillWait = 2;
	public int minDailyTax = 0, maxDailyTax = 50;
	public int loveLostForMaxTax = 30;
	public List<Consequence> consequencesForIgnoredPetition = new List<Consequence>();

	// Main Menu etc UI
	public GameObject mainMenuUI;
	public GameObject gameUI;

	// Game Setup UI
	public GameObject gameSetupPanel;
	public ToggleGroup titleToggleGroup;
	public InputField nameInput;

	// Status UI
	public GameObject leftStuff, rightStuff;
	public Text dayText;
	string dayTextTemplate;
	public Text actionsText;
	string actionsTextTemplate;
	public Text moneyText;
	string moneyTextTemplate;
	public Text taxText;
	string taxTextTemplate;
	public Slider loveSlider;
	public Slider respectSlider;

	// Taxes UI
	public GameObject taxesPanel;
	public Slider taxSlider;
	public Text newTaxText;
	string newTaxTextTemplate;

	// Court UI
	public GameObject courtPanel;
	public Text courtTitle;
	string courtTitleTemplate;
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
	public Text gameOverTitle;
	string gameOverTitleTemplate;
	public Text gameOverText;
	string gameOverTextTemplate;

	void ShowPanel(GameObject panelToShow)
	{
		gameSetupPanel.SetActive(false);
		courtPanel.SetActive(false);
		eventPanel.SetActive(false);
		taxesPanel.SetActive(false);
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
		gameOverTitleTemplate = gameOverTitle.text;
		dayTextTemplate = dayText.text;
		actionsTextTemplate = actionsText.text;
		moneyTextTemplate = moneyText.text;
		taxTextTemplate = taxText.text;
		newTaxTextTemplate = newTaxText.text;
		courtTitleTemplate = courtTitle.text;

		taxSlider.onValueChanged.AddListener(OnTaxSliderChanged);

		mainMenuUI.SetActive(true);
		gameUI.SetActive(false);
	}

	void OnTaxSliderChanged(float newValue)
	{
		newTaxText.text = string.Format(newTaxTextTemplate, (int)newValue);
	}

	public void OnPlayClicked()
	{
		ShowPanel(null);

		mainMenuUI.SetActive(false);
		gameUI.SetActive(true);

		leftStuff.SetActive(false);
		rightStuff.SetActive(false);

		ShowPanel(gameSetupPanel);
	}

	public void OnStartGameClicked()
	{
		string title = "King";
		string name = "Royalpants";

		if (nameInput.text.Length > 0)
		{
			status.name = nameInput.text;
		}
		foreach (var t in titleToggleGroup.ActiveToggles())
		{
			title = t.name;
		}

		status = new Status(nameInput.text, title, initialMoney);

		courtTitle.text = string.Format(courtTitleTemplate, status.title, status.name);

		BeginGame();
	}

	public void BeginGame()
	{
		ShowPanel(null);

		unseenEvents.Clear();
		seenEvents.Clear();
		courtEvents.Clear();

		unseenEvents.AddRange(allEvents);

		StartDay();
		UpdateUI();

		leftStuff.SetActive(true);
		rightStuff.SetActive(true);

		mainMenuUI.SetActive(false);
		gameUI.SetActive(true);
	}

	public void StartDay()
	{
		if (status.day != 0) CollectTaxes();

		status.day++;
		status.actionsLeftToday = actionsPerDay;
		dayText.text = string.Format(dayTextTemplate, status.day);

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

	void CollectTaxes()
	{
		status.money += status.dailyTax;

		float taxRange = maxDailyTax - minDailyTax;
		float taxPercent = ((float)(status.dailyTax - minDailyTax)) / taxRange;
		float lostLove = taxPercent * (float)loveLostForMaxTax;

		status.love -= (int)lostLove;
	}

	public void OnNextDayClicked()
	{
		if (status.gameOver) return;
		StartDay();
	}

	public void OnSetTaxesClicked()
	{
		if (status.gameOver) return;
		taxSlider.minValue = minDailyTax;
		taxSlider.maxValue = maxDailyTax;
		taxSlider.value = status.dailyTax;
		ShowPanel(taxesPanel);
	}

	public void OnSaveTaxesClicked()
	{
		status.dailyTax = (int)taxSlider.value;
		UpdateUI();
		ShowCourt();
	}

	public void ShowCourt()
	{
		// Now, init the court display

		foreach (Transform oldButton in petitionButtonsGroup.transform)
		{
			GameObject.Destroy(oldButton.gameObject);
		}

		if (status.actionsLeftToday > 0)
		{
			courtDescriptionText.text = string.Format("There are {0} people waiting in your court.", courtEvents.Count);

			foreach (var e in courtEvents)
			{
				var button = Instantiate(petitionButtonPrefab, petitionButtonsGroup.transform) as PetitionButton;
				button.Init(this, e);
			}
		}
		else
		{
			courtDescriptionText.text = string.Format("There are {0} people waiting in your court, but you are out of actions for today!", courtEvents.Count);
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
		courtEvents.Remove(currentEvent);

		var choice = currentEvent.choices[choiceIndex];

		foreach (var consequence in choice.consequences)
		{
			SufferTheConsequence(consequence);
		}

		seenEvents.Add(currentEvent);
		status.actionsLeftToday--;

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

		string lossReason = "";

		if (status.love <= -100)
		{
			weLost = true;
			lossReason = "Your people despise you so much that they overthrew you and had you executed. People were queuing up for hours to dance on your grave.";
		}
		if (status.respect <= -100)
		{
			weLost = true;
			lossReason = "Your rulership was so corrupt that your people revolted. Your head is currently displayed on a pole outside your former palace.";
		}
		if (status.money <= 0)
		{
			weLost = true;
			lossReason = "When they discovered that your treasury is empty, all of your subjects left you.";
		}

		if (weLost)
		{
			status.gameOver = true;
			gameOverTitle.text = string.Format(gameOverTitleTemplate, status.title);
			gameOverText.text = string.Format(gameOverTextTemplate, status.day, lossReason);
			ShowPanel(gameOverPanel);
		}
	}

	public void OnRestartClicked()
	{
		mainMenuUI.SetActive(true);
		gameUI.SetActive(false);
	}

	void UpdateUI()
	{
		loveSlider.value = status.love;
		respectSlider.value = status.respect;
		taxText.text = string.Format(taxTextTemplate, status.dailyTax);
		moneyText.text = string.Format(moneyTextTemplate, status.money);
		actionsText.text = string.Format(actionsTextTemplate, status.actionsLeftToday);

		CheckIfWeLost();
	}
}
