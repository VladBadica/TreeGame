using System;
using System.Collections.Generic;
using System.Numerics;

namespace TreeGame.Trees
{
    /// <summary>One branch of a <see cref="BranchSkeleton"/>, as it is when fully grown.</summary>
    public sealed class BranchSpec
    {
        internal BranchSpec(int index, int parentIndex, int level, float attachAlong, float tilt, float yaw,
            float length, float thicknessRatio, float startGrowth, float bend, bool isLimb)
        {
            IsLimb = isLimb;
            Index = index;
            ParentIndex = parentIndex;
            Level = level;
            AttachAlong = attachAlong;
            Tilt = tilt;
            Yaw = yaw;
            Length = length;
            ThicknessRatio = thicknessRatio;
            StartGrowth = startGrowth;
            Bend = bend;
        }

        public int Index { get; }

        /// <summary>
        /// True for a limb: one of the thick branches the trunk splits into at its very top. Limbs are at the main branch
        /// level but aren't among the main branches spaced up the trunk.
        /// </summary>
        public bool IsLimb { get; }

        /// <summary>The branch this one grows out of, or <see cref="BranchSkeleton.Trunk"/> for a main branch.</summary>
        public int ParentIndex { get; }

        /// <summary>1 for a main branch, 2 for a side branch, 3 for a twig.</summary>
        public int Level { get; }

        /// <summary>Where it leaves its parent, as a share of the parent's length (for main branches, the trunk's height).</summary>
        public float AttachAlong { get; }

        /// <summary>Degrees it leans from vertical. Always under 90, so it points at least a little upward.</summary>
        public float Tilt { get; }

        /// <summary>
        /// Degrees it faces around the vertical: 0 is to the right, 180 to the left, and values between turn it away
        /// from the camera (positive) or toward it (negative).
        /// </summary>
        public float Yaw { get; }

        /// <summary>World units at full branch growth.</summary>
        public float Length { get; }

        /// <summary>Its thickness as a share of its parent's; for main branches, the parent is the trunk.</summary>
        public float ThicknessRatio { get; }

        /// <summary>The branch growth at which it starts to sprout. Always after its parent.</summary>
        public float StartGrowth { get; }

        /// <summary>Degrees it curves upward along its length.</summary>
        public float Bend { get; }

        /// <summary>The way it points, as a length of 1, in the tree's space: X to the right, Y up, Z away from the camera.</summary>
        public Vector3 Direction
        {
            get
            {
                float tilt = Tilt * BranchSkeleton.DegreesToRadians;
                float yaw = Yaw * BranchSkeleton.DegreesToRadians;
                return new Vector3(MathF.Sin(tilt) * MathF.Cos(yaw), MathF.Cos(tilt), MathF.Sin(tilt) * MathF.Sin(yaw));
            }
        }
    }

    /// <summary>
    /// The layout of a tree's branches, built once from a seed so the same seed always gives the same tree: main branches
    /// out of the trunk, the limbs it splits into at its top, side branches out of those, and twigs out of the side
    /// branches. It holds every branch the tree
    /// can ever have; branch growth only decides how far each has grown. Positions are straight lines from base to tip;
    /// any curve is for the visuals to draw. Plain C# with no Unity dependency, so it can be checked on its own.
    /// </summary>
    public sealed class BranchSkeleton
    {
        /// <summary>The <see cref="BranchSpec.ParentIndex"/> of a branch that grows out of the trunk.</summary>
        public const int Trunk = -1;

        public const int MainLevel = 1;
        public const int TwigLevel = 3;

        internal const float DegreesToRadians = MathF.PI / 180f;

        private readonly BranchSkeletonTuning tuning;
        private readonly Random random;
        private readonly List<BranchSpec> branches = new List<BranchSpec>();

        public BranchSkeleton(BranchSkeletonTuning tuning, int seed)
        {
            this.tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            tuning.Validate();
            random = new Random(seed);

            for (int i = 0; i < tuning.MainBranchCount; i++)
            {
                AddMainBranch(i);
            }

            // Added last, so the main branches come out the same with or without limbs.
            AddLimbs();
        }

        /// <summary>Every branch, each after the branch it grows out of.</summary>
        public IReadOnlyList<BranchSpec> Branches => branches;

        /// <summary>How far a branch has grown, 0 to 1: nothing before it starts to sprout, fully grown at full branch growth.</summary>
        public static float Progress(BranchSpec branch, float branchGrowth)
        {
            if (!float.IsFinite(branchGrowth))
            {
                return 0f;
            }

            return Math.Clamp((branchGrowth - branch.StartGrowth) / (1f - branch.StartGrowth), 0f, 1f);
        }

        /// <summary>A branch's length at this much branch growth.</summary>
        public float CurrentLength(int index, float branchGrowth)
        {
            BranchSpec branch = branches[index];
            return branch.Length * Progress(branch, branchGrowth);
        }

        /// <summary>
        /// Where a branch leaves its parent, in the tree's space with the trunk's base at the origin. Children are carried
        /// outward as their parent grows, as they sit a set share of the way along it.
        /// </summary>
        public Vector3 BasePosition(int index, float trunkHeight, float branchGrowth)
        {
            BranchSpec branch = branches[index];
            if (branch.ParentIndex == Trunk)
            {
                return new Vector3(0f, branch.AttachAlong * trunkHeight, 0f);
            }

            BranchSpec parent = branches[branch.ParentIndex];
            return BasePosition(parent.Index, trunkHeight, branchGrowth) +
                   parent.Direction * (branch.AttachAlong * CurrentLength(parent.Index, branchGrowth));
        }

        public Vector3 TipPosition(int index, float trunkHeight, float branchGrowth)
        {
            return BasePosition(index, trunkHeight, branchGrowth) + branches[index].Direction * CurrentLength(index, branchGrowth);
        }

        private void AddMainBranch(int order)
        {
            // 0 for the lowest main branch, 1 for the highest, with the spacing packing them toward the top. Everything
            // that changes up the tree follows this, so a branch's length and lean match how high it actually is.
            float evenly = tuning.MainBranchCount > 1 ? order / (tuning.MainBranchCount - 1f) : 0f;
            float height = MathF.Pow(evenly, tuning.MainBranchSpacing);

            // Alternating sides spread the tree across the screen, as suits the side-on view, with a little depth.
            float side = order % 2 == 0 ? 0f : 180f;

            BranchSpec main = Add(
                parentIndex: Trunk,
                level: MainLevel,
                attachAlong: Lerp(tuning.LowestMainBranch, tuning.HighestMainBranch, height),
                tilt: Lerp(tuning.LowestMainBranchTilt, tuning.HighestMainBranchTilt, height),
                yaw: side + Range(-tuning.MainBranchDepthSpread, tuning.MainBranchDepthSpread),
                length: Lerp(tuning.LowestMainBranchLength, tuning.HighestMainBranchLength, height) * Range(0.85f, 1.1f),
                thicknessRatio: tuning.MainBranchThicknessRatio,
                startGrowth: Lerp(tuning.FirstMainBranchStart, tuning.LastMainBranchStart, height));

            AddChildren(main, tuning.MinSideBranches, tuning.MaxSideBranches);
        }

        /// <summary>
        /// The thick limbs the trunk splits into at its top, which hold up most of the crown, the way a broadleaf tree's
        /// trunk forks rather than running on up through its crown. They fan out from nearly upright in the middle to
        /// leaning well out, on alternating sides, so the crown is broad and rounded.
        /// </summary>
        private void AddLimbs()
        {
            int count = random.Next(tuning.MinLimbs, tuning.MaxLimbs + 1);
            float firstSide = random.Next(2) * 180f;

            for (int i = 0; i < count; i++)
            {
                float side = i % 2 == 0 ? firstSide : firstSide + 180f;
                BranchSpec limb = Add(
                    parentIndex: Trunk,
                    level: MainLevel,
                    attachAlong: 1f,
                    tilt: Lerp(tuning.MinLimbTilt, tuning.MaxLimbTilt, (i + Range(0.2f, 0.8f)) / count),
                    yaw: side + Range(-tuning.MainBranchDepthSpread, tuning.MainBranchDepthSpread),
                    length: tuning.LimbLength * Range(0.85f, 1.1f),
                    thicknessRatio: tuning.LimbThicknessRatio,
                    startGrowth: tuning.FirstMainBranchStart,
                    isLimb: true);

                AddChildren(limb, tuning.MinSideBranches, tuning.MaxSideBranches);
            }
        }

        private void AddChildren(BranchSpec parent, int fewest, int most)
        {
            int count = random.Next(fewest, most + 1);

            // Only drawn for limbs, so every other branch comes out the same as it would without them.
            float firstSide = parent.IsLimb ? random.Next(2) * 180f : 0f;

            for (int i = 0; i < count; i++)
            {
                // Spread evenly along the outer part of the parent, each a little off its even spot.
                float along = Lerp(0.35f, 0.95f, (i + Range(0.2f, 0.8f)) / count);

                float tilt;
                float yaw;
                if (parent.IsLimb)
                {
                    // Limbs stand fairly upright, so their side branches don't follow them: they fan out to both sides,
                    // leaning like the main branches do, filling the crown out and rounding it off.
                    tilt = Range(tuning.HighestMainBranchTilt, tuning.LowestMainBranchTilt);
                    yaw = (i % 2 == 0 ? firstSide : firstSide + 180f) + Range(-tuning.MainBranchDepthSpread, tuning.MainBranchDepthSpread);
                }
                else
                {
                    // Either more upright or leaning further out than its parent, so branches fork apart.
                    tilt = Math.Clamp(parent.Tilt + Range(-20f, 25f), 0f, tuning.MaxTilt);
                    yaw = parent.Yaw + Range(-tuning.ChildTurnSpread, tuning.ChildTurnSpread);
                }

                BranchSpec child = Add(
                    parentIndex: parent.Index,
                    level: parent.Level + 1,
                    attachAlong: along,
                    tilt: tilt,
                    yaw: yaw,
                    length: parent.Length * Range(tuning.MinChildLengthRatio, tuning.MaxChildLengthRatio),
                    thicknessRatio: tuning.ChildThicknessRatio,
                    startGrowth: parent.StartGrowth + Range(tuning.MinChildStartDelay, tuning.MaxChildStartDelay));

                if (child.Level < TwigLevel)
                {
                    AddChildren(child, tuning.MinTwigs, tuning.MaxTwigs);
                }
            }
        }

        private BranchSpec Add(int parentIndex, int level, float attachAlong, float tilt, float yaw, float length,
            float thicknessRatio, float startGrowth, bool isLimb = false)
        {
            var branch = new BranchSpec(branches.Count, parentIndex, level, attachAlong, tilt, yaw, length,
                thicknessRatio, startGrowth, bend: Range(0f, tuning.MaxBend), isLimb);
            branches.Add(branch);
            return branch;
        }

        private float Range(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        private static float Lerp(float from, float to, float t)
        {
            return from + (to - from) * t;
        }
    }
}
