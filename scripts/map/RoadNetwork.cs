using Godot;
using System.Collections.Generic;

public class RoadNetwork
{
    public List<Vector3> Points { get; } = new();
    public List<(int a, int b)> Edges { get; } = new();
    public int CandidateCount { get; private set; }

    private readonly int _gridSize;
    private readonly float _cellSize;
    private readonly RandomNumberGenerator _rng;
    private List<int>[] _neighbors;

    public RoadNetwork(int gridSize, float cellSize, RandomNumberGenerator rng)
    {
        _gridSize = gridSize;
        _cellSize = cellSize;
        _rng = rng;
    }

    public void Generate(float diagonalChance, float extraEdgeChance)
    {
        GenerateNodes();
        var candidates = GenerateCandidates(diagonalChance);
        CandidateCount = candidates.Count;
        Shuffle(candidates);
        BuildNetwork(candidates, extraEdgeChance);
        BuildAdjacency();
    }

    private void GenerateNodes()
    {
        float margin = _cellSize * 0.2f;

        for (int x = 0; x < _gridSize; x++)
        {
            for (int z = 0; z < _gridSize; z++)
            {
                var corner = new Vector3(x * _cellSize, 0, z * _cellSize);
                var offsetX = _rng.RandfRange(margin, _cellSize - margin);
                var offsetZ = _rng.RandfRange(margin, _cellSize - margin);
                Points.Add(corner + new Vector3(offsetX, 0, offsetZ));
            }
        }
    }

    private List<(int a, int b)> GenerateCandidates(float diagonalChance)
    {
        var candidates = new List<(int a, int b)>();

        for (int x = 0; x < _gridSize; x++)
        {
            for (int z = 0; z < _gridSize; z++)
            {
                int current = GetIndex(x, z);

                if (x < _gridSize - 1)
                    candidates.Add((current, GetIndex(x + 1, z)));

                if (z < _gridSize - 1)
                    candidates.Add((current, GetIndex(x, z + 1)));

                if (x < _gridSize - 1 && z < _gridSize - 1 && _rng.Randf() < diagonalChance)
                {
                    if (_rng.Randf() < 0.5f)
                        candidates.Add((current, GetIndex(x + 1, z + 1)));
                    else
                        candidates.Add((GetIndex(x + 1, z), GetIndex(x, z + 1)));
                }
            }
        }

        return candidates;
    }

    private void Shuffle(List<(int a, int b)> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.RandiRange(0, i);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void BuildNetwork(List<(int a, int b)> candidates, float extraEdgeChance)
    {
        var parent = new int[Points.Count];
        for (int i = 0; i < parent.Length; i++)
            parent[i] = i;

        var rejected = new List<(int a, int b)>();

        foreach (var edge in candidates)
        {
            int rootA = Find(parent, edge.a);
            int rootB = Find(parent, edge.b);

            if (rootA != rootB)
            {
                parent[rootA] = rootB;
                Edges.Add(edge);
            }
            else
            {
                rejected.Add(edge);
            }
        }

        foreach (var edge in rejected)
        {
            if (_rng.Randf() < extraEdgeChance)
                Edges.Add(edge);
        }
    }

    private static int Find(int[] parent, int i)
    {
        while (parent[i] != i)
        {
            parent[i] = parent[parent[i]];
            i = parent[i];
        }
        return i;
    }

    private int GetIndex(int x, int z) => x * _gridSize + z;

    public Vector3 ClosestPointOnRoad(Vector3 position, out float distance)
    {
        var p = new Vector2(position.X, position.Z);
        var bestPoint = Vector2.Zero;
        float bestDist = float.MaxValue;

        foreach (var edge in Edges)
        {
            var a = new Vector2(Points[edge.a].X, Points[edge.a].Z);
            var b = new Vector2(Points[edge.b].X, Points[edge.b].Z);
            var closest = ClosestOnSegment(p, a, b);
            float dist = p.DistanceTo(closest);

            if (dist < bestDist)
            {
                bestDist = dist;
                bestPoint = closest;
            }
        }

        distance = bestDist;
        return new Vector3(bestPoint.X, 0, bestPoint.Y);
    }

    private static Vector2 ClosestOnSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float t = Mathf.Clamp((p - a).Dot(ab) / ab.LengthSquared(), 0f, 1f);
        return a + ab * t;
    }

    private void BuildAdjacency()
    {
        _neighbors = new List<int>[Points.Count];
        for (int i = 0; i < Points.Count; i++)
            _neighbors[i] = new List<int>();

        foreach (var edge in Edges)
        {
            _neighbors[edge.a].Add(edge.b);
            _neighbors[edge.b].Add(edge.a);
        }
    }

    public float CornerClearance(int node, int other, int side, float halfBand)
    {
        var p = Points[node];
        var d = Points[other] - p;
        var dir = new Vector2(d.X, d.Z).Normalized();
        var right = new Vector2(-dir.Y, dir.X);

        float smallest = Mathf.Tau;
        foreach (int n in _neighbors[node])
        {
            if (n == other)
                continue;

            var w = Points[n] - p;
            var v = new Vector2(w.X, w.Z).Normalized();
            float angle = Mathf.Atan2(v.Dot(right) * side, v.Dot(dir));
            if (angle <= 0f)
                angle += Mathf.Tau;

            smallest = Mathf.Min(smallest, angle);
        }

        if (smallest >= Mathf.Pi)
            return 0f;

        return halfBand / Mathf.Tan(smallest / 2f);
    }
}