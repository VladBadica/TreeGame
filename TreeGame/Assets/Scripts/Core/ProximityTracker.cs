using System.Collections.Generic;
using UnityEngine;

namespace TreeGame.Core
{
    /// <summary>
    /// Keeps track of components of type <typeparamref name="T"/> whose trigger zones its owner is standing in.
    /// The owner forwards its OnTriggerEnter/OnTriggerExit calls here. Destroyed entries are dropped automatically
    /// (Unity sends no exit event for those) and disabled ones are skipped.
    /// </summary>
    public class ProximityTracker<T> where T : Behaviour
    {
        private readonly List<T> inRange = new List<T>();
        private readonly List<T> active = new List<T>();

        public void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out T component) && !inRange.Contains(component))
            {
                inRange.Add(component);
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out T component))
            {
                inRange.Remove(component);
            }
        }

        /// <summary>
        /// Everything currently in range and enabled. The list is reused between calls, so don't keep it;
        /// it is safe to destroy the objects while iterating it.
        /// </summary>
        public IReadOnlyList<T> GetActive()
        {
            active.Clear();
            for (int i = inRange.Count - 1; i >= 0; i--)
            {
                T candidate = inRange[i];
                if (candidate == null)
                {
                    inRange.RemoveAt(i);
                }
                else if (candidate.isActiveAndEnabled)
                {
                    active.Add(candidate);
                }
            }

            return active;
        }

        /// <summary>The active entry closest along X (side-on game: only horizontal distance matters), or null.</summary>
        public T FindNearest(float x)
        {
            T nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (T candidate in GetActive())
            {
                float distance = Mathf.Abs(candidate.transform.position.x - x);
                if (distance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }
    }
}
