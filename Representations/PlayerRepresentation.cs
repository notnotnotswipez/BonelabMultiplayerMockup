using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Nodes;
using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Packets;
using BonelabMultiplayerMockup.Packets.Player;
using BonelabMultiplayerMockup.Patches;
using BonelabMultiplayerMockup.Utils;
using BoneLib;
using Discord;
using HarmonyLib;
using MelonLoader;
using PuppetMasta;
using SLZ.AI;
using SLZ.Combat;
using SLZ.Data;
using SLZ.Interaction;
using SLZ.Marrow.Data;
using SLZ.Rig;
using SLZ.SFX;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using Unity.Barracuda;
using UnityEngine;
using Avatar = SLZ.VRMK.Avatar;

namespace BonelabMultiplayerMockup.Representations
{
    public class PlayerRepresentation
    {
        public static Dictionary<long, PlayerRepresentation> representations =
            new Dictionary<long, PlayerRepresentation>();
        
        public Dictionary<byte, GameObject> boneDictionary = new Dictionary<byte, GameObject>();
        public Dictionary<byte, GameObject> colliderDictionary = new Dictionary<byte, GameObject>();

        private byte currentBoneId;
        private byte currentColliderId = 0;
        public GameObject playerRep;
        public GameObject colliders;
        public User user;
        public string username;
        public string currentBarcode = "";

        private static HandPose softGrab;
        private static List<AudioClip> sounds = new List<AudioClip>();
        private static ImpactProperties _impactProperties;

        public PlayerRepresentation(User user)
        {
            this.user = user;
            username = user.Username;
            var avatarAskData = new AvatarQuestionData()
            {
                // yep
            };
            var catchupBuff = PacketHandler.CompressMessage(NetworkMessageType.AvatarQuestionPacket, avatarAskData);
            Node.activeNode.SendMessage(this.user.Id, (byte)NetworkChannel.Transaction, catchupBuff.getBytes());
        }
        
        public void SetAvatar(string barcode)
        {
            if (currentBarcode == barcode)
            {
                if (playerRep != null)
                {
                    if (playerRep.activeInHierarchy)
                    {
                        return;
                    }
                }
            }

            currentBarcode = barcode;
            currentBoneId = 0;
            currentColliderId = 0;
            foreach (var colliderObject in colliderDictionary.Values) {
                GameObject.Destroy(colliderObject);
            }

            boneDictionary.Clear();
            colliderDictionary.Clear();
            if (playerRep != null)
            {
                UnityEngine.Object.Destroy(colliders);
                UnityEngine.Object.Destroy(playerRep);
            }

            AssetsManager.LoadAvatar(barcode, FinalizeAvatar);
        }

        private IEnumerator FinalizeColliders(string originalBarcode)
        {
            RigManager rigManager = Player.GetRigManager().GetComponent<RigManager>();
            PatchVariables.shouldIgnoreAvatarSwitch = true;
            rigManager.SwitchAvatar(GameObject.Instantiate(playerRep).GetComponent<Avatar>());
            yield return new WaitForSecondsRealtime(1f);
            PopulateColliderDictionary();
            AssetsManager.LoadAvatar(originalBarcode, o =>
            {
                GameObject spawned = GameObject.Instantiate(o);
                Avatar avatar = spawned.GetComponent<Avatar>();
                foreach (var skinnedMesh in avatar.headMeshes)
                {
                    skinnedMesh.enabled = false;
                }

                spawned.transform.parent = rigManager.gameObject.transform;
                rigManager.SwitchAvatar(avatar);
            });
            yield return new WaitForSecondsRealtime(3f);
            PatchVariables.shouldIgnoreAvatarSwitch = false;
            BonelabMultiplayerMockup.PopulateCurrentAvatarData();
        }

        private void FinalizeAvatar(GameObject go)
        {
            string original = Player.GetRigManager().GetComponentInChildren<RigManager>()._avatarCrate._barcode._id;
            if (playerRep != null)
            {
                GameObject.Destroy(colliders);
                GameObject.Destroy(playerRep);
                GameObject backupCopy = GameObject.Instantiate(go);
                backupCopy.name = "(PlayerRep) " + username;
                playerRep = backupCopy;
                Avatar avatarAgain = backupCopy.GetComponentInChildren<Avatar>();
                PopulateBoneDictionary(avatarAgain.gameObject.transform);
                GameObject.DontDestroyOnLoad(playerRep);
                MelonCoroutines.Start(FinalizeColliders(original));
                return;
            }

            GameObject copy = GameObject.Instantiate(go);
            copy.name = "(PlayerRep) " + username;
            playerRep = copy;
            Avatar avatar = copy.GetComponentInChildren<Avatar>();
            PopulateBoneDictionary(avatar.gameObject.transform);
            GameObject.DontDestroyOnLoad(copy);
            MelonCoroutines.Start(FinalizeColliders(original));
        }
        
        

        private void PopulateBoneDictionary(Transform parent)
        {
            var childCount = parent.childCount;

            for (var i = 0; i < childCount; i++)
            {
                var child = parent.GetChild(i).gameObject;
                boneDictionary.Add(currentBoneId++, child);

                if (child.transform.childCount > 0) PopulateBoneDictionary(child.transform);
            }
        }

        private void AddCorrectProperties(GameObject gameObject, Collider collider, GenericGrip genericGripOriginal)
        {
            gameObject.layer = LayerMask.NameToLayer("Interactable");
            
            ImpactProperties impactProperties = gameObject.AddComponent<ImpactProperties>();
            impactProperties.surfaceData = _impactProperties.surfaceData;
            impactProperties.DecalMeshObj = _impactProperties.DecalMeshObj;
            impactProperties.decalType = _impactProperties.decalType;
            

            ImpactSFX sfx = gameObject.AddComponent<ImpactSFX>();
            sfx.impactSoft = sounds.ToArray();
            sfx.impactHard = sounds.ToArray();
            sfx.pitchMod = 1;
            sfx.bluntDamageMult = 1;
            sfx.minVelocity = 0.4f;
            sfx.velocityClipSplit = 4;
            sfx.jointBreakVolume = 1;
            
            InteractableHost interactableHost = gameObject.AddComponent<InteractableHost>();
            interactableHost.HasRigidbody = true;

            GenericGrip genericGrip = gameObject.AddComponent<GenericGrip>();
            genericGrip.handPose = softGrab;
            genericGrip.primaryMovementAxis = new Vector3(0, 0, 1);
            genericGrip.secondaryMovementAxis = new Vector3(0, 1, 0);
            genericGrip.gripOptions = InteractionOptions.MultipleHands;
            genericGrip.priority = 1;
            genericGrip.minBreakForce = 5000;
            genericGrip.maxBreakForce = 10000;
            genericGrip._handJointConfig = genericGripOriginal._handJointConfig;
            genericGrip.defaultGripDistance = Single.PositiveInfinity;
            genericGrip.radius = 1;
            genericGrip.handleAmplifyCurve = AnimationCurve.Linear(0, 0, 1, 0);
        }

        private void HandleColliderObject(GameObject gameObject, Collider collider, GenericGrip genericGripOriginal, InteractableHostManager manager)
        {
            if (softGrab == null)
            {
                HandPose[] poses = Resources.FindObjectsOfTypeAll<HandPose>();
                foreach (var p in poses)
                {
                    if (p.name == "SoftGrab")
                        softGrab = p;
                } 
            }

            if (sounds.Count == 0)
            {
                AudioClip[] clips = Resources.FindObjectsOfTypeAll<AudioClip>();
                sounds = new List<AudioClip>();
                foreach (var clip in clips)
                    if (clip.name.Contains("ImpactSoft_SwordBroad"))
                        sounds.Add(clip);
            }

            if (_impactProperties == null)
            {
                PoolManager.SpawnGameObject("c1534c5a-3fd8-4d50-9eaf-0695466f7264", Vector3.zero, Quaternion.identity,
                    o =>
                    { 
                        _impactProperties = GameObject.Instantiate(PoolManager.GetComponentOnObject<ImpactProperties>(o));
                        GameObject.Destroy(o);
                        AddCorrectProperties(gameObject, collider, genericGripOriginal);
                    });
                return;
            }

            AddCorrectProperties(gameObject, collider, genericGripOriginal);
        }

        private void PopulateColliderDictionary()
        {
            if(colliders != null)
            {
                GameObject.Destroy(colliders);
            }

            GameObject colliderParent = new GameObject("allColliders");
            InteractableHostManager manager = colliderParent.AddComponent<InteractableHostManager>();
            colliders = colliderParent;

            GenericGrip genericGrip = null;
            bool addedAiTarget = false;

            foreach (var collider in Player.GetPhysicsRig().GetComponentsInChildren<MeshCollider>())
            {
                if (collider.isTrigger)
                {
                    continue;
                }

                if (collider.GetComponentInParent<SlotContainer>())
                {
                    continue;
                }

                if (genericGrip == null)
                {
                    genericGrip = PoolManager.GetComponentOnObject<GenericGrip>(collider.gameObject);
                }

                GameObject gameObject = new GameObject("collider" + currentColliderId);
                MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.convex = collider.convex;
                meshCollider.sharedMesh = GameObject.Instantiate(collider.sharedMesh);
                meshCollider.inflateMesh = collider.inflateMesh;
                meshCollider.smoothSphereCollisions = collider.smoothSphereCollisions;
                meshCollider.skinWidth = collider.skinWidth;
                gameObject.transform.parent = colliderParent.transform;

                if (!addedAiTarget)
                {
                    AITarget aiTarget = gameObject.AddComponent<AITarget>();
                    aiTarget.type = TriggerManager.TargetTypes.Sphere;
                    aiTarget.radius = 0.1f;
                    aiTarget.tag = "Player";
                    addedAiTarget = true;
                }

                Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.isKinematic = true;
                
                HandleColliderObject(gameObject, meshCollider, genericGrip, manager);

                colliderDictionary.Add(currentColliderId++, gameObject);
            }

            foreach (var collider in Player.GetPhysicsRig().GetComponentsInChildren<BoxCollider>())
            {
                if (collider.isTrigger)
                {
                    continue;
                }
                
                if (collider.GetComponentInParent<SlotContainer>())
                {
                    continue;
                }

                GameObject gameObject = new GameObject("collider" + currentColliderId);
                BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.center = collider.center;
                boxCollider.size = collider.size;
                gameObject.transform.parent = colliderParent.transform;
                Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.isKinematic = true;
                
                HandleColliderObject(gameObject, boxCollider, genericGrip, manager);

                colliderDictionary.Add(currentColliderId++, gameObject);
            }

            foreach (var collider in Player.GetPhysicsRig().GetComponentsInChildren<CapsuleCollider>())
            {
                if (collider.isTrigger)
                {
                    continue;
                }
                
                if (collider.GetComponentInParent<SlotContainer>())
                {
                    continue;
                }

                GameObject gameObject = new GameObject("collider" + currentColliderId);
                
                CapsuleCollider capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
                capsuleCollider.center = collider.center;
                capsuleCollider.direction = collider.direction;
                capsuleCollider.height = collider.height;
                capsuleCollider.radius = collider.radius;
                gameObject.transform.parent = colliderParent.transform;
                Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.isKinematic = true;

                HandleColliderObject(gameObject, capsuleCollider, genericGrip, manager);

                colliderDictionary.Add(currentColliderId++, gameObject);
            }

            foreach (InteractableHost interactableHost in colliders.GetComponentsInChildren<InteractableHost>())
            {
                manager.hosts.AddItem(interactableHost);
            }

            colliderParent.transform.parent = playerRep.transform;
        }

        public void updateIkTransform(byte boneId, CompressedTransform compressedTransform)
        {
            if (playerRep == null) return;
            
            if (!playerRep.activeSelf)
            {
                playerRep.SetActive(true);
            }

            if (boneDictionary.ContainsKey(boneId))
            {
                var selectedBone = boneDictionary[boneId];
                if (selectedBone != null)
                {
                    selectedBone.transform.position = compressedTransform.position;
                    selectedBone.transform.rotation = compressedTransform.rotation;
                }
            }
        }

        public void updateColliderTransform(byte colliderId, CompressedTransform compressedTransform)
        {
            if (playerRep == null) return;

            if (colliderDictionary.ContainsKey(colliderId))
            {
                var selectedBone = colliderDictionary[colliderId];
                if (selectedBone != null)
                {
                    selectedBone.transform.position = compressedTransform.position;
                    selectedBone.transform.eulerAngles = compressedTransform.rotation.eulerAngles;
                }
            }
        }

        public void DeleteRepresentation()
        {
            UnityEngine.Object.Destroy(playerRep);
            representations.Remove(user.Id);
        }
    }
}