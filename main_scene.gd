extends Node3D

@export var spawn_ai_scene: PackedScene = null
@export var debug_target: PackedScene = null
@export var player_node: Node3D = null

func _spawn_enemy_at(world_position: Vector3) -> void:
	if spawn_ai_scene != null:
		var spawned = spawn_ai_scene.instantiate()
		if spawned != null:
			spawned.position = world_position
			add_child(spawned)
			var spawned_ai = spawned as AiEnemy
			if spawned_ai != null && player_node != null:
				spawned_ai.setup(player_node)
