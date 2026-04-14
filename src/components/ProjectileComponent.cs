using Godot;
using System;

public struct ProjectileData
{
	public Vector3 StartPosition;
	public Vector3 TargetEnd;
	public Vector3 Direction;
	public float Speed;
	public float CurrentDelta;
	public bool IsActive;
}

[GlobalClass]
public partial class ProjectileComponent : Node
{
	[Export]
	public String FireActionName { get; set; } = "";

	[Export]
	public float Speed { get; set; } = 1000;

	[Export]
	public float ProjectileReach { get; set; } = 1000;

	[Export]
	public int AmmoCount { get; set; } = 5;

	[Export]
	public float FirePerSecond { get; set; } = 5;

	[Export]
	public Node3D ProjectileOrigin { get; set; } = null;

	[Export]
	public CollisionObject3D OwnerCollision { get; set; } = null;

	[Signal]
	public delegate void ProjectileLineCalculatedEventHandler(Vector3 start, Vector3 end);

	protected ProjectileData[] ProjectilePool = [];

	protected int BufferSize => 2;

	protected int ProjectilePoolSize => AmmoCount * BufferSize;

	protected double ShotInterval => 1 / FirePerSecond;

	protected uint ProjectileCollisionMask => 1 << 32;

	private double ShotCooldown = 0;

	private World3D CurrentWorld;

	private Godot.Collections.Array<Rid> RayExclusionList = [];

	protected bool DidInputFire => FireActionName.Length > 0 && Input.IsActionPressed(FireActionName);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Array.Resize(ref ProjectilePool, ProjectilePoolSize);
		for (int i = 0; i < ProjectilePoolSize; i++)
		{
			ProjectilePool[i] = new ProjectileData();
		}

		CurrentWorld = GetViewport().FindWorld3D();

		if (OwnerCollision != null)
		{
			RayExclusionList.Add(OwnerCollision.GetRid());
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (ProjectileOrigin != null)
		{
			if (ShotCooldown <= 0)
			{
				if (DidInputFire)
				{
					FireBulletTowards(ProjectileOrigin.GlobalPosition + Vector3.Forward);
					FireBulletTowards(ProjectileOrigin.GlobalPosition + Vector3.Forward.Rotated(Vector3.Up, Mathf.DegToRad(10)));
					FireBulletTowards(ProjectileOrigin.GlobalPosition + Vector3.Forward.Rotated(Vector3.Up, Mathf.DegToRad(15)));
					FireBulletTowards(ProjectileOrigin.GlobalPosition + Vector3.Forward.Rotated(Vector3.Up, Mathf.DegToRad(20)));
					FireBulletTowards(ProjectileOrigin.GlobalPosition + Vector3.Forward.Rotated(Vector3.Up, Mathf.DegToRad(25)));
					ShotCooldown = ShotInterval;
				}
			}
			else
			{
				ShotCooldown -= delta;
			}

			ProcessBullet(delta);
		}
	}

	public void FireBulletTowards(Vector3 target)
	{
		for (int i = 0; i < ProjectilePool.Length; i++)
		{
			if (ProjectilePool[i].IsActive)
			{
				continue;
			}
			var start = ProjectileOrigin.GlobalPosition;
			var direction = (target - ProjectileOrigin.GlobalPosition).Normalized();
			ProjectilePool[i].StartPosition = start;
			ProjectilePool[i].TargetEnd = start + (direction * ProjectileReach);
			ProjectilePool[i].Speed = Speed;
			ProjectilePool[i].Direction = direction;
			ProjectilePool[i].IsActive = true;
			ProjectilePool[i].CurrentDelta = 0;
			ShotCooldown = 0;
			break;
		}
	}

	protected void ProcessBullet(double delta)
	{
		for (int i = 0; i < ProjectilePool.Length; i++)
		{
			var bullet = ProjectilePool[i];

			if (bullet.IsActive)
			{
				float prevDelta = bullet.CurrentDelta;
				float curDelta = prevDelta + (float)delta;

				var currentStart = bullet.StartPosition + (bullet.Direction * bullet.Speed * prevDelta);
				var currentEnd = bullet.StartPosition + (bullet.Direction * bullet.Speed * curDelta);

				var query = PhysicsRayQueryParameters3D.Create(currentStart, currentEnd, ProjectileCollisionMask, RayExclusionList);
				var result = CurrentWorld.DirectSpaceState.IntersectRay(query);
				if (result.TryGetValue("position", out Variant outImpactPoint))
				{
					currentEnd = outImpactPoint.AsVector3();
					if (result.TryGetValue("collider", out Variant outCollider))
					{
						GD.Print("Collided with: ", outCollider.AsStringName());
					}
					ProjectilePool[i].IsActive = false;
				}
				else
				{
					var currentReach = bullet.StartPosition.DistanceTo(currentEnd);
					if (currentReach >= ProjectileReach)
					{
						ProjectilePool[i].IsActive = false;
					}
					else
					{
						ProjectilePool[i].CurrentDelta = curDelta;
					}
				}
				EmitSignal(SignalName.ProjectileLineCalculated, currentStart, currentEnd);
			}
		}
	}
}
