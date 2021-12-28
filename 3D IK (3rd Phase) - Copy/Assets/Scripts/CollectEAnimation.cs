using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;


public class CollectEAnimation : MonoBehaviour
{
    public class MyFrames
    {
        public List<string> Name = new List<string>();
        public List<Vector3> LocalPosition = new List<Vector3>();
        public List<Vector3> LocalRotation = new List<Vector3>();
        public List<Vector3> LocalScale = new List<Vector3>();
    }
    

    public Animator animator;
    public Transform ElephantTransformA;
    public Transform ElephantTransformB;
    List<MyFrames> E = new List<MyFrames>();
    bool Done = false;
    bool Shooting = false;
    int i = 0;
    Hashtable ht = new Hashtable();

    // Start is called before the first frame update
    void Start()
    {
        //ElephantTransforms

    }
    
    // Update is called once per frame
    private void Update()
    {
        if (Time.time <= 2.5)
        {
            Transform ElephantTransformSystem = FindDeepChild(ElephantTransformA, ElephantTransformA.name);
            Transform[] allChildren = ElephantTransformSystem.GetComponentsInChildren<Transform>();
            MyFrames MY = new MyFrames();
            for (int k = 0; k < allChildren.Length; k++)
            {
                MY.Name.Add(allChildren[k].name);
                MY.LocalPosition.Add(new Vector3(allChildren[k].localPosition.x, allChildren[k].localPosition.y, allChildren[k].localPosition.z));
                MY.LocalRotation.Add(new Vector3(allChildren[k].localEulerAngles.x, allChildren[k].localEulerAngles.y, allChildren[k].localEulerAngles.z));
                MY.LocalScale.Add(new Vector3(allChildren[k].localScale.x, allChildren[k].localScale.y, allChildren[k].localScale.z));
            }

            E.Add(MY);
            Done = true;
        }
        else if (Done)
        {
            Done = false;
            Shooting = true;
        }

        if (Input.GetKeyDown(KeyCode.M) && Shooting)
        {
            if (i < E.Count)
            {
                Transform HumanTransformSystem = FindDeepChild(ElephantTransformB, ElephantTransformB.name);
                Transform[] allChildren = HumanTransformSystem.GetComponentsInChildren<Transform>();

                for (int j = 0; j < allChildren.Length; j++)
                {
                    allChildren[j].localPosition = new Vector3(E[i].LocalPosition[j].x, E[i].LocalPosition[j].y, E[i].LocalPosition[j].z);
                    allChildren[j].localEulerAngles = new Vector3(E[i].LocalRotation[j].x, E[i].LocalRotation[j].y, E[i].LocalRotation[j].z);
                    allChildren[j].localScale = new Vector3(E[i].LocalScale[j].x, E[i].LocalScale[j].y, E[i].LocalScale[j].z);
                }
                i++;
            }
        }
    }

    public static Transform FindDeepChild(Transform aParent, string aName)
    {
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(aParent);
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            if (c.name == aName)
                return c;
            foreach (Transform t in c)
                queue.Enqueue(t);
        }
        return null;
    }
}

