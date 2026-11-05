using System;
using UnityEngine;

namespace TreeGame.Trees
{
    /// <summary>
    /// Owns the seed this game's tree grows from: the same seed always grows the same tree. Each new game gets a seed of
    /// its own; a loaded game sets the one it was saved with. It knows nothing about visuals or saving; the visuals listen
    /// for changes and the save reads and sets it through its public API.
    /// </summary>
    public class TreeSeed : MonoBehaviour
    {
        /// <summary>The seed used when a game doesn't get a random one, and the one every tree grew from before.</summary>
        public const int DefaultSeed = 12345;

        /// <summary>
        /// The version of the rules that grow a tree from a seed. Raise it whenever a change to those rules (branch
        /// counts, lengths, leaf layout...) would make a seed grow a different tree, so codes from before can be told
        /// apart. A code holds up to <see cref="SeedCode.MaxVersion"/>.
        /// </summary>
        public const int GenerationVersion = 1;

        [Tooltip("Gives every new game a random seed, unless a tree code was entered. Off: every new game without a code " +
                 "grows the tree from the seed below.")]
        [SerializeField] private bool randomSeedEachGame = true;
        [SerializeField] private int seed = DefaultSeed;

        private bool chosen;

        /// <summary>Raised with the new seed whenever the seed changes.</summary>
        public event Action<int> SeedChanged;

        /// <summary>
        /// The seed. A new game's seed is chosen on first asking, so it is ready whichever component starts first: the
        /// one asked for before the game started (see <see cref="PendingSeed"/>), otherwise a random one.
        /// </summary>
        public int Seed
        {
            get
            {
                if (!chosen)
                {
                    chosen = true;
                    if (PendingSeed.TryTake(out int requested))
                    {
                        seed = requested;
                    }
                    else if (randomSeedEachGame)
                    {
                        seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                    }
                }

                return seed;
            }
        }

        /// <summary>The seed as a code people can read, share and type back in.</summary>
        public string Code => SeedCode.Encode(Seed, GenerationVersion);

        /// <summary>Grows the tree from this seed instead, e.g. the one a save was made with.</summary>
        public void SetSeed(int value)
        {
            int current = Seed;
            if (value == current)
            {
                return;
            }

            seed = value;
            SeedChanged?.Invoke(seed);
        }
    }
}
