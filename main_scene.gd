extends Node3D

@export var spawn_ai_scene: PackedScene = null
@export var debug_target: PackedScene = null
@export var player_node: Node3D = null

var debug_ray_cast: Array = []


func _process(delta: float) -> void:
	for i in range(debug_ray_cast.size() - 1, -1, -1):
		debug_ray_cast[i].life_time -= delta
		if debug_ray_cast[i].life_time <= 0:
			debug_ray_cast[i].actor.queue_free()
			debug_ray_cast.remove_at(i)

func _handle_projectile_line(start: Vector3, end: Vector3) -> void:
	draw_debug_line(start, end, Color.GREEN)


func _spawn_enemy_at(world_position: Vector3) -> void:
	if spawn_ai_scene != null:
		var spawned = spawn_ai_scene.instantiate()
		if spawned != null:
			spawned.position = world_position
			add_child(spawned)
			var spawned_ai = spawned as AiEnemy
			if spawned_ai != null && player_node != null:
				spawned_ai.setup(player_node)


func draw_debug_line(start: Vector3, end: Vector3, color: Color, lifetime: float = 0.01):
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
		"life_time": lifetime
	})
