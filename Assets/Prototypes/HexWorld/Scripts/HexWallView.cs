using UnityEngine;
public sealed class HexWallView : MonoBehaviour
{
 public SpriteRenderer image;public BoxCollider obstacle;
 public MeshRenderer volume;
 // Solid walls use the depth buffer rather than an entire sprite's single sorting value.
 void LateUpdate(){if(volume&&image)image.enabled=false;}
 public void Show(bool visible){gameObject.SetActive(visible);}
}
