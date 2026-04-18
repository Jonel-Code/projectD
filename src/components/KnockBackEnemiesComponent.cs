using GlobalSystems;
using Godot;

[GlobalClass]
public partial class KnockBackEnemiesComponent : Node
{
    [Export]
    public Node3D KnockbackOrigin { get; set; }

    [Export(PropertyHint.Layers3DPhysics)]
    public uint HitMask { get; set; }

    protected float KnockbackRadius => 10f;

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("knock_back"))
        {
            if (KnockbackOrigin != null)
            {
                var shape = new SphereShape3D
                {
                    Radius = KnockbackRadius
                };
                var shapeTransform = KnockbackOrigin.Transform;
                var shapeQuery = new PhysicsShapeQueryParameters3D
                {
                    Shape = shape,
                    Transform = shapeTransform,
                    CollisionMask = HitMask
                };
                var hitResult = GetViewport().FindWorld3D().DirectSpaceState.IntersectShape(shapeQuery, maxResults: 100);
                if (hitResult.Count > 0)
                {
                    foreach (var item in hitResult)
                    {
                        if (item.TryGetValue("collider", out Variant outCollider))
                        {
                            var collider = outCollider.As<CharacterBody3D>();
                            var endPosition = collider.GlobalPosition;
                            var displacement = endPosition - KnockbackOrigin.GlobalPosition;
                            float displacementLength = displacement.Length();
                            var direction = displacement.Normalized() * (KnockbackRadius - displacementLength);
                            collider.GlobalPosition += direction;
                        }
                    }
                }
            }
        }
    }
}