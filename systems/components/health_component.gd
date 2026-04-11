class_name HealthComponent extends Node


var saved_attrib: DamageSystem.HealthAttribute

signal on_health_reach_zero()

func _ready() -> void:
    DamageSystem.register_object(self )
    DamageSystem.register_delegate(self , onhealth_changed)

func onhealth_changed(health_attrib: DamageSystem.HealthAttribute) -> void:
    saved_attrib = health_attrib
    if health_attrib.value <= 0:
        on_health_reach_zero.emit()
