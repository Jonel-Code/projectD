using Godot;
using GlobalSystems;

[GlobalClass]
public partial class PlayerMouseMovementComponent : Node
{
    [Export]
    public string ActionName { get; set; } = "";

    [Export]
    public NavigationAgent3D PlayerMovementAgent { get; set; } = null;

    [Export]
    public CharacterBody3D PlayerBody { get; set; } = null;

    public uint CollisionMask = 0;
    public override void _Ready()
    {
        CollisionMask = 1 << 3;
    }

    public override void _Process(double delta)
    {
        if (PlayerMovementAgent != null && PlayerBody != null)
        {
            if (ActionName.Length > 0 && Input.IsActionJustPressed(ActionName))
            {
                if (this.GetMouseSystem().GetMouseWorldPosition(out var hitPosition, out var hitRid, CollisionMask))
                {
                    PlayerMovementAgent.TargetPosition = hitPosition;
                }
            }
            if (!PlayerMovementAgent.IsTargetReached())
            {
                var targetPos = PlayerMovementAgent.GetNextPathPosition();
                var newVelocity = (targetPos - PlayerBody.GlobalPosition).Normalized() * 10;
                newVelocity = PlayerBody.Velocity.Lerp(newVelocity, (float)delta * 50);
                PlayerBody.Velocity = PlayerBody.Velocity with { X = newVelocity.X, Z = newVelocity.Z };
                PlayerBody.MoveAndSlide();
            }
        }
    }
}