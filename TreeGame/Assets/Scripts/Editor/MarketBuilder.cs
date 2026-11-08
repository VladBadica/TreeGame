using TMPro;
using TreeGame.Interaction;
using TreeGame.Items;
using TreeGame.Market;
using TreeGame.Player;
using TreeGame.Trees;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static TreeGame.EditorTools.BuilderHelpers;
using static TreeGame.EditorTools.MenuUIFactory;
using static TreeGame.EditorTools.SceneLayout;
using static TreeGame.EditorTools.UIFactory;

namespace TreeGame.EditorTools
{
    /// <summary>Builds the market: its offers, the stall by the left fence and the menu that opens at it.</summary>
    internal static class MarketBuilder
    {
        private const string OffersFolder = "Assets/Market";
        // Above the HUD and the debug panel, so its background blocks clicks on both; below the pause menu.
        private const int CanvasSortingOrder = 150;
        private const float PanelWidth = 900f;
        private const float PanelHeight = 720f;
        private const float RowHeight = 128f;
        private const float IconSize = 96f;

        /// <summary>The three tonics, one per part of the tree. Existing assets are reused, so Inspector edits survive.</summary>
        public static MarketOffer[] GetOrCreateOffers()
        {
            return new[]
            {
                GetOrCreateOffer("TrunkTonic", "Heartwood Tonic", "The trunk grows taller and thicker faster.",
                    TreeGrowthPart.Trunk, PlaceholderArt.GetOrCreateTrunkTonicIcon()),
                GetOrCreateOffer("BranchTonic", "Twig Tonic", "The branches spread out faster.",
                    TreeGrowthPart.Branches, PlaceholderArt.GetOrCreateBranchTonicIcon()),
                GetOrCreateOffer("LeafTonic", "Leafy Tonic", "The leaves fill in faster.",
                    TreeGrowthPart.Foliage, PlaceholderArt.GetOrCreateLeafTonicIcon()),
            };
        }

        private static MarketOffer GetOrCreateOffer(string assetName, string displayName, string description,
            TreeGrowthPart part, Sprite icon)
        {
            string path = $"{OffersFolder}/{assetName}.asset";
            var offer = AssetDatabase.LoadAssetAtPath<MarketOffer>(path);
            if (offer != null)
            {
                // A picture deleted to be repainted comes back on its own; a picture chosen in the Inspector is kept.
                if (offer.Icon == null)
                {
                    SetReferences(offer, ("icon", icon));
                }

                return offer;
            }

            EnsureFolder(OffersFolder);
            // Price and rate increase come from MarketOffer's field initializers, so they live in one place.
            offer = ScriptableObject.CreateInstance<MarketOffer>();
            AssetDatabase.CreateAsset(offer, path);

            SetReferences(offer, ("icon", icon));
            var serializedOffer = new SerializedObject(offer);
            serializedOffer.FindProperty("displayName").stringValue = displayName;
            serializedOffer.FindProperty("description").stringValue = description;
            serializedOffer.FindProperty("part").intValue = (int)part;
            serializedOffer.ApplyModifiedPropertiesWithoutUndo();
            return offer;
        }

        /// <summary>
        /// The stall in the game scene: its model plus the zone where the player can use it. The inventory is assigned
        /// once the player exists; see PrototypeSceneBuilder.
        /// </summary>
        public static MarketStall CreateStall(MarketOffer[] offers, ItemDefinition coin, TreeGrowthRates growthRates,
            out Interactable interactable)
        {
            var stallObject = new GameObject("Market Stall").transform;
            stallObject.position = MarketPosition;

            CreateStallModel(stallObject, offers);

            // As wide as the counter, so the prompt appears once the player reaches it.
            interactable = CreateInteractionZone(stallObject, width: 1.6f, promptHeight: 2.7f, "shop");
            var stall = stallObject.gameObject.AddComponent<MarketStall>();
            SetReferences(stall,
                ("coin", coin),
                ("growthRates", growthRates));
            SetReferenceArray(stall, "offers", offers);
            return stall;
        }

        /// <summary>
        /// Placeholder boxes, about 2.4 units tall with the base at the parent's origin: a wooden counter with a
        /// bottle of each tonic on it, under a striped awning held up by two posts.
        /// </summary>
        private static void CreateStallModel(Transform stall, MarketOffer[] offers)
        {
            Material wood = GetOrCreateMaterial("Well Wood", new Color(0.55f, 0.38f, 0.24f));
            Material counterTop = GetOrCreateMaterial("Stall Counter", new Color(0.72f, 0.54f, 0.36f));
            Material awningRed = GetOrCreateMaterial("Stall Awning Red", new Color(0.82f, 0.36f, 0.32f));
            Material awningCream = GetOrCreateMaterial("Stall Awning Cream", new Color(0.96f, 0.9f, 0.78f));

            // Cube primitives are 1 unit, so scale is size and position is the box's center.
            CreatePart(PrimitiveType.Cube, "Counter", stall, new Vector3(0f, 0.45f, 0f), new Vector3(1.6f, 0.9f, 0.7f), wood);
            CreatePart(PrimitiveType.Cube, "Counter Top", stall, new Vector3(0f, 0.94f, 0f), new Vector3(1.75f, 0.08f, 0.8f), counterTop);

            // At the back of the counter, so they don't hide the goods.
            CreatePart(PrimitiveType.Cube, "Left Post", stall, new Vector3(-0.8f, 1.1f, 0.3f), new Vector3(0.08f, 2.2f, 0.08f), wood);
            CreatePart(PrimitiveType.Cube, "Right Post", stall, new Vector3(0.8f, 1.1f, 0.3f), new Vector3(0.08f, 2.2f, 0.08f), wood);

            // Alternating stripes, tilted down toward the camera, with a short hanging flap along the front edge.
            const int stripeCount = 6;
            const float awningWidth = 2f;
            const float stripeWidth = awningWidth / stripeCount;
            for (int i = 0; i < stripeCount; i++)
            {
                float x = -awningWidth / 2f + stripeWidth * (i + 0.5f);
                Material stripeMaterial = i % 2 == 0 ? awningRed : awningCream;
                Transform stripe = CreatePart(PrimitiveType.Cube, "Awning Stripe", stall,
                    new Vector3(x, 2.25f, -0.05f), new Vector3(stripeWidth, 0.05f, 1f), stripeMaterial);
                stripe.localRotation = Quaternion.Euler(-15f, 0f, 0f);
                CreatePart(PrimitiveType.Cube, "Awning Flap", stall,
                    new Vector3(x, 2.03f, -0.53f), new Vector3(stripeWidth, 0.22f, 0.03f), stripeMaterial);
            }

            // A bottle of each tonic, colored like its picture, so the stall shows what it sells.
            Color[] bottleColors = { new Color(0.77f, 0.47f, 0.2f), new Color(0.87f, 0.67f, 0.19f), new Color(0.41f, 0.69f, 0.31f) };
            for (int i = 0; i < offers.Length && i < bottleColors.Length; i++)
            {
                Material bottle = GetOrCreateMaterial($"Stall {offers[i].name}", bottleColors[i]);
                float x = (i - (offers.Length - 1) / 2f) * 0.45f;
                CreatePart(PrimitiveType.Cylinder, "Bottle", stall, new Vector3(x, 1.1f, -0.1f), new Vector3(0.16f, 0.12f, 0.16f), bottle);
                CreatePart(PrimitiveType.Cylinder, "Neck", stall, new Vector3(x, 1.26f, -0.1f), new Vector3(0.06f, 0.05f, 0.06f), bottle);
            }
        }

        /// <summary>The market's overlay, hidden until the player uses the stall.</summary>
        public static MarketMenu CreateMenu(MarketStall stall, Interactable interactable, Transform player, Behaviour pauseMenu)
        {
            DefaultControls.Resources resources = GetUIResources();
            Transform canvas = CreateCanvas("Market Menu", CanvasSortingOrder);

            var root = new GameObject("Root", typeof(RectTransform));
            StretchInParent(root, canvas);

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            StretchInParent(background, root.transform);
            background.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

            GameObject panel = CreateScreenPanel(root.transform, "Panel", "Market", PanelHeight);
            ((RectTransform)panel.transform).sizeDelta = new Vector2(PanelWidth, PanelHeight);
            var panelLayout = panel.GetComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(28, 28, 16, 24);
            panelLayout.spacing = 14f;

            TMP_Text coinLabel = CreateCoinLine(panel.transform, stall.Coin.Icon);

            foreach (MarketOffer offer in stall.Offers)
            {
                CreateOfferRow(resources, panel.transform, stall, offer);
            }

            // Takes the spare height, which pushes the close button to the bottom.
            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(panel.transform, false);
            spacer.GetComponent<LayoutElement>().flexibleHeight = 1f;

            Button close = CreateMenuButton(resources, panel.transform, "Close");
            AddClickSound(canvas);

            var menu = canvas.gameObject.AddComponent<MarketMenu>();
            SetReferences(menu,
                ("stall", stall),
                ("interactable", interactable),
                ("root", root),
                ("coinLabel", coinLabel),
                ("closeButton", close),
                ("cancelAction", FindActionReference("UI", "Cancel")),
                ("interactAction", FindActionReference("Player", "Interact")));
            SetReferenceArray(menu, "gameplayInput",
                player.GetComponent<PlayerMovement>(),
                player.GetComponent<Interactor>(),
                player.GetComponent<ItemCollector>(),
                player.GetComponent<HotbarInput>(),
                player.GetComponent<ClickToInteract>(),
                pauseMenu);

            root.SetActive(false); // Saved hidden; it opens at the stall.
            return menu;
        }

        /// <summary>A coin and how many the player has, centered under the title.</summary>
        private static TMP_Text CreateCoinLine(Transform panel, Sprite coinIcon)
        {
            const float height = 44f;

            var row = new GameObject("Coins", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            AddToLayout(row, panel, height);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            CreateIcon(row.transform, "Coin", coinIcon, 36f);

            TMP_Text label = CreateText("Coin Count", 28f, Color.white, TextAlignmentOptions.MidlineLeft);
            label.transform.SetParent(row.transform, false);
            label.text = "You have 0 coins"; // Placeholder; the menu shows the real count at runtime.
            return label;
        }

        /// <summary>
        /// One offer: its picture on the left, then its name, what it does and the growth rate before and after, and on
        /// the right its price above a Buy button. The row fills in its text at runtime.
        /// </summary>
        private static void CreateOfferRow(DefaultControls.Resources resources, Transform panel, MarketStall stall, MarketOffer offer)
        {
            var row = new GameObject($"{offer.DisplayName} Row", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            AddToLayout(row, panel, RowHeight);
            StyleImage(row.GetComponent<Image>(), new Color(1f, 1f, 1f, 0.08f));
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 20, 12, 12);
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            Image icon = CreateIcon(row.transform, "Icon", offer.Icon, IconSize);

            GameObject details = CreateColumn(row.transform, "Details", TextAnchor.MiddleLeft, spacing: 2f);
            details.GetComponent<LayoutElement>().flexibleWidth = 1f;
            TMP_Text nameLabel = CreateColumnText(details.transform, "Name", 30f, TextAlignmentOptions.MidlineLeft);
            nameLabel.fontStyle = FontStyles.Bold;
            TMP_Text descriptionLabel = CreateColumnText(details.transform, "Description", 22f, TextAlignmentOptions.MidlineLeft);
            TMP_Text rateLabel = CreateColumnText(details.transform, "Growth Rate", 20f, TextAlignmentOptions.MidlineLeft);
            rateLabel.color = new Color(1f, 1f, 1f, 0.75f);

            GameObject purchase = CreateColumn(row.transform, "Purchase", TextAnchor.MiddleCenter, spacing: 6f);
            // A fixed width in every row, so the Buy buttons line up. Without flexibleWidth 0 the column would share the
            // row's spare room with the details, and how much is spare depends on each offer's text.
            var purchaseLayout = purchase.GetComponent<LayoutElement>();
            purchaseLayout.minWidth = purchaseLayout.preferredWidth = 150f;
            purchaseLayout.flexibleWidth = 0f;
            TMP_Text priceLabel = CreateColumnText(purchase.transform, "Price", 24f, TextAlignmentOptions.Center);
            Button buy = CreateButton(resources, purchase.transform, "Buy Button", "Buy", 26f);
            buy.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;

            // Placeholders, so the row reads sensibly in the Editor; the row shows the real values at runtime.
            nameLabel.text = offer.DisplayName;
            descriptionLabel.text = offer.Description;
            rateLabel.text = "Growth rate 5, then 6";
            priceLabel.text = "1 coin";

            SetReferences(row.AddComponent<MarketOfferRow>(),
                ("stall", stall),
                ("offer", offer),
                ("icon", icon),
                ("nameLabel", nameLabel),
                ("descriptionLabel", descriptionLabel),
                ("rateLabel", rateLabel),
                ("priceLabel", priceLabel),
                ("buyButton", buy),
                ("buyCaption", buy.GetComponentInChildren<TMP_Text>()));
        }

        private static Image CreateIcon(Transform parent, string objectName, Sprite sprite, float size)
        {
            var iconObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconObject.transform.SetParent(parent, false);
            var iconLayout = iconObject.GetComponent<LayoutElement>();
            iconLayout.preferredWidth = iconLayout.preferredHeight = size;

            var image = iconObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static GameObject CreateColumn(Transform parent, string objectName, TextAnchor alignment, float spacing)
        {
            var column = new GameObject(objectName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            column.transform.SetParent(parent, false);
            var layout = column.GetComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return column;
        }

        /// <summary>Text sized by its own content, so the column stacks its lines without gaps.</summary>
        private static TMP_Text CreateColumnText(Transform column, string objectName, float fontSize, TextAlignmentOptions alignment)
        {
            TMP_Text text = CreateText(objectName, fontSize, Color.white, alignment);
            text.transform.SetParent(column, false);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }
    }
}
