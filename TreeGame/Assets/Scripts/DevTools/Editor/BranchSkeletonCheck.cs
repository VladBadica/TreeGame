using System;
using System.Collections.Generic;
using System.Linq;
using TreeGame.Trees;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif
using Vector3 = System.Numerics.Vector3;

namespace TreeGame.DevTools
{
    /// <summary>
    /// Checks the layout of the tree's branches without a scene or Play Mode. The skeleton has no Unity dependency, so
    /// this also compiles and runs outside Unity; only the menu item is Editor-specific.
    /// </summary>
    public static class BranchSkeletonCheck
    {
        private const int SeedsToTry = 200;
        private const float Tolerance = 0.0001f;

        // From a sapling's trunk to a grown one's, and every stage of branch growth in between.
        private static readonly float[] TrunkHeights = { 0.3f, 1f, 2.4f, 3.5f };
        private static readonly float[] BranchGrowths = { 0f, 0.1f, 0.3f, 0.5f, 0.7f, 0.9f, 1f };

        /// <summary>Runs every check. Returns a description of each failure; empty means all passed.</summary>
        public static List<string> Run()
        {
            var failures = new List<string>();
            CheckSameSeedSameTree(failures);
            CheckDifferentSeedsDiffer(failures);
            CheckStructure(failures);
            CheckAttachPoints(failures);
            CheckDirections(failures);
            CheckAboveGround(failures);
            CheckGrowthOrder(failures);
            CheckProgress(failures);
            CheckLengthsAndThickness(failures);
            CheckBothSides(failures);
            CheckMoreBranchesNearTheTop(failures);
            CheckLimbs(failures);
            CheckBranchesFork(failures);
            CheckBranchesFollowTheirParents(failures);
            CheckTuningChangesTheTree(failures);
            CheckBadTuningIsRejected(failures);
            return failures;
        }

#if UNITY_EDITOR
        [MenuItem("Tree Game/Run Branch Skeleton Check")]
        private static void RunFromMenu()
        {
            List<string> failures = Run();
            if (failures.Count == 0)
            {
                Debug.Log("Branch skeleton check PASSED.");
            }
            else
            {
                Debug.LogError("Branch skeleton check FAILED:\n- " + string.Join("\n- ", failures));
            }
        }
#endif

        private static BranchSkeleton NewSkeleton(int seed = 12345, BranchSkeletonTuning tuning = null)
        {
            return new BranchSkeleton(tuning ?? new BranchSkeletonTuning(), seed);
        }

        /// <summary>The main branches spaced up the trunk, lowest first, leaving out the limbs at its top.</summary>
        private static List<BranchSpec> MainBranches(BranchSkeleton skeleton)
        {
            return skeleton.Branches.Where(b => b.ParentIndex == BranchSkeleton.Trunk && !b.IsLimb).ToList();
        }

        private static IEnumerable<BranchSkeleton> ManySkeletons()
        {
            for (int seed = 0; seed < SeedsToTry; seed++)
            {
                yield return NewSkeleton(seed);
            }
        }

        private static void Expect(List<string> failures, bool condition, string failure)
        {
            if (!condition)
            {
                failures.Add(failure);
            }
        }

        /// <summary>Adds a failure only the first time, so one broken rule doesn't bury the others under hundreds of lines.</summary>
        private static bool ExpectOnce(List<string> failures, bool condition, string failure)
        {
            if (!condition)
            {
                failures.Add(failure);
            }

            return condition;
        }

        private static string Describe(BranchSkeleton skeleton)
        {
            return Describe(skeleton.Branches);
        }

        private static string Describe(IEnumerable<BranchSpec> branches)
        {
            return string.Join("\n", branches.Select(b =>
                $"{b.Index} {b.ParentIndex} {b.Level} {b.IsLimb}{b.AttachAlong:R} {b.Tilt:R} {b.Yaw:R} {b.Length:R} {b.ThicknessRatio:R} {b.StartGrowth:R} {b.Bend:R}"));
        }

        private static void CheckSameSeedSameTree(List<string> failures)
        {
            Expect(failures, Describe(NewSkeleton(42)) == Describe(NewSkeleton(42)), "the same seed gave a different tree");
        }

        private static void CheckDifferentSeedsDiffer(List<string> failures)
        {
            Expect(failures, Describe(NewSkeleton(42)) != Describe(NewSkeleton(43)), "different seeds gave the same tree");
        }

        private static void CheckStructure(List<string> failures)
        {
            var tuning = new BranchSkeletonTuning();
            var levelsSeen = new HashSet<int>();
            var sideBranchCountsSeen = new HashSet<int>();
            var twigCountsSeen = new HashSet<int>();

            foreach (BranchSkeleton skeleton in ManySkeletons())
            {
                IReadOnlyList<BranchSpec> branches = skeleton.Branches;
                bool ok = ExpectOnce(failures, MainBranches(skeleton).Count == tuning.MainBranchCount,
                    $"expected {tuning.MainBranchCount} main branches");
                ok &= ExpectOnce(failures, branches.Count(b => b.IsLimb) >= tuning.MinLimbs && branches.Count(b => b.IsLimb) <= tuning.MaxLimbs,
                    $"expected {tuning.MinLimbs} to {tuning.MaxLimbs} limbs");

                for (int i = 0; ok && i < branches.Count; i++)
                {
                    BranchSpec branch = branches[i];
                    levelsSeen.Add(branch.Level);

                    ok &= ExpectOnce(failures, branch.Index == i, $"branch {i} has index {branch.Index}");
                    ok &= ExpectOnce(failures, branch.Level >= BranchSkeleton.MainLevel && branch.Level <= BranchSkeleton.TwigLevel, $"branch {i} is at level {branch.Level}");

                    if (branch.ParentIndex == BranchSkeleton.Trunk)
                    {
                        ok &= ExpectOnce(failures, branch.Level == BranchSkeleton.MainLevel, $"branch {i} grows from the trunk but is at level {branch.Level}");
                        continue;
                    }

                    // Parents come first, so anything built in order always has a branch's parent ready.
                    ok &= ExpectOnce(failures, branch.ParentIndex >= 0 && branch.ParentIndex < i, $"branch {i} comes before its parent {branch.ParentIndex}");
                    ok &= ok && ExpectOnce(failures, branch.Level == branches[branch.ParentIndex].Level + 1, $"branch {i} isn't one level below its parent");
                }

                // Every branch below twig level has as many children as the tuning allows.
                foreach (BranchSpec parent in branches.Where(b => b.Level < BranchSkeleton.TwigLevel))
                {
                    if (!ok)
                    {
                        break;
                    }

                    int children = branches.Count(b => b.ParentIndex == parent.Index);
                    (int fewest, int most) = parent.Level == BranchSkeleton.MainLevel
                        ? (tuning.MinSideBranches, tuning.MaxSideBranches)
                        : (tuning.MinTwigs, tuning.MaxTwigs);
                    (parent.Level == BranchSkeleton.MainLevel ? sideBranchCountsSeen : twigCountsSeen).Add(children);
                    ok &= ExpectOnce(failures, children >= fewest && children <= most,
                        $"a level {parent.Level} branch has {children} children, expected {fewest} to {most}");
                }

                if (!ok)
                {
                    return;
                }
            }

            Expect(failures, levelsSeen.SetEquals(new[] { 1, 2, 3 }), $"expected main branches, side branches and twigs, got levels {string.Join(", ", levelsSeen)}");

            // Across many trees, both the fewest and the most children allowed come up, not just one of them.
            Expect(failures, sideBranchCountsSeen.Contains(tuning.MinSideBranches) && sideBranchCountsSeen.Contains(tuning.MaxSideBranches),
                $"side branch counts seen were only {string.Join(", ", sideBranchCountsSeen)}");
            Expect(failures, twigCountsSeen.Contains(tuning.MinTwigs) && twigCountsSeen.Contains(tuning.MaxTwigs),
                $"twig counts seen were only {string.Join(", ", twigCountsSeen)}");
        }

        private static void CheckAttachPoints(List<string> failures)
        {
            var tuning = new BranchSkeletonTuning();
            foreach (BranchSkeleton skeleton in ManySkeletons())
            {
                foreach (BranchSpec branch in skeleton.Branches)
                {
                    bool inRange = branch.IsLimb
                        ? branch.AttachAlong == 1f
                        : branch.ParentIndex == BranchSkeleton.Trunk
                            ? branch.AttachAlong >= tuning.LowestMainBranch - Tolerance && branch.AttachAlong <= tuning.HighestMainBranch + Tolerance
                            : branch.AttachAlong > 0f && branch.AttachAlong <= 1f;

                    if (!ExpectOnce(failures, inRange, $"branch {branch.Index} leaves its parent at {branch.AttachAlong}, outside it"))
                    {
                        return;
                    }
                }

                // Main branches climb the trunk in order, lowest first.
                List<float> mainHeights = MainBranches(skeleton).Select(b => b.AttachAlong).ToList();
                if (!ExpectOnce(failures, mainHeights.SequenceEqual(mainHeights.OrderBy(h => h)), "main branches aren't in order up the trunk"))
                {
                    return;
                }
            }
        }

        private static void CheckDirections(List<string> failures)
        {
            var tuning = new BranchSkeletonTuning();
            float lowestUpward = MathF.Cos(tuning.MaxTilt * MathF.PI / 180f);

            foreach (BranchSkeleton skeleton in ManySkeletons())
            {
                foreach (BranchSpec branch in skeleton.Branches)
                {
                    Vector3 direction = branch.Direction;
                    if (!ExpectOnce(failures, MathF.Abs(direction.Length() - 1f) < Tolerance, $"branch {branch.Index} points with a length of {direction.Length()}, not 1") ||
                        !ExpectOnce(failures, branch.Tilt >= 0f && branch.Tilt <= tuning.MaxTilt, $"branch {branch.Index} leans {branch.Tilt} degrees, more than {tuning.MaxTilt}") ||
                        !ExpectOnce(failures, direction.Y >= lowestUpward - Tolerance, $"branch {branch.Index} points too far down: {direction}"))
                    {
                        return;
                    }
                }
            }
        }

        private static void CheckAboveGround(List<string> failures)
        {
            foreach (BranchSkeleton skeleton in ManySkeletons())
            {
                foreach (BranchSpec branch in skeleton.Branches)
                {
                    foreach (float height in TrunkHeights)
                    {
                        foreach (float growth in BranchGrowths)
                        {
                            float baseY = skeleton.BasePosition(branch.Index, height, growth).Y;
                            float tipY = skeleton.TipPosition(branch.Index, height, growth).Y;
                            if (!ExpectOnce(failures, baseY > 0f && tipY > 0f,
                                    $"branch {branch.Index} reaches the ground ({baseY} to {tipY}) with a trunk {height} tall at branch growth {growth}"))
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }

        private static void CheckGrowthOrder(List<string> failures)
        {
            foreach (BranchSkeleton skeleton in ManySkeletons())
            {
                foreach (BranchSpec branch in skeleton.Branches)
                {
                    if (!ExpectOnce(failures, branch.StartGrowth >= 0f && branch.StartGrowth < 1f,
                            $"branch {branch.Index} starts at branch growth {branch.StartGrowth}, outside 0 to under 1"))
                    {
                        return;
                    }

                    if (branch.ParentIndex != BranchSkeleton.Trunk &&
                        !ExpectOnce(failures, branch.StartGrowth > skeleton.Branches[branch.ParentIndex].StartGrowth,
                            $"branch {branch.Index} starts sprouting no later than its parent"))
                    {
                        return;
                    }
                }

                // At any branch growth, a branch that has started has a parent that has started too.
                foreach (float growth in BranchGrowths)
                {
                    foreach (BranchSpec branch in skeleton.Branches.Where(b => b.ParentIndex != BranchSkeleton.Trunk))
                    {
                        bool shown = BranchSkeleton.Progress(branch, growth) > 0f;
                        bool parentShown = BranchSkeleton.Progress(skeleton.Branches[branch.ParentIndex], growth) > 0f;
                        if (!ExpectOnce(failures, !shown || parentShown, $"at branch growth {growth}, branch {branch.Index} shows before its parent"))
                        {
                            return;
                        }
                    }
                }
            }

            // Main branches sprout from the bottom up.
            List<float> mainStarts = MainBranches(NewSkeleton()).Select(b => b.StartGrowth).ToList();
            Expect(failures, mainStarts.SequenceEqual(mainStarts.OrderBy(s => s)), "main branches don't sprout from the bottom up");
        }

        private static void CheckProgress(List<string> failures)
        {
            BranchSkeleton skeleton = NewSkeleton();
            foreach (BranchSpec branch in skeleton.Branches)
            {
                bool ok = ExpectOnce(failures, BranchSkeleton.Progress(branch, 0f) == 0f || branch.StartGrowth == 0f, $"branch {branch.Index} has grown before any branch growth")
                    && ExpectOnce(failures, BranchSkeleton.Progress(branch, branch.StartGrowth) == 0f, $"branch {branch.Index} has grown at the moment it starts")
                    && ExpectOnce(failures, BranchSkeleton.Progress(branch, 1f) == 1f, $"branch {branch.Index} isn't fully grown at full branch growth")
                    && ExpectOnce(failures, MathF.Abs(skeleton.CurrentLength(branch.Index, 1f) - branch.Length) < Tolerance, $"branch {branch.Index} isn't full length at full branch growth")
                    && ExpectOnce(failures, skeleton.CurrentLength(branch.Index, branch.StartGrowth) == 0f, $"branch {branch.Index} has length before it sprouts");

                // Only ever grows as branch growth rises.
                float last = 0f;
                for (int step = 0; ok && step <= 100; step++)
                {
                    float progress = BranchSkeleton.Progress(branch, step / 100f);
                    ok = ExpectOnce(failures, progress >= last && progress >= 0f && progress <= 1f, $"branch {branch.Index} shrank or left 0 to 1 as branch growth rose");
                    last = progress;
                }

                if (!ok)
                {
                    return;
                }

                // Halfway between sprouting and full branch growth, it's half grown.
                float halfway = (branch.StartGrowth + 1f) / 2f;
                if (!ExpectOnce(failures, MathF.Abs(BranchSkeleton.Progress(branch, halfway) - 0.5f) < Tolerance, $"branch {branch.Index} isn't half grown halfway"))
                {
                    return;
                }
            }

            BranchSpec first = skeleton.Branches[0];
            foreach (float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -5f })
            {
                Expect(failures, BranchSkeleton.Progress(first, bad) == 0f, $"branch growth of {bad} gave progress {BranchSkeleton.Progress(first, bad)}");
            }

            Expect(failures, BranchSkeleton.Progress(first, 5f) == 1f, "branch growth above 1 should count as fully grown");
        }

        private static void CheckLengthsAndThickness(List<string> failures)
        {
            foreach (BranchSkeleton skeleton in ManySkeletons())
            {
                foreach (BranchSpec branch in skeleton.Branches)
                {
                    bool ok = ExpectOnce(failures, branch.Length > 0f && float.IsFinite(branch.Length), $"branch {branch.Index} has a length of {branch.Length}")
                        && ExpectOnce(failures, branch.ThicknessRatio > 0f && branch.ThicknessRatio <= 1f, $"branch {branch.Index} is {branch.ThicknessRatio} times its parent's thickness")
                        && ExpectOnce(failures, branch.Bend >= 0f, $"branch {branch.Index} bends {branch.Bend} degrees, downward");

                    if (ok && branch.ParentIndex != BranchSkeleton.Trunk)
                    {
                        float parentLength = skeleton.Branches[branch.ParentIndex].Length;
                        ok = ExpectOnce(failures, branch.Length < parentLength, $"branch {branch.Index} is longer than its parent")
                             && ExpectOnce(failures, branch.Length >= parentLength * new BranchSkeletonTuning().MinChildLengthRatio - Tolerance,
                                 $"branch {branch.Index} is a stub, only {branch.Length / parentLength:P0} of its parent's length");
                    }

                    if (!ok)
                    {
                        return;
                    }
                }
            }

            // Lower main branches are longer, as they have been growing for longest.
            var tuning = new BranchSkeletonTuning { MainBranchCount = 2 };
            int longerLow = 0;
            for (int seed = 0; seed < SeedsToTry; seed++)
            {
                List<BranchSpec> mains = MainBranches(NewSkeleton(seed, tuning));
                longerLow += mains[0].Length > mains[1].Length ? 1 : 0;
            }

            Expect(failures, longerLow > SeedsToTry * 0.8f, $"the lower main branch was longer in only {longerLow} of {SeedsToTry} trees");
        }

        private static void CheckBothSides(List<string> failures)
        {
            foreach (BranchSkeleton skeleton in ManySkeletons())
            {
                List<BranchSpec> mains = MainBranches(skeleton);
                int right = mains.Count(b => b.Direction.X > 0f);
                int left = mains.Count(b => b.Direction.X < 0f);
                if (!ExpectOnce(failures, Math.Abs(right - left) <= 1, $"main branches lean {right} to the right and {left} to the left, not both sides evenly"))
                {
                    return;
                }

                // The side-on view needs the tree spread across the screen more than toward or away from the camera.
                float sideways = mains.Sum(b => MathF.Abs(b.Direction.X));
                float depthways = mains.Sum(b => MathF.Abs(b.Direction.Z));
                if (!ExpectOnce(failures, sideways > depthways * 1.5f, $"main branches spread {sideways} sideways but {depthways} in depth"))
                {
                    return;
                }
            }
        }

        /// <summary>Like a real tree, more main branches grow from the upper part of the trunk than the lower.</summary>
        private static void CheckMoreBranchesNearTheTop(List<string> failures)
        {
            var tuning = new BranchSkeletonTuning();
            float middle = (tuning.LowestMainBranch + tuning.HighestMainBranch) / 2f;
            List<float> heights = MainBranches(NewSkeleton()).Select(b => b.AttachAlong).ToList();
            int upper = heights.Count(h => h > middle);
            int lower = heights.Count - upper;
            Expect(failures, upper > lower, $"{upper} main branches grow from the upper half of their stretch of trunk and {lower} from the lower half; expected more higher up");

            // Evenly spaced when asked to be.
            var even = new BranchSkeletonTuning { MainBranchSpacing = 1f, MainBranchCount = 5 };
            List<float> evenHeights = MainBranches(NewSkeleton(tuning: even)).Select(b => b.AttachAlong).ToList();
            float gap = (even.HighestMainBranch - even.LowestMainBranch) / 4f;
            bool evenlySpaced = true;
            for (int i = 1; i < evenHeights.Count; i++)
            {
                evenlySpaced &= MathF.Abs(evenHeights[i] - evenHeights[i - 1] - gap) < Tolerance;
            }

            Expect(failures, evenlySpaced, "a spacing of 1 should space main branches evenly up the trunk");

            // The top and bottom branches stay where the tuning puts them, whatever the spacing.
            Expect(failures, MathF.Abs(heights.Min() - tuning.LowestMainBranch) < Tolerance && MathF.Abs(heights.Max() - tuning.HighestMainBranch) < Tolerance,
                "the lowest and highest main branches should be at the lowest and highest points the tuning allows");
        }

        /// <summary>
        /// The trunk splits at its top into a few thick limbs that fan out to both sides and hold up the crown, the way a
        /// broadleaf tree forks rather than running one trunk up through its crown.
        /// </summary>
        private static void CheckLimbs(List<string> failures)
        {
            var tuning = new BranchSkeletonTuning();
            var countsSeen = new HashSet<int>();
            foreach (BranchSkeleton skeleton in ManySkeletons())
            {
                List<BranchSpec> limbs = skeleton.Branches.Where(b => b.IsLimb).ToList();
                countsSeen.Add(limbs.Count);
                bool ok = ExpectOnce(failures, limbs.Count >= tuning.MinLimbs && limbs.Count <= tuning.MaxLimbs,
                    $"the trunk splits into {limbs.Count} limbs, expected {tuning.MinLimbs} to {tuning.MaxLimbs}");

                foreach (BranchSpec limb in limbs)
                {
                    ok = ok
                         && ExpectOnce(failures, limb.ParentIndex == BranchSkeleton.Trunk && limb.Level == BranchSkeleton.MainLevel,
                             $"limb {limb.Index} doesn't grow out of the trunk")
                         && ExpectOnce(failures, limb.Tilt >= tuning.MinLimbTilt - Tolerance && limb.Tilt <= tuning.MaxLimbTilt + Tolerance,
                             $"limb {limb.Index} leans {limb.Tilt} degrees, outside {tuning.MinLimbTilt} to {tuning.MaxLimbTilt}")
                         && ExpectOnce(failures, limb.Length >= tuning.LimbLength * 0.85f - Tolerance && limb.Length <= tuning.LimbLength * 1.1f + Tolerance,
                             $"limb {limb.Index} is {limb.Length} long, far from {tuning.LimbLength}")
                         && ExpectOnce(failures, limb.ThicknessRatio == tuning.LimbThicknessRatio,
                             $"limb {limb.Index} is {limb.ThicknessRatio} times the trunk's thickness, expected {tuning.LimbThicknessRatio}")
                         && ExpectOnce(failures, MainBranches(skeleton).All(b => limb.StartGrowth <= b.StartGrowth),
                             $"limb {limb.Index} sprouts after some main branches, though limbs hold up the crown");

                    // Its side branches fill the crown out to both sides rather than crowding along it.
                    List<BranchSpec> sides = skeleton.Branches.Where(b => b.ParentIndex == limb.Index).ToList();
                    int sidesRight = sides.Count(b => b.Direction.X > 0f);
                    int sidesLeft = sides.Count(b => b.Direction.X < 0f);
                    ok = ok && ExpectOnce(failures, Math.Abs(sidesRight - sidesLeft) <= 1,
                        $"limb {limb.Index}'s side branches lean {sidesRight} to the right and {sidesLeft} to the left, not both sides evenly");
                    ok = ok && ExpectOnce(failures, sides.All(b => b.Tilt >= tuning.HighestMainBranchTilt - Tolerance && b.Tilt <= tuning.LowestMainBranchTilt + Tolerance),
                        $"limb {limb.Index}'s side branches don't lean out like the main branches do");
                }

                // They fan out across the crown, to both sides, from upright in the middle to leaning well out.
                int right = limbs.Count(b => b.Direction.X > 0f);
                int left = limbs.Count(b => b.Direction.X < 0f);
                ok = ok && ExpectOnce(failures, Math.Abs(right - left) <= 1, $"limbs lean {right} to the right and {left} to the left, not both sides evenly");
                ok = ok && ExpectOnce(failures, limbs.Max(b => b.Tilt) - limbs.Min(b => b.Tilt) > (tuning.MaxLimbTilt - tuning.MinLimbTilt) * 0.3f,
                    "the limbs all lean about the same amount, rather than fanning out");

                // The limbs top the crown: the highest of them reaches above every main branch, however tall the trunk.
                foreach (float height in TrunkHeights)
                {
                    float limbsTop = limbs.Max(b => skeleton.TipPosition(b.Index, height, 1f).Y);
                    ok = ok && ExpectOnce(failures, MainBranches(skeleton).All(b => skeleton.TipPosition(b.Index, height, 1f).Y < limbsTop),
                        $"a main branch reaches above the limbs with a trunk {height} tall");
                }

                if (!ok)
                {
                    return;
                }
            }

            Expect(failures, countsSeen.Contains(tuning.MinLimbs) && countsSeen.Contains(tuning.MaxLimbs),
                $"limb counts seen were only {string.Join(", ", countsSeen)}");
        }

        /// <summary>
        /// Side branches and twigs fork away from their parent at a clear angle, so the tree branches out rather than
        /// having children run alongside their parents, yet not so wildly that it looks random.
        /// </summary>
        private static void CheckBranchesFork(List<string> failures)
        {
            double totalAngle = 0;
            int forks = 0;
            int curved = 0;
            int branchCount = 0;
            var tuning = new BranchSkeletonTuning();

            foreach (BranchSkeleton skeleton in ManySkeletons())
            {
                foreach (BranchSpec branch in skeleton.Branches)
                {
                    branchCount++;
                    curved += branch.Bend > tuning.MaxBend * 0.5f ? 1 : 0;
                    if (branch.ParentIndex == BranchSkeleton.Trunk)
                    {
                        continue;
                    }

                    float dot = Math.Clamp(Vector3.Dot(branch.Direction, skeleton.Branches[branch.ParentIndex].Direction), -1f, 1f);
                    totalAngle += MathF.Acos(dot) * 180f / MathF.PI;
                    forks++;
                }
            }

            double averageAngle = totalAngle / forks;
            Expect(failures, averageAngle >= 20 && averageAngle <= 45,
                $"branches fork from their parents by {averageAngle:F1} degrees on average, expected 20 to 45");

            // Curves vary from straight to the most allowed, so plenty are well curved.
            Expect(failures, curved > branchCount / 4, $"only {curved} of {branchCount} branches curve more than half the most allowed");
        }

        /// <summary>Compares positions with a differently written calculation, walking up from each branch to the trunk.</summary>
        private static void CheckBranchesFollowTheirParents(List<string> failures)
        {
            BranchSkeleton skeleton = NewSkeleton(7);
            foreach (float height in TrunkHeights)
            {
                foreach (float growth in BranchGrowths)
                {
                    foreach (BranchSpec branch in skeleton.Branches)
                    {
                        Vector3 expected = Vector3.Zero;
                        BranchSpec current = branch;
                        while (current.ParentIndex != BranchSkeleton.Trunk)
                        {
                            BranchSpec parent = skeleton.Branches[current.ParentIndex];
                            float parentLength = parent.Length * BranchSkeleton.Progress(parent, growth);
                            expected += parent.Direction * (current.AttachAlong * parentLength);
                            current = parent;
                        }

                        expected += new Vector3(0f, current.AttachAlong * height, 0f);

                        Vector3 actual = skeleton.BasePosition(branch.Index, height, growth);
                        if (!ExpectOnce(failures, Vector3.Distance(expected, actual) < Tolerance,
                                $"branch {branch.Index} sits at {actual}, expected {expected}, with a trunk {height} tall at branch growth {growth}"))
                        {
                            return;
                        }
                    }
                }
            }

            // A branch that hasn't sprouted has no length, so its tip is at its base.
            BranchSpec last = skeleton.Branches[skeleton.Branches.Count - 1];
            Expect(failures, skeleton.TipPosition(last.Index, 2f, 0f) == skeleton.BasePosition(last.Index, 2f, 0f), "an unsprouted branch has its tip away from its base");
        }

        private static void CheckTuningChangesTheTree(List<string> failures)
        {
            // Eight main branches and three limbs, each with 3 side branches of 3 twigs.
            var bushy = new BranchSkeletonTuning { MainBranchCount = 8, MinLimbs = 3, MaxLimbs = 3, MinSideBranches = 3, MaxSideBranches = 3, MinTwigs = 3, MaxTwigs = 3 };
            Expect(failures, NewSkeleton(tuning: bushy).Branches.Count == 11 + 11 * 3 + 11 * 3 * 3, "a tuning with set counts gave the wrong number of branches");

            var onlyLimbs = new BranchSkeletonTuning { MainBranchCount = 0, MinLimbs = 2, MaxLimbs = 2 };
            Expect(failures, NewSkeleton(tuning: onlyLimbs).Branches.Count(b => b.ParentIndex == BranchSkeleton.Trunk) == 2,
                "a tree with no main branches should still have its limbs");

            var noLimbs = new BranchSkeletonTuning { MinLimbs = 0, MaxLimbs = 0 };
            Expect(failures, !NewSkeleton(tuning: noLimbs).Branches.Any(b => b.IsLimb), "a tuning with no limbs still had limbs");

            var bare = new BranchSkeletonTuning { MainBranchCount = 0, MinLimbs = 0, MaxLimbs = 0 };
            Expect(failures, NewSkeleton(tuning: bare).Branches.Count == 0, "a tree with no main branches and no limbs should have no branches at all");

            // The limbs are added last, so the main branches don't change with or without them.
            Expect(failures, Describe(MainBranches(NewSkeleton(5, noLimbs))) == Describe(MainBranches(NewSkeleton(5))),
                "adding the limbs changed the main branches");

            var noTwigs = new BranchSkeletonTuning { MinTwigs = 0, MaxTwigs = 0 };
            Expect(failures, NewSkeleton(tuning: noTwigs).Branches.All(b => b.Level < BranchSkeleton.TwigLevel), "a tuning with no twigs still had twigs");
        }

        private static void CheckBadTuningIsRejected(List<string> failures)
        {
            void ExpectRejected(string description, BranchSkeletonTuning tuning)
            {
                try
                {
                    NewSkeleton(tuning: tuning);
                    failures.Add($"tuning with {description} should be rejected");
                }
                catch (ArgumentException)
                {
                    // Expected.
                }
            }

            try
            {
                new BranchSkeleton(null, 1);
                failures.Add("no tuning should be rejected");
            }
            catch (ArgumentNullException)
            {
                // Expected.
            }

            ExpectRejected("negative main branches", new BranchSkeletonTuning { MainBranchCount = -1 });
            ExpectRejected("no main branch spacing", new BranchSkeletonTuning { MainBranchSpacing = 0f });
            ExpectRejected("a NaN main branch spacing", new BranchSkeletonTuning { MainBranchSpacing = float.NaN });
            ExpectRejected("fewer most side branches than fewest", new BranchSkeletonTuning { MinSideBranches = 3, MaxSideBranches = 2 });
            ExpectRejected("fewer most twigs than fewest", new BranchSkeletonTuning { MinTwigs = 2, MaxTwigs = 1 });
            ExpectRejected("main branches above the trunk", new BranchSkeletonTuning { HighestMainBranch = 1.5f });
            ExpectRejected("the highest main branch below the lowest", new BranchSkeletonTuning { LowestMainBranch = 0.8f, HighestMainBranch = 0.5f });
            ExpectRejected("branches allowed to point sideways", new BranchSkeletonTuning { MaxTilt = 90f });
            ExpectRejected("a main branch tilt past the limit", new BranchSkeletonTuning { LowestMainBranchTilt = 80f });
            ExpectRejected("a NaN tilt", new BranchSkeletonTuning { MaxTilt = float.NaN });
            ExpectRejected("no main branch length", new BranchSkeletonTuning { HighestMainBranchLength = 0f });
            ExpectRejected("an infinite main branch length", new BranchSkeletonTuning { LowestMainBranchLength = float.PositiveInfinity });
            ExpectRejected("children longer than their parents", new BranchSkeletonTuning { MaxChildLengthRatio = 1.5f });
            ExpectRejected("children with no length", new BranchSkeletonTuning { MinChildLengthRatio = 0f });
            ExpectRejected("branches thicker than the trunk", new BranchSkeletonTuning { MainBranchThicknessRatio = 2f });
            ExpectRejected("branches with no thickness", new BranchSkeletonTuning { ChildThicknessRatio = 0f });
            ExpectRejected("main branches starting after full growth", new BranchSkeletonTuning { LastMainBranchStart = 1.5f });
            ExpectRejected("children sprouting with their parents", new BranchSkeletonTuning { MinChildStartDelay = 0f });
            ExpectRejected("twigs only sprouting at full growth", new BranchSkeletonTuning { LastMainBranchStart = 0.6f, MaxChildStartDelay = 0.25f });
            ExpectRejected("a downward bend limit", new BranchSkeletonTuning { MaxBend = -5f });
            ExpectRejected("negative limbs", new BranchSkeletonTuning { MinLimbs = -1 });
            ExpectRejected("fewer most limbs than fewest", new BranchSkeletonTuning { MinLimbs = 4, MaxLimbs = 3 });
            ExpectRejected("no limb length", new BranchSkeletonTuning { LimbLength = 0f });
            ExpectRejected("a NaN limb length", new BranchSkeletonTuning { LimbLength = float.NaN });
            ExpectRejected("an infinite limb length", new BranchSkeletonTuning { LimbLength = float.PositiveInfinity });
            ExpectRejected("limbs with no thickness", new BranchSkeletonTuning { LimbThicknessRatio = 0f });
            ExpectRejected("limbs thicker than the trunk", new BranchSkeletonTuning { LimbThicknessRatio = 1.5f });
            ExpectRejected("limbs leaning past the limit", new BranchSkeletonTuning { MaxLimbTilt = 80f });
            ExpectRejected("the least limb lean above the most", new BranchSkeletonTuning { MinLimbTilt = 40f, MaxLimbTilt = 20f });
        }
    }
}
