
public interface PlayerAbilityInterface
{
    public void Activate(double delta);

    public void Deactivate();

    public void Setup();

    public void CleanUp();

    protected PlayerContext Player { get; set; }

    public void Bind(PlayerContext owner)
    {
        Player = owner;
    }
}
