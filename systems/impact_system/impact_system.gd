extends Node

signal process_impact(world_position: Vector3, force: Vector3)

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.

func apply_impact(world_position: Vector3, force: Vector3) -> void:
	process_impact.emit(world_position, force)