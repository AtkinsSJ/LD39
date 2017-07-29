
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

	// UI stuff
	public Text characterText;
	public Text eventText;
	public Button choiceButtonPrefab;
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
		var e = events[Random.Range(0, events.Count)];
		characterText.text = e.character;
		eventText.text = e.description;

		foreach (Transform oldButton in choiceButtonsGroup.transform)
		{
			GameObject.Destroy(oldButton.gameObject);
		}
		
		foreach (var choice in e.choices)
		{
			var button = Instantiate(choiceButtonPrefab, choiceButtonsGroup.transform) as Button;
			button.GetComponentInChildren<UnityEngine.UI.Text>().text = choice.description;
		}

	}
	
	// Update is called once per frame
	void Update ()
	{
		
	}
}
