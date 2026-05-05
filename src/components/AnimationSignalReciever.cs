using Godot;

[GlobalClass]
public partial class AnimationSignalReciever : Node
{
    [Signal]
    public delegate void AnimSignalEventHandler(string name);

    public void SendAnimSignal(string name)
    {
        EmitSignal(SignalName.AnimSignal, name);
    }
}