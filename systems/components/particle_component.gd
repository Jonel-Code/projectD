class_name ParticleComponent extends Node

@export var particle: PackedScene = null

class ParticleContainer:
    var particle: Node
    var life_time: float = 1

var spawned: Array[ParticleContainer] = []


func _ready() -> void:
    ImpactSystem.process_impact.connect(_show_particle)

func _show_particle(world_pos: Vector3, _force: Vector3) -> void:
    spawn_particle_at(world_pos)
    

func _process(delta: float) -> void:
    for i in range(spawned.size() - 1, -1, -1):
        spawned[i].life_time -= delta
        if spawned[i].life_time <= 0:
            spawned[i].particle.queue_free()
            spawned.remove_at(i)


func spawn_particle_at(world_pos: Vector3, input_rot: Vector3 = Vector3.ZERO):
    if particle:
        var instance = particle.instantiate()
        instance.position = world_pos
        if instance is GPUParticles3D:
            add_child(instance)
            var par = instance as GPUParticles3D
            par.one_shot = true
            par.emitting = true
            par.rotation = Basis.looking_at(input_rot, Vector3.UP).get_euler()
            var container = ParticleContainer.new()
            container.particle = instance
            container.life_time = par.lifetime * par.amount
            spawned.append(container)
    pass