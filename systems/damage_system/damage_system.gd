extends Node

class HealthAttribute:
	var value: float
	var max_value: float

var base_hp: float = 100

var health_data: Dictionary = {}
var health_data_delegate: Dictionary = {}

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass


func register_object(node: Node) -> void:
	var node_id = node.get_instance_id()
	if !health_data.has(node_id):
		var new_health = HealthAttribute.new()
		new_health.value = base_hp
		new_health.max_value = base_hp
		health_data[node_id] = new_health

func register_delegate(node: Node, delegate: Callable) -> void:
	var node_id = node.get_instance_id()
	health_data_delegate[node_id] = delegate

func unregister_object(node: Node) -> void:
	var node_id = node.get_instance_id()
	if health_data.has(node_id):
		health_data.erase(node_id)
	health_data_delegate.erase(node_id)

func apply_damage(node: Node, damage: float) -> void:
	var node_id = node.get_instance_id()
	if health_data.has(node_id):
		health_data[node_id].value -= damage
		var cb: Callable = health_data_delegate[node_id]
		cb.call(health_data[node_id])
