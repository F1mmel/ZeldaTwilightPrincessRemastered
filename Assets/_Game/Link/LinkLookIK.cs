using System.Collections;
using RootMotion.FinalIK;
using UnityEngine;

public class LinkLookIK : MonoBehaviour
{
    public Transform target;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(StartLook());
    }

    private IEnumerator StartLook()
    {
        yield return new WaitForSeconds(0.5f);

        GameObject bmd = gameObject.transform.parent.parent.gameObject.FindChildren("center").transform.parent.gameObject;
        GameObject center = gameObject.transform.parent.parent.gameObject.FindChildren("center").transform.gameObject;
        
        transform.SetParent(bmd.transform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        
        LookAtController controller = GetComponent<LookAtController>();
        controller.target = target;
        controller.OverrideSource = bmd.transform;
        
        LookAtIK ik = GetComponent<LookAtIK>();
        ik.solver.head.transform = gameObject.transform.parent.parent.gameObject.FindChildren("head").transform;
        ik.solver.spine = new IKSolverLookAt.LookAtBone[4];
        ik.solver.target = target;

        for (int i = 0; i < ik.solver.spine.Length; i++)
        {
            ik.solver.spine[i] = new IKSolverLookAt.LookAtBone();
        }
        
        ik.solver.spine[0].transform = gameObject.transform.parent.parent.gameObject.FindChildren("center").transform;
        ik.solver.spine[1].transform = gameObject.transform.parent.parent.gameObject.FindChildren("backbone1").transform;
        ik.solver.spine[2].transform = gameObject.transform.parent.parent.gameObject.FindChildren("backbone2").transform;
        ik.solver.spine[3].transform = gameObject.transform.parent.parent.gameObject.FindChildren("neck").transform;

        /*MonoBehaviour[] scriptList = transform.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scriptList)
        {
            center.AddComponent(script.GetType());
            System.Reflection.FieldInfo[] fields = script.GetType().GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(center.GetComponent(script.GetType()), field.GetValue(script));
            }
        }
        Destroy(center.GetComponent<LinkLookIK>());
        
        Destroy(this);*/
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
