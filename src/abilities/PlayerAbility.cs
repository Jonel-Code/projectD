
public interface PlayerAbilityInterface
{
    public void Activate(double delta);

    public void Deactivate();

    public void Setup();

    public void CleanUp();

    protected CharacterContext Player { get; set; }

    public void Bind(CharacterContext owner)
    {
        Player = owner;
    }
}
