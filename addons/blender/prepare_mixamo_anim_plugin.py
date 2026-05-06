import bpy
from bpy_extras import anim_utils

bl_info = {
    "name": "Batch Root Bone Setup (collection)",
    "author": "Gemini Ai (Adjusted by Dev-Jonel)",
    "version": (1, 2),
    "blender": (5, 1, 0),
    "location": "View3D > Sidebar > Rigging",
    "description": "Adds a root bone and moves ONLY location channels to a new group",
    "category": "Rigging",
}

class RIG_OT_batch_root_setup(bpy.types.Operator):
    """Adds a new root bone; moves ONLY location channels to a new 'root' group"""
    bl_idname = "rig.batch_root_setup"
    bl_label = "Process Armature Collection"
    bl_options = {'REGISTER', 'UNDO'}

    collection_name: bpy.props.StringProperty(
        name="Collection",
        default="Collection"
    )

    def process_armature(self, obj):
        armature = obj.data
        
        # 1. Identify current root
        original_root = next((b for b in armature.bones if b.parent is None), None)
        if not original_root:
            return False
        original_root_name = original_root.name

        # 2. Add 'root' bone in EDIT MODE
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.mode_set(mode='EDIT')
        new_root_name = "root"
        new_root = armature.edit_bones.get(new_root_name) or armature.edit_bones.new(new_root_name)
        new_root.head = (0, 0, 0)
        new_root.tail = (0, 50, 0)

        # 4. Final Parenting in EDIT MODE
        bpy.ops.object.mode_set(mode='EDIT')
        edit_original = armature.edit_bones.get(original_root_name)
        edit_new_root = armature.edit_bones.get(new_root_name)
        
        if edit_original and edit_new_root and edit_original != edit_new_root:
            edit_original.parent = edit_new_root
            edit_original.use_connect = False
        
        bpy.ops.object.mode_set(mode='OBJECT')
        return True

    def execute(self, context):
        target_col = bpy.data.collections.get(self.collection_name)
        if not target_col:
            self.report({'ERROR'}, f"Collection '{self.collection_name}' not found.")
            return {'CANCELLED'}

        armatures = [o for o in target_col.objects if o.type == 'ARMATURE']
        success_count = sum(1 for arm in armatures if self.process_armature(arm))
        
        self.report({'INFO'}, f"Processed {success_count} armatures.")
        return {'FINISHED'}

# (UI Panel and Register/Unregister remain the same as previous version)
class RIG_PT_root_setup_panel(bpy.types.Panel):
    bl_label = "Root Setup"
    bl_idname = "RIG_PT_root_setup_panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'Rigging'

    def draw(self, context):
        layout = self.layout
        col = layout.column()
        col.prop(context.scene, "target_armature_collection", text="Collection")
        op = col.operator("rig.batch_root_setup", text="Setup Roots")
        op.collection_name = context.scene.target_armature_collection

def register():
    bpy.utils.register_class(RIG_OT_batch_root_setup)
    bpy.utils.register_class(RIG_PT_root_setup_panel)
    bpy.types.Scene.target_armature_collection = bpy.props.StringProperty(name="Collection", default="Collection")

def unregister():
    bpy.utils.unregister_class(RIG_OT_batch_root_setup)
    bpy.utils.unregister_class(RIG_PT_root_setup_panel)
    del bpy.types.Scene.target_armature_collection

if __name__ == "__main__":
    register()