using System.Collections.Generic;
using UnityEngine;

/// <summary>Reveal the prospective ground cell without changing materials or ownership.</summary>
public sealed class HexPlacementVisibility
{
    readonly Dictionary<SpriteRenderer, Color> originals = new Dictionary<SpriteRenderer, Color>();
    readonly List<Vector2> footprint = new List<Vector2>();
    public void Restore()
    {
        foreach (var pair in originals) if (pair.Key) pair.Key.color = pair.Value;
        originals.Clear();
    }
    public void Update(HexTileView preview)
    {
        Restore();
        var camera = Camera.main;
        if (!camera || !preview || !preview.gameObject.activeInHierarchy || !preview.outline) return;
        footprint.Clear();
        var line = preview.outline;
        for (int i = 0; i < line.positionCount - 1; i++)
        {
            Vector3 world = line.useWorldSpace ? line.GetPosition(i) : line.transform.TransformPoint(line.GetPosition(i));
            Vector3 point = camera.WorldToScreenPoint(world);
            if (point.z <= 0) return;
            footprint.Add(point);
        }
        if (footprint.Count < 3) return;
        // Refresh projection even while placement has paused simulation time.
        foreach (var facing in Object.FindObjectsByType<HexCameraFacingSprite>(FindObjectsSortMode.None)) facing.FaceCamera();
        foreach (var sprite in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (!sprite.enabled || !sprite.sprite || sprite.transform.IsChildOf(preview.transform)) continue;
            var bounds = sprite.sprite.bounds;
            var quad = new Vector2[4];
            bool visible = true;
            for (int i = 0; i < 4; i++)
            {
                Vector3 local = new Vector3((i == 0 || i == 3) ? bounds.min.x : bounds.max.x, i < 2 ? bounds.min.y : bounds.max.y, 0);
                Vector3 point = camera.WorldToScreenPoint(sprite.transform.TransformPoint(local));
                if (point.z <= 0) { visible = false; break; }
                quad[i] = point;
            }
            if (!visible || !Overlaps(footprint, quad)) continue;
            Color color = sprite.color;
            originals.Add(sprite, color);
            color.a *= .35f;
            sprite.color = color;
        }
    }
    static Rect Bounds(IList<Vector2> polygon)
    {
        Vector2 min = polygon[0], max = polygon[0];
        foreach (var point in polygon) { min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    static bool Overlaps(IList<Vector2> a, IList<Vector2> b)
    {
        Rect tile = Bounds(a), resource = Bounds(b);
        // At the horizontal camera limit the ground projects to a line. Keep the
        // footprint's horizontal extent and a small pixel tolerance in that case.
        if (tile.height < 2 || tile.width < 2)
            return Rect.MinMaxRect(tile.xMin - 2, tile.yMin - 2, tile.xMax + 2, tile.yMax + 2).Overlaps(resource);
        return tile.Overlaps(resource) && !Separated(a, b) && !Separated(b, a);
    }
    static bool Separated(IList<Vector2> a, IList<Vector2> b)
    {
        for (int i = 0; i < a.Count; i++)
        {
            Vector2 edge = a[(i + 1) % a.Count] - a[i];
            if (edge.sqrMagnitude < .0001f) continue;
            Vector2 axis = new Vector2(-edge.y, edge.x);
            float amin = float.PositiveInfinity, amax = float.NegativeInfinity;
            float bmin = float.PositiveInfinity, bmax = float.NegativeInfinity;
            foreach (var p in a) { float d = Vector2.Dot(p, axis); amin = Mathf.Min(amin, d); amax = Mathf.Max(amax, d); }
            foreach (var p in b) { float d = Vector2.Dot(p, axis); bmin = Mathf.Min(bmin, d); bmax = Mathf.Max(bmax, d); }
            if (amax < bmin || bmax < amin) return true;
        }
        return false;
    }
}
