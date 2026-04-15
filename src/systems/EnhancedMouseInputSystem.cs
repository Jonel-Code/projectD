using Godot;

[GlobalClass]
public partial class EnhancedMouseInputSystem : Node
{
	public static EnhancedMouseInputSystem Instance { get; private set; }

	public float MaxRaycastDistance { get; set; } = 100000;
	public override void _Ready()
	{
		Instance = this;
	}

	public bool GetMouseWorldPosition(out Vector3 hitPosition, out Rid? hitRid, uint collisionMask = (uint)4294967295, Godot.Collections.Array<Rid> ignoredObjects = null)
	{
		var mainCamera = GetViewport().GetCamera3D();
		var mousePosition = GetViewport().GetMousePosition();
		hitPosition = Vector3.Zero;
		hitRid = null;
		if (mainCamera != null)
		{
			var cameraOrigin = mainCamera.GlobalPosition;
			var end = mainCamera.ProjectRayNormal(mousePosition);
			end = cameraOrigin + (end * MaxRaycastDistance);
			var query = PhysicsRayQueryParameters3D.Create(cameraOrigin, end, collisionMask);
			var hit = mainCamera.GetWorld3D().DirectSpaceState.IntersectRay(query);
			if (hit.TryGetValue("position", out var rayHitImpact))
			{
				hitPosition = rayHitImpact.AsVector3();
				if (hit.TryGetValue("collider", out var rayCollider))
				{
					hitRid = rayCollider.As<CollisionObject3D>().GetRid();
				}
				return true;
			}
		}
		return false;
	}
}

