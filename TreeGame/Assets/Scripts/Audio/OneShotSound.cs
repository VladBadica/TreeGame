using UnityEngine;

namespace TreeGame.Audio
{
    /// <summary>
    /// Base for components that play a short sound when something happens. Each one only says what it listens to; the
    /// sound itself, and stopping when something is missing, are handled here. Given several sounds, it plays a
    /// different one from last time on each play, so repeated sounds don't feel mechanical; with two, they alternate.
    /// Keeps sounds apart from the systems they react to, so those systems never need to know about audio.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public abstract class OneShotSound : MonoBehaviour
    {
        [Tooltip("One or more sounds to choose from.")]
        [SerializeField] private AudioClip[] sounds = new AudioClip[0];

        private AudioSource source;
        private int lastPlayed = -1;

        /// <summary>Subscribes to whatever plays the sound. Returns false, without subscribing, if something it needs is missing.</summary>
        protected abstract bool TryConnect();

        /// <summary>Undoes <see cref="TryConnect"/>. Called even if it failed, so it must cope with missing references.</summary>
        protected abstract void Disconnect();

        protected virtual void Awake()
        {
            source = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (sounds.Length == 0 || System.Array.IndexOf(sounds, null) >= 0 || !TryConnect())
            {
                Debug.LogWarning($"{GetType().Name}: is missing a sound or what it listens to.", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            Disconnect();
        }

        protected void Play()
        {
            lastPlayed = PickNext();
            source.PlayOneShot(sounds[lastPlayed]);
        }

        /// <summary>Any sound but the last one played: picks from the others, by skipping over the last one.</summary>
        private int PickNext()
        {
            if (sounds.Length == 1 || lastPlayed < 0)
            {
                return Random.Range(0, sounds.Length);
            }

            int next = Random.Range(0, sounds.Length - 1);
            return next >= lastPlayed ? next + 1 : next;
        }
    }
}
