using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GlobalSystems;
using Godot;
using WorldUtils;

public struct ProjectileLaunchData
{
    public Vector3 StartPosition;
    public Vector3 Endposition;
    public float Radius;
    public double Speed;
    public double AccumulatedDelta;
}

public record ProjectileLauncDataUpdate
{
    public int Index;
    public ProjectileLaunchData Update;
    public Vector3 Position;
    public uint CollisionMask;
}


[GlobalClass]
partial class ProjectileLaunchComponent : Node
{

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
        Projectiles.Capacity = 10000000;
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
                    var projCount = 1;
                    var direction = Vector3.Left;
                    var globalDirection = (hitPosition - origin).Normalized();
                    var radius = 0.5f;
                    var endPosBasis = direction * projCount * radius;
                    var transform = new Transform3D
                    {
                        Origin = hitPosition,
                        Basis = Basis.LookingAt(globalDirection)
                    };

                    for (int i = 0; i < projCount; i++)
                    {
                        var offset = endPosBasis - (direction * i * radius * 2);
                        Projectiles.Add(new ProjectileLaunchData
                        {
                            StartPosition = origin,
                            Endposition = transform * offset,
                            Speed = speed,
                            AccumulatedDelta = 0,
                            Radius = radius,
                        });
                    }
                }
            }
        }


        // if (Projectiles.Count > 0)
        // {
        //     var start = Projectiles[0].StartPosition;
        //     var end = Projectiles[0].Endposition;
        //     var radius = Projectiles[0].Radius;
        //     var currentDelta = Projectiles[0].AccumulatedDelta;
        //     var position = Projectiles[0].StartPosition.Lerp(Projectiles[0].Endposition, (float)currentDelta);
        //     var rid = CurrentWorld.Space;
        //     var space = PhysicsServer3D.Singleton.SpaceGetDirectState(rid);
        //     var shape = new SphereShape3D
        //     {
        //         Radius = radius
        //     };
        //     var query = new PhysicsShapeQueryParameters3D
        //     {
        //         Shape = shape,
        //         CollisionMask = (1 << 30) - 1
        //     };
        //     var task = Task.Run(() =>
        //     {
        //         if (space != null)
        //         {
        //             query.Transform = new Transform3D
        //             {
        //                 Origin = position,
        //                 Basis = Basis.LookingAt(Vector3.Forward)
        //             };
        //             var result = space.IntersectShape(query);
        //             if (result.Count > 0)
        //             {
        //                 if (result[0].TryGetValue("collider", out var hit))
        //                 {
        //                     var collider = hit.As<CollisionObject3D>();
        //                 }
        //             }
        //             Callable.From(() => GD.Print("pos: ", position, " result: ", result)).CallDeferred();
        //         }
        //         else
        //         {
        //             GD.Print("space is null");
        //         }
        //     });
        //     task.Wait();
        //     // WorkerThreadPool.WaitForTaskCompletion(task);
        // }
    }

    public void ProcessHit(CollisionObject3D hitObject)
    {
        GD.Print("hit: ", hitObject);
    }

    public CollisionObject3D CheckShapeCollision(Rid spaceRid, Vector3 position, float radius)
    {
        var space = PhysicsServer3D.Singleton.SpaceGetDirectState(spaceRid);
        if (space != null)
        {
            var shape = new SphereShape3D
            {
                Radius = radius
            };
            var transform = new Transform3D
            {
                Origin = position,
            };
            var query = new PhysicsShapeQueryParameters3D
            {
                Shape = shape,
                Transform = transform,
                CollisionMask = HitMask
            };
            var result = space.IntersectShape(query);
            if (result.Count > 0)
            {
                if (result[0].TryGetValue("collider", out var hit))
                {
                    var collider = hit.As<CollisionObject3D>();
                    return collider;
                }
            }
        }
        return null;
    }

    public override void _Process(double delta)
    {
        if (CurrentWorld != null)
        {
            var batchSize = 100;
            var batchCount = (Projectiles.Count + batchSize - 1) / batchSize;
            var batchUpdate = new Task<ProjectileLaunchData[]>[batchCount];
            var spaceRid = GetViewport().World3D.Space;
            for (int i = 0; i < batchCount; i++)
            {
                int start = i * batchSize;
                int count = Math.Min(batchSize, Projectiles.Count - start);
                var batchTask = Task.Run(() => UpdateLaunchDataAsync(delta, start, count));
                batchUpdate[i] = batchTask;
            }
            Task.WaitAll(batchUpdate);
            // var currentCap = Projectiles.Count;
            // Projectiles.Clear();
            // foreach (var batch in batchUpdate)
            // {
            //     var result = batch.Result;
            //     if (result.Length > 0)
            //     {
            //         Projectiles.AddRange(result);
            //     }
            // }
        }
    }


    protected ProjectileLauncDataUpdate[] UpdateLaunchData(double delta, int start, int count)
    {
        var result = new ProjectileLauncDataUpdate[count];
        for (int i = start; i < start + count; i++)
        {
            if (Projectiles.Count <= i)
            {
                break;
            }
            result[i] = GetUpdatedLauncData(delta, i, Projectiles[i]);
        }
        return result;
    }
    protected async Task<ProjectileLaunchData[]> UpdateLaunchDataAsync(double delta, int start, int count)
    {
        if (start < Projectiles.Count)
        {
            var taskRange = Projectiles.GetRange(start, count)
            .Select((proj, index) => Task.Run(() => GetUpdatedLauncData(delta, index, proj)));
            var result = await Task.WhenAll(taskRange);

            return [.. result
            .Where((e) => e.CollisionMask > 0 && e.Update.AccumulatedDelta < 1 && e.Update.AccumulatedDelta >= 0)
            .Select((e) => e.Update)];
        }
        return [];
    }
    protected ProjectileLauncDataUpdate CheckCollision(ProjectileLauncDataUpdate entry, Rid spaceRid)
    {
        var space = PhysicsServer3D.SpaceGetDirectState(spaceRid);
        return entry;
    }
    protected ProjectileLauncDataUpdate GetUpdatedLauncData(double delta, int i, ProjectileLaunchData projectile)
    {
        var result = new ProjectileLauncDataUpdate
        {
            Index = i,
            Update = projectile,
            CollisionMask = 0,
            Position = projectile.StartPosition
        };
        if (projectile.Speed == 0)
        {
            return result;
        }
        if (projectile.AccumulatedDelta >= 1 || projectile.AccumulatedDelta < 0)
        {
            projectile.Speed = 0;
            return result with { Update = projectile };
        }
        projectile.AccumulatedDelta += delta * projectile.Speed;
        var accu = projectile.AccumulatedDelta;
        // TODO: move this movement to be more customizable
        var position = projectile.StartPosition.Lerp(projectile.Endposition, (float)projectile.AccumulatedDelta);
        float parabolicRadius = 15 / (float)projectile.Speed;
        float parabolicDelta = (float)Mathf.Sin(accu * Mathf.Pi) * parabolicRadius;
        position.Y += parabolicDelta;
        result = result with
        {
            Update = projectile,
            CollisionMask = HitMask,
            Position = position
        };
        return result;
    }


    public void OnProjectileRemoved(ref Node3D projectile, double? delta, double? lifetime)
    {

    }

    public void OnProjectileUpdate(ref Node3D projectile, double delta)
    {

    }

}