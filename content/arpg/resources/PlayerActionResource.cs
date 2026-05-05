using Godot;

[GlobalClass, Tool]
public partial class PlayerActionResource : Resource
{
    [Export]
    public string Name { get; set; }

    [Export]
    public string InputActionName { get; set; }

    [Export]
    public Animation AnimationResource { get; set; } = null;

}