using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Controls the scene-authored UGUI and spawns authored customer prefabs.</summary>
public sealed class StackStorePrototype : MonoBehaviour
{
    [Header("Replaceable stall equipment")]
    public ServiceEquipment equipment = new ServiceEquipment();
    [Header("Customers")]
    [Min(1)] public float patienceSeconds = 10f;
    [Min(0.2f)] public float spawnSeconds = 2.3f;
    [Min(10)] public float walkSpeed = 160f;
    public int maxCustomers = 16;

    [System.Serializable]
    public class ServiceEquipment
    {
        public string displayName = "Basic stall";
        [Min(0.1f)] public float intervalSeconds = 5f;
    }

    enum State { Wandering, Queued, Leaving }
    class Customer
    {
        public RectTransform root;
        public Image bar;
        public Text label;
        public State state;
        public Vector2 destination;
        public float wander, patience;
        public int id;
    }

    readonly List<Customer> customers = new List<Customer>();
    readonly List<Customer> queue = new List<Customer>();
    [Header("Scene UI references")]
    public RectTransform field;
    public Image cooldownBar;
    public Text goldLabel, cooldownLabel, statsLabel, notice;
    public CustomerView customerPrefab;
    float cooldown, spawnTimer, noticeTimer;
    int gold, served, lost, nextId;
    static readonly Color Teal = new Color(.12f, .62f, .53f);

    public void Equip(ServiceEquipment replacement)
    {
        if (replacement == null) return;
        equipment = replacement;
        cooldown = Mathf.Max(.1f, equipment.intervalSeconds);
    }

    void Start()
    {
        if (!field || !cooldownBar || !goldLabel || !cooldownLabel || !statsLabel || !notice || !customerPrefab)
        {
            Debug.LogError("Stack Store: scene UI references or customer prefab are missing.", this);
            enabled = false;
            return;
        }
        cooldown = Interval;
        Spawn();
        spawnTimer = Mathf.Max(.2f, spawnSeconds);
    }

    float Interval => Mathf.Max(.1f, equipment != null ? equipment.intervalSeconds : 5f);

    void Update()
    {
        float dt = Time.deltaTime;
        spawnTimer -= dt;
        if (spawnTimer <= 0f)
        {
            if (customers.Count < Mathf.Max(1, maxCustomers)) Spawn();
            spawnTimer = Mathf.Max(.2f, spawnSeconds);
        }
        for (int i = customers.Count - 1; i >= 0; i--)
        {
            Customer c = customers[i];
            if (c.state != State.Leaving)
            {
                c.patience -= dt;
                c.bar.fillAmount = Mathf.Clamp01(c.patience / Mathf.Max(1f, patienceSeconds));
                c.bar.color = c.patience < 3f ? new Color(.86f, .35f, .29f) : Teal;
                c.label.text = "#" + c.id + "  " + Mathf.Max(0, c.patience).ToString("0.0") + "s";
                if (c.patience <= 0f) { lost++; Leave(c, false); }
            }
            if (c.state == State.Wandering)
            {
                c.wander -= dt;
                if (c.wander <= 0f) { c.state = State.Queued; queue.Add(c); }
                else if (Vector2.Distance(c.root.anchoredPosition, c.destination) < 5f)
                    c.destination = WanderPoint();
            }
            if (c.state == State.Queued)
                c.destination = QueuePoint(queue.IndexOf(c));
            c.root.anchoredPosition = Vector2.MoveTowards(c.root.anchoredPosition, c.destination, walkSpeed * dt);
            if (c.state == State.Leaving && Vector2.Distance(c.root.anchoredPosition, c.destination) < 5f)
            {
                Destroy(c.root.gameObject);
                customers.RemoveAt(i);
            }
        }
        cooldown = Mathf.Max(0f, cooldown - dt);
        if (cooldown <= 0f && queue.Count > 0 &&
            Vector2.Distance(queue[0].root.anchoredPosition, QueuePoint(0)) < 8f)
        {
            Leave(queue[0], true);
            gold++; served++;
            cooldown = Interval;
            notice.text = "+1 GOLD";
            notice.color = Teal;
            noticeTimer = 1.3f;
        }
        cooldownBar.fillAmount = 1f - Mathf.Clamp01(cooldown / Interval);
        cooldownLabel.text = cooldown > 0f ? "NEXT SERVICE  " + cooldown.ToString("0.0") + "s" : "READY — waiting for customer";
        goldLabel.text = "GOLD  " + gold;
        statsLabel.text = "SERVED  " + served + "     LEFT  " + lost + "     QUEUE  " + queue.Count;
        noticeTimer -= dt;
        if (noticeTimer <= 0f) notice.text = "";
    }

    Vector2 QueuePoint(int index)
    {
        // Fold the queue into rows so larger crowds stay on screen.
        return new Vector2(-155f + (index % 6) * 105f, -25f - (index / 6) * 105f);
    }
    Vector2 WanderPoint() => new Vector2(Random.Range(-470f, 470f), Random.Range(145f, 245f));

    void Spawn()
    {
        CustomerView view = Instantiate(customerPrefab, field);
        var root = (RectTransform)view.transform;
        root.anchoredPosition = new Vector2(Random.value > .5f ? -580f : 580f, 220f);
        view.body.color = Color.HSVToRGB(Random.value, .4f, .8f);
        Text label = view.patienceLabel;
        Image bar = view.patienceBar;
        label.text = "#" + (nextId + 1) + "  " + Mathf.Max(1f, patienceSeconds).ToString("0.0") + "s";
        customers.Add(new Customer { root = root, bar = bar, label = label, state = State.Wandering,
            destination = WanderPoint(), wander = Random.Range(1.2f, 2.6f), patience = Mathf.Max(1f, patienceSeconds), id = ++nextId });
    }

    void Leave(Customer c, bool success)
    {
        queue.Remove(c);
        c.state = State.Leaving;
        c.destination = new Vector2(620f, c.root.anchoredPosition.y);
        c.label.text = success ? "+1 GOLD" : "LEFT";
        c.bar.fillAmount = 0f;
        c.label.color = success ? Teal : new Color(.8f, .3f, .25f);
    }

}
