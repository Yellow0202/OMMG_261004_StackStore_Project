using UnityEngine;

/// <summary>Saved furniture art and chairs share the gameplay layout's seat coordinates.</summary>
public sealed class HexTileInterior : MonoBehaviour
{
    public HexTileKind kind;
    public Transform furnishing;
    public SpriteRenderer[] chairs;
    public Sprite chairFront,chairBack;
    int capacity=4;
    public void Bind(HexShopLayout owner,int floor,Vector2Int cell)
    {
        if(owner!=null&&owner.Floor(floor).use.TryGetValue(cell,out var use))capacity=use.capacity;
        Refresh();
    }
    public void Tint(Color color)
    {
        foreach(var image in GetComponentsInChildren<SpriteRenderer>(true))image.color=color;
    }
    void LateUpdate(){Refresh();}
    public void Refresh()
    {
        var options=HexTestSettings.Current?HexTestSettings.Current.interior:new HexTestSettings.InteriorOptions();
        if(furnishing)
        {
            furnishing.localPosition=Vector3.up*options.groundHeight;
            var image=furnishing.GetComponent<SpriteRenderer>();
            if(image&&image.sprite&&options.furnitureSizes!=null&&(int)kind<options.furnitureSizes.Length)
            {
                var size=options.furnitureSizes[(int)kind];var bounds=image.sprite.bounds.size;
                furnishing.localScale=new Vector3(size.x/bounds.x,size.y/bounds.y,1)*options.furnitureScale;
            }
        }
        if(chairs==null)return;
        for(int i=0;i<chairs.Length;i++)
        {
            var chair=chairs[i];if(!chair)continue;
            chair.gameObject.SetActive(kind==HexTileKind.Table&&i<capacity);
            if(!chair.gameObject.activeSelf)continue;
            var seat=HexShopLayout.SeatOffset(i,capacity);
            var outward=new Vector3(seat.x,0,seat.z).normalized;
            chair.transform.localPosition=seat+outward*options.chairDepthOffset;
            var camera=Camera.main;
            bool back=camera&&Vector3.Dot(camera.transform.forward,outward)<0;
            chair.sprite=back?chairBack:chairFront;
            if(chair.sprite)chair.transform.localScale=Vector3.one*(options.chairWidth/chair.sprite.bounds.size.x);
            // Put the near chair's backrest in front of its occupant; the far chair stays behind them.
            var facing=chair.GetComponent<HexCameraFacingSprite>();if(facing)facing.FaceCamera();
        }
    }
}
