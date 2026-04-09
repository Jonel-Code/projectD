extends Node3D

@export var spawn_interval: float = 1.0
@export var spawn_count: int = 1

var last_spawned: float = 0.0

signal spawn_request(world_position: Vector3)

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	if spawn_count > 0:
		last_spawned += delta;
		if last_spawned > spawn_interval:
			last_spawned = 0.0
			spawn_count -= 1
			spawn_target()
	
	pass


func spawn_target() -> void:
	var spawn_pos = Vector3.FORWARD
	spawn_pos = spawn_pos.rotated(Vector3.UP, deg_to_rad(randf() * 360)) * int((randf() * 5) + 1)
	spawn_request.emit(global_position + spawn_pos)
	pass
