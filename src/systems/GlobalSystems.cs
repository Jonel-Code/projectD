using Godot;

namespace GameSystems;

public static class GlobalSystems
{
    public static EnhancedMouseInputSystem GetMouseSystem(this Node instance)
    {
        return EnhancedMouseInputSystem.Instance;
    }
}
