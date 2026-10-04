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
        itemName.text = item.itemName;
        description.text = item.description;
        kindLabel.text = item.kind == ItemKind.Food ? "FOOD" : item.kind == ItemKind.Ability ? "ABILITY" : "SHOP PART";
        levelLabel.text = currentLevel == 0 ? "NEW  /  Lv.1" : "UPGRADE  /  Lv." + currentLevel + " → " + (currentLevel + 1);
        button.interactable = true;
        gameObject.SetActive(true);
    }
}
