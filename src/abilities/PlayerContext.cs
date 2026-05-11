using Godot;

[GlobalClass]
public partial class PlayerContext : Node
{
    [Export]
    public CharacterBody3D CharacterBody { get; set; }

    [Export]
    public Skeleton3D Skeleton { get; set; }

    [Export]
    public PlayerState State { get; set; }
}

[GlobalClass]
public partial class PlayerState : RefCounted
{
    public bool MovingForward;

    public bool RunningForward;
}