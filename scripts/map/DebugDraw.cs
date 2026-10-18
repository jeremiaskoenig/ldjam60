using Godot;

public static class DebugDraw
{
    public static Node3D CreateNodeMarkers(RoadNetwork network, BiomeMap biomes)
    {
        var root = new Node3D() { Name = "DebugNodes" };

        foreach (var point in network.Points)
        {
            root.AddChild(new MeshInstance3D()
            {
                Mesh = new SphereMesh() { Radius = 4, Height = 8 },
                Position = point,
                MaterialOverride = new StandardMaterial3D()
                {
                    AlbedoColor = biomes.GetBiomeAt(point).DebugColor(),
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
                }
            });
        }

        return root;
    }

    public static MeshInstance3D CreateEdgeLines(RoadNetwork network)
    {
        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

        foreach (var edge in network.Edges)
        {
            mesh.SurfaceAddVertex(network.Points[edge.a] + Vector3.Up * 0.5f);
            mesh.SurfaceAddVertex(network.Points[edge.b] + Vector3.Up * 0.5f);
        }

        mesh.SurfaceEnd();

        return new MeshInstance3D()
        {
            Name = "DebugEdges",
            Mesh = mesh,
            MaterialOverride = new StandardMaterial3D()
            {
                AlbedoColor = Colors.Orange,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
            }
        };
    }
}