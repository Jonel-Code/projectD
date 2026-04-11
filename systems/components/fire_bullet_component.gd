class_name FireBulletComponent extends Node


@export var particle_component: ParticleComponent
@export var bullet_origin_socket: Node3D
@export var mouse_input_component: MouseInputComponent
@export var bullet_interval: float = 0.5
@export var collision_to_ignore: Array[CollisionObject3D] = []
@export var bullet_dmg: float = 10

var debug_ray_cast: Array = []
var bullet_range: float = 1000
var fire_cooldown_timer: float = 0
var bullet_collider_ignore_list: Array = []
var bullet_collision_mask: int = 4294967295

func _ready() -> void:
    for item in collision_to_ignore:
        bullet_collider_ignore_list.append(item.get_rid())

func _process(delta: float) -> void:
    if mouse_input_component != null:
        for i in range(debug_ray_cast.size() - 1, -1, -1):
            debug_ray_cast[i].life_time += delta
            if debug_ray_cast[i].life_time >= 1:
                debug_ray_cast[i].actor.queue_free()
                debug_ray_cast.remove_at(i)

        if fire_cooldown_timer <= 0:
            if Input.is_action_pressed("fire_bullet"):
                fire_cooldown_timer = bullet_interval
                var mouse_pos = get_viewport().get_mouse_position()
                var result = mouse_input_component.create_raycast_from_camera(mouse_pos)
                fire_bullet_to(result)
        else:
            fire_cooldown_timer -= delta


func fire_bullet_to(data: MouseInputComponent.MouseInputData) -> void:
    if bullet_origin_socket != null:
        var bullet_origin = bullet_origin_socket.global_position
        var end = data.hit_position
        if data.hit_type == MouseInputComponent.MouseInputHitType.GROUND:
            end = get_bullet_end_position(data.hit_position)
        else:
            var adjustement = (end - bullet_origin).normalized() * bullet_range
            end = bullet_origin + adjustement
        _check_bullet_hit(bullet_origin, end)

func _check_bullet_hit(start: Vector3, end: Vector3) -> void:
    var query = PhysicsRayQueryParameters3D.create(start, end, bullet_collision_mask, bullet_collider_ignore_list)
    var result = get_viewport().get_world_3d().direct_space_state.intersect_ray(query)
    var new_end = end
    if result:
        var dmg = bullet_dmg
        if result.collider is AiEnemy:
            var enemy = result.collider as AiEnemy
            var force = (end - start).normalized() * 5
            enemy.apply_impact(result.position, force)
            # TODO: identify the dmg base on the hit postion
        new_end = result.position
        DamageSystem.apply_damage(result.collider, dmg)
    # draw_debug_line(start, new_end, Color.GREEN)
    if particle_component != null:
        var impact_rot = (end - start).normalized()
        particle_component.spawn_particle_at(new_end, impact_rot)


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
