@tool
extends EditorScenePostImport

const prefix: String = "mixamorig_"

func _post_import(scene):
	var root_name = ""
	var first_root_child = "";
	var skeleton: Skeleton3D = scene.get_node_or_null("%GeneralSkeleton")
	var root_child_offset: Vector3 = Vector3.ZERO
	var root_pos: Vector3 = Vector3.ZERO
	if skeleton:
		if skeleton.get_bone_count() > 1:
			root_name = skeleton.get_bone_name(0)
			var root_child: PackedInt32Array = skeleton.get_bone_children(0)
			first_root_child = skeleton.get_bone_name(root_child[0]);
			root_pos = skeleton.get_bone_pose_position(0)
			var root_child_pos = skeleton.get_bone_pose_position(root_child[0])
			root_child_offset = root_child_pos - root_pos
			
	if first_root_child == "":
		return scene

	var path = get_source_file()
	var file_path = path.split('.').slice(0, -1)
	path = ".".join(file_path)
	var skeletonScene = PackedScene.new()
	var skeletonResult = skeletonScene.pack(skeleton)
	if skeletonResult == OK:
		var res = ResourceSaver.save(skeletonScene, path + '.tscn')
		print("saving: ", path + ".tscn ", res, OK)

	var anim_player: AnimationPlayer = scene.get_node_or_null("AnimationPlayer")
	if anim_player:
		for anim_name in anim_player.get_animation_list():
			var anim = anim_player.get_animation(anim_name)
			for track_idx in anim.get_track_count():
				var type = anim.track_get_type(track_idx)
				var track_name = anim.track_get_path(track_idx)
				var names = track_name.get_concatenated_names()
				var subnames = track_name.get_concatenated_subnames();
				if type == Animation.TYPE_POSITION_3D && subnames.contains(first_root_child):
					var key_count = anim.track_get_key_count(track_idx)
					if key_count > 0:
						var first_key = anim.track_get_key_value(track_idx, 0) as Vector3
						var root_offset = first_key - root_pos
						for key_idx in key_count:
							var keyValue = anim.track_get_key_value(track_idx, key_idx) as Vector3
							var new_key_value = keyValue - root_child_offset;
							anim.track_set_key_value(track_idx, key_idx, new_key_value)
						var new_name = NodePath(str(names) + ":" + subnames.replace(first_root_child, root_name))
						anim.track_set_path(track_idx, new_name)
	return scene
