using System;
using TreeGame.Core;
using TreeGame.Interaction;
using TreeGame.Items;
using UnityEngine;

namespace TreeGame.World
{
    public enum CompostState
    {
        /// <summary>Empty, waiting for leaves.</summary>
        Idle,

        /// <summary>Turning dropped-in leaves into compost; nothing can be done until it finishes.</summary>
        Working,

        /// <summary>Compost is ready to be collected into a compost bucket.</summary>
        Done,
    }

    /// <summary>
    /// The compost heap: the player drops in all their leaves, they compost for a while (time and yield in
    /// <see cref="GameConfig"/>), then the compost can be collected into the player's compost bucket.
    /// Collecting more than the bucket can hold leaves the rest in the heap. Drives the heap's
    /// <see cref="Interactable"/> prompt for each state; connects interaction and inventory at this level.
    /// </summary>
    public class Composter : MonoBehaviour, IAdvancesWithTime
    {
        [SerializeField] private Interactable interactable;
        [SerializeField] private GameConfig config;
        [Tooltip("The item that can be dropped in, e.g. Brown Leaves.")]
        [SerializeField] private ItemDefinition compostableItem;

        [Header("Prompts")]
        [SerializeField] private string dropInAction = "drop leaves";
        [SerializeField] private string workingStatus = "Composting...";
        [SerializeField] private string collectAction = "collect";

        private float workSeconds;
        private float workDuration;
        private float compostGrams;

        /// <summary>Raised with the new state whenever it changes, e.g. so visuals can react.</summary>
        public event Action<CompostState> StateChanged;

        public CompostState State { get; private set; }

        /// <summary>How long the current batch has been working, in seconds.</summary>
        public float WorkSeconds => workSeconds;

        /// <summary>How long the current batch takes in total, in seconds. Kept per batch, so changing the config doesn't affect one already started.</summary>
        public float WorkDuration => workDuration;

        /// <summary>The compost in the heap, in grams, including any still being made.</summary>
        public float CompostGrams => compostGrams;

        /// <summary>How far the current batch is, 0..1: rises while working, 1 when done, 0 when idle.</summary>
        public float Progress
        {
            get
            {
                switch (State)
                {
                    case CompostState.Working:
                        return workDuration > 0f ? Mathf.Clamp01(workSeconds / workDuration) : 1f;
                    case CompostState.Done:
                        return 1f;
                    default:
                        return 0f;
                }
            }
        }

        private void OnEnable()
        {
            if (interactable == null || config == null || compostableItem == null)
            {
                Debug.LogWarning($"{nameof(Composter)}: needs an {nameof(Interactable)}, a {nameof(GameConfig)} and a compostable item.", this);
                enabled = false;
                return;
            }

            interactable.Interacted += OnInteracted;
            UpdatePrompt();
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.Interacted -= OnInteracted;
            }
        }

        private void Update()
        {
            // Scaled time, so pausing via Time.timeScale also pauses composting.
            Advance(Time.deltaTime);
        }

        public void Advance(float seconds)
        {
            if (State != CompostState.Working)
            {
                return;
            }

            workSeconds += seconds;
            if (workSeconds >= workDuration)
            {
                SetState(CompostState.Done);
            }
        }

        /// <summary>
        /// Puts the heap back in a given state, as when loading. A state that couldn't happen, such as compost with
        /// invalid amounts or none at all, leaves the heap idle and empty.
        /// </summary>
        public void RestoreState(CompostState state, float savedWorkSeconds, float savedWorkDuration, float savedCompostGrams)
        {
            bool isUsable = (state == CompostState.Working || state == CompostState.Done) &&
                            float.IsFinite(savedWorkSeconds) && float.IsFinite(savedWorkDuration) &&
                            float.IsFinite(savedCompostGrams) && savedCompostGrams > 0f;

            workSeconds = isUsable ? Mathf.Max(0f, savedWorkSeconds) : 0f;
            workDuration = isUsable ? Mathf.Max(0f, savedWorkDuration) : 0f;
            compostGrams = isUsable ? savedCompostGrams : 0f;
            SetState(isUsable ? state : CompostState.Idle);
        }

        private void OnInteracted(GameObject user)
        {
            if (!user.TryGetComponent(out Inventory inventory))
            {
                return;
            }

            switch (State)
            {
                case CompostState.Idle:
                    DropIn(inventory);
                    break;
                case CompostState.Done:
                    CollectInto(inventory);
                    break;
            }
        }

        private void DropIn(Inventory inventory)
        {
            // Without leaves there is nothing to compost; stay idle.
            int leaves = inventory.RemoveAll(compostableItem);
            if (leaves == 0)
            {
                return;
            }

            compostGrams = leaves * config.CompostGramsPerLeaf;
            workSeconds = 0f;
            workDuration = config.SecondsToCompost;
            SetState(CompostState.Working);
        }

        private void CollectInto(Inventory inventory)
        {
            CompostBucket bucket = inventory.FindFirst<CompostBucket>(candidate => !candidate.IsFull);
            if (bucket == null)
            {
                return;
            }

            float collected = Mathf.Min(compostGrams, bucket.Capacity - bucket.StoredAmount);
            bucket.AddAmount(collected);
            compostGrams -= collected;

            // Anything the bucket couldn't take stays ready for the next collect.
            if (compostGrams <= 0f)
            {
                compostGrams = 0f;
                SetState(CompostState.Idle);
            }
        }

        private void SetState(CompostState state)
        {
            State = state;
            UpdatePrompt();
            StateChanged?.Invoke(State);
        }

        private void UpdatePrompt()
        {
            if (State == CompostState.Working)
            {
                interactable.SetStatusText(workingStatus);
                return;
            }

            interactable.SetStatusText(null);
            interactable.SetActionDescription(State == CompostState.Done ? collectAction : dropInAction);
        }
    }
}
