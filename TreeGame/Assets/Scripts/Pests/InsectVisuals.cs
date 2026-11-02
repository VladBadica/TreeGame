using System;
using TreeGame.Core;
using TreeGame.Interaction;
using TreeGame.Trees;
using UnityEngine;

namespace TreeGame.Pests
{
    /// <summary>
    /// Shows the insects on the trunk. Their spots come from a fixed seed and fill up in order, so the same number of
    /// insects always looks the same. Spots are relative to the trunk, so insects stay on it as it grows and leans.
    /// Can be replaced by anything else listening to <see cref="InsectInfestation.CountChanged"/>. It only reports
    /// clicks on insects; what they lead to is decided elsewhere.
    /// </summary>
    public class InsectVisuals : MonoBehaviour
    {
        // Stepping by this fraction of the range and wrapping around keeps every new spot well away from earlier ones.
        private const float SpotSpacing = 0.618f;

        [SerializeField] private InsectInfestation infestation;
        [SerializeField] private TreeVisuals tree;
        [SerializeField] private GameConfig config;
        [Tooltip("An inactive object with a Clickable, copied once per possible insect at startup.")]
        [SerializeField] private Transform insectTemplate;
        [Tooltip("Same seed = same spots.")]
        [SerializeField] private int seed = 4242;
        [Tooltip("Where insects sit, relative to trunk height (lowest, highest). Below the branches, so leaves don't hide them.")]
        [SerializeField] private Vector2 heightOnTrunk = new Vector2(0.2f, 0.75f);
        [Tooltip("How far from the trunk's middle insects sit: 0 = on the middle, 1 = up to its edges.")]
        [SerializeField, Range(0f, 1f)] private float spreadAcrossTrunk = 0.7f;
        [Tooltip("Most degrees an insect may be turned away from facing straight up.")]
        [SerializeField, Range(0f, 180f)] private float maxTurn = 35f;
        [Tooltip("How far in front of the tree's center insects are drawn. Must be more than the trunk's radius, or it hides them.")]
        [SerializeField, Min(0f)] private float distanceInFront = 0.3f;

        private sealed class Spot
        {
            public Transform Insect;
            public float Height;
            public float Side;
        }

        private Spot[] spots;
        private int visibleCount;

        /// <summary>Raised when the player clicks any insect.</summary>
        public event Action InsectClicked;

        private void OnEnable()
        {
            if (infestation == null || tree == null || config == null || insectTemplate == null)
            {
                Debug.LogWarning($"{nameof(InsectVisuals)}: needs an infestation, tree visuals, a config and an insect template.", this);
                enabled = false;
                return;
            }

            infestation.CountChanged += ShowInsects;
            ShowInsects(infestation.Count);
        }

        private void OnDisable()
        {
            if (infestation != null)
            {
                infestation.CountChanged -= ShowInsects;
            }
        }

        // After Update, so insects follow this frame's growth. Does nothing while there are none to place.
        private void LateUpdate()
        {
            float depth = tree.transform.position.z - distanceInFront;
            for (int i = 0; i < visibleCount; i++)
            {
                Vector3 position = tree.GetTrunkPoint(spots[i].Height, spots[i].Side);
                position.z = depth;
                spots[i].Insect.position = position;
            }
        }

        private void ShowInsects(int count)
        {
            CreateSpotsIfNeeded();

            visibleCount = Mathf.Min(count, spots.Length);
            for (int i = 0; i < spots.Length; i++)
            {
                spots[i].Insect.gameObject.SetActive(i < visibleCount);
            }
        }

        private void CreateSpotsIfNeeded()
        {
            if (spots != null)
            {
                return;
            }

            // A private, seeded generator keeps the spots independent of UnityEngine.Random and of call order elsewhere.
            var random = new System.Random(seed);

            spots = new Spot[config.Insects.MaxOnTree];
            for (int i = 0; i < spots.Length; i++)
            {
                Transform insect = Instantiate(insectTemplate, transform);
                insect.name = $"Insect {i}";
                insect.rotation = Quaternion.Euler(0f, 0f, random.NextFloat(-maxTurn, maxTurn));
                insect.GetComponent<Clickable>().Clicked += () => InsectClicked?.Invoke();

                spots[i] = new Spot
                {
                    Insect = insect,
                    Height = Mathf.Lerp(heightOnTrunk.x, heightOnTrunk.y, Mathf.Repeat((i + 1) * SpotSpacing, 1f)),
                    Side = random.NextFloat(-spreadAcrossTrunk, spreadAcrossTrunk),
                };
            }
        }
    }
}
