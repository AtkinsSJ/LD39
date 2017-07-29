using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceButtonScript : MonoBehaviour
{
	public Controller controller;

	public int choiceIndex;

	public void Init(Controller controller, EventChoice choice, int index)
	{
		this.controller = controller;
		this.choiceIndex = index;

		GetComponentInChildren<Button>().onClick.AddListener(OnClicked);
		GetComponentInChildren<Text>().text = choice.description;
	}

	void OnClicked()
	{
		controller.OnChoiceSelected(choiceIndex);
	}
}
