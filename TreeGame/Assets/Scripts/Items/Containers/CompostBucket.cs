namespace TreeGame.Items
{
    /// <summary>A bucket of compost, measured in grams. Using it feeds the compost to what the player stands at.</summary>
    public class CompostBucket : ContainerItem
    {
        public CompostBucket(CompostBucketDefinition definition) : base(definition)
        {
        }
    }
}
