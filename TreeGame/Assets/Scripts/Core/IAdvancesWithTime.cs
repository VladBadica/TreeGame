namespace TreeGame.Core
{
    /// <summary>
    /// Something in the world that changes as time passes, such as growth or depletion. Normal frames and catching up
    /// after the world was switched off for a while (e.g. during a minigame) go through the same method.
    /// </summary>
    public interface IAdvancesWithTime
    {
        void Advance(float seconds);
    }
}
