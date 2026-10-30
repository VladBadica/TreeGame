using UnityEngine.InputSystem;

namespace TreeGame.UI
{
    /// <summary>Turns input actions into text for prompts, so prompts always name the currently bound key.</summary>
    public static class BindingText
    {
        /// <summary>The key bound to an action for one control scheme (e.g. "F"), or the fallback if there is none.</summary>
        public static string KeyName(InputAction action, string bindingGroup, string fallback)
        {
            string key = action != null ? action.GetBindingDisplayString(InputBinding.MaskByGroup(bindingGroup)) : "";
            return string.IsNullOrEmpty(key) ? fallback : key;
        }

        /// <summary>The standard prompt wording, with the key in bold: "Press <b>F</b> to get water".</summary>
        public static string PressToPrompt(string key, string actionDescription)
        {
            return $"Press <b>{key}</b> to {actionDescription}";
        }
    }
}
