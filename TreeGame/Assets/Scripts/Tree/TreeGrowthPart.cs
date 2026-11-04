namespace TreeGame.Trees
{
    /// <summary>The parts of the tree that grow separately, each with its own growth from 0 to 1.</summary>
    public enum TreeGrowthPart
    {
        /// <summary>How tall and thick the trunk is.</summary>
        Trunk,

        /// <summary>How far the branches have spread.</summary>
        Branches,

        /// <summary>How densely the tree is covered in leaves.</summary>
        Foliage,
    }
}
