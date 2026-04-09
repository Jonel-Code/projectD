extends Node3D

@export var max_life: float = 1

var life_left: float = 0

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	life_left = max_life
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	life_left = clamp(life_left - delta, 0, max_life)
	if life_left <= 0:
		queue_free()
	pass
