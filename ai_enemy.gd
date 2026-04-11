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
	DamageSystem.register_object(self )
	DamageSystem.register_delegate(self , on_health_changed)

	var current = DamageSystem.get_value(self )
	if current != null:
		update_hp_bar(current)

	if nav_3d != null and target_node != null:
		nav_3d.target_position = target_node.position

func _exit_tree() -> void:
	DamageSystem.unregister_object(self )

func on_health_changed(health: DamageSystem.HealthAttribute) -> void:
	update_hp_bar(health)
	if health.value <= 0:
		self.queue_free()

func _process(delta: float) -> void:
	last_check += delta;
	if last_check > check_interval:
		last_check = 0.0
		if nav_3d != null and target_node != null:
			nav_3d.target_position = target_node.position

func _physics_process(delta: float) -> void:
	var path_find_velocity: Vector3 = Vector3.ZERO
	if nav_3d != null:
		if !nav_3d.is_navigation_finished():
			var current_pos = position
			var target_pos = nav_3d.get_next_path_position()
			var velocity_delta = (target_pos - current_pos).normalized() * SPEED
			path_find_velocity = velocity_delta
		else:
			path_find_velocity = Vector3.ZERO

	velocity = velocity.lerp(path_find_velocity, delta * 10)

	if not is_on_floor():
		velocity += get_gravity() * delta
	
	velocity.x = move_toward(velocity.x, 0, delta)
	velocity.z = move_toward(velocity.z, 0, delta)

	move_and_slide()

func apply_impact(world_position: Vector3, force: Vector3):
	var local_pos = to_local(world_position)
	print("applying impact on: ", local_pos)
	velocity += force

func update_hp_bar(health: DamageSystem.HealthAttribute) -> void:
	%HpBarViewport.update_value((health.value / health.max_value) * 100)
