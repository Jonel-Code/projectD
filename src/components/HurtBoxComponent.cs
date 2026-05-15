using System.Collections.Generic;
using GlobalSystems;
using Godot;

public struct BoneHurtBoxData
{
    public int BoneIndex;
    public PhysicsShapeQueryParameters3D Query;
    public List<Rid> CollisionRids;
}

[GlobalClass]
public partial class HurtBoxComponent : Node
{
    [Signal]
    public delegate void HurtBoxHitEventHandler(Vector3 hittingGlobalPos, Rid collision);

    [Export]
    public Skeleton3D Skeleton { get; set; } = null;

    [Export]
    public int DefaultCapacity = 10;

    [Export]
    public bool Debug = false;

    protected List<BoneHurtBoxData> BoneHurtBoxes = [];

    public override void _Ready()
    {
        ResetHurtBoxes();
    }

    public override void _Process(double delta)
    {
        ProcessHurtBoxes(delta);
    }

    protected void ResetHurtBoxes()
    {
        BoneHurtBoxes.EnsureCapacity(DefaultCapacity);
        if (Skeleton != null)
        {
            for (int i = 0; i < DefaultCapacity; i++)
            {
                BoneHurtBoxes.Add(CreateDefaultHurtBox());
            }
        }
    }

    protected BoneHurtBoxData CreateDefaultHurtBox()
    {
        var data = new BoneHurtBoxData
        {
            BoneIndex = -1,
            Query = null,
            CollisionRids = [],
        };
        data.CollisionRids.EnsureCapacity(DefaultCapacity);
        return data;
    }

    protected void ProcessHurtBoxes(double delta)
    {
        if (Skeleton != null)
        {
            foreach (var item in BoneHurtBoxes)
            {
                if (item.BoneIndex >= 0 && item.Query != null)
                {
                    var local = Skeleton.GetBoneGlobalPose(item.BoneIndex);
                    var global = Skeleton.GlobalTransform * local.Origin;
                    if (item.Query != null)
                    {
                        item.Query.Transform = item.Query.Transform with { Origin = global };
                        var result = GetViewport().FindWorld3D().DirectSpaceState.IntersectShape(item.Query);
                        TryDebugData(delta, item, global);
                        foreach (var hit in result)
                        {
                            if (hit.TryGetValue("collider", out Variant outCollider))
                            {
                                var col = outCollider.As<CollisionObject3D>();
                                var hittingRid = col.GetRid();
                                if (item.CollisionRids.Contains(hittingRid))
                                {
                                    continue;
                                }
                                item.CollisionRids.Add(hittingRid);
                                EmitSignal(SignalName.HurtBoxHit, global, hittingRid);
                            }
                        }
                    }
                }
            }
        }
    }

    private void TryDebugData(double delta, BoneHurtBoxData item, Vector3 global)
    {
        if (Debug)
        {
            if (item.Query is PhysicsShapeQueryParameters3D param)
            {
                if (param.Shape is SphereShape3D sphere)
                {
                    this.GetWorldDebugSystem().DebugSphere(global, sphere.Radius, Colors.Red, delta);
                }
            }
        }
    }

    public int AddHurtboxOnBone(string boneName, Shape3D shape, uint collsionMask = 1 << 30, List<Rid> excludeRids = null)
    {
        var boxIndex = -1;
        if (Skeleton != null)
        {
            var boneIndex = Skeleton.FindBone(boneName);
            if (boneIndex >= 0)
            {
                for (int i = 0; i < BoneHurtBoxes.Count; i++)
                {
                    if (BoneHurtBoxes[i].Query == null)
                    {
                        boxIndex = i;
                        var hurtbox = CreateDefaultHurtBox();
                        hurtbox.BoneIndex = boneIndex;
                        hurtbox.Query = new PhysicsShapeQueryParameters3D()
                        {
                            Shape = shape,
                            CollisionMask = collsionMask,
                            Exclude = [.. excludeRids],
                        };
                        BoneHurtBoxes[boxIndex] = hurtbox;
                        break;
                    }
                }
            }
        }
        return boxIndex;
    }

    public bool RemoveHurtboxOnBone(string boneName)
    {
        if (Skeleton != null)
        {
            var boneIndex = Skeleton.FindBone(boneName);
            if (boneIndex >= 0)
            {
                for (int boxIndex = 0; boxIndex < BoneHurtBoxes.Count; boxIndex++)
                {
                    if (BoneHurtBoxes[boxIndex].BoneIndex == boneIndex)
                    {
                        if (boxIndex >= 0 && boxIndex < BoneHurtBoxes.Count)
                        {
                            BoneHurtBoxes[boxIndex].CollisionRids.Clear();
                            BoneHurtBoxes[boxIndex] = CreateDefaultHurtBox();
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

}