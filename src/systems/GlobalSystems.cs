using Godot;

namespace GlobalSystems;

public static class GlobalSystemsExtension
{
    public static EnhancedMouseInputSystem GetMouseSystem(this Node instance)
    {
        return EnhancedMouseInputSystem.Instance;
    }

    public static WorldDebugSystem GetWorldDebugSystem(this Node instance)
    {
        return WorldDebugSystem.Instance;
    }
}
