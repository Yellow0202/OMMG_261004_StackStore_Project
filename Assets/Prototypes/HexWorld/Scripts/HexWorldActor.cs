using UnityEngine;
using UnityEngine.UI;

public sealed class HexWorldActor : MonoBehaviour
{
    public SpriteRenderer body;
    public Sprite[] walkFrames;
    public Sprite seatedFrame;
    public bool seated;
    public Image patience;
    public Transform patienceCanvas;
    public Transform shadow;
    public float patienceFraction = 1;
    public bool waiting;
    Vector3 previous;
    public void ShowPatience(bool visible, float fraction)
    {
        waiting=visible;patienceFraction=Mathf.Clamp01(fraction);
        patienceCanvas.gameObject.SetActive(visible);patience.fillAmount=patienceFraction;
    }
    void LateUpdate()
    {
        var camera = Camera.main;
        if (camera)
        {
            patienceCanvas.rotation = camera.transform.rotation;
            patienceCanvas.position = transform.position + camera.transform.up * 1.5f;
        }
        bool moving = (transform.position - previous).sqrMagnitude > .000001f;
        if(seated&&seatedFrame)body.sprite=seatedFrame;
        else if (walkFrames.Length > 0) body.sprite = walkFrames[moving ? Mathf.FloorToInt(Time.time * 7) % walkFrames.Length : 0];
        ShowPatience(waiting,patienceFraction);
        previous = transform.position;
    }
}
