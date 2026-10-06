using UnityEngine;
public sealed class HexWallView : MonoBehaviour
{
 public SpriteRenderer image;public BoxCollider obstacle;
 public MeshRenderer volume;
 // Solid walls use the depth buffer rather than an entire sprite's single sorting value.
 void LateUpdate()
 {
  if(volume&&image)image.enabled=false;
  if(volume&&HexTestSettings.Current)
  {
   var visual=HexTestSettings.Current.visual;
   volume.transform.localScale=new Vector3(1,visual.wallHeight/1.125f,visual.wallThickness/.24f);
   if(obstacle)obstacle.size=new Vector3(2,visual.wallHeight/.9f,visual.wallThickness);
  }
 }
 public void Show(bool visible){gameObject.SetActive(visible);}
}
