class_name MouseInputComponent extends Node

enum MouseInputHitType {
	GROUND,
	ENEMY,
}

class MouseInputData:
    var hit_type: MouseInputHitType
    var hit_position: Vector3

@export var object_to_ignore: Array[CollisionObject3D] = [];
@export var raycast_length: float = 1000

var mouse_target_rid_ignore: Array = []
var mouse_target_collider: int = 4294967295

func _ready() -> void:
    for i in range(object_to_ignore.size() - 1):
        mouse_target_rid_ignore.append(object_to_ignore[i].get_rid())

func create_raycast_from_camera(mouse_pos: Vector2) -> MouseInputData:
    var main_camera = get_viewport().get_camera_3d()
    if main_camera:
        var origin = main_camera.global_position
        var direction = main_camera.project_ray_normal(mouse_pos)
        var end = (direction * raycast_length) + origin
        var query = PhysicsRayQueryParameters3D.create(origin, end, mouse_target_collider, mouse_target_rid_ignore)
        var result = main_camera.get_world_3d().direct_space_state.intersect_ray(query)
        if result:
            var target_type = MouseInputHitType.GROUND
            if result.collider is AiEnemy:
                target_type = MouseInputHitType.ENEMY
            var data = MouseInputData.new()
            data.hit_type = target_type
            data.hit_position = result.position
            return data
    return null
