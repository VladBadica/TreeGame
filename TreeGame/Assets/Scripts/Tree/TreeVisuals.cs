using TreeGame.Core;
using UnityEngine;

namespace TreeGame.Trees
{
    /// <summary>
    /// Placeholder tree made of Unity primitives that reacts to <see cref="TreeGrowth"/>.
    /// The parts are generated once from a fixed seed; growth changes only move, scale and show/hide them,
    /// so the same growth value always produces the same tree and nothing is rebuilt.
    /// The displayed growth eases toward the actual growth; that is purely presentation, TreeGrowth stays authoritative.
    /// Any other visual (procedural mesh, authored model) can replace this by listening to the same event.
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
        [SerializeField, Range(0, 12)] private int branchCount = 5;
        [SerializeField, Range(0, 64)] private int foliageClusterCount = 24;

        [Header("Trunk (over overall growth)")]
        [SerializeField] private GrowthParameter trunkHeight = new GrowthParameter(0.4f, 3.5f);
        [SerializeField] private GrowthParameter trunkThickness =
            new GrowthParameter(0.06f, 0.45f, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));

        [Header("Branches")]
        [Tooltip("Overall growth at which the first branch starts to sprout.")]
        [SerializeField, Range(0f, 1f)] private float firstBranchGrowth = 0.15f;
        [Tooltip("Overall growth at which the last branch starts to sprout.")]
        [SerializeField, Range(0f, 1f)] private float lastBranchGrowth = 0.55f;
        [Tooltip("Length over each branch's own progress (0 = just sprouted, 1 = tree fully grown).")]
        [SerializeField] private GrowthParameter branchLength =
            new GrowthParameter(0f, 1.6f, new AnimationCurve(new Keyframe(0f, 0f, 2f, 2f), new Keyframe(1f, 1f, 0f, 0f)));
        [Tooltip("Branch thickness relative to the trunk.")]
        [SerializeField, Range(0.05f, 1f)] private float branchThicknessRatio = 0.35f;
        [Tooltip("Where branches attach, relative to trunk height (lowest, highest).")]
        [SerializeField] private Vector2 branchAttachHeight = new Vector2(0.45f, 0.85f);
        [Tooltip("Branch angle away from vertical in degrees (min, max).")]
        [SerializeField] private Vector2 branchTilt = new Vector2(40f, 65f);
        [Tooltip("Max degrees a branch may turn toward or away from the camera. 0 = perfectly flat, side-on tree.")]
        [SerializeField, Range(0f, 90f)] private float branchDepthAngle = 25f;

        [Header("Foliage")]
        [Tooltip("Fraction of foliage clusters shown over overall growth. Clusters fade in one after another.")]
        [SerializeField] private GrowthParameter foliageDensity = new GrowthParameter(0.05f, 1f);
        [Tooltip("Base foliage cluster diameter over overall growth.")]
        [SerializeField] private GrowthParameter foliageClusterSize = new GrowthParameter(0.3f, 0.9f);
        [Tooltip("How far clusters scatter around their anchor, relative to cluster size.")]
        [SerializeField, Min(0f)] private float foliageSpread = 0.6f;

        private sealed class Branch
        {
            public Transform Pivot;
            public Transform Wood;
            public Transform Tip;
            public float AttachHeight;
            public float StartGrowth;
            public float LengthScale;
            public float Progress;
        }

        private sealed class FoliageCluster
        {
            public Transform Transform;
            public int BranchIndex; // -1 = attached to the top of the trunk.
            public Vector3 Offset;
            public float SizeScale;
        }

        private Transform generatedRoot;
        private Transform trunk;
        private Transform crown;
        private Branch[] branches;
        private FoliageCluster[] foliage;

        private float displayedGrowth;
        private float targetGrowth;
        private float growthVelocity;

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
            targetGrowth = treeGrowth.Growth;
            SnapToTarget();
        }

        private void OnDisable()
        {
            if (treeGrowth != null)
            {
                treeGrowth.GrowthChanged -= OnGrowthChanged;
            }
        }

        private void Update()
        {
            // Idle once settled: transforms are only touched while the tree is catching up.
            if (displayedGrowth == targetGrowth)
            {
                return;
            }

            displayedGrowth = Mathf.SmoothDamp(displayedGrowth, targetGrowth, ref growthVelocity, growthSmoothTime);
            if (Mathf.Abs(displayedGrowth - targetGrowth) < SettleThreshold)
            {
                displayedGrowth = targetGrowth;
                growthVelocity = 0f;
            }

            Render(displayedGrowth);
        }

        private void OnValidate()
        {
            // Curve and size tweaks in Play Mode show up immediately. Layout changes (seed, counts) need a rebuild.
            if (Application.isPlaying && generatedRoot != null)
            {
                Render(displayedGrowth);
            }
        }

        /// <summary>Skips the easing and shows the current growth immediately (e.g. after loading or for tests).</summary>
        public void SnapToTarget()
        {
            displayedGrowth = targetGrowth;
            growthVelocity = 0f;
            Render(displayedGrowth);
        }

        private void OnGrowthChanged(float growth)
        {
            targetGrowth = growth;
            if (growthSmoothTime <= 0f)
            {
                SnapToTarget();
            }
        }

        /// <summary>Updates the existing parts to match a normalized growth value. Never creates objects once built.</summary>
        private void Render(float growth)
        {
            BuildIfNeeded();
            growth = Mathf.Clamp01(growth);

            float height = trunkHeight.Evaluate(growth);
            float thickness = trunkThickness.Evaluate(growth);

            // Unity's cylinder is 2 units tall with its pivot in the middle, hence the half-height scale and offset.
            trunk.localScale = new Vector3(thickness, height * 0.5f, thickness);
            trunk.localPosition = new Vector3(0f, height * 0.5f, 0f);
            crown.localPosition = new Vector3(0f, height, 0f);

            foreach (Branch branch in branches)
            {
                ApplyBranch(branch, growth, height, thickness);
            }

            ApplyFoliage(growth);
        }

        private void ApplyBranch(Branch branch, float growth, float trunkHeightValue, float trunkThicknessValue)
        {
            branch.Progress = Mathf.InverseLerp(branch.StartGrowth, 1f, growth);
            float length = branchLength.Evaluate(branch.Progress) * branch.LengthScale;

            bool visible = branch.Progress > 0f && length > MinVisibleSize;
            branch.Pivot.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            // Young branches start thinner and thicken as they mature.
            float thickness = trunkThicknessValue * branchThicknessRatio * Mathf.Lerp(0.4f, 1f, branch.Progress);

            branch.Pivot.localPosition = new Vector3(0f, trunkHeightValue * branch.AttachHeight, 0f);
            branch.Wood.localScale = new Vector3(thickness, length * 0.5f, thickness);
            branch.Wood.localPosition = new Vector3(0f, length * 0.5f, 0f);
            branch.Tip.localPosition = new Vector3(0f, length, 0f);
        }

        private void ApplyFoliage(float growth)
        {
            float visibleCount = Mathf.Clamp01(foliageDensity.Evaluate(growth)) * foliage.Length;
            float baseSize = foliageClusterSize.Evaluate(growth);

            for (int i = 0; i < foliage.Length; i++)
            {
                FoliageCluster cluster = foliage[i];

                // Cluster i fades in as the visible count passes i, giving a continuous increase in density.
                float appear = Mathf.Clamp01(visibleCount - i);
                if (cluster.BranchIndex >= 0)
                {
                    // Leaves on a branch wait until that branch has some length.
                    appear *= Mathf.Clamp01(branches[cluster.BranchIndex].Progress * 3f);
                }

                float size = baseSize * cluster.SizeScale * appear;
                bool visible = size > MinVisibleSize;
                cluster.Transform.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                cluster.Transform.localScale = Vector3.one * size;
                cluster.Transform.localPosition = cluster.Offset * baseSize;
            }
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
            }

            Render(displayedGrowth);
        }

        private void BuildIfNeeded()
        {
            if (generatedRoot != null)
            {
                return;
            }

            generatedRoot = CreateEmpty("Generated", transform);
            trunk = CreatePrimitive(PrimitiveType.Cylinder, "Trunk", generatedRoot, barkMaterial);
            crown = CreateEmpty("Crown", generatedRoot);

            // A private, seeded generator keeps the layout independent of UnityEngine.Random and of call order elsewhere.
            var random = new System.Random(seed);

            branches = new Branch[branchCount];
            for (int i = 0; i < branchCount; i++)
            {
                float t = branchCount > 1 ? i / (branchCount - 1f) : 0f;
                Transform pivot = CreateEmpty($"Branch {i}", generatedRoot);

                // Alternate left/right so branches spread across the screen, as suits the side-on 2.5D view.
                // A limited turn toward or away from the camera keeps a hint of volume.
                float side = i % 2 == 0 ? 0f : 180f;
                float yaw = side + random.NextFloat(-branchDepthAngle, branchDepthAngle);
                float tilt = random.NextFloat(branchTilt.x, branchTilt.y);
                pivot.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, tilt);

                branches[i] = new Branch
                {
                    Pivot = pivot,
                    Wood = CreatePrimitive(PrimitiveType.Cylinder, "Wood", pivot, barkMaterial),
                    Tip = CreateEmpty("Tip", pivot),
                    AttachHeight = Mathf.Clamp01(Mathf.Lerp(branchAttachHeight.x, branchAttachHeight.y, t) + random.NextFloat(-0.05f, 0.05f)),
                    StartGrowth = Mathf.Lerp(firstBranchGrowth, lastBranchGrowth, t),
                    LengthScale = random.NextFloat(0.75f, 1.1f),
                };
            }

            foliage = new FoliageCluster[foliageClusterCount];
            for (int i = 0; i < foliageClusterCount; i++)
            {
                // Round-robin over crown + branches, starting with the crown so a sapling gets leaves first.
                int branchIndex = branchCount > 0 ? i % (branchCount + 1) - 1 : -1;
                Transform anchor = branchIndex >= 0 ? branches[branchIndex].Tip : crown;

                Vector3 offset = InsideUnitSphere(random) * foliageSpread;
                offset.y = Mathf.Abs(offset.y) * 0.6f; // Bias clusters upward so they sit on top of their anchor.

                foliage[i] = new FoliageCluster
                {
                    Transform = CreatePrimitive(PrimitiveType.Sphere, $"Foliage {i}", anchor, foliageMaterial),
                    BranchIndex = branchIndex,
                    Offset = offset,
                    SizeScale = random.NextFloat(0.75f, 1.15f),
                };
            }
        }

        private static Transform CreateEmpty(string objectName, Transform parent)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Transform CreatePrimitive(PrimitiveType type, string objectName, Transform parent, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = objectName;
            go.transform.SetParent(parent, false);

            // The tree is visual only; colliders can be added deliberately once interaction needs them.
            Destroy(go.GetComponent<Collider>());

            if (material != null)
            {
                go.GetComponent<Renderer>().sharedMaterial = material;
            }

            return go.transform;
        }

        private static Vector3 InsideUnitSphere(System.Random random)
        {
            // Rejection sampling: deterministic for a given seed and uniformly distributed.
            while (true)
            {
                var point = new Vector3(random.NextFloat(-1f, 1f), random.NextFloat(-1f, 1f), random.NextFloat(-1f, 1f));
                if (point.sqrMagnitude <= 1f)
                {
                    return point;
                }
            }
        }
    }
}
