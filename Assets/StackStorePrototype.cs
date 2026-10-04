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
    [Header("Passing traffic")]
    [Range(0f, 1f)] public float visitChance = .4f;
    [Min(0f)] public float verticalWanderDistance = 65f;
    [Min(0f)] public float avoidancePadding = 14f;

    [System.Serializable]
    public class ServiceEquipment
    {
        public string displayName = "Basic stall";
        [Min(0.1f)] public float intervalSeconds = 5f;
    }

    enum State { Passing, Wandering, Queued, Leaving }
    class Customer
    {
        public RectTransform root;
        public Image bar;
        public Text label;
        public State state;
        public Vector2 destination;
        public float wander, patience;
        public int id;
        public int direction;
        public float targetY, turnTimer;
        public bool avoiding;
    }

    readonly List<Customer> customers = new List<Customer>();
    readonly List<Customer> queue = new List<Customer>();
    [Header("Scene UI references")]
    public RectTransform field;
    public RectTransform stall;
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
        if (!field || !stall || !cooldownBar || !goldLabel || !cooldownLabel || !statsLabel || !notice || !customerPrefab)
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
            if (c.state == State.Wandering || c.state == State.Queued)
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
            }
            if (c.state == State.Queued)
                c.destination = QueuePoint(queue.IndexOf(c));
            if (c.state == State.Passing || c.state == State.Wandering)
                MoveTraffic(c, dt);
            else
                c.root.anchoredPosition = Vector2.MoveTowards(c.root.anchoredPosition, c.destination, walkSpeed * dt);
            if (c.state == State.Passing && c.direction * c.root.anchoredPosition.x >= ExitX)
            {
                Destroy(c.root.gameObject);
                customers.RemoveAt(i);
                continue;
            }
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
        return stall.anchoredPosition + new Vector2(170f + (index % 4) * 105f, -(index / 4) * 85f);
    }

    // Include the customer's head, label and patience bar inside the visible floor.
    float MinY => field.rect.yMin + 65f;
    float MaxY => field.rect.yMax - 75f;
    float SpawnX => field.rect.width * .5f - 50f;
    float ExitX => field.rect.width * .5f + 90f;

    Rect AvoidanceRect()
    {
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(field, stall);
        Vector2 margin = new Vector2(50f + avoidancePadding, 55f + avoidancePadding);
        return Rect.MinMaxRect(bounds.min.x - margin.x, bounds.min.y - margin.y,
            bounds.max.x + margin.x, bounds.max.y + margin.y);
    }

    void MoveTraffic(Customer c, float dt)
    {
        Vector2 p = c.root.anchoredPosition;
        Rect obstacle = AvoidanceRect();
        bool hasPassed = c.direction > 0 ? p.x > obstacle.xMax : p.x < obstacle.xMin;
        float distanceToEntry = c.direction > 0 ? obstacle.xMin - p.x : p.x - obstacle.xMax;
        // Start steering well before reaching the stall; keep the chosen side until past it.
        bool crossesStallY = (p.y > obstacle.yMin && p.y < obstacle.yMax) ||
            (c.targetY > obstacle.yMin && c.targetY < obstacle.yMax);
        if (!hasPassed && distanceToEntry < 300f && (c.avoiding || crossesStallY))
        {
            if (!c.avoiding)
            {
                float lower = Mathf.Clamp(obstacle.yMin - 5f, MinY, MaxY);
                float upper = Mathf.Clamp(obstacle.yMax + 5f, MinY, MaxY);
                c.targetY = Mathf.Abs(p.y - lower) < Mathf.Abs(p.y - upper) ? lower : upper;
                c.avoiding = true;
            }
        }
        else
        {
            c.avoiding = false;
            c.turnTimer -= dt;
            if (c.turnTimer <= 0f)
            {
                c.targetY = Mathf.Clamp(p.y + Random.Range(-verticalWanderDistance, verticalWanderDistance), MinY, MaxY);
                c.turnTimer = Random.Range(.8f, 1.8f);
            }
        }
        float y = Mathf.MoveTowards(p.y, c.targetY, walkSpeed * .65f * dt);
        float x = p.x + c.direction * walkSpeed * dt;
        // A large frame step must not sweep through the stall before vertical clearance.
        if (!hasPassed && y > obstacle.yMin && y < obstacle.yMax)
        {
            if (c.direction > 0 && p.x <= obstacle.xMin && x > obstacle.xMin) x = obstacle.xMin;
            if (c.direction < 0 && p.x >= obstacle.xMax && x < obstacle.xMax) x = obstacle.xMax;
        }
        c.root.anchoredPosition = new Vector2(x, Mathf.Clamp(y, MinY, MaxY));
    }

    void Spawn()
    {
        CustomerView view = Instantiate(customerPrefab, field);
        var root = (RectTransform)view.transform;
        int direction = Random.value > .5f ? 1 : -1;
        float y = Random.Range(MinY, MaxY);
        bool interested = Random.value < visitChance;
        root.anchoredPosition = new Vector2(-direction * SpawnX, y);
        view.body.color = Color.HSVToRGB(Random.value, .4f, .8f);
        Text label = view.patienceLabel;
        Image bar = view.patienceBar;
        label.text = interested ? "#" + (nextId + 1) + "  " + Mathf.Max(1f, patienceSeconds).ToString("0.0") + "s" : "PASSERBY";
        bar.transform.parent.gameObject.SetActive(interested);
        customers.Add(new Customer { root = root, bar = bar, label = label, state = interested ? State.Wandering : State.Passing,
            direction = direction, targetY = y, turnTimer = Random.Range(.5f, 1.5f),
            wander = Random.Range(1.2f, 2.6f), patience = Mathf.Max(1f, patienceSeconds), id = ++nextId });
    }

    void Leave(Customer c, bool success)
    {
        queue.Remove(c);
        c.state = State.Leaving;
        c.destination = new Vector2(c.direction * ExitX, c.root.anchoredPosition.y);
        c.label.text = success ? "+1 GOLD" : "LEFT";
        c.bar.fillAmount = 0f;
        c.label.color = success ? Teal : new Color(.8f, .3f, .25f);
    }

}
