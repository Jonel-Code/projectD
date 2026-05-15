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

	[Export]
	public Skeleton3D Skeleton { get; set; } = null;

	[Export]
	public HurtBoxComponent HurtBox { get; set; } = null;

	[Export(PropertyHint.Layers3DPhysics)]
	public uint AttackHitMask { get; set; } = 1 << 30;

	protected Vector3 TargetPosition = Vector3.Zero;

	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;
	public double NextPathCheckIn = 0;
	public bool PerformPathCheck = true;
	protected double NextPathCheckInterval = 0.5;

	protected double ScanInterval = 1;
	protected double NextScanIn = 0;
	protected bool ScanForAttack = false;
	protected List<Rid> ExcludeAttackRid = new();

	public override void _Ready()
	{
		IsWalking = false;
		ExcludeAttackRid.Add(GetRid());

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
		if (PerformPathCheck)
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
				if (CanAttack())
				{
					PerformAttack();
				}
			}
			else
			{
				NextScanIn -= delta;
			}
		}
	}

	protected bool CanAttack()
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
					return true;
				}
			}
		}
		return false;
	}

	protected void PerformAttack()
	{
		PerformPathCheck = false;
		ScanForAttack = false;
		if (AnimTree != null)
		{
			AnimTree.Active = false;
		}

		AnimPlayer?.Play("BasicActionAnimLibrary/Boxing");
	}

	protected void EndAttack()
	{
		PerformPathCheck = true;
		ScanForAttack = true;
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

		if (signal == "LeftHandAttackStart")
		{
			HurtBox?.AddHurtboxOnBone(
				"LeftHand",
				new SphereShape3D()
				{
					Radius = 0.5f,
				},
				 AttackHitMask,
				[.. ExcludeAttackRid]);
		}

		if (signal == "LeftHandAttackEnd")
		{
			HurtBox?.RemoveHurtboxOnBone("LeftHand");
		}
	}

	public void HitSomething(Vector3 globalPos, Rid rid)
	{
		if (rid != OwnCollsion.GetRid())
		{
			var instanceId = PhysicsServer3D.BodyGetObjectInstanceId(rid);
			if (instanceId >= 0)
			{
				var hit = InstanceFromId(instanceId);
				if (hit is PlayerCharacter hitChar)
				{
					var charPos = hitChar.GlobalPosition;
					var direction = (globalPos - charPos).Normalized();
					hitChar.ApplyKnockBack(direction);
				}
			}
		}
	}
}
