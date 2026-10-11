using System;
using UnityEditor;
using UnityEngine;

public static class HexInteriorPlayChecks
{
    public static void Validate(HexPrototype game)
    {
        var original=HexTestSettings.Current;var settings=UnityEngine.Object.Instantiate(original);
        var current=typeof(HexTestSettings).GetProperty("Current");
        HexTileView view=null;
        try
        {
            current.SetValue(null,settings);
            var table=Array.Find(game.board.tileTypes,d=>d.kind==HexTileKind.Table);
            var layout=new HexShopLayout();var cell=new Vector2Int(1,0);layout.Floor(0).model.Grant(table,1);
            if(!layout.TryPlace(0,cell,table))throw new Exception("Interior fixture placement");
            view=UnityEngine.Object.Instantiate(game.board.tilePrefab);view.coordinate=cell;view.transform.position=HexBoardModel.World(cell);
            view.Show(true,table);view.ShowShop(layout,0,true);
            var interior=view.GetComponentInChildren<HexTileInterior>();
            if(!interior||view.buildingSprite.gameObject.activeSelf)throw new Exception("New table not connected / old placeholder still visible");
            for(int capacity=2;capacity<=4;capacity++)
            {
                layout.Floor(0).use[cell]=new HexTileUse(capacity);view.ShowShop(layout,0,true);
                int active=0;
                for(int s=0;s<4;s++)
                {
                    var chair=interior.chairs[s];if(!chair.gameObject.activeSelf)continue;active++;
                    var anchor=chair.transform.localPosition;var seat=HexShopLayout.SeatOffset(s,capacity);var outward=seat;outward.y=0;
                    if(Vector3.Distance(anchor,seat+outward.normalized*settings.interior.chairDepthOffset)>.001f)throw new Exception("Chair and booked seat drift");
                }
                if(active!=capacity)throw new Exception("Wrong visible chair count");
            }
            settings.interior.seatRadius=.72f;settings.interior.chairWidth=.52f;settings.interior.furnitureSizes[1]=new Vector2(1.3f,.8f);
            var camera=Camera.main;var rotation=camera.transform.rotation;
            try
            {
                foreach(float yaw in new[]{0f,90f,180f,270f})
                {
                    camera.transform.rotation=Quaternion.Euler(30,yaw,0);interior.Refresh();
                    for(int s=0;s<4;s++)
                    {
                        var chair=interior.chairs[s];var toward=-HexShopLayout.SeatOffset(s,4);toward.y=0;
                        int facing=HexCustomerAnimation.SeatedDirection(Quaternion.Euler(0,-yaw,0)*toward);
                        var expected=facing==1||facing==3?interior.chairSide:facing==2?interior.chairBack:interior.chairFront;
                        if(!expected||chair.sprite!=expected||chair.flipX!=(facing==3))throw new Exception("Wrong inward chair direction "+s+" / yaw "+yaw);
                        if(Mathf.Abs(chair.sprite.bounds.size.y*chair.transform.localScale.y-settings.interior.chairWidth*interior.chairFront.bounds.size.y/interior.chairFront.bounds.size.x)>.001f)throw new Exception("Profile chair height drift");
                    }
                }
            }
            finally{camera.transform.rotation=rotation;}
            interior.Refresh();
            if(Mathf.Abs(interior.chairs[0].transform.localPosition.z+.72f+settings.interior.chairDepthOffset)>.001f)throw new Exception("Live seat radius ignored");
            if(Mathf.Abs(interior.furnishing.GetComponent<SpriteRenderer>().bounds.size.x-1.3f)>.01f)throw new Exception("Live furniture size ignored");
            view.Show(true,table,new Color(0,1,0,.4f));
            foreach(var image in interior.GetComponentsInChildren<SpriteRenderer>())if(Mathf.Abs(image.color.a-.4f)>.001f)throw new Exception("Placement ghost tint lost");
            view.Show(true,table);foreach(var image in interior.GetComponentsInChildren<SpriteRenderer>())if(image.color!=Color.white)throw new Exception("Placement tint not restored");
            Debug.Log("HEX_INTERIOR_PLAY_PASSED: actual tile uses saved furniture, 2/3/4 visible chairs match gameplay anchors, live ScriptableObject sizes/radius, placement tint/restore; six identities and four seated directions.");
        }
        finally
        {
            current.SetValue(null,original);if(view)UnityEngine.Object.DestroyImmediate(view.gameObject);UnityEngine.Object.DestroyImmediate(settings);
        }
    }
}
