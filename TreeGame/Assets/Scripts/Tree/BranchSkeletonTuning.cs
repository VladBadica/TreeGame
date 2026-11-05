using System;

namespace TreeGame.Trees
{
    /// <summary>
    /// The numbers that shape a tree's branches. Angles are in degrees; lengths are in world units at full branch growth.
    /// More branches here means a bushier tree, independently of how fast the branches grow or how leafy they get.
    /// </summary>
    public sealed class BranchSkeletonTuning
    {
        /// <summary>
        /// How many main branches grow out of the trunk below where it splits into limbs. Few, as most of the crown is
        /// held up by the limbs.
        /// </summary>
        public int MainBranchCount { get; set; } = 4;

        /// <summary>
        /// How the main branches are spaced up the trunk: 1 spaces them evenly, and lower values pack more of them toward
        /// the top, the way a real tree branches more and more toward its crown.
        /// </summary>
        public float MainBranchSpacing { get; set; } = 0.6f;

        /// <summary>How many side branches grow out of each main branch (fewest, most).</summary>
        public int MinSideBranches { get; set; } = 2;
        public int MaxSideBranches { get; set; } = 4;

        /// <summary>
        /// How many twigs grow out of each side branch (fewest, most). Plenty, as a real crown is a mass of small
        /// branches that the big ones fork into.
        /// </summary>
        public int MinTwigs { get; set; } = 2;
        public int MaxTwigs { get; set; } = 4;

        /// <summary>
        /// Where the lowest and highest main branches leave the trunk, as a share of its height. High up, just below the
        /// fork, so the lower trunk stays clear: an open-grown broadleaf tree is bare for about the lower half.
        /// </summary>
        public float LowestMainBranch { get; set; } = 0.8f;
        public float HighestMainBranch { get; set; } = 0.96f;

        /// <summary>How far main branches lean from vertical: the lowest the most, the highest the least.</summary>
        public float LowestMainBranchTilt { get; set; } = 72f;
        public float HighestMainBranchTilt { get; set; } = 45f;

        /// <summary>
        /// The most any branch may lean from vertical. Below 90, so every branch points at least a little upward and
        /// none can ever reach the ground.
        /// </summary>
        public float MaxTilt { get; set; } = 75f;

        /// <summary>How far main branches may turn toward or away from the camera, so the tree has some depth.</summary>
        public float MainBranchDepthSpread { get; set; } = 25f;

        /// <summary>
        /// How far side branches and twigs may turn from their parent's direction, either way. Wide, so a branch forks
        /// out in several directions rather than running alongside its parent.
        /// </summary>
        public float ChildTurnSpread { get; set; } = 50f;

        /// <summary>
        /// How many limbs the trunk splits into at its top (fewest, most). A broadleaf tree's trunk doesn't run up through
        /// its crown: it forks into a few thick limbs that lean outward and hold the crown up. 0 for a trunk that just ends.
        /// </summary>
        public int MinLimbs { get; set; } = 3;
        public int MaxLimbs { get; set; } = 4;

        /// <summary>A limb's length; each varies a little around it.</summary>
        public float LimbLength { get; set; } = 1.6f;

        /// <summary>A limb's thickness as a share of the top of the trunk. Thick, as limbs together carry on the trunk.</summary>
        public float LimbThicknessRatio { get; set; } = 0.7f;

        /// <summary>
        /// How far the limbs lean from vertical, spread between these: the most upright stands at the middle of the
        /// crown, the others lean further out to either side.
        /// </summary>
        public float MinLimbTilt { get; set; } = 10f;
        public float MaxLimbTilt { get; set; } = 40f;

        /// <summary>
        /// The lengths of the lowest and highest main branches; those in between are spaced evenly. The lowest are the
        /// longest and most level, so the crown is broadest near its bottom, as on a tree grown in the open.
        /// </summary>
        public float LowestMainBranchLength { get; set; } = 1.8f;
        public float HighestMainBranchLength { get; set; } = 1.3f;

        /// <summary>
        /// A side branch or twig is this share of its parent's length (shortest, longest). Close to its parent's length,
        /// so a branch reads as forking in two rather than as a spoke with short stubs along it.
        /// </summary>
        public float MinChildLengthRatio { get; set; } = 0.6f;
        public float MaxChildLengthRatio { get; set; } = 0.8f;

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

        /// <summary>The most a branch curves upward along its length, so branches sweep up rather than run ruler-straight.</summary>
        public float MaxBend { get; set; } = 30f;

        /// <summary>Rejects numbers no tree could be built with, so mistakes show up at once rather than as odd behavior.</summary>
        public void Validate()
        {
            if (MainBranchCount < 0 || MinSideBranches < 0 || MinTwigs < 0 ||
                MaxSideBranches < MinSideBranches || MaxTwigs < MinTwigs)
            {
                throw new ArgumentException("Branch counts can't be negative, and each most must be at least its fewest.");
            }

            if (!(MainBranchSpacing > 0f && MainBranchSpacing <= 5f))
            {
                throw new ArgumentException("Main branch spacing must be above 0 (packed toward the top) and at most 5.");
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

            if (MinLimbs < 0 || MaxLimbs < MinLimbs || !IsPositive(LimbLength) || !IsShareAboveZero(LimbThicknessRatio))
            {
                throw new ArgumentException("Limb counts can't be negative (most at least fewest), and limbs need a positive " +
                                            "length and a thickness ratio above 0 and up to 1.");
            }

            if (!IsBetween(MinLimbTilt, 0f, MaxTilt) || !IsBetween(MaxLimbTilt, 0f, MaxTilt) || MinLimbTilt > MaxLimbTilt)
            {
                throw new ArgumentException("Limb tilts must be within the most a branch may lean, least first.");
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
