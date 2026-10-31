using TreeGame.Core;
using TreeGame.Interaction;
using TreeGame.Items;
using TreeGame.UI;
using TreeGame.World;
using UnityEngine;
using UnityEngine.UI;
using static TreeGame.EditorTools.BuilderHelpers;
using static TreeGame.EditorTools.SceneLayout;
using static TreeGame.EditorTools.UIFactory;

namespace TreeGame.EditorTools
{
    /// <summary>Builds the places in the world the player works at: the well and the compost heap.</summary>
    internal static class StationBuilder
    {
        /// <summary>
        /// Placeholder compost heap: a single box of dark soil resting on the ground. Leaves dropped in turn into
        /// compost that the player collects into the compost bucket.
        /// </summary>
        public static void CreateCompostHeap(GameConfig config, ItemDefinition leaves)
        {
            Material compost = GetOrCreateMaterial("Compost", new Color(0.36f, 0.26f, 0.18f));

            var heap = new GameObject("Compost Heap").transform;
            heap.position = CompostPosition;
            // Raised by half its height so the box rests on the ground.
            CreatePart(PrimitiveType.Cube, "Heap", heap, new Vector3(0f, 0.3f, 0f), new Vector3(1f, 0.6f, 0.8f), compost);

            // As wide as the heap; the Composter sets the prompt's wording for each of its states.
            Interactable interactable = CreateInteractionZone(heap, width: 1f, promptHeight: 1.2f, "drop leaves");
            var composter = heap.gameObject.AddComponent<Composter>();
            SetReferences(composter,
                ("interactable", interactable),
                ("config", config),
                ("compostableItem", leaves));

            CreateCompostProgressBar(heap, composter);
        }

        /// <summary>
        /// A small world-space bar just above the heap (below its prompt), so it stays with the heap on screen
        /// without any following code. Hidden until the composter starts working.
        /// </summary>
        private static void CreateCompostProgressBar(Transform heap, Composter composter)
        {
            // Canvas units are scaled down to world units: 100 x 12 units becomes 1 x 0.12 world units.
            const float worldUnitsPerCanvasUnit = 0.01f;

            var canvasObject = new GameObject("Progress Bar", typeof(RectTransform), typeof(Canvas));
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.SetParent(heap, false);
            // Above the heap's top (0.6) and just in front of its face, so the box never cuts through the bar.
            canvasRect.localPosition = new Vector3(0f, 0.85f, -0.45f);
            canvasRect.localScale = Vector3.one * worldUnitsPerCanvasUnit;
            canvasRect.sizeDelta = new Vector2(100f, 12f);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            StretchInParent(background, canvasRect);
            StyleImage(background.GetComponent<Image>(), new Color(0f, 0f, 0f, 0.5f));

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            RectTransform fillRect = StretchInParent(fill, background.transform);
            StyleImage(fill.GetComponent<Image>(), new Color(0.56f, 0.74f, 0.32f));

            var fillBar = background.AddComponent<FillBar>();
            SetReferences(fillBar, ("fill", fillRect));
            canvasObject.SetActive(false); // Shown by the progress bar component once composting starts.

            // On the heap itself (always active) so it can show and hide the bar's canvas.
            SetReferences(heap.gameObject.AddComponent<CompostProgressBar>(),
                ("composter", composter),
                ("bar", fillBar),
                ("barRoot", canvasObject));
        }

        /// <summary>Placeholder well built from boxes, about 2.2 units tall with its base at ground level.</summary>
        public static void CreateWell(GameConfig config)
        {
            Material stone = GetOrCreateMaterial("Well Stone", new Color(0.62f, 0.6f, 0.58f));
            Material rim = GetOrCreateMaterial("Well Rim", new Color(0.5f, 0.48f, 0.47f));
            Material wood = GetOrCreateMaterial("Well Wood", new Color(0.55f, 0.38f, 0.24f));
            Material roof = GetOrCreateMaterial("Well Roof", new Color(0.66f, 0.3f, 0.24f));
            Material rope = GetOrCreateMaterial("Well Rope", new Color(0.85f, 0.76f, 0.56f));

            var well = new GameObject("Well").transform;
            well.position = WellPosition;

            // Cube primitives are 1 unit, so scale is size and position is the box's center.
            CreatePart(PrimitiveType.Cube, "Base", well, new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 0.8f), stone);
            CreatePart(PrimitiveType.Cube, "Rim", well, new Vector3(0f, 0.75f, 0f), new Vector3(1.3f, 0.1f, 0.9f), rim);

            // Posts end where they meet the underside of the roof.
            CreatePart(PrimitiveType.Cube, "Left Post", well, new Vector3(-0.5f, 1.3f, 0f), new Vector3(0.1f, 1f, 0.1f), wood);
            CreatePart(PrimitiveType.Cube, "Right Post", well, new Vector3(0.5f, 1.3f, 0f), new Vector3(0.1f, 1f, 0.1f), wood);
            CreatePart(PrimitiveType.Cube, "Crossbeam", well, new Vector3(0f, 1.55f, 0f), new Vector3(1.1f, 0.08f, 0.08f), wood);

            CreatePart(PrimitiveType.Cube, "Rope", well, new Vector3(0f, 1.33f, 0f), new Vector3(0.02f, 0.36f, 0.02f), rope);
            CreatePart(PrimitiveType.Cube, "Bucket", well, new Vector3(0f, 1.05f, 0f), new Vector3(0.2f, 0.2f, 0.2f), wood);

            // Two panels tilted 35 degrees meet at a ridge above the center; their outer ends overhang the posts.
            const float roofPitch = 35f;
            Transform leftRoof = CreatePart(PrimitiveType.Cube, "Left Roof", well,
                new Vector3(-0.33f, 1.92f, 0f), new Vector3(0.8f, 0.06f, 1f), roof);
            leftRoof.localRotation = Quaternion.Euler(0f, 0f, roofPitch);
            Transform rightRoof = CreatePart(PrimitiveType.Cube, "Right Roof", well,
                new Vector3(0.33f, 1.92f, 0f), new Vector3(0.8f, 0.06f, 1f), roof);
            rightRoof.localRotation = Quaternion.Euler(0f, 0f, -roofPitch);

            // As wide as the well's base, so the prompt appears once the player reaches the well's edge
            // (the player's own trigger capsule, radius 0.3, adds to this), leaving room for other interactables.
            Interactable interactable = CreateInteractionZone(well, width: 1.2f, promptHeight: 2.5f, "get water");
            SetReferences(well.gameObject.AddComponent<WaterWell>(),
                ("interactable", interactable),
                ("config", config));
        }
    }
}
