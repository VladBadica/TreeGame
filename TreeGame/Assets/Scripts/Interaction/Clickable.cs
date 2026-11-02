using System;
using UnityEngine;

namespace TreeGame.Interaction
{
    /// <summary>
    /// Something in the world that reacts to being clicked wherever the player stands, unlike an
    /// <see cref="Interactable"/>, which needs them next to it. It needs a collider on the same object.
    /// </summary>
    public class Clickable : MonoBehaviour
    {
        /// <summary>Raised when the player clicks this.</summary>
        public event Action Clicked;

        public void Click()
        {
            Clicked?.Invoke();
        }
    }
}
