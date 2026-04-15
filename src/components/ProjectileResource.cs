using Godot;

[GlobalClass]
public partial class ProjectileResource : Resource
{
    [Export]
    public float Speed { get; set; } = 1000;

    [Export]
    public float ProjectileReach { get; set; } = 1000;

    [Export]
    public int AmmoCount { get; set; } = 5;

    [Export]
    public float FirePerSecond { get; set; } = 5;

    protected int BufferSize => 2;
    public int ProjectilePoolSize => AmmoCount * BufferSize;
    public double ShotInterval => 1 / FirePerSecond;
}