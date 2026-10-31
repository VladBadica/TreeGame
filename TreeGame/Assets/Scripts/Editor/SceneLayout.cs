using UnityEngine;

namespace TreeGame.EditorTools
{
    /// <summary>Where things stand in the game scene. Several builders depend on these, so they live in one place.</summary>
    internal static class SceneLayout
    {
        // How far the player may walk to either side of the tree (which sits at the origin).
        // Ground and backdrop are sized from this so the world never ends on screen.
        public const float MaxDistanceFromTree = 8f;
        // Extra world beyond the walk limit, wider than half the camera view, so no edge is ever visible.
        public const float WorldEdgeMargin = 20f;
        // Space between the walk limit and a fence's inner edge, about half the character's width,
        // so the player stops just in front of the fence instead of inside it.
        public const float FenceClearance = 0.35f;

        // The well stands just inside the right fence, a little behind the player's walking line (level with the tree),
        // so the player walks past in front of it.
        public static readonly Vector3 WellPosition = new Vector3(MaxDistanceFromTree - 1.8f, 0f, 0.3f);

        // The compost heap sits just left of the tree, at the same depth as the well, clear of the trunk and lowest branches.
        public static readonly Vector3 CompostPosition = new Vector3(-2.5f, 0f, 0.3f);

        // The ground is a shallow strip seen almost side-on, like a stage: wide along X, shallow in depth (Z).
        // It is thick so its front face always fills the bottom of the screen.
        public const float GroundWidth = 2f * (MaxDistanceFromTree + WorldEdgeMargin);
        public const float GroundThickness = 4f;
        public const float GroundFrontZ = -3.5f;
        public const float GroundBackZ = 1.5f;

        // The player walks in front of the tree (negative Z is toward the camera) and starts just left of it.
        // The player's origin is at its feet.
        public static readonly Vector3 PlayerStartPosition = new Vector3(-1.5f, 0f, -2f);

        // Camera framing: centered on the player, nearly side-on. A small pitch still shows a sliver of ground top.
        public static readonly Vector3 CameraTarget = new Vector3(PlayerStartPosition.x, 2f, 0f);
        public const float CameraPitch = 5f;
        public const float CameraDistance = 20f;
        public const float CameraOrthographicSize = 3f;

        // Shared with the main menu, so going from one to the other feels like the same place.
        public static readonly Color SkyColor = new Color(0.78f, 0.88f, 0.95f);
    }
}
