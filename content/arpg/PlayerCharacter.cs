using Godot;

public partial class PlayerCharacter : CharacterBody3D
{

	[Signal]
	public delegate void AnimSignalEventHandler(string name);

	[Export]
	public Node3D CameraRoot { get; set; } = null;

	[Export]
	public Skeleton3D Skeleton { get; set; } = null;

	[Export]
	public CharacterResource Resource { get; set; } = null;

	[Export]
	public AnimationPlayer AnimPlayer { get; set; } = null;

	[Export]
	public AnimationTree AnimTree { get; set; } = null;

	[Export]
	public RootMotionPlayerComponent RootMotionPlayer { get; set; } = null;

	protected string CurrentRootMotionAnimation = "";

	[Export]
	public bool Traversing { get; set; } = false;

	protected const double StillOnFloorThreshold = 0.2;
	protected double StillOnFloorBias = 0;

	public const float Gravity = 9.81f;
	public const float JumpHeight = 3.0f; // units
	public Vector3 JumpVelocity = Mathf.Sqrt(JumpHeight * 9.81f) * Vector3.Up;

	public const int AllowedAirJump = 1;
	public const int MaximumJumpCount = AllowedAirJump + 1;
	public int CurrentJumpCount = 0;
	protected bool AllowJump { get; set; } = false;

	public const float BaseSpeed = 5f;
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
	protected Godot.Collections.Array<Rid> CollisionRids = new();

	protected Basis AnimationLocalRotation { get; set; } = Basis.Identity;

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

		AnimSignal += OnAnimSignal;
		CollisionRids.Add(GetRid());

		SyncAnimationRotationToSkeletonRotation();
	}

	protected void SyncAnimationRotationToSkeletonRotation()
	{
		if (Skeleton != null)
		{
			AnimationLocalRotation = Skeleton.Basis;
		}
	}

	public void OnAnimSignal(string name)
	{
		GD.Print("Animation signal received: " + name);
	}

	public override void _PhysicsProcess(double delta)
	{
		// ProcessAirTime(delta);
		ProcessInputAction(delta);
		ProcessGravity(delta);
		SyncCameraToPlayer(delta);
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

	private Vector3 GetCameraForwardDirection()
	{
		if (CameraRoot != null)
		{
			var newbasis = CameraRoot.GlobalTransform.Basis.Z with { Y = 0 };
			return -newbasis.Normalized();
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

	private void SyncCameraToPlayer(double delta)
	{
		if (CameraRoot != null)
		{
			var cameraSocket = GetNode<Node3D>("%CameraSocket");
			if (cameraSocket != null)
			{
				var targetPosition = cameraSocket.GlobalPosition;
				var currentPosition = CameraRoot.GlobalPosition;
				var alpha = (float)delta * 10f;
				var newPosition = new Vector3(
					Mathf.Lerp(currentPosition.X, targetPosition.X, alpha),
					Mathf.Lerp(currentPosition.Y, targetPosition.Y, alpha / 2),
					Mathf.Lerp(currentPosition.Z, targetPosition.Z, alpha)
				);
				CameraRoot.GlobalPosition = newPosition;
			}
		}
	}

	protected void ProcessInputAction(double delta)
	{
		if (Input.IsActionJustPressed("ui_accept")) // && AllowJump)
		{
			Jump();
			CurrentJumpCount++;
		}

		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();
		if (direction != Vector3.Zero)
		{
			var forward = GetCameraForwardDirection();
			var forward2d = new Vector2(forward.X, forward.Z);
			var direction2d = new Vector2(direction.X, direction.Z);
			var directionAngle = Vector2.Up.AngleTo(direction2d);
			var move2dRot = forward2d.Rotated(directionAngle);
			var mainDirection = new Vector3(move2dRot.X, 0, move2dRot.Y);
			GlobalTransform = GlobalTransform.LookingAt(GlobalPosition + mainDirection.Normalized(), Vector3.Up);
			RootMotionPlayer?.PlayRootMotion("Imported/Walking");
			SyncRootMotionPostion(delta);
		}
		else
		{
			RootMotionPlayer?.StopCurrentRootMotion();
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

	protected void SyncRootMotionPostion(double delta)
	{
		if (AnimTree != null)
		{
			var rootPos = AnimTree.GetRootMotionPosition();
			var globalRootPos = AnimationLocalRotation * GlobalTransform.Basis * rootPos;
			var rootVel = globalRootPos / (float)delta;
			Velocity = rootVel with { Y = Velocity.Y };
		}
	}
}
