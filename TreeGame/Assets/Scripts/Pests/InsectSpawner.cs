using TreeGame.Core;
using UnityEngine;

namespace TreeGame.Pests
{
    /// <summary>Brings insects to the tree at random intervals (settings in <see cref="GameConfig"/>).</summary>
    public class InsectSpawner : MonoBehaviour, IAdvancesWithTime
    {
        [SerializeField] private InsectInfestation infestation;
        [SerializeField] private GameConfig config;

        private float secondsUntilNextInsect;

        private void OnEnable()
        {
            if (infestation == null || config == null)
            {
                Debug.LogWarning($"{nameof(InsectSpawner)}: needs an {nameof(InsectInfestation)} and a {nameof(GameConfig)}.", this);
                enabled = false;
            }
        }

        private void Start()
        {
            secondsUntilNextInsect = config.Insects.RandomSecondsBetweenInsects();
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        public void Advance(float seconds)
        {
            secondsUntilNextInsect -= seconds;

            // A loop, because catching up after a long time can mean several insects are due at once.
            while (secondsUntilNextInsect <= 0f)
            {
                infestation.TryAddInsect();
                secondsUntilNextInsect += config.Insects.RandomSecondsBetweenInsects();
            }
        }
    }
}
