using System;
using TMPro;
using TreeGame.Saving;
using TreeGame.Trees;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TreeGame.Menus
{
    /// <summary>
    /// The main menu's save slots: a card per slot, the chosen slot's tree shown live, and what can be done with it —
    /// Continue and Delete for a slot with a tree, New Tree for an empty one. Choosing a slot also makes it the slot the
    /// game saves to and loads from. The menu opens on the slot played most recently.
    /// Deleting asks first, on a screen of its own.
    /// </summary>
    public class SaveSlotMenu : MonoBehaviour
    {
        [SerializeField] private SaveSlotCard[] cards = new SaveSlotCard[0];
        [SerializeField] private MenuTreePreview preview;

        [Header("Actions for the chosen slot")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button newTreeButton;

        [Header("Deleting")]
        [SerializeField] private MenuPanels panels;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject deletePanel;
        [Tooltip("Asks which tree is about to be deleted.")]
        [SerializeField] private TMP_Text deleteQuestion;
        [SerializeField] private Button confirmDeleteButton;

        private enum SlotState
        {
            Empty,
            HasTree,
            Unreadable,
        }

        private readonly SlotState[] states = new SlotState[SaveSlots.Count];
        private readonly string[] titles = new string[SaveSlots.Count];
        private UnityAction[] cardHandlers = new UnityAction[0];
        private int chosen;

        private bool HasWhatItNeeds =>
            cards.Length == SaveSlots.Count && Array.TrueForAll(cards, card => card != null) && preview != null &&
            continueButton != null && deleteButton != null && newTreeButton != null && panels != null &&
            mainPanel != null && deletePanel != null && deleteQuestion != null && confirmDeleteButton != null;

        private void OnEnable()
        {
            if (!HasWhatItNeeds)
            {
                Debug.LogWarning($"{nameof(SaveSlotMenu)}: needs a card per slot, the tree preview, the slot actions and the delete screen.", this);
                enabled = false;
                return;
            }

            cardHandlers = new UnityAction[cards.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                int slot = i;
                cardHandlers[i] = () => Choose(slot);
                cards[i].Button.onClick.AddListener(cardHandlers[i]);
            }

            deleteButton.onClick.AddListener(AskToDelete);
            confirmDeleteButton.onClick.AddListener(DeleteChosen);
        }

        private void OnDisable()
        {
            for (int i = 0; i < cardHandlers.Length && i < cards.Length; i++)
            {
                if (cards[i] != null)
                {
                    cards[i].Button.onClick.RemoveListener(cardHandlers[i]);
                }
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveListener(AskToDelete);
            }

            if (confirmDeleteButton != null)
            {
                confirmDeleteButton.onClick.RemoveListener(DeleteChosen);
            }
        }

        private void Start()
        {
            if (!enabled)
            {
                return;
            }

            RefreshCards(out int mostRecent);
            Choose(mostRecent);
        }

        /// <summary>Reads every slot and shows it on its card. Gives the slot played most recently, or the first if none.</summary>
        private void RefreshCards(out int mostRecent)
        {
            mostRecent = 0;
            DateTime latest = DateTime.MinValue;
            SaveSlots slots = SaveSlots.ForThisGame();

            for (int i = 0; i < SaveSlots.Count; i++)
            {
                string title = SlotName(i);
                SaveFile file = slots.Slot(i);
                if (file.TryRead(out SaveData data))
                {
                    states[i] = SlotState.HasTree;
                    title = TreeNames.Display(data.treeName, title);
                    titles[i] = title;
                    string played = data.TryGetSavedAt(out DateTime savedUtc)
                        ? SlotText.LastPlayed(savedUtc.ToLocalTime(), DateTime.Now)
                        : "Last played: unknown";
                    cards[i].Show(title, $"{SlotText.Growth(data.trunkGrowth, data.branchGrowth, data.foliageGrowth)} · {played}");

                    if (savedUtc > latest)
                    {
                        latest = savedUtc;
                        mostRecent = i;
                    }
                }
                else if (file.Exists)
                {
                    titles[i] = title;
                    // Damaged, or from a newer version of the game: it can't be played, but can be deleted to free the slot.
                    states[i] = SlotState.Unreadable;
                    cards[i].Show(title, "This save can't be read");
                }
                else
                {
                    titles[i] = title;
                    states[i] = SlotState.Empty;
                    cards[i].Show(title, "Empty: plant a new tree");
                }
            }
        }

        /// <summary>Makes a slot the chosen one: highlights its card, shows its tree and what can be done with it.</summary>
        private void Choose(int slot)
        {
            chosen = Mathf.Clamp(slot, 0, SaveSlots.Count - 1);
            CurrentSaveSlot.Choose(chosen);

            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].SetChosen(i == chosen);
            }

            preview.ShowSlot(chosen);

            SlotState state = states[chosen];
            continueButton.gameObject.SetActive(state == SlotState.HasTree);
            deleteButton.gameObject.SetActive(state != SlotState.Empty);
            newTreeButton.gameObject.SetActive(state == SlotState.Empty);
        }

        /// <summary>A slot's own name, used for a tree the player didn't name.</summary>
        private static string SlotName(int slot)
        {
            return $"Tree {slot + 1}";
        }

        private void AskToDelete()
        {
            deleteQuestion.text = $"Delete {titles[chosen]}? It can't be played again once it is gone.";
            panels.Show(deletePanel);
        }

        private void DeleteChosen()
        {
            SaveSlots.ForThisGame().Slot(chosen).TryDelete();
            RefreshCards(out _);
            panels.Show(mainPanel);
            Choose(chosen);
        }
    }
}
