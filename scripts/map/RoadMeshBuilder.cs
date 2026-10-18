using Godot;

public static class RoadMeshBuilder
{
    private const int IntersectionSegments = 16;

    public static ArrayMesh BuildRoads(RoadNetwork network, float width, float height)
    {
        var st = BeginSurface();
        var lift = Vector3.Up * height;
        float halfWidth = width / 2f;

        foreach (var edge in network.Edges)
        {
            var a = network.Points[edge.a] + lift;
            var b = network.Points[edge.b] + lift;
            var right = (b - a).Normalized().Cross(Vector3.Up);
            AddStrip(st, a, b, right, 0f, halfWidth);
        }

        foreach (var point in network.Points)
            AddDisc(st, point + lift, halfWidth);

        return st.Commit();
    }

    public static ArrayMesh BuildMarkings(RoadNetwork network, float roadWidth, float lineWidth,
        float edgeInset, float dashLength, float dashGap, float height)
    {
        var st = BeginSurface();
        var lift = Vector3.Up * height;
        float halfLine = lineWidth / 2f;
        float edgeOffset = roadWidth / 2f - edgeInset - halfLine;
        float gap = roadWidth;
        float period = dashLength + dashGap;

        foreach (var edge in network.Edges)
        {
            var a = network.Points[edge.a] + lift;
            var b = network.Points[edge.b] + lift;
            var dir = (b - a).Normalized();
            var right = dir.Cross(Vector3.Up);
            float length = a.DistanceTo(b);

            if (length <= gap * 2f)
                continue;

            var start = a + dir * gap;
            var end = b - dir * gap;
            float markLength = length - gap * 2f;

            AddStrip(st, start, end, right, edgeOffset, halfLine);
            AddStrip(st, start, end, right, -edgeOffset, halfLine);

            float startOffset = ((markLength - dashLength) % period) / 2f;
            for (float d = startOffset; d + dashLength <= markLength; d += period)
            {
                var dashStart = start + dir * d;
                AddStrip(st, dashStart, dashStart + dir * dashLength, right, 0f, halfLine);
            }
        }

        return st.Commit();
    }

    private static SurfaceTool BeginSurface()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        st.SetNormal(Vector3.Up);
        return st;
    }

    private static void AddStrip(SurfaceTool st, Vector3 from, Vector3 to, Vector3 right, float offset, float halfWidth)
    {
        var shift = right * offset;
        var side = right * halfWidth;

        var fromLeft = from + shift - side;
        var fromRight = from + shift + side;
        var toLeft = to + shift - side;
        var toRight = to + shift + side;

        st.AddVertex(toLeft);
        st.AddVertex(toRight);
        st.AddVertex(fromRight);

        st.AddVertex(toLeft);
        st.AddVertex(fromRight);
        st.AddVertex(fromLeft);
    }

    private static void AddDisc(SurfaceTool st, Vector3 center, float radius)
    {
        for (int i = 0; i < IntersectionSegments; i++)
        {
            float angle0 = Mathf.Tau * i / IntersectionSegments;
            float angle1 = Mathf.Tau * (i + 1) / IntersectionSegments;

            st.AddVertex(center);
            st.AddVertex(center + new Vector3(Mathf.Cos(angle0), 0, Mathf.Sin(angle0)) * radius);
            st.AddVertex(center + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius);
        }
    }

    public static ArrayMesh BuildSidewalks(RoadNetwork network, BiomeMap biomes,
        float roadWidth, float sidewalkWidth, float height, float segmentLength = 10f)
    {
        var st = BeginSurface();
        bool hasGeometry = false;

        float inner = roadWidth / 2f;
        float outer = inner + sidewalkWidth;
        float center = (inner + outer) / 2f;
        var up = Vector3.Up * height;

        foreach (var edge in network.Edges)
        {
            var a = network.Points[edge.a];
            var b = network.Points[edge.b];
            var dir = (b - a).Normalized();
            var right = dir.Cross(Vector3.Up);
            float length = a.DistanceTo(b);

            for (int side = -1; side <= 1; side += 2)
            {
                float gapStart = network.CornerClearance(edge.a, edge.b, side, outer);
                float gapEnd = network.CornerClearance(edge.b, edge.a, -side, outer);
                float usable = length - gapStart - gapEnd;

                if (usable <= 1f)
                    continue;

                var start = a + dir * gapStart;
                int pieces = Mathf.Max(1, Mathf.CeilToInt(usable / segmentLength));
                float pieceLength = usable / pieces;

                var inCity = new bool[pieces];
                for (int i = 0; i < pieces; i++)
                {
                    var middle = start + dir * ((i + 0.5f) * pieceLength) + right * center * side;
                    inCity[i] = biomes.GetBiomeAt(middle) == Biome.City;
                }

                for (int i = 0; i < pieces; i++)
                {
                    if (!inCity[i])
                        continue;

                    var from = start + dir * (i * pieceLength);
                    var to = from + dir * pieceLength;

                    st.SetNormal(Vector3.Up);
                    AddStrip(st, from + up, to + up, right, center * side, sidewalkWidth / 2f);

                    AddVerticalQuad(st, from + right * inner * side, to + right * inner * side, 0f, height, -right * side);
                    AddVerticalQuad(st, from + right * outer * side, to + right * outer * side, 0f, height, right * side);

                    if (i == 0 || !inCity[i - 1])
                        AddVerticalQuad(st, from + right * inner * side, from + right * outer * side, 0f, height, -dir);

                    if (i == pieces - 1 || !inCity[i + 1])
                        AddVerticalQuad(st, to + right * inner * side, to + right * outer * side, 0f, height, dir);

                    hasGeometry = true;
                }
            }
        }

        return hasGeometry ? st.Commit() : null;
    }

    private static void AddVerticalQuad(SurfaceTool st, Vector3 p0, Vector3 p1, float bottom, float top, Vector3 normal)
    {
        var b0 = new Vector3(p0.X, bottom, p0.Z);
        var b1 = new Vector3(p1.X, bottom, p1.Z);
        var t0 = new Vector3(p0.X, top, p0.Z);
        var t1 = new Vector3(p1.X, top, p1.Z);

        st.SetNormal(normal);

        if ((b1 - b0).Cross(t1 - b0).Dot(normal) > 0f)
        {
            st.AddVertex(b0); st.AddVertex(t1); st.AddVertex(b1);
            st.AddVertex(b0); st.AddVertex(t0); st.AddVertex(t1);
        }
        else
        {
            st.AddVertex(b0); st.AddVertex(b1); st.AddVertex(t1);
            st.AddVertex(b0); st.AddVertex(t1); st.AddVertex(t0);
        }
    }
}