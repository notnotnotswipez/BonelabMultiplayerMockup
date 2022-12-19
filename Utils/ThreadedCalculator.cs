using System.Collections.Concurrent;
using System.Threading;
using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Representations;
using UnityEngine;

namespace BonelabMultiplayerMockup.Utils
{
    
    public class ThreadedCalculators
    {
        private static ConcurrentQueue<CalculatedPlayerPositionData> finishedCalculatedPlayerData = new ConcurrentQueue<CalculatedPlayerPositionData>();
        private static ConcurrentQueue<QueuedCalculationData> queuedCalculations = new ConcurrentQueue<QueuedCalculationData>();

        public void Init()
        {
            Thread thread = new Thread(CalculateQueued);
            thread.Start();
        }

        public static void QueueCalculation(PlayerRepresentation playerRepresentation, byte index, PlayerPosVariant posVariant, CompressedTransform compressedTransform)
        {
            QueuedCalculationData queuedCalculationData = new QueuedCalculationData()
            {
                playerRepresentation = playerRepresentation,
                index = index,
                variant = posVariant,
                compressedTransform = compressedTransform
            };
            queuedCalculations.Enqueue(queuedCalculationData);
        }

        void CalculateQueued()
        {
            while (true)
            {
                while (queuedCalculations.Count > 0)
                {
                    QueuedCalculationData calculationData;
                    
                    while (!queuedCalculations.TryDequeue(out calculationData)) continue;

                    if (calculationData != null)
                    {
                        calculationData.compressedTransform.Read();
                        CalculatedPlayerPositionData calculatedPlayerPositionData = new CalculatedPlayerPositionData()
                        {
                            playerRepresentation = calculationData.playerRepresentation,
                            index = calculationData.index,
                            variant = calculationData.variant,
                            quaternion = calculationData.compressedTransform.rotation,
                            vector3 = calculationData.compressedTransform.position
                        };
                    
                        finishedCalculatedPlayerData.Enqueue(calculatedPlayerPositionData);
                    }
                }
            }
        }

        public static void ProcessCalculated()
        {
            while (finishedCalculatedPlayerData.Count > 0)
            {
                CalculatedPlayerPositionData calculationData;
                while (!finishedCalculatedPlayerData.TryDequeue(out calculationData)) continue;

                PlayerRepresentation playerRepresentation = calculationData.playerRepresentation;
                if (calculationData.variant == PlayerPosVariant.BONE)
                {
                    playerRepresentation.updateIkTransform(calculationData.index, calculationData.vector3, calculationData.quaternion);
                }
                else if (calculationData.variant == PlayerPosVariant.COLLIDER)
                {
                    playerRepresentation.updateColliderTransform(calculationData.index, calculationData.vector3, calculationData.quaternion);
                }
            }
        }
    }
    
    public class QueuedCalculationData
    {
        public PlayerRepresentation playerRepresentation;
        public byte index;
        public PlayerPosVariant variant;
        public CompressedTransform compressedTransform;
    }

    public class CalculatedPlayerPositionData
    {
        public PlayerRepresentation playerRepresentation;
        public byte index;
        public PlayerPosVariant variant;
        public Quaternion quaternion;
        public Vector3 vector3;
    }

    public enum PlayerPosVariant
    {
        BONE,
        COLLIDER
    }
}