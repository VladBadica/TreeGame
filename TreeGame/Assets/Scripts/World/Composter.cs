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
    public class Composter : MonoBehaviour
    {
        [SerializeField] private Interactable interactable;
        [SerializeField] private GameConfig config;
        [Tooltip("The item that can be dropped in, e.g. Brown Leaves.")]
        [SerializeField] private ItemDefinition compostableItem;

        [Header("Prompts")]
        [SerializeField] private string dropInAction = "drop leaves";
        [SerializeField] private string workingStatus = "Composting...";
        [SerializeField] private string collectAction = "collect";

        private float workStartTime;
        private float readyTime;
        private float compostGrams;

        /// <summary>Raised with the new state whenever it changes, e.g. so visuals can react.</summary>
        public event Action<CompostState> StateChanged;

        public CompostState State { get; private set; }

        /// <summary>How far the current batch is, 0..1: rises while working, 1 when done, 0 when idle.</summary>
        public float Progress
        {
            get
            {
                switch (State)
                {
                    case CompostState.Working:
                        float duration = readyTime - workStartTime;
                        return duration > 0f ? Mathf.Clamp01((Time.time - workStartTime) / duration) : 1f;
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
            if (State == CompostState.Working && Time.time >= readyTime)
            {
                SetState(CompostState.Done);
            }
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
            workStartTime = Time.time;
            readyTime = workStartTime + config.SecondsToCompost;
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
