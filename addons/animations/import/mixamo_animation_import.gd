@tool
extends EditorScenePostImport

const prefix: String = "mixamorig_"

func _post_import(scene):
	var anim_player: AnimationPlayer = scene.get_node_or_null("AnimationPlayer")
	if anim_player:
		for anim_name in anim_player.get_animation_list():
			var anim = anim_player.get_animation(anim_name)
			for track_idx in anim.get_track_count():
				var type = anim.track_get_type(track_idx)
				var track_name = anim.track_get_path(track_idx)
				var names = track_name.get_concatenated_names()
				var subnames = track_name.get_concatenated_subnames();
				var new_name = NodePath(str(names) + ":" + subnames.replace(prefix, ""))
				print("anim_track: ", anim_name, " -> ", track_name, " to: ", new_name)
				anim.track_set_path(track_idx, new_name)
	
	var skeleton: Skeleton3D = scene.get_node_or_null("Armature/Skeleton3D")
	if skeleton:
		for bone_idx in skeleton.get_bone_count():
			var bone_name = skeleton.get_bone_name(bone_idx)
			var new_bone_name = bone_name.replace(prefix, "")
			print("bone: ", bone_name, " to ", new_bone_name)
			skeleton.set_bone_name(bone_idx, new_bone_name)

	return scene
