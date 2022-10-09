using System;
using System.Collections.Generic;
using UnityEngine;

namespace HBMP.Extensions
{
    public static class TransformExtensions
    {
        // This class is needed for the "SimplifiedTransforms" class by Entanglement devs.
        public static bool InHierarchyOf(this Transform t, string parentName)
        {
            if (t.name == parentName)
                return true;

            if (t.parent == null)
                return false;

            t = t.parent;

            return InHierarchyOf(t, parentName);
        }

        public static string GetPathToRoot(this Transform t, Transform root)
        {
            var path = "/" + t.name;
            while (t.parent != null && t != root)
            {
                t = t.parent;
                path = "/" + t.name + path;
            }

            return path;
        }

        public static void ForceActivate(this Transform transform)
        {
            transform.gameObject.SetActive(true);
            if (transform.parent != null)
                transform.parent.ForceActivate();
        }

        // Credits to https://answers.unity.com/questions/8500/how-can-i-get-the-full-path-to-a-gameobject.html
        // Modified for different child indexes
        public static string GetFullPath(this Transform current, string otherRootName = null)
        {
            if (current.parent == null)
                return otherRootName != null ? otherRootName : current.name;

            return current.parent.GetFullPath(otherRootName) + "/" +
                   Array.FindIndex(GetChildrenWithName(current.parent, current.name), o => o == current) + "/" +
                   current.name;
        }

        public static Transform GetChildWithName(this Transform transform, int index, string name)
        {
            return transform.GetChildrenWithName(name)[index];
        }

        public static Transform[] GetChildrenWithName(this Transform t, string name)
        {
            var children = new Transform[t.childCount];
            for (var i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                if (child.name == name) children[i] = child;
            }

            return children;
        }

        public static Transform[] GetGrandChildren(this Transform t)
        {
            var transforms = new List<Transform>(t.childCount * t.childCount);
            for (var i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                transforms.Add(child);
                transforms.AddRange(child.GetGrandChildren());
            }

            return transforms.ToArray();
        }

        public static Vector3 TransformPosition(this Transform t, Vector3 position)
        {
            return position + t.position;
        }

        public static Vector3 TransformPosition(this Vector3 v, Vector3 position)
        {
            return position + v;
        }

        public static Vector3 InverseTransformPosition(this Transform t, Vector3 position)
        {
            return position - t.position;
        }

        public static Vector3 InverseTransformPosition(this Vector3 v, Vector3 position)
        {
            return position - v;
        }
    }
}