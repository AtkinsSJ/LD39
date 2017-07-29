using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PetitionButton : MonoBehaviour
{
	public Controller controller;
	public Event e;

	public void Init(Controller controller, Event e)
	{
		this.controller = controller;
		this.e = e;

		GetComponentInChildren<Button>().onClick.AddListener(OnClicked);
		GetComponentInChildren<Text>().text = string.Format("{0} ({1} days)", e.character, e.daysWaited);
	}

	void OnClicked()
	{
		controller.OnEventSelected(e);
	}
}
