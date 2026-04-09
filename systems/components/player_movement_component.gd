class_name PlayerMovementComponent extends Node

@export var character_body: CharacterBody3D
@export var ms: float = 5.0

const JUMP_VELOCITY = 4.5

func _process(delta: float) -> void:
	if character_body is CharacterBody3D:
		# Add the gravity.
		if not character_body.is_on_floor():
			character_body.velocity += character_body.get_gravity() * delta

		# Handle jump.
		if Input.is_action_just_pressed("ui_accept") and character_body.is_on_floor():
			character_body.velocity.y = JUMP_VELOCITY

		# Get the input direction and handle the movement/deceleration.
		# As good practice, you should replace UI actions with custom gameplay actions.
		var input_dir := Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
		var direction := (character_body.transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
		if direction:
			character_body.velocity.x = direction.x * ms
			character_body.velocity.z = direction.z * ms
		else:
			character_body.velocity.x = move_toward(character_body.velocity.x, 0, ms)
			character_body.velocity.z = move_toward(character_body.velocity.z, 0, ms)

		character_body.move_and_slide()
