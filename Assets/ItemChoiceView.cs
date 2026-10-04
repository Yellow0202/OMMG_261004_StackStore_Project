using UnityEngine;
using UnityEngine.UI;

public sealed class ItemChoiceView : MonoBehaviour
{
    public Button button;
    public Image picture;
    public Text itemName, description, kindLabel, levelLabel;

    public void Display(ItemDefinition item, int currentLevel)
    {
        picture.sprite = item.picture;
        itemName.text = item.DisplayName;
        description.text = item.DisplayDescription();
        kindLabel.text = LocalizationService.Text("item.kind." + item.kind);
        levelLabel.text = currentLevel == 0 ? LocalizationService.Text("item.new", "level", 1) :
            LocalizationService.Text("item.upgrade", "current", currentLevel, "next", currentLevel + 1);
        button.interactable = true;
        gameObject.SetActive(true);
    }
}
