namespace TreeGame.Items
{
    /// <summary>
    /// Definition of an item that holds an amount of something, e.g. a bucket of water. Each container type
    /// declares its own capacity (and its default), in its own unit such as liters or grams.
    /// </summary>
    public abstract class ContainerDefinition : ItemDefinition
    {
        public abstract float Capacity { get; }
    }
}
