using UnityEngine;
using UnityEngine.UI;

public sealed class HexWorldActor : MonoBehaviour
{
    public SpriteRenderer body;
    public Sprite[] walkFrames;
    public Image patience;
    public Transform patienceCanvas;
    public Transform shadow;
    public float patienceFraction = 1;
    public bool waiting;
    Vector3 previous;
    void LateUpdate()
    {
        var camera = Camera.main;
        if (camera)
        {
            patienceCanvas.rotation = camera.transform.rotation;
            patienceCanvas.position = transform.position + camera.transform.up * 1.5f;
        }
        bool moving = (transform.position - previous).sqrMagnitude > .000001f;
        if (walkFrames.Length > 0) body.sprite = walkFrames[moving ? Mathf.FloorToInt(Time.time * 7) % walkFrames.Length : 0];
        patienceCanvas.gameObject.SetActive(waiting);
        patience.fillAmount = Mathf.Clamp01(patienceFraction);
        previous = transform.position;
    }
}
