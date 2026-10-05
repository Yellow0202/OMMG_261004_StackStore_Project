using UnityEngine;
public sealed class HexWallView : MonoBehaviour
{
 public SpriteRenderer image;public BoxCollider obstacle;
 void LateUpdate(){var camera=Camera.main;if(camera&&image)image.sortingOrder=Mathf.Clamp(Mathf.RoundToInt(-Vector3.Dot(camera.transform.forward,transform.position-camera.transform.position)*100),-20000,20000);}
 public void Show(bool visible){gameObject.SetActive(visible);}
}
