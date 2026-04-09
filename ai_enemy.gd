extends CharacterBody3D
class_name AiEnemy

@export var nav_3d: NavigationAgent3D = null
@export var target_node: Node3D = null

@export var check_interval: float = 1;
var last_check: float = 0;


const SPEED = 5.0
const JUMP_VELOCITY = 4.5

func setup(navtarget: Node3D) -> void:
	target_node = navtarget

func _ready() -> void:
	if nav_3d != null and target_node != null:
		nav_3d.target_position = target_node.position

func _process(delta: float) -> void:
	last_check += delta;
	if last_check > check_interval:
		last_check = 0.0
		if nav_3d != null and target_node != null:
			nav_3d.target_position = target_node.position


func _physics_process(delta: float) -> void:
	if nav_3d != null:
		if !nav_3d.is_navigation_finished():
			var current_pos = position
			var target_pos = nav_3d.get_next_path_position()
			var velocity_delta = (target_pos - current_pos).normalized() * SPEED
			velocity = velocity_delta
		else:
			velocity = Vector3.ZERO

	if not is_on_floor():
		velocity += get_gravity() * delta

	move_and_slide()
