using System.Collections;
using RootMotion.FinalIK;
using UnityEngine;

public class LinkIKHelper : MonoBehaviour
{
    public string[] Left = new string[3];
    public string[] Right = new string[3];
    
    public AvatarIKGoal[] Goals = new AvatarIKGoal[2];
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(CreateFeet());
    }

    private IEnumerator CreateFeet()
    {
        //yield return new WaitForSeconds(0.25f);
        yield return null;
        
        GrounderIK ik = transform.GetComponent<GrounderIK>();
        ik.pelvis = gameObject.transform.parent.parent.gameObject.FindChildren("center").transform;
        ik.characterRoot = gameObject.transform.parent.parent.gameObject.FindChildren("center").transform.parent;
        ik.solver.footSpeed = 2f;
        ik.solver.maxStep = 0.5f;
        //ik.solver.footSpeed = 5f;
        ik.solver.footRadius = 0.0001f;
        ik.maxRootRotationAngle = 0f;
        ik.enabled = false;
        
        
        
        LimbIK ikL = ik.legs[0].GetComponent<LimbIK>();
        ikL.solver.bone1.transform = gameObject.transform.parent.parent.gameObject.FindChildren(Left[0]).transform;
        ikL.solver.bone2.transform = gameObject.transform.parent.parent.gameObject.FindChildren(Left[1]).transform;
        ikL.solver.bone3.transform = gameObject.transform.parent.parent.gameObject.FindChildren(Left[2]).transform;
        ikL.solver.goal = Goals[0];
        
        LimbIK ikR = ik.legs[1].GetComponent<LimbIK>();
        ikR.solver.bone1.transform = gameObject.transform.parent.parent.gameObject.FindChildren(Right[0]).transform;
        ikR.solver.bone2.transform = gameObject.transform.parent.parent.gameObject.FindChildren(Right[1]).transform;
        ikR.solver.bone3.transform = gameObject.transform.parent.parent.gameObject.FindChildren(Right[2]).transform;
        ikR.solver.goal = Goals[1];
        
        transform.SetParent(gameObject.transform.parent.parent.gameObject.FindChildren("center").transform.parent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        ik.enabled = true;
    }

    private GameObject CreateChild(string name)
    {
        GameObject a = new GameObject(name);
        a.transform.parent = transform;
        a.transform.localPosition = Vector3.zero;
        a.transform.localRotation = Quaternion.identity;

        return a;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
