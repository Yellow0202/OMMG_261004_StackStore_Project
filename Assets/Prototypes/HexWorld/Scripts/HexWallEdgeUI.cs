using UnityEngine;
using UnityEngine.UI;

/// <summary>Authored UGUI buttons follow the selected tile's six ground edges.</summary>
public sealed class HexWallEdgeUI : MonoBehaviour
{
    public HexTileBoard board;
    public RectTransform root;
    public Button[] buttons;
    public Image[] icons,connectors;
    public Text[] symbols;
    readonly Vector2[] positions=new Vector2[6],edges=new Vector2[6];
    void Awake(){for(int i=0;i<buttons.Length;i++){int side=i;buttons[i].onClick.AddListener(()=>board.ToggleWall(side));}}
    void LateUpdate()
    {
        bool visible=board.game.IsBuilding&&board.WallSelection.HasValue;
        var camera=Camera.main;var canvas=GetComponentInParent<Canvas>();
        if(visible&&camera)
        {
            Vector3 center=HexBoardModel.World(board.WallSelection.Value);
            for(int d=0;d<6;d++)
            {
                Vector3 edge=center+HexBoardModel.World(HexBoardModel.Directions[d])*.5f+Vector3.up*.12f;
                var screen=camera.WorldToScreenPoint(edge);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out edges[d]);
                positions[d]=edges[d];
            }
            // At a nearly horizontal view opposite edges project close together.
            // Keep icons separately clickable and connect displaced icons to their edge.
            for(int pass=0;pass<12;pass++)for(int a=0;a<6;a++)for(int b=a+1;b<6;b++)
                if(Mathf.Abs(positions[a].x-positions[b].x)<40&&Mathf.Abs(positions[a].y-positions[b].y)<40)
                {float offset=(40-Mathf.Abs(positions[a].y-positions[b].y))*.5f+1;positions[a].y-=offset;positions[b].y+=offset;}
        }
        for(int d=0;d<6;d++)
        {
            buttons[d].gameObject.SetActive(visible);connectors[d].gameObject.SetActive(visible);
            if(!visible)continue;
            buttons[d].GetComponent<RectTransform>().anchoredPosition=positions[d];
            bool wall=board.Layout.HasWall(board.CurrentFloor,board.WallSelection.Value,d);
            icons[d].color=wall?new Color(1,.5f,.45f):new Color(.5f,1,.75f);
            symbols[d].text=LocalizationService.Text(wall?"wall.icon.remove":"wall.icon.add");
            Vector2 delta=positions[d]-edges[d];var line=connectors[d].rectTransform;
            line.anchoredPosition=(positions[d]+edges[d])*.5f;line.sizeDelta=new Vector2(delta.magnitude,2);line.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }
    }
}
