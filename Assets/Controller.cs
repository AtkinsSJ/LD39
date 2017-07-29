
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
public struct Event
{
	public string character;
	public string description;
	public EventChoice[] choices;
}

[System.Serializable]
public struct EventArray
{
	public Event[] events;
}

public class Controller : MonoBehaviour {
	public List<Event> events = new List<Event>();
	public Event currentEvent;

	// UI stuff
	public Text characterText;
	public Text eventText;
	public ChoiceButtonScript choiceButtonPrefab;
	public GameObject choiceButtonsGroup;

	// Use this for initialization
	void Start ()
	{
		var eventGuids = AssetDatabase.FindAssets("t:TextAsset", new string[] { "Assets/events" });
		foreach (var guid in eventGuids)
		{
			TextAsset eventAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guid));
			var loadedEvents = JsonUtility.FromJson<EventArray>(eventAsset.text);
			events.AddRange(loadedEvents.events);
		}

		ShowRandomEvent();
	}

	public void ShowRandomEvent()
	{
		// TODO: We probably want some kind of random bucket system: at first we put all events in the bucket,
		// then we take a random one out each time. This eliminates repeats until we've seen every event once.
		// (Then we just swap the used/unused buckets.)
		currentEvent = events[Random.Range(0, events.Count)];
		characterText.text = currentEvent.character;
		eventText.text = currentEvent.description;

		foreach (Transform oldButton in choiceButtonsGroup.transform)
		{
			GameObject.Destroy(oldButton.gameObject);
		}
		
		for (int choiceIndex=0; choiceIndex < currentEvent.choices.Length; choiceIndex++)
		{
			var choice = currentEvent.choices[choiceIndex];
			var button = Instantiate(choiceButtonPrefab, choiceButtonsGroup.transform) as ChoiceButtonScript;
			button.Init(this, choice, choiceIndex);
		}
	}

	public void OnChoiceSelected(int choiceIndex)
	{
		Debug.Log("Selected choice " + choiceIndex + "!");
		ShowRandomEvent();
	}
	
	// Update is called once per frame
	void Update ()
	{
		
	}
}
