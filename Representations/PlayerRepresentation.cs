using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BonelabMultiplayerMockup.Extention;
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
using SLZ;
using SLZ.AI;
using SLZ.Combat;
using SLZ.Data;
using SLZ.Interaction;
using SLZ.Marrow.Data;
using SLZ.Rig;
using SLZ.SFX;
using Steamworks;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.XR;
using Avatar = SLZ.VRMK.Avatar;

namespace BonelabMultiplayerMockup.Representations
{
    public class PlayerRepresentation
    {
        public static Dictionary<ulong, PlayerRepresentation> representations =
            new Dictionary<ulong, PlayerRepresentation>();
        
        public Dictionary<byte, InterpolatedObject> boneDictionary = new Dictionary<byte, InterpolatedObject>();
        public Dictionary<byte, InterpolatedObject> colliderDictionary = new Dictionary<byte, InterpolatedObject>();

        private byte currentBoneId;
        private byte currentColliderId = 0;
        public GameObject playerRep;
        public GameObject colliders;
        public GameObject pelvis;
        public GameObject lHand;
        public GameObject rHand;
        public byte pelvisIndex = 0;
        public bool simulated = false;
        public Friend user;
        public string username;
        public string currentBarcode = "";

        public float avatarMass = 0;

        public FixedJoint rHandJoint;
        public FixedJoint lHandJoint;

        private static HandPose softGrab;
        private static ImpactProperties _impactProperties;

        public PlayerRepresentation(Friend user)
        {
            this.user = user;
            username = user.Name;
            MelonLogger.Msg("FULLY created player rep.");
        }
        
        public void SetAvatar(string barcode)
        {
            //BonelabMultiplayerMockup.pauseNetworkingThread = true;
            try
            {
                MelonLogger.Msg("Setting avatar for: "+username+" to: "+barcode);
                if (currentBarcode == barcode)
                {
                    if (playerRep != null)
                    {
                        if (playerRep.activeInHierarchy)
                        {
                            //BonelabMultiplayerMockup.pauseNetworkingThread = false;
                            return;
                        }
                    }
                }
                
                MelonLogger.Msg("Request was different than stored.");
                currentBarcode = barcode;
                currentBoneId = 0;
                currentColliderId = 0;
                foreach (var colliderObject in colliderDictionary.Values) {
                    GameObject.Destroy(colliderObject.go);
                }
                MelonLogger.Msg("Destroyed all colliders.");

                boneDictionary.Clear();
                colliderDictionary.Clear();
                if (playerRep != null)
                {
                    MelonLogger.Msg("Rep was not null. Destroying.");
                    UnityEngine.Object.Destroy(colliders);
                    UnityEngine.Object.Destroy(playerRep);
                    MelonLogger.Msg("Destroyed player rep and colliders.");
                }
                
                MelonLogger.Msg("Loading Avatar....");
                try
                {
                    AssetsManager.LoadAvatar(barcode, FinalizeAvatar);
                }
                catch (Exception e)
                {
                    throw;
                }
                
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private IEnumerator FinalizeColliders(string originalBarcode)
        {
            PatchVariables.shouldIgnoreAvatarSwitch = true;
            
            RigManager rigManager = Player.rigManager;
            MelonLogger.Msg("Switched avatar to prepare for collider storage.");
            rigManager.SwitchAvatar(GameObject.Instantiate(playerRep).GetComponent<Avatar>());
            MelonLogger.Msg("Finished switching avatar.");
            yield return new WaitForSecondsRealtime(1);
            try
            {
                MelonLogger.Msg("Populating collider dictionary.");
                PopulateColliderDictionary();
                MelonLogger.Msg("Finished populating collider dictionary");
                MelonLogger.Msg("Loading previous avatar...");
                AssetsManager.LoadAvatar(originalBarcode, o =>
                {
                    GameObject spawned = GameObject.Instantiate(o);
                    Avatar avatar = spawned.GetComponent<Avatar>();
                    foreach (var skinnedMesh in avatar.headMeshes)
                    {
                        skinnedMesh.enabled = false;
                    }
                    foreach (var skinnedMesh in avatar.hairMeshes)
                    {
                        skinnedMesh.enabled = false;
                    }

                    spawned.transform.parent = rigManager.gameObject.transform;
                    rigManager.SwitchAvatar(avatar);
                    MelonLogger.Msg("Previous avatar loaded...");
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            yield return new WaitForSecondsRealtime(1);
            PatchVariables.shouldIgnoreAvatarSwitch = false;
            
            yield return new WaitForSecondsRealtime(3);
            BonelabMultiplayerMockup.PopulateCurrentAvatarData();
            SendAvatarConfirmation();
            MelonLogger.Msg("Sent avatar confirmation data.");
        }

        private void FinalizeAvatar(GameObject go)
        {
            try
            {
                MelonLogger.Msg("Loaded avatar.");
                string original = Player.rigManager._avatarCrate._barcode._id;
                if (playerRep != null)
                {
                    MelonLogger.Msg("Player rep is not null.");
                    GameObject.Destroy(colliders);
                    GameObject.Destroy(playerRep);
                    GameObject backupCopy = GameObject.Instantiate(go);
                    backupCopy.name = "(PlayerRep) " + username;
                    playerRep = backupCopy;
                    Avatar avatarAgain = backupCopy.GetComponentInChildren<Avatar>();
                    MelonLogger.Msg("Destroyed previous player rep and made a new one.");
                    MelonLogger.Msg("Populating bone dictionary");
                    PopulateBoneDictionary(avatarAgain.gameObject.transform);
                    MelonLogger.Msg("Finished populating bone dictionary.");
                    GameObject.DontDestroyOnLoad(playerRep);
                    MelonLogger.Msg("Finalizing colliders.");
                    MelonCoroutines.Start(FinalizeColliders(original));
                    return;
                }

                GameObject copy = GameObject.Instantiate(go);
                copy.name = "(PlayerRep) " + username;
                playerRep = copy;
                Avatar avatar = copy.GetComponentInChildren<Avatar>();
                MelonLogger.Msg("Populating bone dictionary");
                PopulateBoneDictionary(avatar.gameObject.transform);
                MelonLogger.Msg("Finished populating bone dictionary.");
                GameObject.DontDestroyOnLoad(copy);
                MelonLogger.Msg("Finalizing colliders.");
                MelonCoroutines.Start(FinalizeColliders(original));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        // Performance...? Less unity iterations throughout the children cause we dont really need to iterate.
        public void ParentObjectsAndSuch(GameObject parent)
        {
            foreach (var bone in boneDictionary.Values)
            {
                bone.go.transform.parent = parent.transform;
            }
            
            playerRep = parent;
        }

        public void SendAvatarConfirmation()
        {
            AvatarConfirmationData avatarConfirmationData = new AvatarConfirmationData()
            {
                userId = SteamIntegration.currentId
            };

            PacketByteBuf packetByteBuf =
                PacketHandler.CompressMessage(NetworkMessageType.AvatarConfirmationPacket, avatarConfirmationData);
            
            SteamPacketNode.SendMessage(user.Id, NetworkChannel.Reliable, packetByteBuf.getBytes());
        }



        private void PopulateBoneDictionary(Transform parent)
        {
            var childCount = parent.childCount;

            for (var i = 0; i < childCount; i++)
            {
                var child = parent.GetChild(i).gameObject;
                if (currentBoneId == 254)
                {
                    if (!boneDictionary.ContainsKey(254))
                    {
                        boneDictionary.Add(currentBoneId, new InterpolatedObject(child));
                    }
                    return;
                }
                boneDictionary.Add(currentBoneId++, new InterpolatedObject(child));

                if (child.transform.childCount > 0) PopulateBoneDictionary(child.transform);
            }
        }

        public void GrabClientCollider(byte personalIndex, Handedness handedness)
        {
            GameObject physicsBone = null;

            foreach (var collider in BonelabMultiplayerMockup.colliderDictionary)
            {
                if (collider.Key == personalIndex)
                {
                    physicsBone = collider.Value;
                }
            }

            // The collider might be nested inside a physics bone, no point in moving a singular lose collider GO so we search upwards.
            while (physicsBone.transform.parent.gameObject != Player.GetPhysicsRig().gameObject)
            {
                physicsBone = physicsBone.transform.parent.gameObject;
            }

            if (physicsBone == null || Utils.Utils.IsSoftBody(physicsBone))
            {
                physicsBone = BonelabMultiplayerMockup.pelvis;
            }

            if (handedness == Handedness.LEFT)
            {
                lHandJoint = physicsBone.AddComponent<FixedJoint>();
                lHandJoint.connectedBody = lHand.GetComponent<Rigidbody>();
                lHandJoint.breakForce = Single.PositiveInfinity;
                lHandJoint.breakTorque = Single.PositiveInfinity;
            }

            if (handedness == Handedness.RIGHT)
            {
                rHandJoint = physicsBone.AddComponent<FixedJoint>();
                rHandJoint.connectedBody = rHand.GetComponent<Rigidbody>();
                rHandJoint.breakForce = Single.PositiveInfinity;
                rHandJoint.breakTorque = Single.PositiveInfinity;
            }
        }

        public void LetGoOfClientCollider(Handedness handedness)
        {
            if (handedness == Handedness.LEFT)
            {
                if (lHandJoint != null)
                {
                    GameObject.Destroy(lHandJoint);
                    lHandJoint = null;
                }
            }
            if (handedness == Handedness.RIGHT)
            {
                if (rHandJoint != null)
                {
                    GameObject.Destroy(rHandJoint);
                    rHandJoint = null;
                }
            }
        }

        public static void CacheThings()
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

            /*if (sounds.Count == 0)
            {
                AudioClip[] clips = Resources.FindObjectsOfTypeAll<AudioClip>();
                sounds = new List<AudioClip>();
                foreach (var clip in clips)
                    if (clip.name.Contains("ImpactSoft_SwordBroad"))
                        sounds.Add(clip);
            }*/

            if (_impactProperties == null)
            {
                PoolManager.SpawnGameObject("c1534c5a-3fd8-4d50-9eaf-0695466f7264", Vector3.zero, Quaternion.identity,
                    o =>
                    { 
                        _impactProperties = GameObject.Instantiate(PoolManager.GetComponentOnObject<ImpactProperties>(o));
                        GameObject.Destroy(o);
                    });
            }
        }

        public void GrabThisGuy(Handedness handedness, GameObject grabbedCollider)
        {
            simulated = true;
            if (handedness == Handedness.LEFT)
            {
                // This is a hilariously shitty workaround but connecting a fixed joint seems to make the pelvis fall
                pelvis.transform.parent = Player.leftHand.transform;
                HandVariables.lRepPelvisJoint = pelvis.AddComponent<FixedJoint>();
                //HandVariables.lRepPelvisJoint.connectedBody = Player.leftHand.GetComponentInChildren<Rigidbody>();
                HandVariables.lRepPelvisJoint.breakForce = Single.PositiveInfinity;
                HandVariables.lRepPelvisJoint.breakTorque = Single.PositiveInfinity;
                HandVariables.lGrabbedPlayerRep = this;
            }
            if (handedness == Handedness.RIGHT)
            {
                pelvis.transform.parent = Player.rightHand.transform;
                HandVariables.rRepPelvisJoint = pelvis.AddComponent<FixedJoint>();
                //HandVariables.rRepPelvisJoint.connectedBody = Player.rightHand.GetComponentInChildren<Rigidbody>();
                HandVariables.rRepPelvisJoint.breakForce = Single.PositiveInfinity;
                HandVariables.rRepPelvisJoint.breakTorque = Single.PositiveInfinity;
                HandVariables.rGrabbedPlayerRep = this;
            }

            byte colliderIndex = 0;

            foreach (var colliders in colliderDictionary)
            {
                if (colliders.Value.go == grabbedCollider)
                {
                    colliderIndex = colliders.Key;
                    break;
                }
            }
            
            var playerGrabData = new PlayerStartGrabData()
            {
                userIdGrabber = SteamIntegration.currentId,
                hand = (byte)(handedness == Handedness.RIGHT ? 1 : 0),
                pelvisAtGrabEvent = new CompressedTransform(pelvis.transform.position, pelvis.transform.rotation),
                colliderIndex = colliderIndex
            };
            var catchupBuff = PacketHandler.CompressMessage(NetworkMessageType.PlayerStartGrabPacket, playerGrabData);
            SteamPacketNode.SendMessage(this.user.Id, NetworkChannel.Transaction, catchupBuff.getBytes());
        }

        public void LetGoOfThisGuy(Handedness handedness)
        {
            if (handedness == Handedness.LEFT)
            {
                if (HandVariables.lRepPelvisJoint != null)
                {
                    GameObject.Destroy(HandVariables.lRepPelvisJoint);
                    HandVariables.lRepPelvisJoint = null;
                    HandVariables.lGrabbedPlayerRep = null;
                    if (HandVariables.rRepPelvisJoint == null)
                    {
                        simulated = false;
                        pelvis.transform.parent = playerRep.transform;
                    }
                }
            }
            if (handedness == Handedness.RIGHT)
            {
                GameObject.Destroy(HandVariables.rRepPelvisJoint);
                HandVariables.rRepPelvisJoint = null;
                HandVariables.rGrabbedPlayerRep = null;
                if (HandVariables.lRepPelvisJoint == null)
                {
                    simulated = false;
                    pelvis.transform.parent = playerRep.transform;
                }
            }
            
            var playerEndGrabData = new PlayerEndGrabData()
            {
                userIdGrabber = SteamIntegration.currentId,
                hand = (byte)(handedness == Handedness.RIGHT ? 1 : 0),
            };
            var catchupBuff = PacketHandler.CompressMessage(NetworkMessageType.PlayerEndGrabPacket, playerEndGrabData);
            SteamPacketNode.SendMessage(this.user.Id, NetworkChannel.Transaction, catchupBuff.getBytes());
        }

        private void AddCorrectProperties(GameObject gameObject, Collider collider, GenericGrip genericGripOriginal)
        {
            if (gameObject == null) return;
            gameObject.layer = LayerMask.NameToLayer("Interactable");
            
            ImpactProperties impactProperties = gameObject.AddComponent<ImpactProperties>();
            impactProperties.surfaceData = _impactProperties.surfaceData;
            impactProperties.DecalMeshObj = _impactProperties.DecalMeshObj;
            impactProperties.decalType = _impactProperties.decalType;
            
            /*ImpactSFX sfx = gameObject.AddComponent<ImpactSFX>();
            sfx.impactSoft = sounds.ToArray();
            sfx.impactHard = sounds.ToArray();
            sfx.pitchMod = 1;
            sfx.bluntDamageMult = 1;
            sfx.minVelocity = 0.4f;
            sfx.velocityClipSplit = 4;
            sfx.jointBreakVolume = 1;*/
            
            
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

        private void HandleColliderObject(GameObject gameObject, Collider collider, GenericGrip genericGripOriginal)
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

            /*if (sounds.Count == 0)
            {
                AudioClip[] clips = Resources.FindObjectsOfTypeAll<AudioClip>();
                sounds = new List<AudioClip>();
                foreach (var clip in clips)
                    if (clip.name.Contains("ImpactSoft_SwordBroad"))
                        sounds.Add(clip);
            }*/

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
            avatarMass = Player.GetCurrentAvatar().massTotal;
            if(colliders != null)
            {
                GameObject.Destroy(colliders);
            }

            GameObject colliderParent = new GameObject("allColliders");
            colliders = colliderParent;

            GenericGrip genericGrip = null;
            bool addedAiTarget = false;
            
            MelonLogger.Msg("Starting to finalize colliders.");

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

                if (collider.gameObject.name.Equals("Pelvis"))
                {
                    pelvis = gameObject;
                    pelvisIndex = currentColliderId;
                }

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
                
                HandleColliderObject(gameObject, meshCollider, genericGrip);

                colliderDictionary.Add(currentColliderId++, new InterpolatedObject(gameObject));
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
                
                if (collider.gameObject.name.Equals("Pelvis"))
                {
                    pelvis = gameObject;
                    pelvisIndex = currentColliderId;
                }
                
                if (collider.gameObject.name.Equals("Hand (right)"))
                {
                    rHand = gameObject;
                }
                
                if (collider.gameObject.name.Equals("Hand (left)"))
                {
                    lHand = gameObject;
                }
                
                HandleColliderObject(gameObject, boxCollider, genericGrip);

                colliderDictionary.Add(currentColliderId++, new InterpolatedObject(gameObject));
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
                
                if (collider.gameObject.name.Equals("Pelvis"))
                {
                    pelvis = gameObject;
                    pelvisIndex = currentColliderId;
                }

                HandleColliderObject(gameObject, capsuleCollider, genericGrip);

                colliderDictionary.Add(currentColliderId++, new InterpolatedObject(gameObject));
            }
            
            MelonLogger.Msg("All colliders stored.");

            if (pelvis != null)
            {
                MelonLogger.Msg("Pelvis was not null.");
                MelonLogger.Msg("Pelvis index is: "+pelvisIndex);
                pelvis.name = "PelvisRoot";
                pelvis.transform.parent = playerRep.transform;
                Rigidbody pelvisBody = pelvis.GetComponent<Rigidbody>();
                pelvisBody.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;
                colliders.transform.parent = pelvis.transform;
                
                for (int i = 0; i < playerRep.transform.childCount; i++)
                {
                    // Get all the top level stuff (Meshes and bone roots) and parent it. Better than full searching and parenting every collider and bone individually.
                    GameObject child = playerRep.transform.GetChild(i).gameObject;
                    
                    // We want everything parented to the pelvis because this is our "root" which we can simulate client-side if we want.
                    // Means we can move the player with no latency as long as the positions and rotations sent are relative to the pelvis.
                    if (child != pelvis)
                    {
                        child.transform.parent = pelvis.transform;
                    }
                }
                MelonLogger.Msg("Set all children to pelvis.");
            }
            colliderParent.transform.parent = playerRep.transform;
        }

        public void updateIkTransform(byte boneId, Vector3 pos, Quaternion rot)
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
                    //selectedBone.UpdateTarget(pos, rot, true);
                    if (selectedBone.go != null){
                        if (pelvis != null)
                        {
                            Quaternion rotation = pelvis.transform.rotation.Add(rot);
                            Vector3 position = pelvis.transform.position + pos;

                            selectedBone.UpdateTarget(position, rotation, true);
                        }
                    }
                }
            }
        }

        public void Update()
        {
            foreach (InterpolatedObject interpolatedObject in boneDictionary.Values)
            {
                interpolatedObject.Lerp(BonelabMultiplayerMockup.playerMotionSmoothing.Value);
            }
            foreach (InterpolatedObject interpolatedObject in colliderDictionary.Values)
            {
                interpolatedObject.Lerp(BonelabMultiplayerMockup.playerMotionSmoothing.Value);
            }
        }

        public void updateColliderTransform(byte colliderId, Vector3 pos, Quaternion rot)
        {
            if (playerRep == null) return;

            if (pelvis == null) return;

            if (colliderDictionary.ContainsKey(colliderId))
            {

                var selectedBone = colliderDictionary[colliderId];
                if (selectedBone != null)
                {
                    bool teleport = true;
                    
                    if (selectedBone.go == null)
                    {
                            return;
                    }

                    if (colliderId == pelvisIndex)
                    {
                        if (simulated)
                        {
                            pelvis.transform.rotation = rot;
                            return;
                        }
                    }
                    Quaternion rotation = pelvis.transform.rotation.Add(rot);
                    Vector3 position = pelvis.transform.position + pos;
                    if (colliderId == pelvisIndex)
                    {
                        pelvis.transform.position = pos;
                        pelvis.transform.rotation = rot;
                        return;
                    }

                    selectedBone.UpdateTarget(position, rotation, teleport);
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