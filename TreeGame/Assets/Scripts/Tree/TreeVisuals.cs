using System.Collections.Generic;
using TreeGame.Core;
using UnityEngine;
using NumericsVector3 = System.Numerics.Vector3;

namespace TreeGame.Trees
{
    /// <summary>
    /// The tree as seen, built from bark-textured wood and clusters of leaf cards, reacting to <see cref="TreeGrowth"/>:
    /// the trunk follows trunk growth, the branches branch growth and the leaves foliage growth. The branches come from a
    /// <see cref="BranchSkeleton"/> built once from a fixed seed; growth changes only move, scale and show/hide the parts,
    /// so the same growth values always produce the same tree and nothing is rebuilt. Leaves at the top of the trunk need
    /// no branches, so a tree with foliage but few branches still has a small crown; they give way to the leader's leaves
    /// as the branches grow.
    /// The displayed growth eases toward the actual growth; that is purely presentation, TreeGrowth stays authoritative.
    /// Any other visual (authored model, sprites) can replace this by listening to the same event.
    /// </summary>
    public class TreeVisuals : MonoBehaviour
    {
        private const float MinVisibleSize = 0.001f;
        private const float SettleThreshold = 0.0005f;

        [Header("Source")]
        [SerializeField] private TreeGrowth treeGrowth;

        [Header("Animation")]
        [Tooltip("Approximate time in seconds for the tree to catch up with a growth change. 0 = instant.")]
        [SerializeField, Min(0f)] private float growthSmoothTime = 0.35f;

        [Header("Materials")]
        [SerializeField] private Material barkMaterial;
        [SerializeField] private Material foliageMaterial;

        [Header("Layout (applied when the tree is built)")]
        [Tooltip("Same seed = same branch and foliage placement.")]
        [SerializeField] private int seed = 12345;
        [Tooltip("Main branches growing out of the trunk. More makes a bushier tree.")]
        [SerializeField, Range(0, 16)] private int mainBranchCount = 10;
        [Tooltip("How main branches are spaced up the trunk: 1 spaces them evenly, lower values pack more toward the top.")]
        [SerializeField, Range(0.2f, 1f)] private float mainBranchSpacing = 0.6f;
        [Tooltip("Side branches growing out of each main branch (fewest, most).")]
        [SerializeField] private Vector2Int sideBranchesPerBranch = new Vector2Int(2, 3);
        [Tooltip("Twigs growing out of each side branch (fewest, most).")]
        [SerializeField] private Vector2Int twigsPerBranch = new Vector2Int(1, 3);
        [Tooltip("Length of the leader, the top of the trunk carrying on upward with side branches of its own, which " +
                 "rounds off the top of the crown. 0 for none.")]
        [SerializeField, Range(0f, 3f)] private float leaderLength = 1.3f;
        [Tooltip("Clusters of leaves, spread along the side branches and twigs.")]
        [SerializeField, Range(0, 200)] private int foliageClusterCount = 140;
        [Tooltip("How many of the leaf clusters sit on top of the trunk rather than on branches. They need no branches, " +
                 "so a young tree has leaves before it has branches.")]
        [SerializeField, Range(0, 24)] private int crownClusterCount = 8;
        [Tooltip("Leaf cards in each cluster. More looks fuller but costs more.")]
        [SerializeField, Range(1, 6)] private int leafCardsPerCluster = 3;

        [Header("Trunk (over trunk growth)")]
        [SerializeField] private GrowthParameter trunkHeight = new GrowthParameter(0.4f, 3.5f);
        [SerializeField] private GrowthParameter trunkThickness =
            new GrowthParameter(0.06f, 0.45f, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
        [Tooltip("How much wider the very base of the trunk is, spreading out like the start of roots.")]
        [SerializeField, Range(0f, 1.5f)] private float trunkFlare = 0.5f;
        [Tooltip("The trunk's thickness at the top, as a share of its thickness at the base.")]
        [SerializeField, Range(0.1f, 1f)] private float trunkTipRatio = 0.55f;

        [Header("Branches (over branch growth)")]
        [Tooltip("How a branch's length builds up over its own progress (0 = just sprouted, 1 = branches fully grown).")]
        [SerializeField] private AnimationCurve branchLengthOverProgress =
            new AnimationCurve(new Keyframe(0f, 0f, 2f, 2f), new Keyframe(1f, 1f, 0f, 0f));
        [Tooltip("How thick a just-sprouted branch is, as a share of its full thickness.")]
        [SerializeField, Range(0.05f, 1f)] private float sproutThickness = 0.4f;
        [Tooltip("A branch's thickness at its tip, as a share of its thickness at its base.")]
        [SerializeField, Range(0.1f, 1f)] private float branchTipRatio = 0.35f;
        [Tooltip("The leader's thickness at its tip, as a share of its base. It carries on from the trunk, so it tapers " +
                 "more gently than other branches, keeping the trunk's narrowing going instead of pinching in.")]
        [SerializeField, Range(0.1f, 1f)] private float leaderTipRatio = 0.6f;
        [Tooltip("The least a branch is thick for its length, so long branches on a young, thin trunk don't turn into " +
                 "hairs. Never thicker than what it grows from.")]
        [SerializeField, Range(0f, 0.2f)] private float minThicknessPerLength = 0.035f;

        [Header("Detail (applied when the tree is built)")]
        [Tooltip("Corners around the trunk and branches. More is rounder but costs more.")]
        [SerializeField, Range(3, 24)] private int trunkSides = 12;
        [SerializeField, Range(3, 16)] private int branchSides = 7;
        [SerializeField, Range(3, 16)] private int twigSides = 5;
        [Tooltip("Sections along the trunk and branches. More makes smoother curves.")]
        [SerializeField, Range(1, 16)] private int trunkRings = 6;
        [SerializeField, Range(1, 12)] private int branchRings = 4;

        [Header("Foliage (over foliage growth)")]
        [Tooltip("Fraction of leaf clusters shown over foliage growth. Clusters grow in one after another.")]
        [SerializeField] private GrowthParameter foliageDensity = new GrowthParameter(0.05f, 1f);
        [Tooltip("How big a leaf cluster is over foliage growth, in world units.")]
        [SerializeField] private GrowthParameter foliageClusterSize = new GrowthParameter(0.25f, 0.7f);
        [Tooltip("How much of the leaves on top of the trunk show, over branch growth. They are a young tree's crown; " +
                 "by default they give way to the leader's leaves as the branches grow in.")]
        [SerializeField] private GrowthParameter crownLeavesOverBranchGrowth = new GrowthParameter(1f, 0f,
            new AnimationCurve(new Keyframe(0.15f, 0f, 0f, 0f), new Keyframe(0.5f, 1f, 0f, 0f)));
        [Tooltip("How many times as many leaf clusters a twig gets as a side branch. Real crowns carry most of their " +
                 "leaves on the outer, sunlit twigs.")]
        [SerializeField, Range(1, 4)] private int twigLeafWeight = 2;
        [Tooltip("How far below the top of the trunk the clusters there may grow out of it, relative to cluster size. " +
                 "Clusters on branches grow right from them.")]
        [SerializeField, Min(0f)] private float foliageSpread = 0.35f;
        [Tooltip("Where along a branch leaves start growing, as a share of its length; they grow from there to its tip.")]
        [SerializeField, Range(0f, 1f)] private float leavesFromAlongBranch = 0.4f;
        [Tooltip("Leaf clusters are tinted between these, so the crown isn't one flat green.")]
        [SerializeField] private Color darkLeafTint = new Color(0.78f, 0.88f, 0.76f);
        [SerializeField] private Color lightLeafTint = new Color(1.08f, 1.06f, 0.86f);

        [Header("Wind Response (at wind strength 1)")]
        [Tooltip("Degrees the whole tree leans from its base, over trunk growth. Saplings bend; by default the " +
                 "trunk eases to fully rigid at 40% trunk growth (the curve reaches 1 there), leaving only branches and leaves moving.")]
        [SerializeField] private GrowthParameter trunkSway = new GrowthParameter(2f, 0f,
            // Flat tangents: eases smoothly from flexible to rigid instead of stopping abruptly at 40%.
            new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(0.4f, 1f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f)));
        [Tooltip("Degrees main branches bend with the wind. Side branches and twigs bend on top of what they grow from.")]
        [SerializeField, Min(0f)] private float branchSway = 6f;
        [Tooltip("Each level of branch bends this share as much as the one it grows from, on top of it.")]
        [SerializeField, Range(0f, 1f)] private float childSwayShare = 0.6f;
        [Tooltip("The leader bends and flutters this share as much as a main branch. It is the trunk carrying on, so it " +
                 "stays nearly as still as the trunk; its side branches still move on top of it.")]
        [SerializeField, Range(0f, 1f)] private float leaderSwayShare = 0.2f;
        [Tooltip("Degrees of individual, uneven branch movement, so branches don't move in lockstep.")]
        [SerializeField, Min(0f)] private float branchFlutter = 4f;
        [SerializeField, Min(0f)] private float branchFlutterFrequency = 2f;
        [Tooltip("Degrees leaf clusters twist back and forth about where they grow, so they stay attached as they move.")]
        [SerializeField, Min(0f)] private float foliageTwist = 10f;
        [SerializeField, Min(0f)] private float foliageFlutterFrequency = 3f;

        private sealed class Branch
        {
            public BranchSpec Spec;
            public Branch Parent; // Null for a main branch, which grows out of the trunk.
            public bool HasChildren;
            public Transform Pivot;
            public Transform Wood;
            public Transform Tip;
            public TubeShape Shape;
            public Quaternion RestRotation; // Relative to what it grows from.
            public Quaternion RestRotationInTree;
            public Vector3 WindAxis; // The tree's front-to-back axis, in this branch's parent's space.
            public float FullThickness;

            // As shown now.
            public float Progress;
            public float Length;
            public float Thickness;
        }

        private sealed class FoliageCluster
        {
            public Transform Transform;
            public Branch Anchor; // Null for a cluster on top of the trunk.
            public float Along; // Where on its branch it grows, as a share of the branch's length.
            public Vector3 Offset;
            public float SizeScale;
            public Quaternion RestRotation;
        }

        private readonly List<Mesh> meshes = new List<Mesh>();
        private Transform generatedRoot;
        private Transform trunk;
        private Transform crown;
        private TubeShape trunkShape;
        private Branch[] branches;
        private FoliageCluster[] foliage;
        private bool hasLeavesOnBranches;

        // One entry per growth part, indexed by TreeGrowthPart. Each eases toward its target on its own.
        private readonly float[] displayedGrowth = new float[TreeGrowth.Parts.Length];
        private readonly float[] targetGrowth = new float[TreeGrowth.Parts.Length];
        private readonly float[] growthVelocity = new float[TreeGrowth.Parts.Length];

        private float windStrength;
        private float appliedWindStrength;

        private void Awake()
        {
            BuildIfNeeded();
        }

        private void OnEnable()
        {
            if (treeGrowth == null)
            {
                Debug.LogWarning($"{nameof(TreeVisuals)}: no {nameof(TreeGrowth)} assigned.", this);
                return;
            }

            treeGrowth.GrowthChanged += OnGrowthChanged;
            foreach (TreeGrowthPart part in TreeGrowth.Parts)
            {
                targetGrowth[(int)part] = treeGrowth.GetGrowth(part);
            }

            SnapToTarget();
        }

        private void OnDisable()
        {
            if (treeGrowth != null)
            {
                treeGrowth.GrowthChanged -= OnGrowthChanged;
            }
        }

        private void OnDestroy()
        {
            DestroyMeshes();
        }

        private void Update()
        {
            // Idle once settled: transforms are only touched while the tree is catching up.
            bool anyChanged = false;
            for (int i = 0; i < displayedGrowth.Length; i++)
            {
                if (displayedGrowth[i] == targetGrowth[i])
                {
                    continue;
                }

                displayedGrowth[i] = Mathf.SmoothDamp(displayedGrowth[i], targetGrowth[i], ref growthVelocity[i], growthSmoothTime);
                if (Mathf.Abs(displayedGrowth[i] - targetGrowth[i]) < SettleThreshold)
                {
                    displayedGrowth[i] = targetGrowth[i];
                    growthVelocity[i] = 0f;
                }

                anyChanged = true;
            }

            if (anyChanged)
            {
                Render();
            }
        }

        // After Update, so wind is layered on top of this frame's growth pose (Render sets the rest pose).
        private void LateUpdate()
        {
            // Calm and already at rest: nothing to do.
            if (windStrength == 0f && appliedWindStrength == 0f)
            {
                return;
            }

            ApplyWindPose();
        }

        private void OnValidate()
        {
            // Curve and size tweaks in Play Mode show up immediately. Layout changes (seed, counts) need a rebuild.
            if (Application.isPlaying && generatedRoot != null)
            {
                Render();
            }
        }

        /// <summary>Skips the easing and shows the current growth immediately (e.g. after loading or for tests).</summary>
        public void SnapToTarget()
        {
            for (int i = 0; i < displayedGrowth.Length; i++)
            {
                displayedGrowth[i] = targetGrowth[i];
                growthVelocity[i] = 0f;
            }

            Render();
        }

        /// <summary>
        /// Sets the horizontal wind acting on the tree: signed, positive pushes to the right, roughly 0..1 in magnitude.
        /// The tree doesn't know where wind comes from; it just reacts to this value.
        /// </summary>
        public void SetWind(float strength)
        {
            windStrength = float.IsFinite(strength) ? strength : 0f;
        }

        /// <summary>
        /// A world position on the trunk as it looks right now, for things that sit on it. Height is 0 at the base and
        /// 1 at the top; side is -1 at its left edge and 1 at its right edge.
        /// </summary>
        public Vector3 GetTrunkPoint(float height, float side)
        {
            BuildIfNeeded();
            return trunk.TransformPoint(new Vector3(side * TubeMesh.Radius(height, trunkShape), height, 0f));
        }

        private void OnGrowthChanged(TreeGrowthPart part, float growth)
        {
            targetGrowth[(int)part] = growth;
            if (growthSmoothTime <= 0f)
            {
                SnapToTarget();
            }
        }

        private float Displayed(TreeGrowthPart part)
        {
            return Mathf.Clamp01(displayedGrowth[(int)part]);
        }

        /// <summary>Updates the existing parts to match the displayed growth. Never creates objects once built.</summary>
        private void Render()
        {
            BuildIfNeeded();

            float height = trunkHeight.Evaluate(Displayed(TreeGrowthPart.Trunk));
            float thickness = trunkThickness.Evaluate(Displayed(TreeGrowthPart.Trunk));

            // The trunk's mesh is 1 long and 1 thick, so it scales straight to its size.
            trunk.localScale = new Vector3(thickness, height, thickness);
            crown.localPosition = new Vector3(0f, height, 0f);

            // Parents come before their children, so each branch can sit on its parent as it is now.
            float branchGrowth = Displayed(TreeGrowthPart.Branches);
            foreach (Branch branch in branches)
            {
                ApplyBranch(branch, branchGrowth, height, thickness);
            }

            ApplyFoliage(Displayed(TreeGrowthPart.Foliage), branchGrowth);
        }

        private void ApplyBranch(Branch branch, float branchGrowth, float trunkHeightValue, float trunkThicknessValue)
        {
            branch.Progress = BranchSkeleton.Progress(branch.Spec, branchGrowth);
            branch.Length = branch.Spec.Length * branchLengthOverProgress.Evaluate(branch.Progress);

            bool parentVisible = branch.Parent == null || branch.Parent.Pivot.gameObject.activeSelf;
            bool visible = parentVisible && branch.Progress > 0f && branch.Length > MinVisibleSize;
            branch.Pivot.gameObject.SetActive(visible);
            if (!visible)
            {
                branch.Progress = 0f;
                return;
            }

            // Where it leaves its parent, and how thick the parent is there: it is a share of that, thinner while young.
            float along = branch.Spec.AttachAlong;
            float parentThickness;
            if (branch.Parent == null)
            {
                branch.Pivot.localPosition = new Vector3(0f, along * trunkHeightValue, 0f);
                parentThickness = trunkThicknessValue * TubeMesh.Radius(along, trunkShape) / TubeMesh.BaseRadius;
            }
            else
            {
                branch.Pivot.localPosition = PointAlong(branch.Parent, along);
                parentThickness = branch.Parent.Thickness * TubeMesh.Radius(along, branch.Parent.Shape) / TubeMesh.BaseRadius;
            }

            // The leader is the trunk carrying on, not a new shoot, so it is as thick as the trunk's top from the start.
            float sprouting = branch.Spec.IsLeader ? 1f : Mathf.Lerp(sproutThickness, 1f, branch.Progress);
            float thickness = parentThickness * branch.Spec.ThicknessRatio * sprouting;
            branch.Thickness = Mathf.Min(Mathf.Max(thickness, branch.Length * minThicknessPerLength), parentThickness);

            // The wood's mesh is built at full size, so it scales by how much of that it has reached.
            branch.Wood.localScale = new Vector3(
                branch.Thickness / branch.FullThickness,
                branch.Length / branch.Spec.Length,
                branch.Thickness / branch.FullThickness);
            branch.Tip.localPosition = PointAlong(branch, 1f);
        }

        /// <summary>A point a share of the way along a branch's spine, in its pivot's space, as it is scaled now.</summary>
        private static Vector3 PointAlong(Branch branch, float along)
        {
            NumericsVector3 point = TubeMesh.SpinePoint(along, branch.Spec.Bend) * branch.Spec.Length;
            Vector3 scale = branch.Wood.localScale;
            return new Vector3(point.X * scale.x, point.Y * scale.y, point.Z * scale.z);
        }

        private void ApplyFoliage(float foliageGrowth, float branchGrowth)
        {
            float visibleCount = Mathf.Clamp01(foliageDensity.Evaluate(foliageGrowth)) * foliage.Length;
            float baseSize = foliageClusterSize.Evaluate(foliageGrowth);
            // With no branches to take over, the leaves on top of the trunk are all the crown there is, so they stay.
            float crownShare = hasLeavesOnBranches ? Mathf.Clamp01(crownLeavesOverBranchGrowth.Evaluate(branchGrowth)) : 1f;

            for (int i = 0; i < foliage.Length; i++)
            {
                FoliageCluster cluster = foliage[i];

                // Cluster i fades in as the visible count passes i, giving a continuous increase in density.
                float appear = Mathf.Clamp01(visibleCount - i);
                if (cluster.Anchor != null)
                {
                    // Leaves on a branch wait until that branch has some length.
                    appear *= Mathf.Clamp01(cluster.Anchor.Progress * 3f);
                }
                else
                {
                    // Leaves on top of the trunk would sit below the branches around them on a grown tree.
                    appear *= crownShare;
                }

                float size = baseSize * cluster.SizeScale * appear;
                if (size <= MinVisibleSize)
                {
                    cluster.Transform.gameObject.SetActive(false);
                    continue;
                }

                // Its branch's pivot isn't scaled, so the cluster keeps its shape; its spot moves out as the branch grows.
                Vector3 growsAt = cluster.Anchor != null ? PointAlong(cluster.Anchor, cluster.Along) : Vector3.zero;
                cluster.Transform.localScale = Vector3.one * size;
                cluster.Transform.localPosition = growsAt + cluster.Offset * baseSize;

                // Leaves low on a young tree could reach into the ground. Those wait until the tree has grown them clear
                // of it, as moving them up would leave them floating away from their branch.
                float lowest = cluster.Transform.position.y - size * LeafCardMesh.MaxReach;
                cluster.Transform.gameObject.SetActive(lowest >= transform.position.y);
            }
        }

        /// <summary>
        /// Layers wind on top of the rest pose: the tree leans from its base, branches bend and flutter, each level on
        /// top of the one it grows from, and leaf clusters twist about their stems. Only rotations change, so growth
        /// stays deterministic and leaves stay attached.
        /// </summary>
        private void ApplyWindPose()
        {
            BuildIfNeeded();
            float time = Time.time;
            float flutterAmount = Mathf.Abs(windStrength);

            // Negative Z rotation tilts the top toward +X, i.e. downwind for positive strength.
            generatedRoot.localRotation = Quaternion.Euler(0f, 0f, -windStrength * trunkSway.Evaluate(Displayed(TreeGrowthPart.Trunk)));

            for (int i = 0; i < branches.Length; i++)
            {
                Branch branch = branches[i];
                float levelShare = Mathf.Pow(childSwayShare, branch.Spec.Level - BranchSkeleton.MainLevel);
                float stiffness = branch.Spec.IsLeader ? leaderSwayShare : 1f;
                float flutter = SignedNoise(time * branchFlutterFrequency, i) * branchFlutter * flutterAmount;
                float bend = (windStrength * branchSway * levelShare + flutter) * stiffness;

                // About the tree's own front-to-back axis, so every branch bends the same way, whichever way it faces.
                branch.Pivot.localRotation = Quaternion.AngleAxis(-bend, branch.WindAxis) * branch.RestRotation;
            }

            for (int i = 0; i < foliage.Length; i++)
            {
                FoliageCluster cluster = foliage[i];
                if (!cluster.Transform.gameObject.activeSelf)
                {
                    continue;
                }

                // Offset noise rows per cluster so leaves move independently.
                float noiseTime = time * foliageFlutterFrequency;
                float twist = SignedNoise(noiseTime, i + 100.5f) * foliageTwist * flutterAmount;
                cluster.Transform.localRotation = cluster.RestRotation * Quaternion.Euler(0f, 0f, twist);
            }

            appliedWindStrength = windStrength;
        }

        /// <summary>Smooth noise in -1..1. Each row gives an independent but deterministic pattern.</summary>
        private static float SignedNoise(float time, float row)
        {
            return (Mathf.PerlinNoise(time, row) - 0.5f) * 2f;
        }

        /// <summary>Discards and regenerates the parts, e.g. after changing the seed or counts in Play Mode.</summary>
        [ContextMenu("Rebuild Tree")]
        private void Rebuild()
        {
            if (!Application.isPlaying)
            {
                Debug.Log($"{nameof(TreeVisuals)}: the tree is generated in Play Mode only.", this);
                return;
            }

            if (generatedRoot != null)
            {
                Destroy(generatedRoot.gameObject);
                generatedRoot = null;
                DestroyMeshes();
            }

            Render();
        }

        private void BuildIfNeeded()
        {
            if (generatedRoot != null)
            {
                return;
            }

            generatedRoot = CreateEmpty("Generated", transform);

            // Straight, so it can be built once at length and thickness 1 and scaled to any size.
            trunkShape = new TubeShape
            {
                Sides = trunkSides,
                Rings = trunkRings,
                TipRadiusRatio = trunkTipRatio,
                Flare = trunkFlare,
                TextureRepeatsAlong = BarkRepeatsAlong(trunkHeight.Evaluate(1f), trunkThickness.Evaluate(1f)),
            };
            trunk = CreateWood("Trunk", generatedRoot, trunkShape);
            crown = CreateEmpty("Crown", generatedRoot);

            BuildBranches();
            BuildFoliage();
        }

        private void BuildBranches()
        {
            var skeleton = new BranchSkeleton(LayoutTuning(), seed);
            branches = new Branch[skeleton.Branches.Count];
            float fullTrunkThickness = trunkThickness.Evaluate(1f);

            foreach (BranchSpec spec in skeleton.Branches)
            {
                Branch parent = spec.ParentIndex == BranchSkeleton.Trunk ? null : branches[spec.ParentIndex];
                if (parent != null)
                {
                    parent.HasChildren = true;
                }

                // The thickness it reaches once everything is fully grown, from what it grows out of at that point.
                float parentFullThickness = parent?.FullThickness ?? fullTrunkThickness;
                TubeShape parentShape = parent?.Shape ?? trunkShape;
                float fullThickness = parentFullThickness * TubeMesh.Radius(spec.AttachAlong, parentShape) / TubeMesh.BaseRadius * spec.ThicknessRatio;

                // Built at its full size, so its curve keeps its shape as it is scaled down while growing.
                var shape = new TubeShape
                {
                    Sides = spec.Level == BranchSkeleton.TwigLevel ? twigSides : branchSides,
                    Rings = branchRings,
                    TipRadiusRatio = spec.IsLeader ? leaderTipRatio : branchTipRatio,
                    Bend = spec.Bend,
                    Length = spec.Length,
                    Thickness = fullThickness,
                    TextureRepeatsAlong = BarkRepeatsAlong(spec.Length, fullThickness),
                };

                // Its mesh runs up its own +Y and curves toward its own -X, which this turns to point upward and outward.
                Quaternion restInTree = Quaternion.Euler(0f, -spec.Yaw, 0f) * Quaternion.Euler(0f, 0f, -spec.Tilt);
                Quaternion parentInTree = parent?.RestRotationInTree ?? Quaternion.identity;
                Quaternion rest = Quaternion.Inverse(parentInTree) * restInTree;

                Transform pivot = CreateEmpty($"Branch {spec.Index}", parent?.Pivot ?? generatedRoot);
                pivot.localRotation = rest;

                branches[spec.Index] = new Branch
                {
                    Spec = spec,
                    Parent = parent,
                    Pivot = pivot,
                    Wood = CreateWood("Wood", pivot, shape),
                    Tip = CreateEmpty("Tip", pivot),
                    Shape = shape,
                    RestRotation = rest,
                    RestRotationInTree = restInTree,
                    WindAxis = Quaternion.Inverse(parentInTree) * Vector3.forward,
                    FullThickness = fullThickness,
                };
            }
        }

        private void BuildFoliage()
        {
            // A private, seeded generator keeps the layout independent of UnityEngine.Random and of call order elsewhere.
            var random = new System.Random(seed);
            List<Branch> branchSlots = LeafSlotsOnBranches(foliageClusterCount - crownClusterCount, random);
            hasLeavesOnBranches = branchSlots.Count > 0;

            // One mesh shared by every cluster; each cluster is turned and tinted differently, so they don't look alike.
            Mesh leafCards = ToUnityMesh(LeafCardMesh.Build(leafCardsPerCluster, seed), "Leaf Cards");
            meshes.Add(leafCards);

            var tint = new MaterialPropertyBlock();

            foliage = new FoliageCluster[foliageClusterCount];
            for (int i = 0; i < foliageClusterCount; i++)
            {
                // The first ones sit on top of the trunk, so they show first on a young tree. The rest go on the branches.
                bool onCrown = i < crownClusterCount || branchSlots.Count == 0;
                Branch anchor = onCrown ? null : branchSlots[i - crownClusterCount];

                // Clusters on a branch grow right from it. Those on the trunk grow out of it at points a little below
                // its top, so their stems start inside the wood too.
                Vector3 offset = onCrown ? Vector3.down * random.NextFloat(0f, foliageSpread) : Vector3.zero;

                Quaternion rest = LeafClusterRotation(anchor, random);
                Transform cluster = CreateLeafCluster($"Foliage {i}", anchor?.Pivot ?? crown, leafCards);
                cluster.localRotation = rest;
                tint.SetColor("_BaseColor", Color.Lerp(darkLeafTint, lightLeafTint, random.NextFloat(0f, 1f)));
                cluster.GetComponent<MeshRenderer>().SetPropertyBlock(tint);

                foliage[i] = new FoliageCluster
                {
                    Transform = cluster,
                    Anchor = anchor,
                    Along = random.NextFloat(leavesFromAlongBranch, 1f),
                    Offset = offset,
                    SizeScale = random.NextFloat(0.75f, 1.2f),
                    RestRotation = rest,
                };
            }
        }

        /// <summary>
        /// The branch each of the leaf clusters on branches grows from, in the order they appear as foliage grows. Leaves
        /// grow on the outer branches, the side branches and twigs, with the twigs getting more, as a real crown carries
        /// most of its leaves on its sunlit outside. The clusters are spread evenly over those, then put in a seeded
        /// shuffled order, so the crown fills in all over at once instead of from the lowest branch up.
        /// Empty if there are no branches to grow on.
        /// </summary>
        private List<Branch> LeafSlotsOnBranches(int count, System.Random random)
        {
            var weighted = new List<Branch>();
            foreach (Branch branch in branches)
            {
                if (branch.Spec.Level <= BranchSkeleton.MainLevel)
                {
                    continue;
                }

                int weight = branch.Spec.Level == BranchSkeleton.TwigLevel ? twigLeafWeight : 1;
                for (int i = 0; i < weight; i++)
                {
                    weighted.Add(branch);
                }
            }

            var slots = new List<Branch>();
            if (weighted.Count == 0)
            {
                return slots;
            }

            // Evenly over the whole list, so with fewer clusters than places no part of the tree is left out.
            for (int i = 0; i < count; i++)
            {
                slots.Add(weighted[i * weighted.Count / count]);
            }

            for (int i = slots.Count - 1; i > 0; i--)
            {
                int swap = random.Next(i + 1);
                (slots[i], slots[swap]) = (slots[swap], slots[i]);
            }

            return slots;
        }

        /// <summary>
        /// Turns a cluster, whose sprigs grow up its own +Y from their stems, so they grow outward along its branch and a
        /// little upward, the way leaves reach for light, while still facing the camera. On top of the trunk they grow
        /// upward. Worked out in the tree's space, then made relative to what the cluster hangs from.
        /// </summary>
        private static Quaternion LeafClusterRotation(Branch anchor, System.Random random)
        {
            Vector3 growthDirection = anchor != null
                ? (anchor.RestRotationInTree * Vector3.up + Vector3.up * 0.5f).normalized
                : Vector3.up;

            // Keeps the cards facing the camera (along -Z), whichever way the branch points, with a little variety.
            Quaternion inTree = Quaternion.LookRotation(Vector3.forward, growthDirection) *
                                Quaternion.Euler(0f, random.NextFloat(-20f, 20f), random.NextFloat(-30f, 30f));
            Quaternion parentInTree = anchor?.RestRotationInTree ?? Quaternion.identity;
            return Quaternion.Inverse(parentInTree) * inTree;
        }

        private Transform CreateLeafCluster(string objectName, Transform parent, Mesh leafCards)
        {
            var go = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = leafCards;
            if (foliageMaterial != null)
            {
                go.GetComponent<MeshRenderer>().sharedMaterial = foliageMaterial;
            }

            return go.transform;
        }

        private BranchSkeletonTuning LayoutTuning()
        {
            return new BranchSkeletonTuning
            {
                MainBranchCount = mainBranchCount,
                MainBranchSpacing = mainBranchSpacing,
                MinSideBranches = Mathf.Max(0, Mathf.Min(sideBranchesPerBranch.x, sideBranchesPerBranch.y)),
                MaxSideBranches = Mathf.Max(0, Mathf.Max(sideBranchesPerBranch.x, sideBranchesPerBranch.y)),
                MinTwigs = Mathf.Max(0, Mathf.Min(twigsPerBranch.x, twigsPerBranch.y)),
                MaxTwigs = Mathf.Max(0, Mathf.Max(twigsPerBranch.x, twigsPerBranch.y)),
                LeaderLength = Mathf.Max(0f, leaderLength),
            };
        }

        /// <summary>How often the bark repeats along a piece of wood, so it keeps the same proportions as around it.</summary>
        private static float BarkRepeatsAlong(float length, float thickness)
        {
            return Mathf.Max(0.1f, length / (Mathf.PI * thickness));
        }

        private Transform CreateWood(string objectName, Transform parent, TubeShape shape)
        {
            Mesh mesh = ToUnityMesh(TubeMesh.Build(shape), objectName);
            meshes.Add(mesh);

            var go = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            if (barkMaterial != null)
            {
                go.GetComponent<MeshRenderer>().sharedMaterial = barkMaterial;
            }

            return go.transform;
        }

        private static Mesh ToUnityMesh(MeshData data, string meshName)
        {
            var vertices = new Vector3[data.Vertices.Length];
            var normals = new Vector3[data.Normals.Length];
            var uvs = new Vector2[data.Uvs.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector3(data.Vertices[i].X, data.Vertices[i].Y, data.Vertices[i].Z);
                normals[i] = new Vector3(data.Normals[i].X, data.Normals[i].Y, data.Normals[i].Z);
                uvs[i] = new Vector2(data.Uvs[i].X, data.Uvs[i].Y);
            }

            var mesh = new Mesh { name = meshName, vertices = vertices, normals = normals, uv = uvs, triangles = data.Triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void DestroyMeshes()
        {
            foreach (Mesh mesh in meshes)
            {
                Destroy(mesh);
            }

            meshes.Clear();
        }

        private static Transform CreateEmpty(string objectName, Transform parent)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            return go.transform;
        }
    }
}
