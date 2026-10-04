using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[Serializable]
public sealed class OwnedItem
{
    public ItemDefinition definition;
    public int level;
}

public sealed class GoldLevelSystem : MonoBehaviour
{
    public StackStorePrototype store;
    public GoldLevelCurve curve;
    public ItemCatalog catalog;
    [Header("Authored UGUI")]
    public GameObject choicePanel;
    public Text levelLabel, progressLabel, inventoryLabel, choiceTitle;
    public Image progressBar;
    public ItemChoiceView[] cards;
    public Button continueButton;
    [SerializeField] int playerLevel = 1;
    [SerializeField] List<OwnedItem> ownedItems = new List<OwnedItem>();
    readonly List<ItemDefinition> offered = new List<ItemDefinition>();
    bool ownsPause;
    float previousTimeScale;

    public int PlayerLevel => playerLevel;
    public bool IsChoosing => ownsPause;
    public long NextGoldThreshold => curve.ThresholdForNextLevel(playerLevel);
    public IReadOnlyList<OwnedItem> OwnedItems => ownedItems;

    void Awake()
    {
        if (!store || !curve || !catalog || !choicePanel || !levelLabel || !progressLabel || !inventoryLabel ||
            !choiceTitle || !progressBar || !continueButton || cards == null || cards.Length != 3)
        {
            Debug.LogError("Gold level system: authored UI or data references missing.", this);
            enabled = false;
            return;
        }
        for (int i = 0; i < cards.Length; i++)
        {
            int index = i;
            cards[i].button.onClick.AddListener(() => Choose(index));
        }
        continueButton.onClick.AddListener(ContinueWithoutItem);
        choicePanel.SetActive(false);
        ApplyEffects();
        RefreshHUD();
    }

    void Update()
    {
        RefreshHUD();
        if (!ownsPause && store.Gold >= NextGoldThreshold) OpenChoice();
    }

    public int ItemLevel(string key)
    {
        var item = ownedItems.Find(x => x.definition && x.definition.key == key);
        return item == null ? 0 : item.level;
    }

    void OpenChoice()
    {
        playerLevel++;
        offered.Clear();
        var eligible = new List<ItemDefinition>();
        var keys = new HashSet<string>();
        foreach (var item in catalog.items)
        {
            if (!item || string.IsNullOrWhiteSpace(item.key)) continue;
            if (!keys.Add(item.key)) { Debug.LogWarning("Duplicate item key ignored: " + item.key, catalog); continue; }
            if (item.maxLevel == 0 || ItemLevel(item.key) < item.maxLevel) eligible.Add(item);
        }
        while (eligible.Count > 0 && offered.Count < cards.Length)
        {
            int index = UnityEngine.Random.Range(0, eligible.Count);
            offered.Add(eligible[index]);
            eligible.RemoveAt(index);
        }
        for (int i = 0; i < cards.Length; i++)
        {
            if (i < offered.Count) cards[i].Display(offered[i], ItemLevel(offered[i].key));
            else cards[i].gameObject.SetActive(false);
        }
        choiceTitle.text = "LEVEL " + playerLevel + "  /  CHOOSE ONE";
        if (offered.Count == 0) choiceTitle.text = "LEVEL " + playerLevel + "  /  ALL ITEMS MAXED";
        continueButton.gameObject.SetActive(offered.Count == 0);
        previousTimeScale = Time.timeScale;
        ownsPause = true;
        Time.timeScale = 0;
        choicePanel.SetActive(true);
        if (EventSystem.current)
            EventSystem.current.SetSelectedGameObject(offered.Count > 0 ? cards[0].button.gameObject : continueButton.gameObject);
        RefreshHUD();
    }

    public void Choose(int index)
    {
        if (!ownsPause || index < 0 || index >= offered.Count) return;
        ItemDefinition definition = offered[index];
        var item = ownedItems.Find(x => x.definition && x.definition.key == definition.key);
        if (item == null) { item = new OwnedItem { definition = definition }; ownedItems.Add(item); }
        item.level++;
        ApplyEffects();
        CloseChoice();
    }

    public void ContinueWithoutItem()
    {
        if (ownsPause && offered.Count == 0) CloseChoice();
    }

    void CloseChoice()
    {
        choicePanel.SetActive(false);
        ownsPause = false;
        Time.timeScale = previousTimeScale;
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        offered.Clear();
        // A large gold gain may cross several thresholds. Present every selection separately.
        if (store.Gold >= NextGoldThreshold) OpenChoice();
        else RefreshHUD();
    }

    void ApplyEffects()
    {
        float speed = 0, patience = 0, visitors = 0;
        foreach (var item in ownedItems)
        {
            if (!item.definition) continue;
            foreach (var effect in item.definition.effects)
            {
                float value = effect.amountPerLevel * item.level;
                switch (effect.kind)
                {
                    case ItemEffectKind.ServiceSpeedPercent: speed += value; break;
                    case ItemEffectKind.CustomerPatienceSeconds: patience += value; break;
                    case ItemEffectKind.VisitorChance: visitors += value; break;
                }
            }
        }
        store.SetItemBonuses(speed, patience, visitors);
    }

    void RefreshHUD()
    {
        long previous = playerLevel <= 1 ? 0 : curve.ThresholdForNextLevel(playerLevel - 1);
        long next = NextGoldThreshold;
        levelLabel.text = "LEVEL " + playerLevel;
        progressLabel.text = "GOLD " + store.Gold + " / " + next + "  TO NEXT LEVEL";
        progressBar.fillAmount = Mathf.Clamp01((float)(store.Gold - previous) / Math.Max(1, next - previous));
        var text = new StringBuilder("OWNED ITEMS\n");
        if (ownedItems.Count == 0) text.Append("Reach ").Append(next).Append(" gold to choose an item.");
        foreach (var item in ownedItems)
            if (item.definition) text.Append(item.definition.itemName).Append("  Lv.").Append(item.level).Append('\n');
        inventoryLabel.text = text.ToString();
    }

    void OnDisable()
    {
        if (ownsPause) { ownsPause = false; Time.timeScale = previousTimeScale; }
        if (choicePanel) choicePanel.SetActive(false);
    }
}
