@tool
extends EditorScenePostImport

func _post_import(scene):
	var skeleton: Skeleton3D = scene.get_node_or_null("%GeneralSkeleton")
	if skeleton:
		set_owner_recursive(skeleton, skeleton)
		var path = get_source_file()
		var file_path = path.split('.').slice(0, -1)
		path = ".".join(file_path)
		var skeletonScene = PackedScene.new()
		var skeletonResult = skeletonScene.pack(skeleton)
		if skeletonResult == OK:
			ResourceSaver.save(skeletonScene, path + '.tscn')

	return scene

# Helper function to ensure children are recognized as part of the scene branch
func set_owner_recursive(node: Node, root: Node):
	for child in node.get_children():
		child.owner = root
		set_owner_recursive(child, root)