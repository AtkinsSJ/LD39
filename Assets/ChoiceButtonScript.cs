using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceButtonScript : MonoBehaviour
{
	public Controller controller;

	public int choiceIndex;

	public void Init(Controller controller, EventChoice choice, int index, bool enabled)
	{
		this.controller = controller;
		this.choiceIndex = index;

		if (enabled)
		{
			GetComponentInChildren<Button>().onClick.AddListener(OnClicked);
			GetComponentInChildren<Text>().text = choice.description;
		}
		else
		{
			GetComponentInChildren<Button>().interactable = false;
			GetComponentInChildren<Text>().text = "Not enough gold: " + choice.description;
			GetComponentInChildren<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
		}
	}

	void OnClicked()
	{
		controller.OnChoiceSelected(choiceIndex);
	}
}
