using TreeGame.Interaction;
using TreeGame.Items;
using TreeGame.Menus;
using TreeGame.Player;
using UnityEngine;
using UnityEngine.UI;
using static TreeGame.EditorTools.BuilderHelpers;
using static TreeGame.EditorTools.MenuUIFactory;
using static TreeGame.EditorTools.UIFactory;

namespace TreeGame.EditorTools
{
    /// <summary>Builds the game scene's pause menu: main screen, settings, and a confirmation before quitting.</summary>
    internal static class PauseMenuBuilder
    {
        // Above the HUD and the debug panel, so the background blocks clicks on both.
        private const int CanvasSortingOrder = 200;

        public static void CreatePauseMenu(Transform player)
        {
            DefaultControls.Resources resources = GetUIResources();
            Transform canvas = CreateCanvas("Pause Menu", CanvasSortingOrder);

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            StretchInParent(background, canvas);
            background.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

            GameObject mainPanel = CreateScreenPanel(canvas, "Main Panel", "Paused", height: 330f);
            Button resume = CreateMenuButton(resources, mainPanel.transform, "Resume");
            Button settings = CreateMenuButton(resources, mainPanel.transform, "Settings");
            Button quitToMenu = CreateMenuButton(resources, mainPanel.transform, "Quit to Menu");

            GameObject settingsPanel = CreateSettingsPanel(canvas);
            Button settingsBack = CreateMenuButton(resources, settingsPanel.transform, "Back");

            GameObject confirmPanel = CreateInfoPanel(canvas, "Confirm Quit Panel", "Quit to Menu?",
                "All progress will be lost.", height: 390f);
            // Cancel comes first, so it is the button selected by default and a stray Enter can't lose progress.
            Button cancelQuit = CreateMenuButton(resources, confirmPanel.transform, "Cancel");
            Button confirmQuit = CreateMenuButton(resources, confirmPanel.transform, "Quit");

            var panels = canvas.gameObject.AddComponent<MenuPanels>();
            SetReferenceArray(panels, "panels", mainPanel, settingsPanel, confirmPanel);

            ConnectPanelButton(settings, panels, settingsPanel);
            ConnectPanelButton(settingsBack, panels, mainPanel);
            ConnectPanelButton(quitToMenu, panels, confirmPanel);
            ConnectPanelButton(cancelQuit, panels, mainPanel);
            ConnectLoadSceneButton(confirmQuit, BuildSceneList.MainMenuPath);

            var pauseMenu = canvas.gameObject.AddComponent<PauseMenu>();
            SetReferences(pauseMenu,
                ("pauseAction", FindActionReference("UI", "Cancel")),
                ("panels", panels),
                ("mainPanel", mainPanel),
                ("background", background),
                ("resumeButton", resume));
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
            confirmPanel.SetActive(false);
        }
    }
}
