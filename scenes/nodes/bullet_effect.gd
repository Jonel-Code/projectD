extends Node3D

class_name BulletEffect

var lifetime: float = 1
var body_lifetime: float = 1

func _ready() -> void:
	lifetime = %bullet_dust.lifetime
	%bullet_dust.one_shot = true
	%bullet_dust.emitting = true
	

func _process(delta: float) -> void:
	lifetime -= delta
	body_lifetime -= delta
	if lifetime <= 0:
		queue_free()
	if body_lifetime <= 0:
		%bullet_body.hide()

func set_length(new_length: float) -> void:
	(%bullet_dust.process_material as ParticleProcessMaterial).emission_ring_height = new_length
	(%bullet_body.mesh as CylinderMesh).height = new_length

func set_body_lifetime(new_lifetime: float) -> void:
	body_lifetime = new_lifetime
