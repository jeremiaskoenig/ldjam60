using Godot;

public static class MeshUtil
{
    public static SurfaceTool Begin()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        return st;
    }

    public static void AddTriangle(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
    {
        st.SetNormal(normal);
        if ((b - a).Cross(c - a).Dot(normal) > 0f)
        {
            st.AddVertex(a); st.AddVertex(c); st.AddVertex(b);
        }
        else
        {
            st.AddVertex(a); st.AddVertex(b); st.AddVertex(c);
        }
    }

    public static void AddQuad(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        => AddQuad(st, a, b, c, d, normal, normal, normal, normal);

    public static void AddQuad(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, Vector3 d,
        Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd)
    {
        var face = na + nb + nc + nd;
        if ((b - a).Cross(c - a).Dot(face) > 0f)
        {
            Vertex(st, a, na); Vertex(st, c, nc); Vertex(st, b, nb);
            Vertex(st, a, na); Vertex(st, d, nd); Vertex(st, c, nc);
        }
        else
        {
            Vertex(st, a, na); Vertex(st, b, nb); Vertex(st, c, nc);
            Vertex(st, a, na); Vertex(st, c, nc); Vertex(st, d, nd);
        }
    }

    private static void Vertex(SurfaceTool st, Vector3 p, Vector3 n)
    {
        st.SetNormal(n);
        st.AddVertex(p);
    }
}