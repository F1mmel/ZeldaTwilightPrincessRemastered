using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameObjectExtensions
{
    public static List<GameObject> GetAllChildren(this GameObject gameObject)
    {
        List<GameObject> children = new List<GameObject>();
        GetAllChildrenRecursive(gameObject.transform, children);
        return children;
    }

    private static void GetAllChildrenRecursive(Transform parent, List<GameObject> children)
    {
        foreach (Transform child in parent)
        {
            children.Add(child.gameObject);
            GetAllChildrenRecursive(child, children);
        }
    }

    public static GameObject FindChildren(this GameObject gameObject, string name)
    {
        foreach (GameObject child in gameObject.GetAllChildren())
        {
            if (child.name.Equals(name))
            {
                return child;
            }
        }

        return null;
    }

    public static Transform FindChildrenTransform(this GameObject gameObject, string name)
    {
        foreach (GameObject child in gameObject.GetAllChildren())
        {
            if (child.name.Equals(name))
            {
                return child.transform;
            }
        }

        return null;
    }
}