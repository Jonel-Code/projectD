extends Node3D

enum MouseTargetType {
	GROUND,
	ENEMY,
}


@export var spawn_ai_scene: PackedScene = null
@export var debug_target: PackedScene = null
@export var player_node: Node3D = null
@export var point_target_raycast_length: float = 1000
@export var character: CharacterBody3D = null;
@export var character_bullet_origin: Node3D = null;

var mouse_target_rid_ignore: Array = []
var mouse_target_collider: int = 4294967295
var debug_ray_cast: Array = []
var bullet_range: float = 1000


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	if character != null:
		mouse_target_rid_ignore.append(character.get_rid())

	pass # Replace with function body.

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	if Input.is_action_just_pressed("point_target"):
		_raycast_mouse_postion()
		
	# manage debug lines
	for i in range(debug_ray_cast.size() - 1, -1, -1):
		debug_ray_cast[i].life_time += delta
		if debug_ray_cast[i].life_time >= 1:
			debug_ray_cast[i].actor.queue_free()
			debug_ray_cast.remove_at(i)


func _spawn_enemy_at(world_position: Vector3) -> void:
	if spawn_ai_scene != null:
		var spawned = spawn_ai_scene.instantiate()
		if spawned != null:
			spawned.position = world_position
			add_child(spawned)
			var spawned_ai = spawned as AiEnemy
			if spawned_ai != null && player_node != null:
				spawned_ai.setup(player_node)


func _handle_player_target(target_position: Vector3, target_type: MouseTargetType = MouseTargetType.GROUND) -> void:
	if debug_target:
		var spawned = debug_target.instantiate()
		if spawned != null:
			spawned.position = target_position
			add_child(spawned)
	if character_bullet_origin != null:
		if target_type == MouseTargetType.ENEMY:
			fire_bullet_on_target(target_position)
		if target_type == MouseTargetType.GROUND:
			fire_bullet_on_ground(target_position)


func fire_bullet_on_target(target_position: Vector3) -> void:
	var bullet_origin = character_bullet_origin.global_position
	# TODO: instantly fire bullet towards the hit target
	draw_debug_line(bullet_origin, target_position, Color.RED)


func fire_bullet_on_ground(world_position: Vector3) -> void:
	var bullet_origin = character_bullet_origin.global_position
	# ground target uses the target as basis for the bullet angle
	# Adjust the bullet end to match the y (elevation) of the character's bullet origin
	# straight forward approach is just simply using y value of bullet origin as y value to the target point, this will introduce misalignment.
	# the ideal approach is to calculate the "linear interpolation" for a given y value in a segment (target_point -> camera_position)
	var tar_y = bullet_origin.y
	var end = get_viewport().get_camera_3d().global_position
	var start = world_position
	var t = (tar_y - start.y) / (end.y - start.y)
	var x = start.x + (t * (end.x - start.x))
	var z = start.z + (t * (end.z - start.z))
	var adjusted = Vector3(x, tar_y, z)
	# calculate the new bullet end
	var bullet_end = bullet_origin + ((adjusted - bullet_origin).normalized() * bullet_range)
	# TODO: fire bullet with from start to end
	draw_debug_line(bullet_origin, bullet_end, Color.GREEN)


func _raycast_mouse_postion() -> void:
	var mouse_pos = get_viewport().get_mouse_position()
	var main_camera = get_viewport().get_camera_3d()
	if main_camera:
		var origin = main_camera.global_position
		var direction = main_camera.project_ray_normal(mouse_pos)
		var end = (direction * point_target_raycast_length) + origin
		var query = PhysicsRayQueryParameters3D.create(origin, end, mouse_target_collider, mouse_target_rid_ignore)
		var result = get_world_3d().direct_space_state.intersect_ray(query)
		if result:
			var target_type = MouseTargetType.GROUND
			if result.collider is AiEnemy:
				target_type = MouseTargetType.ENEMY
			_handle_player_target(result.position, target_type)
			

func draw_debug_line(start: Vector3, end: Vector3, color: Color):
	var mesh_instance = MeshInstance3D.new()
	var immediate_mesh = ImmediateMesh.new()
	var material = ORMMaterial3D.new()
	mesh_instance.mesh = immediate_mesh
	mesh_instance.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	immediate_mesh.surface_begin(Mesh.PRIMITIVE_LINES, material)
	immediate_mesh.surface_add_vertex(start)
	immediate_mesh.surface_add_vertex(end)
	immediate_mesh.surface_end()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.albedo_color = color
	get_tree().get_root().add_child(mesh_instance)
	
	debug_ray_cast.append({
		"actor": mesh_instance,
		"life_time": 0
	})
