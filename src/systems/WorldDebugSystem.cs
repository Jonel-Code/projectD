using System.Collections.Generic;
using Godot;
using WorldUtils;


public struct WorldDebugData
{
	public Node3D Node;
	public double Lifetime;
}

public partial class WorldDebugSystem : Node
{
	public static WorldDebugSystem Instance { get; private set; }

	protected List<WorldDebugData> DebugNodes = new List<WorldDebugData>();


	public override void _Ready()
	{
		Instance = this;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		UpdateDebugLifetime(delta);
	}

	public void UpdateDebugLifetime(double delta)
	{
		if (DebugNodes.Count == 0)
		{
			return;
		}
		/// TODO: move this to worker since this should be none-blocking
		for (int i = DebugNodes.Count - 1; i >= 0; i--)
		{

			if (DebugNodes[i].Lifetime <= 0)
			{
				DebugNodes[i].Node.QueueFree();
				DebugNodes.RemoveAt(i);
				continue;
			}

			DebugNodes[i] = DebugNodes[i] with { Lifetime = DebugNodes[i].Lifetime - delta };
		}
	}

	public void DebugLine(Vector3 start, Vector3 end, Color color, double lifetime = 0.01)
	{
		var meshInstance = new MeshInstance3D();
		var immediateMesh = new ImmediateMesh();
		var material = new OrmMaterial3D();
		meshInstance.Mesh = immediateMesh;
		meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Lines, material);
		immediateMesh.SurfaceAddVertex(start);
		immediateMesh.SurfaceAddVertex(end);
		immediateMesh.SurfaceEnd();
		material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		material.AlbedoColor = color;
		GetTree().Root.AddChild(meshInstance);
		DebugNodes.Add(new WorldDebugData { Node = meshInstance, Lifetime = lifetime });
	}

	public void DebugCapsule(Transform3D transform, float radius, float height, Color color, double lifetime = 0.01)
	{
		var meshInstance = new MeshInstance3D();
		var capsuleMesh = new CapsuleMesh();
		var material = new OrmMaterial3D();
		meshInstance.Mesh = capsuleMesh;
		meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		material.AlbedoColor = color;
		meshInstance.Transform = transform;
		capsuleMesh.Radius = radius;
		capsuleMesh.Height = height;
		GetTree().Root.AddChild(meshInstance);
		DebugNodes.Add(new WorldDebugData { Node = meshInstance, Lifetime = lifetime });
	}

	public void DebugCapsule(Vector3 feet, Vector3 head, float radius, Color color, double lifetime = 0.01)
	{
		var direction = (head - feet).Normalized();
		var height = feet.DistanceTo(head);
		var capsuleTransform = new Transform3D
		{
			Origin = feet + (direction * height * 0.5f),
		};
		capsuleTransform = capsuleTransform.PointYTowards(direction);
		DebugCapsule(capsuleTransform, radius, height, color, lifetime);
	}

	public void DebugSphere(Vector3 Position, float radius, Color color, double lifetime = 0.01)
	{
		var meshInstance = new MeshInstance3D();
		var sphereMesh = new SphereMesh();
		var material = new OrmMaterial3D();
		meshInstance.Mesh = sphereMesh;
		meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		material.AlbedoColor = color;
		sphereMesh.Radius = radius;
		sphereMesh.Height = radius * 2;
		GetTree().Root.AddChild(meshInstance);
		meshInstance.GlobalPosition = Position;
		DebugNodes.Add(new WorldDebugData { Node = meshInstance, Lifetime = lifetime });
	}
}
