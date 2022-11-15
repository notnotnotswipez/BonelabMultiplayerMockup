using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Representations;
using BoneLib;
using Il2CppSystem.Reflection;
using MelonLoader;
using SLZ.Interaction;
using UnhollowerRuntimeLib;
using UnityEngine;
using Avatar = SLZ.VRMK.Avatar;

namespace BonelabMultiplayerMockup.Utils
{
    public static class Utils
    {
        public static T CopyComponent<T>(this Component original, GameObject destination) where T : Component
        {
            Il2CppSystem.Type type = Il2CppType.Of<T>(original);
            var dst = destination.GetComponent(type) as T;
            if (!dst) dst = destination.AddComponent(type) as T;
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            var fields = type.GetFields(flags);
            foreach (var field in fields)
            {
                if (field.IsStatic) continue;
                field.SetValue(dst, field.GetValue(original));
            }
            var props = type.GetProperties(flags);
            foreach (var prop in props)
            {
                if (!prop.CanWrite || !prop.CanWrite || prop.Name == "name") continue;
                prop.SetValue(dst, prop.GetValue(original, null), null);
            }
            return dst as T;
        }

        public static bool IsPlayerPart(GameObject gameObject)
        {
            string path = SyncedObject.GetGameObjectPath(gameObject);
            if (path.ToLower().Contains("(playerrep)"))
            {
                return true;
            }

            return false;
        }

        public static bool CanPickup(PlayerRepresentation rep)
        {
            Avatar currentAvatar = Player.GetCurrentAvatar();
            
            // Seems about right. Unfortunately it doesnt seem like the values get set on another persons avatar object, so we store the mass ourselves.
            // Might make this configurable but im going with reference to what I think should be possible. And I think strong should be able to pickup fast.
            if (currentAvatar.massTotal - rep.avatarMass > 30)
            {
                return true;
            }

            return false;
        }

        public static PlayerRepresentation GetRepresentation(GameObject gameObject)
        {
            string playerName = gameObject.transform.root.name.Replace("(PlayerRep) ", "");
                
            PlayerRepresentation foundRep = null;
            foreach (PlayerRepresentation playerRepresentation in PlayerRepresentation.representations.Values) {
                if (playerName == playerRepresentation.username)
                {
                    foundRep = playerRepresentation;
                    break;
                }
            }

            return foundRep;
        }

        public static bool IsSoftBody(GameObject gameObject)
        {
            if (gameObject.name.Contains("BreastLf") || gameObject.name.Contains("BreastRt"))
            {
                return true;
            }
            if (gameObject.name.Contains("ButtLf") || gameObject.name.Contains("ButtRt"))
            {
                return true;
            }

            return false;
        }

        public static bool IsGrabbedStill(SyncedObject syncedObject)
        {
            SyncedObject rSynced = Player.GetComponentInHand<SyncedObject>(Player.rightHand);
            SyncedObject lSynced = Player.GetComponentInHand<SyncedObject>(Player.rightHand);

            if (!SyncedObject.relatedSyncedObjects.ContainsKey(syncedObject.groupId))
            {
                return false;
            }

            foreach (var gropedSynced in SyncedObject.relatedSyncedObjects[syncedObject.groupId])
            {
                if (rSynced)
                {
                    if (gropedSynced.currentId == rSynced.currentId)
                    {
                        return true;
                    }
                }
                if (lSynced)
                {
                    if (gropedSynced.currentId == lSynced.currentId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int GetRecursedChildren(GameObject gameObject, int starting)
        {
            if (gameObject.transform.childCount > 0)
            {
                int total = starting + gameObject.transform.childCount;
                for (int i = 0; i < gameObject.transform.childCount; i++)
                {
                    GameObject child = gameObject.transform.GetChild(i).gameObject;
                    if (child.transform.childCount > 0)
                    {
                        total += child.transform.childCount;
                        total = GetRecursedChildren(child, total);
                    }
                }
                return total;
            }
            return starting;
        }
    }
}