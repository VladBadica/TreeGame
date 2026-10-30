namespace TreeGame.Items
{
    /// <summary>A bucket of water, measured in liters. Using it pours the water onto what the player stands at.</summary>
    public class WaterBucket : ContainerItem
    {
        public WaterBucket(WaterBucketDefinition definition) : base(definition)
        {
        }
    }
}
