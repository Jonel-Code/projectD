using Godot;

public partial class PlayerCharacter : CharacterBody3D
{
	public const float Gravity = 9.81f;
	public const float JumpHeight = 5.0f; // units
	public Vector3 JumpVelocity = Mathf.Sqrt(JumpHeight * 9.81f) * Vector3.Up;

	public const float Speed = 5.0f;
	protected float floatingSince = 0.0f;
	protected bool AllowJump { get; set; } = false;



	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}


	public override void _PhysicsProcess(double delta)
	{
		ProcessGravity(delta);
		ProcessAirTime(delta);
		ProcessInputAction(delta);
		MoveAndSlide();
	}

	private void ProcessGravity(double delta)
	{
		if (!IsOnFloor())
		{
			Velocity += Gravity * Vector3.Down * (float)delta;
		}
	}

	private void ProcessAirTime(double delta)
	{
		if (!IsOnFloor())
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
			AllowJump = true;
		}
	}

	protected void ProcessInputAction(double delta)
	{
		if (Input.IsActionJustPressed("ui_accept") && AllowJump == true)
		{
			Jump();
		}

		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			var newVelocity = direction * Speed;
			Velocity = Velocity with
			{
				X = newVelocity.X,
				Z = newVelocity.Z,
			};
		}
		else
		{
			Velocity = Velocity with
			{
				X = Mathf.MoveToward(Velocity.X, 0, Speed),
				Z = Mathf.MoveToward(Velocity.Z, 0, Speed),
			};
		}
	}

	private void Jump()
	{
		Velocity = Velocity with
		{
			Y = JumpVelocity.Y,
		};
	}
}
