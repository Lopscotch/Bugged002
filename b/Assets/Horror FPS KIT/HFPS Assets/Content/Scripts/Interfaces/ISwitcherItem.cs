public interface ISwitcher
{
    void Select();
    void Deselect();
    void Disable();
    void EnableItem();
    void OnItemBlock(bool blocked);
}

public interface ISwitcherWallHit
{
    void OnWallHit(bool Hit);
}