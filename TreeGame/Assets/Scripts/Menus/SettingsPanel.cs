using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TreeGame.Menus
{
    /// <summary>
    /// The settings screen, used by both the main menu and the pause menu. It works directly on Unity's screen state,
    /// which the standalone player remembers between runs, so nothing needs saving yet. Reads the current state each
    /// time it opens. These changes have no effect on the Editor's Game view; test them in a build.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        private static readonly (string label, FullScreenMode mode)[] DisplayModes =
        {
            ("Fullscreen", FullScreenMode.FullScreenWindow),
            ("Windowed", FullScreenMode.Windowed),
        };

        private static readonly Vector2Int[] CommonResolutions =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1366, 768),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
            new Vector2Int(3840, 2160),
        };

        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown displayModeDropdown;

        private readonly List<Vector2Int> resolutions = new List<Vector2Int>();

        private void OnEnable()
        {
            if (resolutionDropdown == null || displayModeDropdown == null)
            {
                Debug.LogWarning($"{nameof(SettingsPanel)}: needs a resolution and a display mode dropdown.", this);
                enabled = false;
                return;
            }

            ShowCurrentResolution();
            ShowCurrentDisplayMode();

            resolutionDropdown.onValueChanged.AddListener(ApplyResolution);
            displayModeDropdown.onValueChanged.AddListener(ApplyDisplayMode);
        }

        private void OnDisable()
        {
            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.RemoveListener(ApplyResolution);
            }

            if (displayModeDropdown != null)
            {
                displayModeDropdown.onValueChanged.RemoveListener(ApplyDisplayMode);
            }
        }

        private void ShowCurrentResolution()
        {
            var current = new Vector2Int(Screen.width, Screen.height);
            Vector2Int largest = GetLargestMonitorSize(current);

            // A short list of common sizes that fit the monitor, instead of every mode it reports. The monitor's own
            // size and the current one are always included, e.g. for ultrawide monitors or a resized window.
            resolutions.Clear();
            resolutions.AddRange(CommonResolutions
                .Where(size => size.x <= largest.x && size.y <= largest.y)
                .Append(largest)
                .Append(current)
                .Distinct()
                .OrderBy(size => size.x)
                .ThenBy(size => size.y));

            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(resolutions.Select(size => $"{size.x} x {size.y}").ToList());
            resolutionDropdown.SetValueWithoutNotify(resolutions.IndexOf(current));
            resolutionDropdown.RefreshShownValue();
        }

        private static Vector2Int GetLargestMonitorSize(Vector2Int fallback)
        {
            Vector2Int largest = fallback;
            foreach (Resolution resolution in Screen.resolutions)
            {
                if (resolution.width * resolution.height > largest.x * largest.y)
                {
                    largest = new Vector2Int(resolution.width, resolution.height);
                }
            }

            return largest;
        }

        private void ShowCurrentDisplayMode()
        {
            displayModeDropdown.ClearOptions();
            displayModeDropdown.AddOptions(DisplayModes.Select(option => option.label).ToList());

            // Any fullscreen variant counts as Fullscreen, e.g. if the build started in exclusive mode.
            int index = Screen.fullScreenMode == FullScreenMode.Windowed ? 1 : 0;
            displayModeDropdown.SetValueWithoutNotify(index);
            displayModeDropdown.RefreshShownValue();
        }

        private void ApplyResolution(int index)
        {
            Vector2Int size = resolutions[index];
            Screen.SetResolution(size.x, size.y, Screen.fullScreenMode);
        }

        private void ApplyDisplayMode(int index)
        {
            Screen.fullScreenMode = DisplayModes[index].mode;
        }
    }
}
