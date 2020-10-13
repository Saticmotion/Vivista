﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabularDataPanelSphere : MonoBehaviour
{
	public Text title;
	public List<string> tabularData;
	public RectTransform tabularDataWrapper;
	public RectTransform tabularDataCellPrefab;
	public RectTransform scrollPanel;
	public Button backButton;
	public Button nextButton;

	private int currentColumns = 0;
	private int currentRows = 0;
	private int currentPage = 0;

	private const float GRIDCELLSIZEX = 430;
	private const float GRIDCELLSIZEY = 220;
	private const int MAXROWSPAGE = 5;

	public void Init(string newTitle, List<string> newTabularData)
	{
		backButton.onClick.AddListener(BackButtonClick);
		nextButton.onClick.AddListener(NextButtonClick);

		ClearTable();

		title.text = newTitle;
		tabularData = newTabularData;

		if (newTabularData != null && newTabularData.Count > 0)
		{
			if (tabularData[tabularData.Count - 1].Contains(":"))
			{
				var rowsColumns = tabularData[tabularData.Count - 1].Split(':');
				currentRows = Convert.ToInt32(rowsColumns[0]);
				currentColumns = Convert.ToInt32(rowsColumns[1]);
			}
		}

		backButton.interactable = false;
		if (currentRows <= MAXROWSPAGE)
		{
			nextButton.interactable = false;
		}

		PopulateTable();

		tabularDataWrapper.GetComponent<GridLayoutGroup2>().constraintCount = currentColumns;
		float cellSizeX = scrollPanel.rect.width / currentColumns;
		float cellSizeY = GRIDCELLSIZEY / currentRows;

		if (cellSizeY < 50)
		{
			cellSizeY = 50;
		}
		if (cellSizeX < 50)
		{
			cellSizeX = 50;
		}
		tabularDataWrapper.GetComponent<GridLayoutGroup2>().cellSize = new Vector2(cellSizeX, cellSizeY);
	}

	//TODO(Jitse): Choose between destroying objects or pooling.
	//TODO(cont.): Uncomment the parts from here on out to destroy objects (and then also delete the uncommented code.)
	private void PopulateTable()
	{
		/*int rowLimit = currentPage * MAXROWSPAGE + MAXROWSPAGE;
		if (rowLimit > currentRows)
		{
			rowLimit = currentRows;
		}
		for (int row = currentPage * MAXROWSPAGE; row < rowLimit; row++)
		{
			for (int column = 0; column < currentColumns; column++)
			{
				var dataCell = Instantiate(tabularDataCellPrefab, tabularDataWrapper);
				var cellText = dataCell.transform.GetComponentInChildren<InputField>();
				cellText.interactable = false;
				string newText = tabularData[row * currentColumns + column];

				if (newText.Contains("//comma//"))
				{
					newText = newText.Replace("//comma//", ",");
				}

				cellText.text = newText;
				cellText.textComponent.fontSize = 16;
				cellText.textComponent.color = Color.black;

				dataCell.transform.SetAsLastSibling();
			}
		}*/

		for (int row = 0; row < currentRows; row++)
		{
			for (int column = 0; column < currentColumns; column++)
			{
				var dataCell = Instantiate(tabularDataCellPrefab, tabularDataWrapper);
				if (row >= MAXROWSPAGE)
				{
					dataCell.gameObject.SetActive(false);
				}
				var cellText = dataCell.transform.GetComponentInChildren<InputField>();
				cellText.interactable = false;
				string newText = tabularData[row * currentColumns + column];

				if (newText.Contains("//comma//"))
				{
					newText = newText.Replace("//comma//", ",");
				}

				cellText.text = newText;
				cellText.textComponent.fontSize = 16;
				cellText.textComponent.color = Color.black;

				dataCell.transform.SetAsLastSibling();
			}
		}
	}

	private void ClearTable()
	{
		/*if (currentRows > 0)
		{
			for (int i = tabularDataWrapper.childCount - 1; i >= 0; i--)
			{
				Destroy(tabularDataWrapper.GetChild(i).gameObject);
			}
		}*/

		int rowLimit = currentPage * MAXROWSPAGE + MAXROWSPAGE;
		if (rowLimit > currentRows)
		{
			rowLimit = currentRows;
		}
		for (int row = currentPage * MAXROWSPAGE; row < rowLimit; row++)
		{
			for (int column = 0; column < currentColumns; column++)
			{
				tabularDataWrapper.GetChild(row * currentColumns + column).gameObject.SetActive(false);
			}
		}
	}

	private void NextButtonClick()
	{
		/*ClearTable();

		currentPage++;

		if (currentPage == 1)
		{
			backButton.interactable = true;
		}
		if (currentPage + 1 == (currentRows + MAXROWSPAGE - 1) / MAXROWSPAGE)
		{
			nextButton.interactable = false;
		}

		PopulateTable();*/

		ClearTable();

		currentPage++;

		if (currentPage == 1)
		{
			backButton.interactable = true;
		}
		if (currentPage + 1 == (currentRows + MAXROWSPAGE - 1) / MAXROWSPAGE)
		{
			nextButton.interactable = false;
		}

		ActivateTableChildren();
	}

	private void BackButtonClick()
	{
		/*ClearTable();

		currentPage--;

		if (currentPage == 0)
		{
			backButton.interactable = false;
		}
		if (currentPage + 1 != (currentRows + MAXROWSPAGE - 1) / MAXROWSPAGE)
		{
			nextButton.interactable = true;
		}

		PopulateTable();*/

		ClearTable();

		currentPage--;

		if (currentPage == 0)
		{
			backButton.interactable = false;
		}
		if (currentPage + 1 != (currentRows + MAXROWSPAGE - 1) / MAXROWSPAGE)
		{
			nextButton.interactable = true;
		}

		ActivateTableChildren();
	}	

	private void ActivateTableChildren()
	{
		int rowLimit = currentPage * MAXROWSPAGE + MAXROWSPAGE;
		if (rowLimit > currentRows)
		{
			rowLimit = currentRows;
		}
		for (int row = currentPage * MAXROWSPAGE; row < rowLimit; row++)
		{
			for (int column = 0; column < currentColumns; column++)
			{
				tabularDataWrapper.GetChild(row * currentColumns + column).gameObject.SetActive(true);
			}
		}
	}
}