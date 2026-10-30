using TreeGame.Care;

namespace TreeGame.Items
{
    /// <summary>
    /// Definition of an item that holds an amount of something, e.g. a bucket of water. Each container type
    /// declares its own capacity (and its default) in its own unit such as liters or grams, and which resource
    /// its contents restore when poured out.
    /// </summary>
    public abstract class ContainerDefinition : ItemDefinition
    {
        public abstract float Capacity { get; }

        /// <summary>The resource the contents go into when poured onto something, e.g. Water for a water bucket.</summary>
        public abstract TreeResourceType PouredResource { get; }
    }
}
