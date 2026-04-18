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

    protected Dictionary<int, List<Rid>> ProjectileHits = new();

    protected Queue<int> ProjectileToRemove = new();

    protected uint AimingMask => 1 << 1;
    protected uint HitMask => 1 << 30;

    protected World3D CurrentWorld;

    public override void _Ready()
    {
        Projectiles.Capacity = 100;
        CurrentWorld = GetViewport().FindWorld3D();
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
                var projectileRadius = 2f;
                this.GetWorldDebugSystem().DebugSphere(position, projectileRadius, Colors.Red, delta);

                if (CurrentWorld != null)
                {
                    var shape = new SphereShape3D
                    {
                        Radius = projectileRadius
                    };
                    var transform = new Transform3D
                    {
                        Origin = position,
                        Basis = Basis.LookingAt(Vector3.Forward)
                    };
                    var shapeQuery = new PhysicsShapeQueryParameters3D
                    {
                        Shape = shape,
                        Transform = transform,
                        CollisionMask = HitMask
                    };
                    var hitResult = CurrentWorld.DirectSpaceState.IntersectShape(shapeQuery);
                    if (hitResult.Count > 0)
                    {
                        // ProjectileToRemove.Enqueue(i);
                        // span[i].Speed = 0;
                        if (!ProjectileHits.ContainsKey(i))
                        {
                            var list = new List<Rid>
                            {
                                Capacity = 10
                            };
                            ProjectileHits.Add(i, list);
                        }
                        foreach (var item in hitResult)
                        {
                            if (item.TryGetValue("collider", out Variant outCollider))
                            {
                                var collider = outCollider.As<CharacterBody3D>();

                                if (collider != null)
                                {
                                    if (ProjectileHits[i].Contains(collider.GetRid()))
                                    {
                                        continue;
                                    }
                                    // TODO: move this impact somewhere more easy to control
                                    var impactDirection = (collider.GlobalPosition - position).Normalized() with { Y = 0 };
                                    collider.Velocity += impactDirection * 25;
                                    collider.MoveAndSlide();
                                    ProjectileHits[i].Add(collider.GetRid());
                                }
                            }
                        }
                    }
                }
            }
        }

        while (ProjectileToRemove.Count > 0)
        {
            var index = ProjectileToRemove.Dequeue();
            if (Projectiles.Count > index)
            {
                Projectiles.RemoveAt(index);
                if (ProjectileHits.ContainsKey(index))
                {
                    ProjectileHits[index].Clear();
                    ProjectileHits[index].Capacity = 10;
                }
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