using TreeGame.Core;
using TreeGame.Interaction;
using TreeGame.Items;
using TreeGame.Player;
using UnityEditor;
using UnityEngine;
using static TreeGame.EditorTools.BuilderHelpers;
using static TreeGame.EditorTools.SceneLayout;

namespace TreeGame.EditorTools
{
    /// <summary>Builds the player: the placeholder character and the components that control and animate it.</summary>
    internal static class PlayerBuilder
    {
        /// <summary>The animated pivots of the placeholder character.</summary>
        private readonly struct CharacterRig
        {
            public readonly Transform Body;
            public readonly Transform Head;
            public readonly Transform LeftLeg;
            public readonly Transform RightLeg;
            public readonly Transform LeftArm;
            public readonly Transform RightArm;

            public CharacterRig(Transform body, Transform head, Transform leftLeg, Transform rightLeg,
                Transform leftArm, Transform rightArm)
            {
                Body = body;
                Head = head;
                LeftLeg = leftLeg;
                RightLeg = rightLeg;
                LeftArm = leftArm;
                RightArm = rightArm;
            }
        }

        public static Transform CreatePlayer(GameConfig config, Transform tree)
        {
            var player = new GameObject("Player");
            player.transform.position = PlayerStartPosition;

            CharacterRig rig = CreateCharacterModel(player.transform);

            var walkAnimation = player.AddComponent<CharacterWalkAnimation>();
            SetReferences(walkAnimation,
                ("body", rig.Body),
                ("leftLeg", rig.LeftLeg),
                ("rightLeg", rig.RightLeg),
                ("leftArm", rig.LeftArm),
                ("rightArm", rig.RightArm));

            // The camera is assigned once it exists; see ConnectToCamera.
            SetReferences(player.AddComponent<CharacterHeadLook>(),
                ("pointAction", FindActionReference("UI", "Point")),
                ("head", rig.Head),
                ("walkAnimation", walkAnimation));

            // Trigger capsule + kinematic body: detects interaction zones without any physical collisions.
            var body = player.AddComponent<CapsuleCollider>();
            body.isTrigger = true;
            body.center = new Vector3(0f, 0.9f, 0f);
            body.height = 1.8f;
            body.radius = 0.3f;
            var rigidbody = player.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            SetReferences(player.AddComponent<Interactor>(), ("interactAction", FindActionReference("Player", "Interact")));

            var inventory = player.AddComponent<Inventory>();
            SetReferences(inventory, ("config", config));
            SetReferences(player.AddComponent<HotbarInput>(),
                ("hotbarAction", FindActionReference("Player", "Hotbar")),
                ("inventory", inventory));
            SetReferences(player.AddComponent<ItemCollector>(),
                ("collectAction", FindActionReference("Player", "Collect")),
                ("inventory", inventory));

            var movement = player.AddComponent<PlayerMovement>();
            SetReferences(movement,
                ("moveAction", FindActionReference("Player", "Move")),
                ("config", config),
                ("walkAreaCenter", tree));
            var serializedMovement = new SerializedObject(movement);
            serializedMovement.FindProperty("maxDistanceFromCenter").floatValue = MaxDistanceFromTree;
            serializedMovement.ApplyModifiedPropertiesWithoutUndo();

            return player.transform;
        }

        /// <summary>Wires up what needs the camera, which is created after the player because it follows the player.</summary>
        public static void ConnectToCamera(Transform player, Camera viewCamera)
        {
            SetReferences(player.GetComponent<CharacterHeadLook>(), ("viewCamera", viewCamera));
            SetReferences(player.gameObject.AddComponent<ClickToInteract>(),
                ("clickAction", FindActionReference("UI", "Click")),
                ("pointAction", FindActionReference("UI", "Point")),
                ("viewCamera", viewCamera),
                ("interactor", player.GetComponent<Interactor>()),
                ("collector", player.GetComponent<ItemCollector>()));
        }

        /// <summary>
        /// Placeholder gardener built from primitives, about 1.8 units tall with its feet at the origin.
        /// The front faces local -Z (toward the camera); the walk animation turns it toward the walking direction.
        /// Limbs and head hang from empty pivots at the joints (hips, shoulders, neck) so they rotate from there.
        /// </summary>
        private static CharacterRig CreateCharacterModel(Transform parent)
        {
            Material skin = GetOrCreateMaterial("Player Skin", new Color(0.96f, 0.8f, 0.66f));
            Material shirt = GetOrCreateMaterial("Player Shirt", new Color(0.86f, 0.46f, 0.36f));
            Material pants = GetOrCreateMaterial("Player Pants", new Color(0.33f, 0.38f, 0.52f));
            Material hat = GetOrCreateMaterial("Player Hat", new Color(0.88f, 0.75f, 0.45f));
            Material eyes = GetOrCreateMaterial("Player Eyes", new Color(0.12f, 0.1f, 0.1f));

            Transform body = new GameObject("Body").transform;
            body.SetParent(parent, false);

            // Unity's capsule and cylinder are 2 units tall at scale 1, so a Y scale of h/2 gives height h.
            Transform leftLeg = CreatePivot("Left Leg", body, new Vector3(-0.1f, 0.5f, 0f));
            Transform rightLeg = CreatePivot("Right Leg", body, new Vector3(0.1f, 0.5f, 0f));
            CreatePart(PrimitiveType.Capsule, "Leg", leftLeg, new Vector3(0f, -0.25f, 0f), new Vector3(0.16f, 0.25f, 0.16f), pants);
            CreatePart(PrimitiveType.Capsule, "Leg", rightLeg, new Vector3(0f, -0.25f, 0f), new Vector3(0.16f, 0.25f, 0.16f), pants);

            CreatePart(PrimitiveType.Capsule, "Torso", body, new Vector3(0f, 0.85f, 0f), new Vector3(0.42f, 0.38f, 0.34f), shirt);

            Transform leftArm = CreatePivot("Left Arm", body, new Vector3(-0.27f, 1.12f, 0f));
            Transform rightArm = CreatePivot("Right Arm", body, new Vector3(0.27f, 1.12f, 0f));
            CreatePart(PrimitiveType.Capsule, "Arm", leftArm, new Vector3(0f, -0.2f, 0f), new Vector3(0.12f, 0.2f, 0.12f), skin);
            CreatePart(PrimitiveType.Capsule, "Arm", rightArm, new Vector3(0f, -0.2f, 0f), new Vector3(0.12f, 0.2f, 0.12f), skin);

            // Eyes and hat are children of the neck pivot so they turn with the head.
            Transform head = CreatePivot("Head", body, new Vector3(0f, 1.28f, 0f));
            CreatePart(PrimitiveType.Sphere, "Skull", head, new Vector3(0f, 0.19f, 0f), Vector3.one * 0.42f, skin);
            CreatePart(PrimitiveType.Sphere, "Left Eye", head, new Vector3(-0.08f, 0.22f, -0.19f), Vector3.one * 0.06f, eyes);
            CreatePart(PrimitiveType.Sphere, "Right Eye", head, new Vector3(0.08f, 0.22f, -0.19f), Vector3.one * 0.06f, eyes);
            CreatePart(PrimitiveType.Cylinder, "Hat Brim", head, new Vector3(0f, 0.38f, 0f), new Vector3(0.56f, 0.015f, 0.56f), hat);
            CreatePart(PrimitiveType.Cylinder, "Hat Crown", head, new Vector3(0f, 0.46f, 0f), new Vector3(0.32f, 0.08f, 0.32f), hat);

            return new CharacterRig(body, head, leftLeg, rightLeg, leftArm, rightArm);
        }
    }
}
