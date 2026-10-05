using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable] public sealed class HexShopRow { public HexShopOffer offer; public Image icon; public Text title, description, price; public Button buy, use; }
public sealed class HexShopSystem : MonoBehaviour
{
    public HexPrototype game;
    public GameObject panel;
    public Button openButton, closeButton, quickUseButton;
    public Text balanceLabel, messageLabel, buffLabel, quickUseLabel;
    public HexShopOffer quickConsumable;
    public HexShopRow[] rows;
    public bool IsOpen { get; private set; }
    readonly Dictionary<HexShopOffer,int> consumables = new Dictionary<HexShopOffer,int>();
    string message = "shop.help";
    void Awake()
    {
        openButton.onClick.AddListener(Open);closeButton.onClick.AddListener(Close);
        quickUseButton.onClick.AddListener(()=>Use(quickConsumable));
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
    public bool Use(HexShopOffer offer)
    {
        if(!offer||offer.kind!=HexShopKind.Consumable||Stock(offer)<=0)return false;
        if(!game.ActivateServiceBuff(offer.buffSeconds,offer.speedBonus)){message="shop.buff.active";Refresh();return false;}
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
            row.use.interactable=Stock(offer)>0&&game.BuffRemaining<=0;
        }
        openButton.interactable=!game.IsBuilding&&!game.IsChoosing&&!IsOpen;
        quickUseButton.interactable=Stock(quickConsumable)>0&&game.BuffRemaining<=0&&!game.IsChoosing;
        quickUseButton.gameObject.SetActive(!game.IsChoosing);openButton.gameObject.SetActive(!game.IsChoosing);buffLabel.gameObject.SetActive(!game.IsChoosing);
        quickUseLabel.text=LocalizationService.Text("shop.quick","count",Stock(quickConsumable));
        buffLabel.text=LocalizationService.Text("shop.buff.status","seconds",game.BuffRemaining.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));
    }
}
