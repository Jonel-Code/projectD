using System;
using Godot;

public partial class PlayerCharacter : CharacterBody3D
{

	[Export]
	public Node3D CameraRoot { get; set; } = null;

	[Export]
	public Node3D PlayerBodyRoot { get; set; } = null;

	[Export]
	public CharacterResource Resource { get; set; } = null;

	[Export]
	public AnimationPlayer AnimationPlayer { get; set; } = null;

	protected const double StillOnFloorThreshold = 0.2;
	protected double StillOnFloorBias = 0;

	public const float Gravity = 9.81f;
	public const float JumpHeight = 3.0f; // units
	public Vector3 JumpVelocity = Mathf.Sqrt(JumpHeight * 9.81f) * Vector3.Up;

	public const int AllowedAirJump = 1;
	public const int MaximumJumpCount = AllowedAirJump + 1;
	public int CurrentJumpCount = 0;
	protected bool AllowJump { get; set; } = false;

	public const float BaseSpeed = 3f;
	public float Speed
	{
		get
		{
			if (Resource != null)
			{
				return BaseSpeed + (Resource.Agility >> 1);
			}
			return BaseSpeed;
		}
	}
	protected Input.MouseModeEnum MouseMode = Input.MouseModeEnum.Captured;

	public string ResourcePath = "res://content/arpg/resources/PlayerCharacterResource.tres";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Input.MouseMode = MouseMode;

		Resource = ResourceLoader.Load<CharacterResource>(ResourcePath);
		Resource ??= new CharacterResource
		{
			Name = "Player",
			MaxHealth = 100,
			MaxMana = 50,
			Strength = 1,
			Agility = 1,
			Intelligence = 1,
		};
		ResourceSaver.Save(Resource, ResourcePath);
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
	public override void _Input(InputEvent e)
	{
		if (e is InputEventMouseMotion inputEventMouseMotion)
		{
			if (CameraRoot != null)
			{
				var lookDirection = inputEventMouseMotion.Relative;
				var yaw = CameraRoot.Rotation.Y;
				var yawDelta = Mathf.DegToRad(-lookDirection.X) * 0.1f;
				yaw += yawDelta;

				var roll = CameraRoot.Rotation.X;
				var rollDelta = Mathf.DegToRad(-lookDirection.Y) * 0.1f;
				var minRoll = Mathf.DegToRad(-80);
				var maxRoll = Mathf.DegToRad(80);
				roll = Mathf.Clamp(roll + rollDelta, minRoll, maxRoll);

				CameraRoot.Rotation = new Vector3(roll, yaw, 0);
			}

		}
	}

	private void ProcessAirTime(double delta)
	{
		if (CurrentJumpCount >= MaximumJumpCount)
		{
			AllowJump = false;
		}

		if (!IsOnFloor())
		{
			if (StillOnFloorBias >= StillOnFloorThreshold)
			{
				if (CurrentJumpCount == 0)
				{
					CurrentJumpCount++;
				}
			}
			else
			{
				StillOnFloorBias += (float)delta;
			}
		}
		else
		{
			StillOnFloorBias = 0;
			AllowJump = true;
			CurrentJumpCount = 0;
		}
	}

	private Vector3 GetCameraForwardDirection()
	{
		if (CameraRoot != null)
		{
			var newbasis = CameraRoot.GlobalTransform.Basis.Z with { Y = 0 };
			return newbasis.Normalized();
		}
		return GlobalTransform.Basis.Z.Normalized();
	}

	public void ToggleMouse()
	{
		if (Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		else
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	private void ProcessGravity(double delta)
	{
		if (!IsOnFloor())
		{
			Velocity += Gravity * Vector3.Down * (float)delta;
		}

	}

	protected void ProcessInputAction(double delta)
	{
		if (Input.IsActionJustPressed("ui_accept") && AllowJump)
		{
			Jump();
			CurrentJumpCount++;
		}

		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();
		if (direction != Vector3.Zero)
		{
			var forward = GetCameraForwardDirection();
			var quat = new Quaternion(GlobalTransform.Basis.Z, forward);
			direction = quat * direction;

			var newVelocity = direction * Speed;
			Velocity = Velocity with
			{
				X = newVelocity.X,
				Z = newVelocity.Z,
			};

			if (PlayerBodyRoot != null)
			{
				var movementQuat = new Quaternion(GlobalTransform.Basis.Z, -direction);
				PlayerBodyRoot.Basis = new Basis(movementQuat);
			}
		}
		else
		{
			Velocity = Velocity with
			{
				X = Mathf.MoveToward(Velocity.X, 0, Speed),
				Z = Mathf.MoveToward(Velocity.Z, 0, Speed),
			};
		}

		if (Input.IsActionJustPressed("ui_toggle_mouse"))
		{
			ToggleMouse();
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
