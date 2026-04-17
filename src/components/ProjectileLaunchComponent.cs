using System.Collections.Generic;
using System.Runtime.InteropServices;
using GlobalSystems;
using Godot;

public struct ProjectileLaunchData
{
    public Vector3 StartPosition;
    public Vector3 Endposition;
    public double Speed;
    public double AccumulatedDelta;
}


[GlobalClass]
partial class ProjectileLaunchComponent : Node
{
    // protected LifetimeContainer<Node3D> ProjectileContainer = new();

    [Export]
    public Node3D ProjectileOrigin { get; set; } = null;

    protected List<ProjectileLaunchData> Projectiles = new();

    protected Queue<int> ProjectileToRemove = new();

    protected uint AimingMask => 1 << 1;

    public override void _Ready()
    {
        Projectiles.Capacity = 100;
        // GD.Print("gravity: ", Gravity);
        // ProjectileContainer.Reserve(100);
        // ProjectileContainer.UpdateContentDelagate += OnProjectileUpdate;
        // ProjectileContainer.OnRemoveDelegate += OnProjectileRemoved;
    }

    public override void _ExitTree()
    {

    }


    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsActionJustPressed("fire_projectile"))
        {
            if (this.GetMouseSystem().GetMouseWorldPosition(out var hitPosition, out var HitRid, AimingMask))
            {
                this.GetWorldDebugSystem().DebugSphere(hitPosition, .5f, Colors.Green, 5);
                if (ProjectileOrigin != null)
                {
                    var origin = ProjectileOrigin.GlobalPosition;
                    var displacementLength = hitPosition.DistanceTo(origin);
                    var effectiveDistance = 10;
                    /// the further the travel the slower and near it is to 1, the shorted the faster
                    var speed = (3 * displacementLength + effectiveDistance) / displacementLength;
                    Projectiles.Add(new ProjectileLaunchData
                    {
                        StartPosition = origin,
                        Endposition = hitPosition,
                        Speed = speed,
                        AccumulatedDelta = 0
                    });
                }
            }
        }

        var span = CollectionsMarshal.AsSpan(Projectiles);
        if (span.Length > 0)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i].Speed == 0)
                {
                    continue;
                }
                if (span[i].AccumulatedDelta >= 1 || span[i].AccumulatedDelta < 0)
                {
                    ProjectileToRemove.Enqueue(i);
                    span[i].Speed = 0;
                    continue;
                }
                span[i].AccumulatedDelta += delta * span[i].Speed;
                var accu = span[i].AccumulatedDelta;
                // TODO: move this movement to be more customizable
                var position = span[i].StartPosition.Lerp(span[i].Endposition, (float)span[i].AccumulatedDelta);
                float parabolicRadius = 15 / (float)span[i].Speed;
                float parabolicDelta = (float)Mathf.Sin(accu * Mathf.Pi) * parabolicRadius;
                position.Y += parabolicDelta;
                this.GetWorldDebugSystem().DebugSphere(position, .5f, Colors.Red, delta);
            }
        }

        while (ProjectileToRemove.Count > 0)
        {
            var index = ProjectileToRemove.Dequeue();
            if (Projectiles.Count > index)
            {
                Projectiles.RemoveAt(index);
            }
        }
    }

    public void OnProjectileRemoved(ref Node3D projectile, double? delta, double? lifetime)
    {

    }

    public void OnProjectileUpdate(ref Node3D projectile, double delta)
    {

    }

}