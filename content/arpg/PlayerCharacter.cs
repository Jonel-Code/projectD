using Godot;
using System;

public partial class PlayerCharacter : RigidBody3D
{

	[Export]
	public string FloorGroupName { get; set; } = "Floor";

	public float FloorAngleJump = Mathf.Cos(Mathf.DegToRad(75.0f));
	public const float JumpHeight = 5.0f; // units
	public Vector3 JumpVelocity = Mathf.Sqrt(JumpHeight * 9.81f) * Vector3.Up;

	public const float Speed = 10.0f;

	protected float floatingSince = 0.0f;
	protected Vector3 Velocity = Vector3.Zero;
	protected bool IsOnFloor { get; set; } = false;
	protected bool AllowJump { get; set; } = false;



	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		BodyShapeEntered += OnCollisionStart;
		BodyShapeExited += OnCollisionExit;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}


	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		var velocity = state.LinearVelocity;

		velocity = velocity with
		{
			X = Velocity.X,
			Z = Velocity.Z
		};

		state.LinearVelocity = velocity;
	}


	public override void _PhysicsProcess(double delta)
	{
		if (!IsOnFloor)
		{
			floatingSince += (float)delta;
			if (floatingSince >= 0.5f && AllowJump == true)
			{
				AllowJump = false;
			}
		}
		else
		{
			floatingSince = 0.0f;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && AllowJump == true)
		{
			Jump();
		}

		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			Velocity = Velocity with
			{
				X = direction.X * Speed,
				Z = direction.Z * Speed
			};
		}
		else
		{
			Velocity = Velocity with
			{
				X = Mathf.MoveToward(Velocity.X, 0, Speed),
				Z = Mathf.MoveToward(Velocity.Z, 0, Speed)
			};
		}

		// ConsumeVelocity(delta);
	}

	private void Jump()
	{
		// means he did air jump
		if (floatingSince != 0.0f)
		{
			AllowJump = false;
			// resets the linear velocity because we don't want jump impulse to be accumulative, 
			// rather we want it to be more consistent regardless the air jump timings
			LinearVelocity = LinearVelocity with { Y = 0 };
		}
		ApplyImpulse(JumpVelocity);
	}

	protected void ConsumeVelocity(double delta)
	{
		var displacement = GlobalPosition + Velocity;
		GlobalPosition = GlobalPosition.Lerp(displacement, (float)delta);
	}

	public void OnCollisionStart(Rid bodyRid, Node body, long shapeIndex, long localShapeIndex)
	{
		var collision = MoveAndCollide(Vector3.Down, true);
		if (collision != null)
		{
			if (collision.GetNormal().Y > FloorAngleJump)
			{
				IsOnFloor = true;
				AllowJump = true;
			}
		}

		if (body.IsInGroup(FloorGroupName))
		{
			IsOnFloor = true;
			AllowJump = true;
		}
	}

	public void OnCollisionExit(Rid bodyRid, Node body, long shapeIndex, long localShapeIndex)
	{
		IsOnFloor = false;
	}
}
