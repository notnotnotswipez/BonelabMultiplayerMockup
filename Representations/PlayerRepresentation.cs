using System.Collections.Generic;
using BonelabMultiplayerMockup.Messages;
using BonelabMultiplayerMockup.Messages.Handlers.Player;
using BonelabMultiplayerMockup.Nodes;
using BonelabMultiplayerMockup.Utils;
using BoneLib;
using Discord;
using HBMP.DataType;
using MelonLoader;
using UnityEngine;
using Avatar = SLZ.VRMK.Avatar;

namespace BonelabMultiplayerMockup.Representations
{
    public class PlayerRepresentation
    {
        public static Dictionary<long, PlayerRepresentation> representations =
            new Dictionary<long, PlayerRepresentation>();
        
        public Dictionary<byte, GameObject> boneDictionary = new Dictionary<byte, GameObject>();

        private byte currentBoneId;
        public Transform handL;
        public Transform handR;

        public Transform head;
        public GameObject playerRep;
        public User user;
        public string username;

        public PlayerRepresentation(User user)
        {
            this.user = user;
            username = user.Username;
            var avatarAskData = new AvatarQuestionData()
            {
                // yep
            };
            var catchupBuff = MessageHandler.CompressMessage(NetworkMessageType.AvatarQuestionMessage, avatarAskData);
            Node.activeNode.SendMessage(this.user.Id, (byte)NetworkChannel.Transaction, catchupBuff.getBytes());
        }

        public void SetAvatar(string barcode)
        {
            currentBoneId = 0;
            boneDictionary.Clear();
            if (playerRep != null) UnityEngine.Object.Destroy(playerRep);

            AssetsManager.LoadAvatar(barcode, FinalizeAvatar);
        }

        private void FinalizeAvatar(GameObject go)
        {
            if (playerRep != null)
            {
                GameObject.Destroy(playerRep);
                GameObject backupCopy = GameObject.Instantiate(go);
                backupCopy.name = "(PlayerRep) " + username;
                playerRep = backupCopy;
                PopulateBoneDictionary(backupCopy.GetComponentInChildren<Avatar>().gameObject.transform);
                GameObject.DontDestroyOnLoad(backupCopy);
                return;
            }

            GameObject copy = GameObject.Instantiate(go);
            copy.name = "(PlayerRep) " + username;
            playerRep = copy;
            PopulateBoneDictionary(copy.GetComponentInChildren<Avatar>().gameObject.transform);
            GameObject.DontDestroyOnLoad(copy);
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

        public void updateIkTransform(byte boneId, SimplifiedTransform simplifiedTransform)
        {
            if (playerRep == null) return;

            if (boneDictionary.ContainsKey(boneId))
            {
                var selectedBone = boneDictionary[boneId];
                if (selectedBone != null)
                {
                    selectedBone.transform.position = simplifiedTransform.position;
                    selectedBone.transform.eulerAngles = simplifiedTransform.rotation.ExpandQuat().eulerAngles;
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