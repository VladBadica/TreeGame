using TMPro;
using TreeGame.Interaction;
using TreeGame.Items;
using TreeGame.Menus;
using TreeGame.Minigames;
using TreeGame.Player;
using TreeGame.Trees;
using UnityEngine;
using UnityEngine.UI;
using static TreeGame.EditorTools.BuilderHelpers;
using static TreeGame.EditorTools.MenuUIFactory;
using static TreeGame.EditorTools.UIFactory;

namespace TreeGame.EditorTools
{
    /// <summary>Builds the game scene's pause menu: main screen and settings. Quitting needs no confirmation, as it saves.</summary>
    internal static class PauseMenuBuilder
    {
        // Above the HUD and the debug panel, so the background blocks clicks on both.
        private const int CanvasSortingOrder = 200;

        public static PauseMenu CreatePauseMenu(Transform player, MinigameRunner minigameRunner, TreeSeed treeSeed, out Button quitToMenuButton)
        {
            DefaultControls.Resources resources = GetUIResources();
            Transform canvas = CreateCanvas("Pause Menu", CanvasSortingOrder);

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            StretchInParent(background, canvas);
            background.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

            GameObject mainPanel = CreateScreenPanel(canvas, "Main Panel", "Paused", height: 460f);
            Button resume = CreateMenuButton(resources, mainPanel.transform, "Resume");
            Button settings = CreateMenuButton(resources, mainPanel.transform, "Settings");
            Button quitToMenu = CreateMenuButton(resources, mainPanel.transform, "Quit to Menu");
            CreateTreeCode(resources, mainPanel, treeSeed);

            GameObject settingsPanel = CreateSettingsPanel(canvas);
            Button settingsBack = CreateMenuButton(resources, settingsPanel.transform, "Back");

            var panels = canvas.gameObject.AddComponent<MenuPanels>();
            SetReferenceArray(panels, "panels", mainPanel, settingsPanel);

            ConnectPanelButton(settings, panels, settingsPanel);
            ConnectPanelButton(settingsBack, panels, mainPanel);
            ConnectLoadSceneButton(quitToMenu, BuildSceneList.MainMenuPath);
            AddClickSound(canvas);

            var pauseMenu = canvas.gameObject.AddComponent<PauseMenu>();
            SetReferences(pauseMenu,
                ("pauseAction", FindActionReference("UI", "Cancel")),
                ("panels", panels),
                ("mainPanel", mainPanel),
                ("background", background),
                ("resumeButton", resume),
                ("minigameRunner", minigameRunner));
            SetReferenceArray(pauseMenu, "gameplayInput",
                player.GetComponent<PlayerMovement>(),
                player.GetComponent<Interactor>(),
                player.GetComponent<ItemCollector>(),
                player.GetComponent<HotbarInput>(),
                player.GetComponent<ClickToInteract>());

            // Saved hidden; the game starts unpaused.
            background.SetActive(false);
            mainPanel.SetActive(false);
            settingsPanel.SetActive(false);

            quitToMenuButton = quitToMenu;
            return pauseMenu;
        }

        /// <summary>The code for this game's tree under the buttons, with a button that copies it for sharing.</summary>
        private static void CreateTreeCode(DefaultControls.Resources resources, GameObject mainPanel, TreeSeed treeSeed)
        {
            TMP_Text codeLabel = CreateText("Tree Code", 24f, Color.white, TextAlignmentOptions.Center);
            AddToLayout(codeLabel.gameObject, mainPanel.transform, 40f);
            Button copy = CreateMenuButton(resources, mainPanel.transform, "Copy Tree Code");

            SetReferences(mainPanel.AddComponent<TreeCodeDisplay>(),
                ("treeSeed", treeSeed),
                ("codeLabel", codeLabel),
                ("copyButton", copy),
                ("copyButtonLabel", copy.GetComponentInChildren<TMP_Text>()));
        }
    }
}
