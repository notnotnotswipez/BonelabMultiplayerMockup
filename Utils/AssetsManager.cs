using System;
using MelonLoader;
using SLZ.Marrow.Warehouse;
using UnityEngine;

namespace BonelabMultiplayerMockup.Utils
{
    public class AssetsManager
    {
        public static void LoadAvatar(string barcode, Action<GameObject> callback)
        {
            AvatarCrate avatarCrate = AssetWarehouse.Instance.GetCrate<AvatarCrate>(barcode);
            avatarCrate.LoadAsset(callback);
        }
    }
}