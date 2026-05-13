using Godot;

public partial class EnemyCharacter : CharacterBody3D
{
	[Export]
	public Node3D TargetFollow { get; set; } = null;

	[Export]
	public NavigationAgent3D NavAgent { get; set; } = null;

	[Export]
	public bool IsWalking { get; set; } = false;

	[Export]
	public Vector3 AnimForwardVector { get; set; } = Vector3.Forward;

	[Export]
	public AnimationTree AnimTree { get; set; } = null;

	protected Vector3 TargetPosition = Vector3.Zero;


	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;
	public double NextPathCheckIn = 0;
	protected double NextPathCheckInterval = 1;

	public override void _Ready()
	{
		IsWalking = false;
	}


	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		if (IsWalking)
		{
			if (AnimTree != null)
			{
				var rootPos = AnimTree.GetRootMotionPosition();
				rootPos *= new Quaternion(Vector3.Forward, AnimForwardVector);
				var globalRootPos = GlobalTransform.Basis * rootPos;
				var displacement = globalRootPos / (float)delta;
				velocity = displacement with { Y = velocity.Y };
			}
		}
		else
		{
			velocity = velocity with { X = 0, Z = 0 };
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public override void _Process(double delta)
	{
		if (NextPathCheckIn <= 0)
		{
			NextPathCheckIn = NextPathCheckInterval;
			if (TargetFollow != null && NavAgent != null)
			{
				NavAgent.TargetPosition = TargetFollow.GlobalPosition;
				IsWalking = NavAgent.IsTargetReachable();
			}
		}
		else
		{
			NextPathCheckIn -= delta;
		}

		if (IsWalking)
		{
			var preCheckPassed = !NavAgent.IsNavigationFinished() || !NavAgent.IsTargetReached();
			if (preCheckPassed)
			{
				if (NavAgent != null)
				{
					var next = NavAgent.GetNextPathPosition();
					var direction = next - GlobalPosition;
					IsWalking = direction != Vector3.Zero;
					if (IsWalking)
					{
						GlobalTransform = GlobalTransform.LookingAt(next with { Y = GlobalPosition.Y });
					}
				}
			}
			else
			{
				IsWalking = false;
			}
		}
	}

}
