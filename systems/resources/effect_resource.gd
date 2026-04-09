@tool
extends Resource
class_name EffectResource

var effectName: String
var effectTypes: int

enum EffectType {
    DAMAGE = 1 << 1,
    BUFF = 1 << 2,
    DEBUFF = 1 << 3,
}
var effect_type_keys: Array = EffectType.keys()
var effect_type_list: String = ",".join(effect_type_keys)


func _get_property_list() -> Array[Dictionary]:
    var props: Array[Dictionary] = []

    props.append({
        "name": "effectName",
        "type": TYPE_STRING,
        "usage": PROPERTY_USAGE_DEFAULT,
    })

    props.append({
        "name": "effectTypes",
        "type": TYPE_INT,
        "usage": PROPERTY_USAGE_DEFAULT,
        "hint": PROPERTY_HINT_FLAGS,
        "hint_string": effect_type_list
    })

    return props