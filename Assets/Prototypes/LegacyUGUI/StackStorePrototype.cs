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
    [Min(0.2f)] public float spawnSeconds = 2.3f / 3f;
    [Min(10)] public float walkSpeed = 160f;
    public int maxCustomers = 48;
    [Header("Passing traffic")]
    [Range(0f, 1f)] public float visitChance = .4f;
    [Min(0f)] public float verticalWanderDistance = 65f;
    [Min(0f)] public float avoidancePadding = 14f;
    [Header("Browsing around the shop")]
    [Min(0f)] public float minBrowseSeconds = 3f;
    [Min(0f)] public float maxBrowseSeconds = 6f;
    [Range(0f, 1f)] public float returnToOriginChance = .5f;

    [System.Serializable]
    public class ServiceEquipment
    {
        public string displayName = "Basic stall";
        [Min(0.1f)] public float intervalSeconds = 5f;
    }

    enum State { Browsing, Passing, Wandering, Queued, Leaving }
    class Customer
    {
        public RectTransform root;
        public Image bar;
        public Text label;
        public State state;
        public Vector2 destination;
        public float wander, patience, initialPatience;
        public int id;
        public int direction;
        public float targetY, turnTimer;
        public bool avoiding;
        public int originalDirection;
        public bool reachedShop;
        public string statusKey = "customer.browsing";
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
    float itemSpeedBonus, itemPatienceBonus, itemVisitorBonus;
    public int Gold => gold;
    public float EffectivePatience => Mathf.Max(1f, patienceSeconds + itemPatienceBonus);
    public float EffectiveVisitChance => Mathf.Clamp01(visitChance + itemVisitorBonus);
    public float ServiceInterval => Interval;

    public void SetItemBonuses(float speed, float patience, float visitors)
    {
        float oldInterval = Interval;
        itemSpeedBonus = speed;
        itemPatienceBonus = patience;
        itemVisitorBonus = visitors;
        cooldown *= Interval / oldInterval;
    }

    public void Equip(ServiceEquipment replacement)
    {
        if (replacement == null) return;
        equipment = replacement;
        cooldown = Interval;
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

    float Interval => Mathf.Max(.1f, (equipment != null ? equipment.intervalSeconds : 5f) / Mathf.Max(.1f, 1f + itemSpeedBonus));

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        spawnTimer -= dt;
        while (spawnTimer <= 0f)
        {
            if (customers.Count >= Mathf.Max(1, maxCustomers))
            {
                spawnTimer = Mathf.Max(.2f, spawnSeconds);
                break;
            }
            Spawn();
            spawnTimer += Mathf.Max(.2f, spawnSeconds);
        }
        for (int i = customers.Count - 1; i >= 0; i--)
        {
            Customer c = customers[i];
            if (c.state == State.Wandering || c.state == State.Queued)
            {
                c.patience -= dt;
                c.bar.fillAmount = Mathf.Clamp01(c.patience / c.initialPatience);
                c.bar.color = c.patience < 3f ? new Color(.86f, .35f, .29f) : Teal;
                c.label.text = LocalizationService.Text("customer.patience", "id", c.id, "seconds", Mathf.Max(0, c.patience).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
                if (c.patience <= 0f) { lost++; Leave(c, false); }
            }
            else c.label.text = LocalizationService.Text(c.statusKey);
            if (c.state == State.Wandering)
            {
                c.wander -= dt;
                if (c.wander <= 0f) { c.state = State.Queued; queue.Add(c); }
            }
            if (c.state == State.Queued)
                c.destination = QueuePoint(queue.IndexOf(c));
            if (c.state == State.Browsing)
                MoveBrowsing(c, dt);
            else if (c.state == State.Passing || c.state == State.Wandering)
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
            notice.text = LocalizationService.Text("hud.reward", "gold", 1);
            notice.color = Teal;
            noticeTimer = 1.3f;
        }
        cooldownBar.fillAmount = 1f - Mathf.Clamp01(cooldown / Interval);
        cooldownLabel.text = cooldown > 0f ? LocalizationService.Text("hud.cooldown", "seconds", cooldown.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)) : LocalizationService.Text("hud.ready");
        goldLabel.text = LocalizationService.Text("hud.gold", "gold", gold);
        statsLabel.text = LocalizationService.Text("hud.stats", "served", served, "lost", lost, "queue", queue.Count);
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

    Vector2 BrowsePoint(Customer c)
    {
        Rect obstacle = AvoidanceRect();
        // Stay on the arrival side of the stall, keeping its full visual bounds clear.
        float x = c.originalDirection > 0
            ? Random.Range(Mathf.Max(-SpawnX, obstacle.xMin - 160f), obstacle.xMin - 20f)
            : Random.Range(obstacle.xMax + 20f, Mathf.Min(SpawnX, obstacle.xMax + 160f));
        float y = Random.Range(Mathf.Max(MinY, stall.anchoredPosition.y - 180f),
            Mathf.Min(MaxY, stall.anchoredPosition.y + 180f));
        return new Vector2(x, y);
    }

    void MoveBrowsing(Customer c, float dt)
    {
        c.root.anchoredPosition = Vector2.MoveTowards(c.root.anchoredPosition, c.destination,
            walkSpeed * (c.reachedShop ? .55f : 1f) * dt);
        if (!c.reachedShop)
        {
            if (Vector2.Distance(c.root.anchoredPosition, c.destination) >= 5f) return;
            c.reachedShop = true;
        }
        c.wander -= dt;
        if (c.wander <= 0f)
        {
            c.direction = Random.value < returnToOriginChance ? -c.originalDirection : c.originalDirection;
            c.state = State.Passing;
            c.avoiding = false;
            c.targetY = c.root.anchoredPosition.y;
            c.turnTimer = 0f;
            c.statusKey = c.direction == c.originalDirection ? "customer.continuing" : "customer.returning";
            c.label.text = LocalizationService.Text(c.statusKey);
        }
        else if (Vector2.Distance(c.root.anchoredPosition, c.destination) < 5f)
            c.destination = BrowsePoint(c);
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
        bool interested = Random.value < EffectiveVisitChance;
        root.anchoredPosition = new Vector2(-direction * SpawnX, y);
        view.body.color = Color.HSVToRGB(Random.value, .4f, .8f);
        Text label = view.patienceLabel;
        Image bar = view.patienceBar;
        label.text = interested ? LocalizationService.Text("customer.patience", "id", nextId + 1, "seconds", EffectivePatience.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)) : LocalizationService.Text("customer.browsing");
        bar.transform.parent.gameObject.SetActive(interested);
        var customer = new Customer { root = root, bar = bar, label = label, state = interested ? State.Wandering : State.Browsing,
            direction = direction, targetY = y, turnTimer = Random.Range(.5f, 1.5f),
            originalDirection = direction,
            wander = interested ? Random.Range(1.2f, 2.6f) : Random.Range(Mathf.Max(0, minBrowseSeconds), Mathf.Max(minBrowseSeconds, maxBrowseSeconds)),
            patience = EffectivePatience, initialPatience = EffectivePatience, id = ++nextId };
        if (!interested) customer.destination = BrowsePoint(customer);
        customers.Add(customer);
    }

    void Leave(Customer c, bool success)
    {
        queue.Remove(c);
        c.state = State.Leaving;
        c.destination = new Vector2(c.direction * ExitX, c.root.anchoredPosition.y);
        c.statusKey = success ? "customer.served" : "customer.left";
        c.label.text = LocalizationService.Text(c.statusKey);
        c.bar.fillAmount = 0f;
        c.label.color = success ? Teal : new Color(.8f, .3f, .25f);
    }

}
