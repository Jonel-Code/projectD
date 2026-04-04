extends PlayerMovement


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta: float) -> void:
	var yaw_input = Input.get_vector("ui_left", "ui_right", "ui_down", "ui_up")
	if yaw_input.length() > 0:
		MoveYaw(yaw_input)
	pass
