using Godot;

[GlobalClass]
public partial class CharacterContext : Node
{
    [Export]
    public CharacterBody3D CharacterBody { get; set; }

    [Export]
    public Skeleton3D Skeleton { get; set; }

    [Export]
    public CharacterState State { get; set; }
}

[GlobalClass]
public partial class CharacterState : RefCounted
{
    public bool Walking;

    public bool Running;
}