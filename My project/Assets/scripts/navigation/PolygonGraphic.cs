using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PolygonGraphic : Graphic
{
    private const float GeometryEpsilon = 0.001f;

    private readonly List<Vector2> _points = new List<Vector2>();

    public void SetPolygon(IList<Vector2> points, Color fill)
    {
        color = fill;
        _points.Clear();
        if (points != null)
        {
            _points.AddRange(points);
        }

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        var points = BuildMeshPoints(_points);
        if (points.Count < 3)
        {
            return;
        }

        for (int i = 0; i < points.Count; i++)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = points[i];
            vertex.color = color;
            vh.AddVert(vertex);
        }

        var triangles = Triangulate(points);
        if (triangles.Count == 0)
        {
            AddTriangleFan(vh, points);
            return;
        }

        for (var i = 0; i + 2 < triangles.Count; i += 3)
        {
            vh.AddTriangle(triangles[i], triangles[i + 1], triangles[i + 2]);
        }
    }

    private static List<Vector2> BuildMeshPoints(IReadOnlyList<Vector2> source)
    {
        var points = new List<Vector2>();
        if (source == null)
        {
            return points;
        }

        for (var i = 0; i < source.Count; i++)
        {
            var point = source[i];
            if (points.Count == 0 || (points[points.Count - 1] - point).sqrMagnitude > GeometryEpsilon * GeometryEpsilon)
            {
                points.Add(point);
            }
        }

        if (points.Count > 1 && (points[0] - points[points.Count - 1]).sqrMagnitude <= GeometryEpsilon * GeometryEpsilon)
        {
            points.RemoveAt(points.Count - 1);
        }

        RemoveCollinearPoints(points);
        return points;
    }

    private static void RemoveCollinearPoints(List<Vector2> points)
    {
        if (points == null || points.Count < 3)
        {
            return;
        }

        var removed = true;
        while (removed && points.Count >= 3)
        {
            removed = false;
            for (var i = points.Count - 1; i >= 0; i--)
            {
                var previous = points[(i - 1 + points.Count) % points.Count];
                var current = points[i];
                var next = points[(i + 1) % points.Count];

                if (Mathf.Abs(Cross(previous, current, next)) <= GeometryEpsilon)
                {
                    points.RemoveAt(i);
                    removed = true;
                }
            }
        }
    }

    private static List<int> Triangulate(IReadOnlyList<Vector2> points)
    {
        var triangles = new List<int>();
        if (points == null || points.Count < 3)
        {
            return triangles;
        }

        var isCounterClockwise = SignedArea(points) > 0f;
        var remaining = new List<int>(points.Count);
        for (var i = 0; i < points.Count; i++)
        {
            remaining.Add(i);
        }

        var guard = points.Count * points.Count;
        while (remaining.Count > 3 && guard-- > 0)
        {
            var earFound = false;
            for (var i = 0; i < remaining.Count; i++)
            {
                var previousIndex = remaining[(i - 1 + remaining.Count) % remaining.Count];
                var currentIndex = remaining[i];
                var nextIndex = remaining[(i + 1) % remaining.Count];

                if (!IsConvex(points[previousIndex], points[currentIndex], points[nextIndex], isCounterClockwise))
                {
                    continue;
                }

                if (ContainsPointInTriangle(points, remaining, previousIndex, currentIndex, nextIndex))
                {
                    continue;
                }

                AddTriangle(triangles, previousIndex, currentIndex, nextIndex, isCounterClockwise);
                remaining.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound)
            {
                triangles.Clear();
                return triangles;
            }
        }

        if (remaining.Count == 3)
        {
            AddTriangle(triangles, remaining[0], remaining[1], remaining[2], isCounterClockwise);
        }

        return triangles;
    }

    private static void AddTriangle(List<int> triangles, int a, int b, int c, bool isCounterClockwise)
    {
        if (isCounterClockwise)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }
        else
        {
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(b);
        }
    }

    private static bool IsConvex(Vector2 previous, Vector2 current, Vector2 next, bool isCounterClockwise)
    {
        var cross = Cross(previous, current, next);
        return isCounterClockwise ? cross > GeometryEpsilon : cross < -GeometryEpsilon;
    }

    private static bool ContainsPointInTriangle(
        IReadOnlyList<Vector2> points,
        IReadOnlyList<int> remaining,
        int aIndex,
        int bIndex,
        int cIndex)
    {
        var a = points[aIndex];
        var b = points[bIndex];
        var c = points[cIndex];

        for (var i = 0; i < remaining.Count; i++)
        {
            var pointIndex = remaining[i];
            if (pointIndex == aIndex || pointIndex == bIndex || pointIndex == cIndex)
            {
                continue;
            }

            if (IsPointInTriangle(points[pointIndex], a, b, c))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        var area = Cross(a, b, c);
        if (Mathf.Abs(area) <= GeometryEpsilon)
        {
            return false;
        }

        var sign = area > 0f ? 1f : -1f;
        var edgeA = Cross(a, b, point) * sign;
        var edgeB = Cross(b, c, point) * sign;
        var edgeC = Cross(c, a, point) * sign;

        return edgeA >= -GeometryEpsilon &&
               edgeB >= -GeometryEpsilon &&
               edgeC >= -GeometryEpsilon;
    }

    private static float SignedArea(IReadOnlyList<Vector2> points)
    {
        var area = 0f;
        for (var i = 0; i < points.Count; i++)
        {
            var current = points[i];
            var next = points[(i + 1) % points.Count];
            area += current.x * next.y - next.x * current.y;
        }

        return area * 0.5f;
    }

    private static float Cross(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    private static void AddTriangleFan(VertexHelper vh, IReadOnlyList<Vector2> points)
    {
        for (var i = 1; i < points.Count - 1; i++)
        {
            vh.AddTriangle(0, i, i + 1);
        }
    }
}
