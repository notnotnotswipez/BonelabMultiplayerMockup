using UnityEngine;

namespace BonelabMultiplayerMockup.Object
{
    public class InterpolatedObject
    {
        public GameObject go;
        public Vector3 targetPos;
        public Quaternion targetRot;
        float timeElapsed = 0;
        float lerpDuration = 0.1f;

        public InterpolatedObject(GameObject gameObject)
        {
            go = gameObject;
            targetPos = go.transform.position;
            targetRot = go.transform.rotation;
            timeElapsed = 0;
        }

        public void UpdateTarget(Vector3 position, Quaternion rotation)
        {
            targetPos = position;
            targetRot = rotation;
            timeElapsed = 0;
        }

        public void Lerp()
        {
            if (timeElapsed < lerpDuration)
            {
                timeElapsed += Time.deltaTime;
                go.transform.position = Vector3.Lerp(go.transform.position, targetPos, timeElapsed / lerpDuration);
                go.transform.rotation = Quaternion.Lerp(go.transform.rotation, targetRot, timeElapsed / lerpDuration);
            }
        }
    }
}