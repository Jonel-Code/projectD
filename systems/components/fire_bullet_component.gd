class_name FireBulletComponent extends Node


@export var particle_component: ParticleComponent
@export var bullet_origin_socket: Node3D
@export var mouse_input_component: MouseInputComponent
@export var bullet_interval: float = 0.5
@export var collision_to_ignore: Array[CollisionObject3D] = []
@export var bullet_dmg: float = 10
@export var bullet_speed: float = 1000
@export var bullet_range: float = 1000
@export var bullet_particle_scene: PackedScene

class BulletData:
	var current_delta: float
	var origin: Vector3
	var end: Vector3
	var direction: Vector3
	var accel: float
	var bullet_lifetime: float

var spawned_bullet: Array[BulletData] = []
var debug_ray_cast: Array = []
var fire_cooldown_timer: float = 0
var bullet_collider_ignore_list: Array = []
var bullet_collision_mask: int = 4294967295

func _ready() -> void:
	for item in collision_to_ignore:
		bullet_collider_ignore_list.append(item.get_rid())

func _process(delta: float) -> void:
	if mouse_input_component != null:
		for i in range(debug_ray_cast.size() - 1, -1, -1):
			debug_ray_cast[i].life_time -= delta
			if debug_ray_cast[i].life_time <= 0:
				debug_ray_cast[i].actor.queue_free()
				debug_ray_cast.remove_at(i)

		if fire_cooldown_timer <= 0:
			if Input.is_action_pressed("fire_bullet"):
				fire_cooldown_timer = bullet_interval
				var mouse_pos = get_viewport().get_mouse_position()
				var result = mouse_input_component.create_raycast_from_camera(mouse_pos)
				fire_bullet(result)
		else:
			fire_cooldown_timer -= delta
	
	for i in range(spawned_bullet.size() - 1, -1, -1):
		var prev_delta = spawned_bullet[i].current_delta
		var cur_delta = spawned_bullet[i].current_delta + delta
		var dir = spawned_bullet[i].direction
		var line_start = spawned_bullet[i].origin + (dir * prev_delta * spawned_bullet[i].accel)
		var line_end = spawned_bullet[i].origin + (dir * cur_delta * spawned_bullet[i].accel)

		if cur_delta >= spawned_bullet[i].bullet_lifetime:
			spawned_bullet.remove_at(i)
			continue
		elif cur_delta >= spawned_bullet[i].bullet_lifetime * 0.9:
			if line_start.distance_to(line_end) > line_start.distance_to(spawned_bullet[i].end):
				line_end = spawned_bullet[i].end

		# TODO: render projectile particle
		# draw_debug_line(line_start, line_end, Color.RED, 0.1)
		var hit = _check_bullet_hit(line_start, line_end)
		if hit:
			spawned_bullet.remove_at(i)
			continue
		spawned_bullet[i].current_delta = cur_delta


func spawn_bullet_particle_at(start: Vector3, end: Vector3) -> void:
	if bullet_particle_scene != null:
		var position = (start + end) / 2
		var rotation = (end - start).normalized()
		var instance = bullet_particle_scene.instantiate() as BulletEffect
		instance.set_length(end.distance_to(start))
		instance.set_body_lifetime(get_process_delta_time() * 2) # show only the body for 2 frames
		var euler_rot = Basis.looking_at(rotation, Vector3.UP).get_euler()
		instance.rotation = euler_rot
		add_child(instance)
		instance.global_position = position


func fire_bullet(data: MouseInputComponent.MouseInputData) -> void:
	if bullet_origin_socket != null:
		var bullet_origin = bullet_origin_socket.global_position
		var end = data.hit_position
		if data.hit_type == MouseInputComponent.MouseInputHitType.GROUND:
			end = get_bullet_end_position(data.hit_position)
		else:
			var adjustement = (end - bullet_origin).normalized() * bullet_range
			end = bullet_origin + adjustement
		var new_bullet = BulletData.new()
		new_bullet.current_delta = 0
		new_bullet.origin = bullet_origin
		new_bullet.direction = (end - bullet_origin).normalized()
		new_bullet.end = bullet_origin + (new_bullet.direction * bullet_range)
		new_bullet.accel = bullet_speed
		new_bullet.bullet_lifetime = bullet_range / bullet_speed
		spawned_bullet.append(new_bullet)


func _check_bullet_hit(start: Vector3, end: Vector3) -> bool:
	var query = PhysicsRayQueryParameters3D.create(start, end, bullet_collision_mask, bullet_collider_ignore_list)
	var result = get_viewport().get_world_3d().direct_space_state.intersect_ray(query)
	var new_end = end
	var did_hit = false
	if result:
		var dmg = bullet_dmg
		did_hit = true
		new_end = result.position
		if result.collider is AiEnemy:
			var enemy = result.collider as AiEnemy
			var force = (end - start).normalized() * 5
			enemy.apply_impact(result.position, force)
			# TODO: identify the dmg base on the hit postion
			DamageSystem.apply_damage(result.collider, dmg)
			# draw_debug_line(start, new_end, Color.GREEN)
			if particle_component != null:
				var impact_rot = (end - start).normalized()
				particle_component.spawn_particle_at(new_end - impact_rot, impact_rot)
	spawn_bullet_particle_at(start, new_end)
	return did_hit


func get_bullet_end_position(worldspace_mouse_input: Vector3) -> Vector3:
	if bullet_origin_socket != null:
		var bullet_origin = bullet_origin_socket.global_position
		# ground target uses the target as basis for the bullet angle
		# Adjust the bullet end to match the y (elevation) of the character's bullet origin
		# straightforward approach is just simply using y value of bullet origin as y value to the target point, this will introduce misalignment.
		# the ideal approach is to calculate the "linear interpolation" for a given y value in a segment (target_point -> camera_position)
		var tar_y = bullet_origin.y
		var end = get_viewport().get_camera_3d().global_position
		var start = worldspace_mouse_input
		var t = (tar_y - start.y) / (end.y - start.y)
		var x = start.x + (t * (end.x - start.x))
		var z = start.z + (t * (end.z - start.z))
		var adjusted = Vector3(x, tar_y, z)
		# calculate the new bullet end
		return bullet_origin + ((adjusted - bullet_origin).normalized() * bullet_range)

	return worldspace_mouse_input

	
func draw_debug_line(start: Vector3, end: Vector3, color: Color, lifetime: float = 1):
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
