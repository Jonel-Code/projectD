using System.Collections.Generic;
using GlobalSystems;
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

	[Export]
	public AnimationPlayer AnimPlayer { get; set; } = null;

	[Export]
	public float AttackDistance = 1f;

	[Export]
	public CollisionObject3D OwnCollsion { get; set; } = null;

	protected Vector3 TargetPosition = Vector3.Zero;

	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;
	public double NextPathCheckIn = 0;
	protected double NextPathCheckInterval = 1;

	protected double ScanInterval = 1;
	protected double NextScanIn = 0;
	protected bool ScanForAttack = false;
	protected List<Rid> ExcludeAttackRid = new();

	public override void _Ready()
	{
		IsWalking = false;
		if (OwnCollsion != null)
		{
			ExcludeAttackRid.Add(OwnCollsion.GetRid());
		}
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
			var targetReached = NavAgent.IsNavigationFinished() || NavAgent.IsTargetReached();
			if (targetReached)
			{
				IsWalking = false;
				ScanForAttack = true;
			}
			else
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
				ScanForAttack = false;
			}
		}

		if (ScanForAttack)
		{
			if (NextScanIn <= 0)
			{
				NextScanIn = ScanInterval;
				ScanAttack();
			}
			else
			{
				NextScanIn -= delta;
			}
		}
	}

	protected void ScanAttack()
	{
		var attackScanShape = new SphereShape3D()
		{
			Radius = AttackDistance,
		};
		var query = new PhysicsShapeQueryParameters3D()
		{
			Shape = attackScanShape,
			Transform = GlobalTransform,
			CollisionMask = 1 << 30,
			Exclude = [.. ExcludeAttackRid],
		};

		var results = GetViewport().FindWorld3D().DirectSpaceState.IntersectShape(query, maxResults: 1);
		if (results.Count > 0)
		{
			var first = results[0];
			if (first.TryGetValue("collider", out Variant outCollider))
			{
				var collider = outCollider.As<CharacterBody3D>();
				if (collider != this)
				{
					GlobalTransform = GlobalTransform.LookingAt(collider.GlobalPosition with { Y = GlobalPosition.Y });
					PerformAttack();
				}
			}
		}

	}

	protected void PerformAttack()
	{
		if (AnimTree != null)
		{
			AnimTree.Active = false;
		}

		AnimPlayer?.Play("BasicActionAnimLibrary/Boxing");
	}

	protected void EndAttack()
	{
		if (AnimTree != null)
		{
			AnimTree.Active = true;
		}
	}

	public void OnAnimationSigalRecieve(string signal)
	{
		if (signal == "AttackDone")
		{
			EndAttack();
		}
	}
}
