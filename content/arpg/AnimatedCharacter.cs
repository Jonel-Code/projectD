using System;
using Godot;

public partial class AnimatedCharacter : CharacterBody3D
{
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;

	[Export]
	public AnimationTree AnimTree;

	[Export]
	public Node3D CameraRoot { get; set; } = null;

	[Export]
	public Skeleton3D Skeleton { get; set; } = null;

	[Export]
	public bool is_running { get; set; } = false;

	protected Node3D CameraSocket { get; set; } = null;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		CameraSocket = GetNode<Node3D>("%CameraSocket");
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

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (CameraSocket != null && CameraRoot != null)
		{
			var targetPos = CameraSocket.GlobalPosition;
			var currentPos = CameraRoot.GlobalPosition;
			var displacement = targetPos - currentPos;
			var dLenght = displacement.Length();
			if (dLenght > 0.1f)
			{
				var alpha = Mathf.Min((float)delta * 20f * dLenght, 1f);
				CameraRoot.GlobalPosition = new Vector3(
					Mathf.Lerp(currentPos.X, targetPos.X, alpha),
					Mathf.Lerp(currentPos.Y, targetPos.Y, alpha / 2),
					Mathf.Lerp(currentPos.Z, targetPos.Z, alpha)
				);
			}

		}


		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			is_running = true;
		}
		else
		{
			is_running = false;
		}

		if (AnimTree != null)
		{
			var rootPos = AnimTree.GetRootMotionPosition();
			var quat = Transform.Basis;
			velocity = (quat * rootPos) / (float)delta;
		}

		if (!is_running)
		{
			velocity.X = 0;
			velocity.Z = 0;
		}

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		Velocity = velocity;
		MoveAndSlide();

		if (Input.IsActionJustPressed("ui_toggle_mouse"))
		{
			ToggleMouse();
		}
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


}
