using System;
using BoneLib;
using SLZ.Marrow.Data;
using SLZ.Marrow.Pool;
using SLZ.Marrow.Warehouse;
using UnityEngine;

namespace BonelabMultiplayerMockup.Utils
{
    public class PoolManager
    {
        public static AssetPool GetAssetPool(string barcode)
        {
            foreach (var pair in AssetSpawner._instance._barcodeToPool)
                if (pair.key._id.Equals(barcode))
                    return pair.value;

            return null;
        }
        
        public static T GetComponentOnObject<T>(GameObject go) where T : Component
        {
            if (go != null)
            {
                if (go.GetComponent<T>())
                {
                    return go.GetComponent<T>();
                }
                if (go.GetComponentInParent<T>())
                {
                    return go.GetComponentInParent<T>();
                }
                if (go.GetComponentInChildren<T>())
                {
                    return go.GetComponentInChildren<T>();
                }
            }
            return null;
        }

        public static Barcode GetSpawnableBarcode(GameObject gameObject)
        {
            var assetPoolee = gameObject.GetComponent<AssetPoolee>();
            if (assetPoolee == null)
            {
                assetPoolee = gameObject.GetComponentInParent<AssetPoolee>();
                if (assetPoolee == null) assetPoolee = gameObject.GetComponentInChildren<AssetPoolee>();
            }

            if (assetPoolee == null)
            {
                var barcode = new Barcode();
                barcode._id = "empty";
                return barcode;
            }

            return assetPoolee.spawnableCrate._barcode;
        }

        public static bool IsCrate(string barcode)
        {
            GameObjectCrate gameObjectCrate =
                AssetWarehouse.Instance.GetCrate<GameObjectCrate>(barcode);
            if (gameObjectCrate == null)
            {
                return false;
            }

            return true;
        }

        public static void SpawnGameObject(string barcode, Vector3 position, Quaternion rotation,
            Action<GameObject> onSpawn)
        {
            GameObjectCrate gameObjectCrate =
                AssetWarehouse.Instance.GetCrate<GameObjectCrate>(barcode);
            Action<GameObject> action = new Action<GameObject>(o =>
            {
                GameObject copy = GameObject.Instantiate(o);
                copy.transform.position = position;
                copy.transform.rotation = rotation;
                onSpawn.Invoke(copy);
            });
            gameObjectCrate.LoadAsset(action);            
        }
    }
}