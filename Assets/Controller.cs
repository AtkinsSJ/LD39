
using System.Collections.Generic;
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

[System.Serializable]
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
	public Status status;

	public List<Event> allEvents = new List<Event>();

	public List<Event> unseenEvents = new List<Event>();
	public List<Event> seenEvents = new List<Event>();
	public List<Event> courtEvents = new List<Event>(); // events currently available as options
	public List<Event> deadEvents = new List<Event>(); // events where you chose to kill them. you monster.

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
	public Button setTaxButton;

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
	public Text gameLostTitle;
	string gameLostTitleTemplate;
	public Text gameLostText;
	string gameLostTextTemplate;
	public Text gameWonTitle;
	public Text gameWonText;
	string gameWonTextTemplate;

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

		Object[] eventAssets = Resources.LoadAll("events", typeof(TextAsset));
		foreach (var obj in eventAssets)
		{
			TextAsset eventAsset = obj as TextAsset;
			var loadedEvents = JsonUtility.FromJson<EventArray>(eventAsset.text);
			allEvents.AddRange(loadedEvents.events);
		}

		// Below is the old code we were using to load events, but it only works inside the editor!
		//var eventGuids = AssetDatabase.FindAssets("t:TextAsset", new string[] { "Assets/events" });
		//foreach (var guid in eventGuids)
		//{
		//	TextAsset eventAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guid));
		//	var loadedEvents = JsonUtility.FromJson<EventArray>(eventAsset.text);
		//	allEvents.AddRange(loadedEvents.events);
		//}

		gameWonTextTemplate = gameWonText.text;
		gameLostTextTemplate = gameLostText.text;
		gameLostTitleTemplate = gameLostTitle.text;
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
			name = nameInput.text;
		}
		foreach (var t in titleToggleGroup.ActiveToggles())
		{
			title = t.name;
		}

		status = new Status(name, title, initialMoney);

		courtTitle.text = string.Format(courtTitleTemplate, status.title, status.name);

		BeginGame();
	}

	public void BeginGame()
	{
		ShowPanel(null);

		unseenEvents.Clear();
		seenEvents.Clear();
		courtEvents.Clear();
		deadEvents.Clear();

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

		if (courtEvents.Count == 0)
		{
			// We still have no events even after lookig EVERWHERE!
			// This means everyone must be dead. Oops.
			ShowGameOverScreen(false, "You killed every single person in the Kingdom. There's nobody left to grow food or defend you. You die sad and alone.");

			return;
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
		if (status.actionsLeftToday == 0) return;

		taxSlider.minValue = minDailyTax;
		taxSlider.maxValue = maxDailyTax;
		taxSlider.value = status.dailyTax;
		ShowPanel(taxesPanel);
	}

	public void OnSaveTaxesClicked()
	{
		int newTax = (int)taxSlider.value;
		if (newTax != status.dailyTax)
		{
			status.actionsLeftToday--;
			status.dailyTax = newTax;
		}

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
			if (courtEvents.Count == 1)
			{
				courtDescriptionText.text = string.Format("There is 1 person waiting in your court.");
			}
			else
			{
				courtDescriptionText.text = string.Format("There are {0} people waiting in your court.", courtEvents.Count);
			}

			foreach (var e in courtEvents)
			{
				var button = Instantiate(petitionButtonPrefab, petitionButtonsGroup.transform) as PetitionButton;
				button.Init(this, e);
			}
		}
		else
		{
			switch (courtEvents.Count)
			{
				case 0:
					courtDescriptionText.text = string.Format("You are out of actions for today!");
					break;
				case 1:
					courtDescriptionText.text = string.Format("There is 1 person waiting in your court, but you are out of actions for today!");
					break;
				default:
					courtDescriptionText.text = string.Format("There are {0} people waiting in your court, but you are out of actions for today!", courtEvents.Count);
					break;
			}
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

		foreach (ChoiceButtonScript oldButton in choiceButtonsGroup.GetComponentsInChildren<ChoiceButtonScript>())
		{
			GameObject.Destroy(oldButton.gameObject);
		}

		for (int choiceIndex = 0; choiceIndex < currentEvent.choices.Length; choiceIndex++)
		{
			var choice = currentEvent.choices[choiceIndex];
			var button = Instantiate(choiceButtonPrefab, choiceButtonsGroup.transform) as ChoiceButtonScript;
			button.Init(this, choice, choiceIndex, CanAfford(choice));
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

		if (choice.description.StartsWith("Kill "))
		{
			deadEvents.Add(currentEvent);
		}
		else
		{
			seenEvents.Add(currentEvent);
		}

		status.actionsLeftToday--;

		ShowCourt();

		UpdateUI();
	}

	bool CanAfford(EventChoice choice)
	{
		bool result = true;

		foreach (var consequence in choice.consequences)
		{
			if ((consequence.field == "money") || (consequence.field == "gold"))
			{
				if (consequence.minChange != consequence.maxChange)
				{
					Debug.LogError(string.Format("ERROR: We don't support randomised money consequences! {0} vs {1}", consequence.minChange, consequence.maxChange));
				}
				else
				{
					if ((status.money + consequence.minChange) < 0)
					{
						result = false;
					}
				}
			}
		}

		return result;
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

	void CheckIfGameOver()
	{
		bool gameOver = false;
		bool weLost = false;

		string explanation = "";

		if (status.love <= -100)
		{
			weLost = true;
			gameOver = true;
			explanation = "Your people despise you so much that they overthrew you and had you executed. People were queuing up for hours to dance on your grave.";
		}
		else if (status.respect <= -100)
		{
			weLost = true;
			gameOver = true;
			explanation = "Your rulership was so corrupt that your people revolted. Your head is currently displayed on a pole outside your former palace.";
		}
		else if (status.money < 0)
		{
			weLost = true;
			gameOver = true;
			explanation = "When they discovered that your treasury is empty, all of your subjects left you.";
		}
		else if (status.love >= 100)
		{
			weLost = false;
			gameOver = true;
			explanation = string.Format("Your people adore you! You will go down in history as {0} {1} the Beloved, most popular monarch ever to have ruled on Earth.", status.title, status.name);
		}
		else if (status.respect >= 100)
		{
			weLost = false;
			gameOver = true;
			explanation = string.Format("You are the most respected {0} in known history. Your reign is marked by outstanding justice and wisdom, and rulers from across the Earth come to seek your advice.", status.title, status.name);
		}

		if (gameOver)
		{
			ShowGameOverScreen(!weLost, explanation);
		}
	}

	void ShowGameOverScreen(bool wonGame, string explanation)
	{
		status.gameOver = true;

		gameWonTitle.gameObject.SetActive(wonGame);
		gameWonText.gameObject.SetActive(wonGame);
		gameLostTitle.gameObject.SetActive(!wonGame);
		gameLostText.gameObject.SetActive(!wonGame);

		if (wonGame)
		{
			gameWonText.text = string.Format(gameWonTextTemplate, status.day, explanation);
		}
		else
		{
			gameLostTitle.text = string.Format(gameLostTitleTemplate, status.title);
			gameLostText.text = string.Format(gameLostTextTemplate, status.day, explanation);
		}

		ShowPanel(gameOverPanel);
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

		bool canTakeAction = status.actionsLeftToday > 0;
		setTaxButton.gameObject.SetActive(canTakeAction);

		CheckIfGameOver();
	}
}
