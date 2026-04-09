extends Node3D

@export var follow_target: Node3D
@export var camera_distance: float
@export var max_lag: float = 1
@export var camera_instance: Camera3D
@export var camera_downward_angle: float = 0

var follow_offset: Vector3
var current_camera_lag: float = 1
var focused_camera_lag: float = 1

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	current_camera_lag = max_lag
	focused_camera_lag = max_lag / 4

	var real_camera_angle = - camera_downward_angle
	var camera_forward_vector = Vector2.RIGHT.rotated(deg_to_rad(real_camera_angle))
	var camera_offset = camera_forward_vector * -camera_distance
	if camera_instance:
		# uses -camera_offset.x as z value because godot's coordinate system is negative z value is going forward
		camera_instance.position = Vector3(0, camera_offset.y, -camera_offset.x)
		camera_instance.rotation = Vector3(deg_to_rad(real_camera_angle), 0, 0)


	pass # Replace with function body.

func _process(delta: float) -> void:
	#  camera focus reduces the camera lag shorter allowing for character to be more in-center
	if Input.is_action_pressed("camera_focus"):
		current_camera_lag = lerp(current_camera_lag, focused_camera_lag, delta * 4)
	else:
		if current_camera_lag != max_lag:
			current_camera_lag = max_lag


		
func _physics_process(delta: float) -> void:
	if follow_target:
		var base_pos = global_position
		var tar_pos = follow_target.global_position
		var alpha = (tar_pos - base_pos)
		var new_base = tar_pos
		var alpha_length = alpha.length()
		if alpha_length > current_camera_lag:
			new_base = tar_pos - (alpha.normalized() * current_camera_lag)
		else:
			new_base = global_position.lerp(tar_pos, delta * (current_camera_lag / (current_camera_lag - alpha_length)))
		global_position = new_base
