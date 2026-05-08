using System;
using Godot;

/// <summary>
/// Currently works only when AnimTree is a AnimationBlendspace1D 
/// </summary>
[GlobalClass]
public partial class RootMotionPlayerComponent : Node
{
    [Export]
    public AnimationSignalReciever AnimSignalReciever { get; set; } = null;

    [Export]
    public AnimationTree AnimTree { get; set; } = null;

    [Export]
    public float RootBlendTime = 0.0f;

    [Signal]
    public delegate void OnRootAnimationEndEventHandler(string animationName);

    protected string CurrentRootMotionAnimation = "";
    protected float CurrentBlendPos = 0;
    protected float TargetBlendPos = 0;
    protected bool ShouldCleanupRootPos = false;
    protected string EndSignal = "";

    public override void _Ready()
    {
        if (AnimSignalReciever != null)
        {
            AnimSignalReciever.AnimSignal += OnAnimSignal;
        }

        if (AnimTree != null)
        {
            AnimTree.AnimationFinished += OnAnimationFinished;
        }
    }

    public override void _ExitTree()
    {
        if (AnimSignalReciever != null)
        {
            AnimSignalReciever.AnimSignal -= OnAnimSignal;
        }

        if (AnimTree != null)
        {
            AnimTree.AnimationFinished -= OnAnimationFinished;
        }
    }

    private void OnAnimationFinished(StringName animName)
    {
        if (EndSignal == "" && CurrentRootMotionAnimation != "")
        {
            EmitSignal(SignalName.OnRootAnimationEnd, CurrentRootMotionAnimation);
        }
    }

    private void OnAnimSignal(string name)
    {
        if (EndSignal != "" && name == EndSignal)
        {
            StopCurrentRootMotion();
            EmitSignal(SignalName.OnRootAnimationEnd, CurrentRootMotionAnimation);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (AnimTree != null && CurrentBlendPos != TargetBlendPos)
        {
            CurrentBlendPos = (float)AnimTree.Get("parameters/blend_position").AsDouble();
            if (CurrentBlendPos > TargetBlendPos)
            {
                CurrentBlendPos = Mathf.Max(TargetBlendPos, CurrentBlendPos - ((float)delta) / RootBlendTime);
            }
            else
            {
                CurrentBlendPos = Mathf.Min(TargetBlendPos, CurrentBlendPos + ((float)delta / RootBlendTime));
            }
            AnimTree.Set("parameters/blend_position", CurrentBlendPos);
        }

        if (AnimTree != null && TargetBlendPos == 0 && CurrentBlendPos == 0 && ShouldCleanupRootPos)
        {
            ResetCurrentRootMotion();
            ShouldCleanupRootPos = false;
        }
    }

    public void PlayRootMotion(string animationName, string playUntil = "")
    {
        if (CurrentRootMotionAnimation == animationName)
        {
            return;
        }
        AnimTree.Active = false;
        ResetCurrentRootMotion();
        if (AnimTree.TreeRoot is AnimationNodeBlendSpace1D blendNode)
        {
            var node = new AnimationNodeAnimation
            {
                Animation = animationName
            };
            blendNode.AddBlendPoint(node, 1);
            TargetBlendPos = 1;
        }
        AnimTree.Active = true;
        CurrentRootMotionAnimation = animationName;
        EndSignal = playUntil;
    }

    public void StopCurrentRootMotion()
    {
        if (CurrentRootMotionAnimation == "")
        {
            return;
        }
        AnimTree.Active = false;
        ShouldCleanupRootPos = true;
        TargetBlendPos = 0;
        AnimTree.Active = true;
        CurrentRootMotionAnimation = "";
    }

    protected void ResetCurrentRootMotion()
    {
        if (AnimTree.TreeRoot is AnimationNodeBlendSpace1D root)
        {
            for (int i = root.GetBlendPointCount() - 1; i >= 0; i--)
            {
                var pos = root.GetBlendPointPosition(i);
                if (pos > 0)
                {
                    root.RemoveBlendPoint(i);
                }
            }
        }
    }

}