using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class HexShopBuildingUI : MonoBehaviour
{
    public HexTileBoard board;
    public Button wallMode, previousFloor, nextFloor;
    public Dropdown floorDropdown;
    public Text selection;
    public Button[] sides;
    int floorCount = -1;

    void Awake()
    {
        wallMode.onClick.AddListener(() =>
        {
            if (!board.game.IsBuilding) board.game.BeginBuilding();
            if (board.game.IsBuilding) board.SetMode(HexBuildMode.Walls);
        });
        previousFloor.onClick.AddListener(() => board.SetFloor(board.CurrentFloor - 1));
        nextFloor.onClick.AddListener(() => board.SetFloor(board.CurrentFloor + 1));
        floorDropdown.onValueChanged.AddListener(board.SetFloor);
        for (int i = 0; i < sides.Length; i++)
        {
            int side = i;
            sides[i].onClick.AddListener(() => board.ToggleWall(side));
        }
    }

    void OnEnable() { LocalizationService.LanguageChanged += LanguageChanged; }
    void OnDisable() { LocalizationService.LanguageChanged -= LanguageChanged; }
    void LanguageChanged() { floorCount = -1; }

    void Update()
    {
        if (board.Layout == null) return;
        if (floorCount != board.Layout.floors.Count)
        {
            floorCount = board.Layout.floors.Count;
            floorDropdown.ClearOptions();
            var names = new List<string>();
            for (int i = 0; i < floorCount; i++) names.Add(LocalizationService.Text("shop.floor", "floor", i + 1));
            floorDropdown.AddOptions(names);
        }
        floorDropdown.SetValueWithoutNotify(board.CurrentFloor);
        previousFloor.interactable = board.CurrentFloor > 0;
        nextFloor.interactable = board.CurrentFloor < floorCount - 1;
        wallMode.interactable = !board.game.IsChoosing && !board.game.IsShopping;
        selection.text = LocalizationService.Text(board.WallSelection.HasValue ? "shop.wall.selected" : "shop.wall.select",
            "x", board.WallSelection.GetValueOrDefault().x, "y", board.WallSelection.GetValueOrDefault().y);
        for (int i = 0; i < sides.Length; i++)
        {
            sides[i].interactable = board.game.IsBuilding && board.Mode == HexBuildMode.Walls && board.WallSelection.HasValue;
            bool present = board.WallSelection.HasValue && board.Layout.HasWall(board.CurrentFloor, board.WallSelection.Value, i);
            sides[i].GetComponentInChildren<Text>().text = LocalizationService.Text("shop.wall.side.label",
                "direction", LocalizationService.Text("shop.wall.side." + i),
                "action", LocalizationService.Text(present ? "shop.wall.demolish" : "shop.wall.build"));
        }
    }
}
