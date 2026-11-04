using System;

namespace TreeGame.Trees
{
    /// <summary>
    /// The numbers that shape a tree's branches. Angles are in degrees; lengths are in world units at full branch growth.
    /// More branches here means a bushier tree, independently of how fast the branches grow or how leafy they get.
    /// </summary>
    public sealed class BranchSkeletonTuning
    {
        /// <summary>How many main branches grow out of the trunk.</summary>
        public int MainBranchCount { get; set; } = 6;

        /// <summary>How many side branches grow out of each main branch (fewest, most).</summary>
        public int MinSideBranches { get; set; } = 2;
        public int MaxSideBranches { get; set; } = 3;

        /// <summary>How many twigs grow out of each side branch (fewest, most).</summary>
        public int MinTwigs { get; set; } = 1;
        public int MaxTwigs { get; set; } = 3;

        /// <summary>Where the lowest and highest main branches leave the trunk, as a share of its height.</summary>
        public float LowestMainBranch { get; set; } = 0.35f;
        public float HighestMainBranch { get; set; } = 0.9f;

        /// <summary>How far main branches lean from vertical: the lowest the most, the highest the least.</summary>
        public float LowestMainBranchTilt { get; set; } = 68f;
        public float HighestMainBranchTilt { get; set; } = 35f;

        /// <summary>
        /// The most any branch may lean from vertical. Below 90, so every branch points at least a little upward and
        /// none can ever reach the ground.
        /// </summary>
        public float MaxTilt { get; set; } = 75f;

        /// <summary>How far main branches may turn toward or away from the camera, so the tree has some depth.</summary>
        public float MainBranchDepthSpread { get; set; } = 25f;

        /// <summary>How far side branches and twigs may turn from their parent's direction, either way.</summary>
        public float ChildTurnSpread { get; set; } = 40f;

        /// <summary>The lengths of the lowest and highest main branches; those in between are spaced evenly.</summary>
        public float LowestMainBranchLength { get; set; } = 1.7f;
        public float HighestMainBranchLength { get; set; } = 1f;

        /// <summary>A side branch or twig is this share of its parent's length (shortest, longest).</summary>
        public float MinChildLengthRatio { get; set; } = 0.45f;
        public float MaxChildLengthRatio { get; set; } = 0.6f;

        /// <summary>A branch is this share of its parent's thickness; for main branches, the parent is the trunk.</summary>
        public float MainBranchThicknessRatio { get; set; } = 0.4f;
        public float ChildThicknessRatio { get; set; } = 0.55f;

        /// <summary>Branch growth at which the lowest and highest main branches start to sprout.</summary>
        public float FirstMainBranchStart { get; set; } = 0f;
        public float LastMainBranchStart { get; set; } = 0.35f;

        /// <summary>
        /// How much later than its parent a side branch or twig starts to sprout, in branch growth (soonest, latest).
        /// Always later, so a tree never shows a twig before the branch it grows from.
        /// </summary>
        public float MinChildStartDelay { get; set; } = 0.15f;
        public float MaxChildStartDelay { get; set; } = 0.25f;

        /// <summary>The most a branch curves upward along its length, for a less rigid look.</summary>
        public float MaxBend { get; set; } = 15f;

        /// <summary>Rejects numbers no tree could be built with, so mistakes show up at once rather than as odd behavior.</summary>
        public void Validate()
        {
            if (MainBranchCount < 0 || MinSideBranches < 0 || MinTwigs < 0 ||
                MaxSideBranches < MinSideBranches || MaxTwigs < MinTwigs)
            {
                throw new ArgumentException("Branch counts can't be negative, and each most must be at least its fewest.");
            }

            if (!IsShare(LowestMainBranch) || !IsShare(HighestMainBranch) || LowestMainBranch > HighestMainBranch)
            {
                throw new ArgumentException("Main branches must leave the trunk between its base and top, lowest first.");
            }

            if (!IsBetween(MaxTilt, 0f, 90f, inclusiveMax: false))
            {
                throw new ArgumentException("The most a branch leans must be under 90 degrees, so every branch points upward.");
            }

            if (!IsBetween(LowestMainBranchTilt, 0f, MaxTilt) || !IsBetween(HighestMainBranchTilt, 0f, MaxTilt))
            {
                throw new ArgumentException("Main branch tilts must be between 0 and the most a branch may lean.");
            }

            if (!IsBetween(MainBranchDepthSpread, 0f, 90f) || !IsBetween(ChildTurnSpread, 0f, 180f) || !IsBetween(MaxBend, 0f, 90f))
            {
                throw new ArgumentException("Spreads and bends must be sensible angles.");
            }

            if (!IsPositive(LowestMainBranchLength) || !IsPositive(HighestMainBranchLength))
            {
                throw new ArgumentException("Main branch lengths must be positive.");
            }

            if (!IsShareAboveZero(MinChildLengthRatio) || !IsShareAboveZero(MaxChildLengthRatio) || MinChildLengthRatio > MaxChildLengthRatio)
            {
                throw new ArgumentException("Children must be shorter than their parents, by a ratio above 0 and up to 1.");
            }

            if (!IsShareAboveZero(MainBranchThicknessRatio) || !IsShareAboveZero(ChildThicknessRatio))
            {
                throw new ArgumentException("Branches must be thinner than what they grow from, by a ratio above 0 and up to 1.");
            }

            if (!IsShare(FirstMainBranchStart) || !IsShare(LastMainBranchStart) || FirstMainBranchStart > LastMainBranchStart)
            {
                throw new ArgumentException("Main branches must start sprouting between 0 and 1, lowest first.");
            }

            if (!IsPositive(MinChildStartDelay) || MaxChildStartDelay < MinChildStartDelay)
            {
                throw new ArgumentException("Children must start sprouting after their parents.");
            }

            // A twig starts two delays after its main branch, and must still have room to grow before branch growth is full.
            if (!(LastMainBranchStart + 2f * MaxChildStartDelay < 1f))
            {
                throw new ArgumentException("The last twigs would only start sprouting once branch growth is full.");
            }
        }

        private static bool IsShare(float value) => value >= 0f && value <= 1f;

        private static bool IsShareAboveZero(float value) => value > 0f && value <= 1f;

        private static bool IsPositive(float value) => value > 0f && float.IsFinite(value);

        private static bool IsBetween(float value, float min, float max, bool inclusiveMax = true)
        {
            return value >= min && (inclusiveMax ? value <= max : value < max);
        }
    }
}
