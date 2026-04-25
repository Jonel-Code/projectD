using Godot;

[GlobalClass]
public partial class CharacterResource : Resource
{
    [Export]
    public string Name { get; set; }

    [Export]
    public int MaxHealth { get; set; }

    [Export]
    public int MaxMana { get; set; }

    [Export]
    public int Strength { get; set; }

    [Export]
    public int Agility { get; set; }

    [Export]
    public int Intelligence { get; set; }
}