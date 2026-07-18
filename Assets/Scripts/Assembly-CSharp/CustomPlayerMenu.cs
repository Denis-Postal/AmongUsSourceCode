using UnityEngine;

public class CustomPlayerMenu : MonoBehaviour
{
	public static CustomPlayerMenu Instance;

	public TabButton[] Tabs;

	public Sprite NormalColor;

	public Sprite SelectedColor;

	public void Start()
	{
		Instance = this;
	}

	public void OpenTab(GameObject tab)
	{
		for (int i = 0; i < Tabs.Length; i++)
		{
			TabButton tabButton = Tabs[i];
			if (tabButton.Tab == tab)
			{
				tabButton.Tab.SetActive(value: true);
				tabButton.Button.sprite = SelectedColor;
			}
			else
			{
				tabButton.Tab.SetActive(value: false);
				tabButton.Button.sprite = NormalColor;
			}
		}
	}

	public void Close(bool canMove)
	{
		Object.Destroy(base.gameObject);
	}
}
