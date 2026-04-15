using System;
using GlobalSystems;
using Godot;
using WorldUtils;

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
	public ProjectileResource Resource { get; set; }

	[Export]
	public string FireActionName { get; set; } = "";

	[Export]
	public Node3D ProjectileOrigin { get; set; } = null;

	[Export]
	public CollisionObject3D OwnerCollision { get; set; } = null;

	[Signal]
	public delegate void ProjectileLineCalculatedEventHandler(Vector3 start, Vector3 end);

	protected ProjectileData[] ProjectilePool = [];

	protected uint ProjectileCollisionMask => (1 << 5) - 1;

	protected bool DidInputFire => FireActionName.Length > 0 && Input.IsActionPressed(FireActionName);

	private double ShotCooldown = 0;

	private World3D CurrentWorld;

	private Godot.Collections.Array<Rid> RayExclusionList = [];


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Array.Resize(ref ProjectilePool, Resource.ProjectilePoolSize);
		for (int i = 0; i < Resource.ProjectilePoolSize; i++)
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
					FireBulletTowards(ProjectileOrigin.GlobalPosition + new Vector3(0f, 0f, -1f));
					ShotCooldown = Resource.ShotInterval;
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
			ProjectilePool[i].TargetEnd = start + (direction * Resource.ProjectileReach);
			ProjectilePool[i].Speed = Resource.Speed;
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
			if (ProjectilePool[i].IsActive)
			{
				var bullet = ProjectilePool[i];
				float prevDelta = bullet.CurrentDelta;
				float curDelta = prevDelta + (float)delta;

				var currentStart = bullet.StartPosition + (bullet.Direction * bullet.Speed * prevDelta);
				var currentEnd = bullet.StartPosition + (bullet.Direction * bullet.Speed * curDelta);
				ProjectilePool[i].CurrentDelta = curDelta;

				var direction = (currentEnd - currentStart).Normalized();
				var height = currentStart.DistanceTo(currentEnd);
				var capsuleWorldPosition = currentStart + (direction * height * 0.5f);
				var capsule = new CapsuleShape3D
				{
					Radius = 0.9f,
					Height = height
				};
				var capsuleTransform = new Transform3D
				{
					Origin = capsuleWorldPosition,
				};
				capsuleTransform = capsuleTransform.PointYTowards(direction);
				var shapedQuery = new PhysicsShapeQueryParameters3D
				{
					Shape = capsule,
					Transform = capsuleTransform,
					CollisionMask = ProjectileCollisionMask,
					Exclude = RayExclusionList
				};

				var results = CurrentWorld.DirectSpaceState.IntersectShape(shapedQuery);
				if (results.Count > 0)
				{
					var result = results[0];
					if (result.TryGetValue("collider", out Variant outCollider))
					{
						var collider = outCollider.As<CollisionObject3D>();
						// Intersect shape does not provide intersection point, 
						// so we create a new raycast and get the projection of the ray 
						// towards the current end to identify the actual end point 
						var rayQuery = PhysicsRayQueryParameters3D.Create(currentStart, collider.GlobalPosition, ProjectileCollisionMask, RayExclusionList);
						var rayResult = CurrentWorld.DirectSpaceState.IntersectRay(rayQuery);
						if (rayResult.TryGetValue("position", out Variant outImpact))
						{
							var impactEnd = outImpact.AsVector3();
							currentEnd = currentStart.ProjectPoints(impactEnd, currentEnd);
						}
					}
					ProjectilePool[i].IsActive = false;
				}
				else
				{
					var currentReach = bullet.StartPosition.DistanceTo(currentEnd);
					if (currentReach >= Resource.ProjectileReach)
					{
						ProjectilePool[i].IsActive = false;
					}
				}

				// TODO: replace this with GPU particle/effect
				this.GetWorldDebugSystem().DebugCapsule(currentStart, currentEnd, capsule.Radius, Colors.Red, 0.1);
			}
		}
	}
}
