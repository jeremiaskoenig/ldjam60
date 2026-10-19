using Godot;
using System;

public static class TrackMeshBuilder
{
    public static ArrayMesh BuildRoad(Track track, float width, float height)
    {
        var st = MeshUtil.Begin();
        AddRibbon(st, track, -width / 2f, width / 2f, height);
        return st.Commit();
    }

    public static ArrayMesh BuildMarkings(Track track, float roadWidth, float lineWidth,
        float edgeInset, float dashLength, float dashGap, float height)
    {
        var st = MeshUtil.Begin();
        float halfLine = lineWidth / 2f;
        float edge = roadWidth / 2f - edgeInset - halfLine;

        AddRibbon(st, track, edge - halfLine, edge + halfLine, height);
        AddRibbon(st, track, -edge - halfLine, -edge + halfLine, height);

        var up = Vector3.Up * height;
        for (float s = 0f; s + dashLength < track.Length; s += dashLength + dashGap)
        {
            var from = track.SampleAt(s, out var tangent) + up;
            var to = track.SampleAt(s + dashLength, out _) + up;
            var side = tangent.Cross(Vector3.Up).Normalized() * halfLine;
            MeshUtil.AddQuad(st, from - side, from + side, to + side, to - side, Vector3.Up);
        }

        return st.Commit();
    }

    public static ArrayMesh BuildStartLine(Track track, float progress, float roadWidth, int columns, float height)
    {
        var st = MeshUtil.Begin();
        var center = track.SampleAt(progress * track.Length, out var tangent) + Vector3.Up * height;
        var right = tangent.Cross(Vector3.Up).Normalized();
        float cell = roadWidth / columns;
        const int rows = 2;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                st.SetColor((r + c) % 2 == 0 ? Colors.White : new Color(0.05f, 0.05f, 0.05f));
                var o = center + right * (-roadWidth / 2f + c * cell) + tangent * (r * cell - rows * cell / 2f);
                MeshUtil.AddQuad(st, o, o + right * cell, o + right * cell + tangent * cell, o + tangent * cell, Vector3.Up);
            }
        }

        return st.Commit();
    }

    public static ArrayMesh BuildSidewalks(Track track, Func<Vector3, bool> allowed,
        float roadWidth, float sidewalkWidth, float height)
    {
        var st = MeshUtil.Begin();
        bool any = false;
        int n = track.Count;
        float inner = roadWidth / 2f;
        float outer = inner + sidewalkWidth;
        float center = (inner + outer) / 2f;
        var up = Vector3.Up * height;

        for (int side = -1; side <= 1; side += 2)
        {
            var mask = new bool[n];
            for (int i = 0; i < n; i++)
                mask[i] = allowed(track.Points[i] + track.Right(i) * center * side);

            for (int i = 0; i < n; i++)
            {
                if (!mask[i])
                    continue;

                int j = (i + 1) % n;
                var pi = track.Points[i];
                var pj = track.Points[j];
                var ri = track.Right(i) * side;
                var rj = track.Right(j) * side;

                MeshUtil.AddQuad(st, pi + ri * inner + up, pi + ri * outer + up,
                    pj + rj * outer + up, pj + rj * inner + up, Vector3.Up);

                MeshUtil.AddQuad(st, pi + ri * inner, pj + rj * inner,
                    pj + rj * inner + up, pi + ri * inner + up, -ri);

                MeshUtil.AddQuad(st, pi + ri * outer, pj + rj * outer,
                    pj + rj * outer + up, pi + ri * outer + up, ri);

                if (!mask[(i - 1 + n) % n])
                    MeshUtil.AddQuad(st, pi + ri * inner, pi + ri * outer,
                        pi + ri * outer + up, pi + ri * inner + up, -track.Tangent(i));

                if (!mask[j])
                    MeshUtil.AddQuad(st, pj + rj * inner, pj + rj * outer,
                        pj + rj * outer + up, pj + rj * inner + up, track.Tangent(j));

                any = true;
            }
        }

        return any ? st.Commit() : null;
    }

    private static void AddRibbon(SurfaceTool st, Track track, float offsetA, float offsetB, float height)
    {
        int n = track.Count;
        var up = Vector3.Up * height;

        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            var pi = track.Points[i] + up;
            var pj = track.Points[j] + up;
            var ri = track.Right(i);
            var rj = track.Right(j);

            MeshUtil.AddQuad(st, pi + ri * offsetA, pi + ri * offsetB, pj + rj * offsetB, pj + rj * offsetA, Vector3.Up);
        }
    }
}