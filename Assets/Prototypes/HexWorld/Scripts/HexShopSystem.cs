using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable] public sealed class HexShopRow { public HexShopOffer offer; public Image icon; public Text title, description, price; public Button buy, use; }
[Serializable] public sealed class HexQuickSlot { public HexShopOffer offer; public Button button; public Image icon; public Text title,count,status; }
public sealed class HexShopSystem : MonoBehaviour
{
    public HexPrototype game;
    public GameObject panel;
    public Button openButton, closeButton, quickUseButton;
    public Text balanceLabel, messageLabel, buffLabel, quickUseLabel;
    public HexShopOffer quickConsumable;
    public HexShopRow[] rows;
    public GameObject quickSlotPanel;
    public HexQuickSlot[] quickSlots=Array.Empty<HexQuickSlot>();
    public bool IsOpen { get; private set; }
    readonly Dictionary<HexShopOffer,int> consumables = new Dictionary<HexShopOffer,int>();
    string message = "shop.help";
    void Awake()
    {
        openButton.onClick.AddListener(Open);closeButton.onClick.AddListener(Close);
        if(quickUseButton)quickUseButton.gameObject.SetActive(false);
        foreach(var slot in quickSlots){var offer=slot.offer;slot.button.onClick.AddListener(()=>Use(offer));}
        foreach(var row in rows){var offer=row.offer;row.buy.onClick.AddListener(()=>Buy(offer));row.use.onClick.AddListener(()=>Use(offer));}
        panel.SetActive(false);Refresh();
    }
    void OnEnable(){LocalizationService.LanguageChanged+=Refresh;}
    void OnDisable(){LocalizationService.LanguageChanged-=Refresh;if(IsOpen)Close();}
    void Update(){Refresh();}
    public int Stock(HexShopOffer offer) => offer && consumables.TryGetValue(offer,out int count) ? count : 0;
    public void Open(){if(!game.BeginShopping())return;IsOpen=true;message="shop.help";panel.SetActive(true);panel.transform.SetAsLastSibling();Refresh();}
    public void Close(){if(!IsOpen)return;IsOpen=false;panel.SetActive(false);game.EndShopping();}
    public bool Buy(HexShopOffer offer)
    {
        if(!IsOpen||!offer||!offer.IsValid)return false;
        if(offer.kind==HexShopKind.Upgrade&&!game.CanGrantItem(offer.upgrade)){message="shop.max";Refresh();return false;}
        if(!game.TrySpendGold(offer.price)){message="shop.insufficient";Refresh();return false;}
        if(offer.kind==HexShopKind.Tile)game.board.Grant(offer.tile,offer.quantity);
        else if(offer.kind==HexShopKind.Upgrade)game.GrantItem(offer.upgrade);
        else consumables[offer]=Stock(offer)+offer.quantity;
        message="shop.purchased";Refresh();return true;
    }
    public float Remaining(HexShopOffer offer)=>offer.effect==HexConsumableEffect.ServiceSpeed?game.BuffRemaining:offer.effect==HexConsumableEffect.DeliverySpeed?game.DeliveryBuffRemaining:0;
    public bool CanUse(HexShopOffer offer)=>!game.IsGameOver&&offer&&offer.kind==HexShopKind.Consumable&&Stock(offer)>0&&!game.IsChoosing&&!game.IsBuilding&&Remaining(offer)<=0&&(offer.effect!=HexConsumableEffect.Patience||game.HasSnackTargets);
    public bool Use(HexShopOffer offer)
    {
        if(!CanUse(offer))return false;
        bool applied=offer.effect==HexConsumableEffect.Patience?game.GiveSnacks(offer.patienceRecovery,offer.targetCount,offer.repeatedSnackMultiplier):
            offer.effect==HexConsumableEffect.ReadyMeals?game.GiveReadyMeals(offer.readyMeals):
            offer.effect==HexConsumableEffect.DeliverySpeed?game.ActivateDeliveryBuff(offer.buffSeconds,offer.speedBonus):game.ActivateServiceBuff(offer.buffSeconds,offer.speedBonus);
        if(!applied){message="shop.buff.active";Refresh();return false;}
        consumables[offer]--;message="shop.used";Refresh();return true;
    }
    void Refresh()
    {
        if(!game||!balanceLabel)return;
        balanceLabel.text=LocalizationService.Text("shop.balance","gold",game.Gold,"earned",game.EarnedGold);
        messageLabel.text=LocalizationService.Text(message);
        foreach(var row in rows)
        {
            var offer=row.offer;if(!offer)continue;
            row.icon.sprite=offer.picture;row.title.text=LocalizationService.Text(offer.nameKey);row.description.text=offer.Description;
            row.price.text=LocalizationService.Text("shop.price","price",offer.price);
            row.buy.interactable=IsOpen&&offer.IsValid&&game.Gold>=offer.price&&(offer.kind!=HexShopKind.Upgrade||game.CanGrantItem(offer.upgrade));
            row.use.gameObject.SetActive(offer.kind==HexShopKind.Consumable);
            row.use.interactable=CanUse(offer);
            if(offer.kind==HexShopKind.Consumable)row.use.GetComponentInChildren<Text>().text=LocalizationService.Text("shop.use.stock","count",Stock(offer));
        }
        openButton.interactable=!game.IsGameOver&&!game.IsBuilding&&!game.IsChoosing&&!IsOpen;
        if(quickSlotPanel)quickSlotPanel.SetActive(!game.IsGameOver&&!game.IsChoosing&&!game.IsBuilding&&!IsOpen);
        foreach(var slot in quickSlots)
        {
            var offer=slot.offer;if(!offer)continue;
            slot.icon.sprite=offer.picture;slot.title.text=LocalizationService.Text(offer.key=="PotionOffer"?"shop.potion.short":offer.nameKey);
            slot.count.text=LocalizationService.Text("shop.slot.count","count",Stock(offer));
            float left=Remaining(offer);
            slot.status.text=left>0?LocalizationService.Text("shop.slot.active","seconds",left.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)):LocalizationService.Text(Stock(offer)<=0?"shop.slot.empty":offer.effect==HexConsumableEffect.Patience&&!game.HasSnackTargets?"shop.slot.no.target":"shop.slot.ready");
            slot.button.interactable=CanUse(offer);
        }
        openButton.gameObject.SetActive(!game.IsGameOver&&!game.IsChoosing);buffLabel.gameObject.SetActive(!game.IsGameOver&&!game.IsChoosing);
        buffLabel.text=LocalizationService.Text("shop.buff.all","seconds",game.BuffRemaining.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture),"delivery",game.DeliveryBuffRemaining.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture),"meals",game.service?game.service.Food.ReservedMeals:0);
    }
}
