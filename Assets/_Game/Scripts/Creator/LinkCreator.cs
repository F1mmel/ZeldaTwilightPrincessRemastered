using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using System.IO;
using System.Runtime.Serialization;
using SharpYaml.Tokens;
using WiiExplorer;
using UnityEngine;

public enum LinkType
{
    [EnumMember(Value = "alSumou, bls")]
    Sumo,
    [EnumMember(Value = "Bmdl, bl")]
    Ordon,
    [EnumMember(Value = "Kmdl, al")]
    Hero,
    [EnumMember(Value = "Zmdl, zl")]
    Zora,
    [EnumMember(Value = "Mmdl, ml")]
    MagicArmor
}

[Serializable]
public class ExternalLinkAnimation
{
    public string ExternalArchive;
    public string Animation;
}

public class LinkCreator : MonoBehaviour
{
    public LinkType LinkType;
    public string SelectedBCK;

    [Space] public ExternalLinkAnimation ExternalLinkAnimation;

    private static string path = "Assets/GameFiles/res/Object/";
    private static string pathHD = "Assets/GameFilesHD/res/Object/";
    
    // Start is called before the first frame update
    void Start()
    {
        string typeValue = LinkType.ToEnumMember();
        string archiveName = typeValue.Split(", ")[0];
        string bmdName = typeValue.Split(", ")[1];
        
        // Create model from selected arc
        Archive archive = ArcReader.Read(path + archiveName + ".arc");
        //BMD Bmd = BMD.CreateModelFromPathInPlace(archive, bmdName, null, transform, false);

        string faceModel = "";
        if (LinkType == LinkType.Ordon) faceModel = "al_face";
        if (LinkType == LinkType.Zora) faceModel = "zl_face";
        if (LinkType == LinkType.Hero) faceModel = "al_face";
        if (LinkType == LinkType.MagicArmor) faceModel = "al_face";
        
        BMD link = BMDCreator.CreateModel(archiveName, bmdName, transform);
        BMD face = BMDCreator.CreateModel(archiveName, faceModel, transform);
        BMD head = BMDCreator.CreateModel(archiveName, bmdName + "_head", transform);
        
        // Adjust scales
        link.transform.localScale = new Vector3(0.01f, 0.01f, .01f);
        face.transform.localScale = new Vector3(0.01f, 0.01f, .01f);
        head.transform.localScale = new Vector3(0.01f, 0.01f, .01f);

        face.SetParentJoint(link, "head");
        head.SetParentJoint(link, "head");

        link.transform.position = transform.position;
        link.transform.eulerAngles = transform.eulerAngles;
        
        if(LinkType == LinkType.Zora || LinkType == LinkType.Hero || LinkType == LinkType.MagicArmor) 
            link.transform.AddComponent<CapPhysics>().AddBone(true);

            if (!SelectedBCK.Equals(""))
            {
                //link.LoadEveryAnimationInArchive(ArcReader.Read(@"Assets\GameFiles\res\Object\AlAnm.arc"));
                //link.PlayAnimation(SelectedBCK);
            }
            else
            {
                // Use external
                //link.LoadEveryAnimationInArchive(ArcReader.Read(@"Assets\GameFiles\res\Object\" + ExternalLinkAnimation.ExternalArchive + ".arc"));
                //link.PlayAnimation(ExternalLinkAnimation.Animation);
            }
    }
}