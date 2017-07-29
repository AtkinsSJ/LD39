using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
public struct Consequence
{
	public string field;
	public float minChange, maxChange;
}

[Serializable]
public struct EventChoice
{
	public string description;
	public Consequence[] consequences;
}

[Serializable]
public struct Event
{
	public string character;
	public string description;
	public EventChoice[] choices;
}

[Serializable]
public struct EventArray
{
	public Event[] events;
}

public class Controller : MonoBehaviour {
	public List<Event> events = new List<Event>();

	// Use this for initialization
	void Start () {
		var eventGuids = AssetDatabase.FindAssets("t:TextAsset", new string[] { "Assets/events" });
		foreach (var guid in eventGuids)
		{
			TextAsset eventAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guid));
			var loadedEvents = JsonUtility.FromJson<EventArray>(eventAsset.text);
			events.AddRange(loadedEvents.events);
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
