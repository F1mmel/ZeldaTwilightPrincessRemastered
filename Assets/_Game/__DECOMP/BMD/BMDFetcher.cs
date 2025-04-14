using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using WiiExplorer;
using Color = UnityEngine.Color;

public class BMDFetcher
{
    public const string OBJ_PATH = "Assets/GameFiles/res/Object";

    public static GameObject temp;
    private static int amount;
    private static int FetchAmount;
    private static int FetchAmountSeperate;

    private static Dictionary<string, Archive> ARCHIVES = new Dictionary<string, Archive>();
    private static Dictionary<string, BMD> MODELS = new Dictionary<string, BMD>();

    private static BMD CurrentBMD;
    private static BMD CachedBMD;

    private static string tempOName = "";
    public class Cache
    {
        public string ObjectName;
        public string BmdName;
        public Actor Actor;
        public BMD Bmd;
        public string AnimationName;
        public string ExternalArchive;
    }

    public static List<Cache> CacheData = new List<Cache>();
 
    public static void Fetch(string name, GameObject cube, Actor actor, Archive stage)
    {
        CurrentBMD = null;
        tempOName = name;
        FetchAmount = 0;

        string[] ignoreCaching = { "Hinit", "Gstart", "scnChg", "CamChg", "RMback0", "Mstop", "ClearB", "mmvbg"};
        if (!ignoreCaching.ToList().Contains(name))
        {
            // Check cache
            if (ZeldaManager.Instance.EnableCashing)
            {
                foreach (var c in CacheData)
                {
                    if(c.Actor == null) continue;
                    if (c.ObjectName == name/* && c.Actor.Type == actor.Type && c.Actor.ItemNo == actor.ItemNo*/)
                    {
                        CreateFromExistingBMD(cube, c, actor);

                        return;
                    }
                }
            }
        }

        //Debug.LogWarning("Fetch: " + name);
        temp = cube;
        amount = 0;
        FetchAmountSeperate = 0;
                    
        /*int door_type = ((parameter >> 5) & 7);
        string resName = "door-knob_0" + door_type + ".bmd";
        byte[] doorBuffer = ArcReader.GetBuffer(stage, resName);*/
        
        if (name.Equals("fireWd2")) CreateParticle(StageLoader.Instance.Fire);
        else if (name.Equals("bonbori")) CreateParticle(StageLoader.Instance.Fire);
        //else if (name.Contains("tboxA0") || name.Contains("tboxB0")) FetchArchive("Dalways", "boxa");
        //else if (name.Contains("tboxB1")) FetchArchive("Tbox2", "boxb");
        
        // Treasure Chest
        if (name.Contains("tboxA0") || name.Contains("tboxB0") || name.Contains("tboxB1") || name.Contains("tboxW0")) {
            int model_type = ((actor.Parameter >> 0x14) & 0xF);
            
            Chest chest = null;

            // Small Chest
            if (model_type == 0x00)
            {
                chest = FetchArchive("Dalways", "boxa").AddComponent<Chest>();
                chest.ChestType = 0;
            }
            // Big Chest
            else if (model_type == 0x01)
            {
                chest = FetchArchive("Dalways", "boxb").AddComponent<Chest>();
                chest.ChestType = 1;
                //chest = FetchArchive("Tbox2", "boxb").AddComponent<Chest>();
            }
            // Boss Chest
            else if (model_type == 0x02) {
                chest = FetchArchive("BoxC", "boxc").AddComponent<Chest>();
                chest.ChestType = 2;
            }
            
            GameObject gotoPoint = CreateGoTo("GoToPoint", chest.transform, new Vector3(-4, 0, 90), Vector3.zero);
            GameObject createCameraA  = CreateCamera("OpenCameraRight", gotoPoint.transform, new Vector3(1.35f, 1.67f, -1.45f), new Vector3(12.4f, -38f, 0f));
            GameObject itemCameraA  = CreateCamera("ItemCameraRight", gotoPoint.transform, new Vector3(0.73f, 1.44f, -1.42f), new Vector3(12.4f, -35.5f, 0f));
            
            GameObject createCameraB  = CreateCamera("OpenCameraLeft", gotoPoint.transform, new Vector3(-1.35f, 1.67f, -1.45f), new Vector3(12.4f, 38f, 0f));
            GameObject itemCameraB  = CreateCamera("ItemCameraLeft", gotoPoint.transform, new Vector3(-0.73f, 1.44f, -1.42f), new Vector3(12.4f, 35.5f, 0f));
        }
        // Door (Knob type)
        else if (name.Equals("kdoor")) {
            int door_type = ((actor.Parameter >> 5) & 7);
            string res_name = "door-knob_0" + door_type;

            BMD m = FetchFromStage(stage, res_name).Translate(new Vector3(-0.69f, 0, 0));
            
            Door door = m.AddComponent<Door>();
            door.DoorType = door_type;
            
            // Create goto point
            GameObject gotoPointA = CreateGoTo("GoToPointA", door.transform, new Vector3(75, 0, 80), Vector3.zero);
            GameObject cameraA = CreateCamera("OpenCamera", gotoPointA.transform, new Vector3(-1.11f, 1.9f, -2.59f), new Vector3(8.5f, 18.7f, 0f));
            
            GameObject gotoPointB = CreateGoTo("GoToPointB", door.transform, new Vector3(75, 0, -80), Vector3.zero);
            GameObject cameraB = CreateCamera("OpenCamera", gotoPointB.transform, new Vector3(1.11f, 1.9f, 2.59f), new Vector3(8.5f, -159f, 0f));
        }
        // Door (Shutter type)
        else if (name.Equals("door") || name.Equals("ndoor") || name.Equals("tadoor") || name.Equals("yodoor") || name.Equals("nadoor") || name.Equals("l9door") || name.Equals("l7door") || name.Equals("bigdoor"))
        {
            int door_type = (actor.Parameter & 0x1F);
            int model = ((actor.Parameter >> 5) & 7);

            string bmdName = "";
            switch (door_type) {
                default:
                case 10:
                    //bmdName = "door-shutter_" + LeftPad(model + "", 2);
                    bmdName = "door-shutter_00";
                    break;
                case 9:
                    //bmdName = "door-knob_" + LeftPad(model + "", 2);
                    bmdName = "door-knob_00";
                    break;
            }

            if (!bmdName.Equals(""))
            {
                /*MultipleObject multipleObject = new MultipleObject();
                multipleObject.Name = bmdName;
                GameObject multiple1 = multipleObject.CreateTransform(temp);
                GameObject multiple2 = multipleObject.CreateTransform(temp);
                
                multipleObject.BMDs.Add(FetchFromStageMultiple(stage, bmdName, multiple1));
                multipleObject.BMDs.Add(FetchFromStageMultiple(stage, bmdName, multiple2));*/

                //FetchFromStage(stage, bmdName);
                //BMD m2 = FetchFromStage(stage, bmdName);


                if (door_type == 9)
                {
                    MultipleObject multipleObject = new MultipleObject(stage, bmdName, bmdName);
                    multipleObject.CreateTransforms();
                    
                    /*multipleObject.BMDs[0].Translate(new Vector3(-1.50f, 0, 0)).RotateY(180);
                    multipleObject.BMDs[1].Translate(new Vector3(1.50f, 0, 0));*/
                    
                    multipleObject.BMDs[0].transform.localEulerAngles = Vector3.zero;
                    multipleObject.BMDs[1].transform.localEulerAngles = Vector3.zero;
                    multipleObject.BMDs[0].RotateY(-180).TranslateLocal(new Vector3(-150f, 0, 0));
                    multipleObject.BMDs[1].TranslateLocal(new Vector3(150f, 0, 0));
                    
                    //multipleObject.BMDs[0].RotateY(90).TranslateLocal(new Vector3(-150f, 0, 0));
                    //multipleObject.BMDs[1].RotateY(-90).Translate(new Vector3(1.50f, 0, 0));
                    //m2.RotateY(180);

                    // m1 y180 -150
                    // m2 y0 150

                    //m.Translate(new Vector3(1.50f, 0, 0));
                    //m2.Translate(new Vector3(-3.00f, 0, 0)).RotateY(180);

                    //m.Translate(new Vector3(.150f, 0, 0));
                    //m2.Translate(new Vector3(.150f, 0, 0));
                }
                else
                {
                    FetchFromStage(stage, bmdName);
                }
                
                //mat4.rotateY(m2.modelMatrix, m2.modelMatrix, Math.PI);
            }
        }
        // Door Shutter
        else if (name.Equals("kshtr00") || name.Equals("vshuter") || name.Equals("L3Bdoor"))
        {
            int type = GetParamBit(actor.Parameter, 8, 8);
            int idx = (type + 1) & 0xFF;

            string[] l_arcName = {"S_shut00", "S_shut00", "Lv3shut00", "K_l3bdoor", "V_Shutter"};
            string[] bmdName = {"s_shut_rou", "s_shut_rou", "door-shutter_00", "k_l3bdoor", "v_shutter"};
            
            FetchArchive(l_arcName[idx], bmdName[idx]);
        }
        else if (name.Equals("Obj_kn2"))
        {
            int sign_type = (actor.Parameter & 0x3FFFF);
            if (sign_type == 0x3FFFF)
            {
                FetchArchive("Obj_kn2", "j_kanban00");
            }
        }
        else if (name.Equals("L9Chand"))
        {
            FetchArchive("L9Chand", "lv9_chandelier");
        }
        else if (name.Equals("Obj_sui"))
        {
            BMD m = FetchArchive("Obj_sui", "m_suisya");
            RotateObject rotateObject = m.AddComponent<RotateObject>();
            rotateObject.rotationAxis = RotateObject.RotationAxis.X;
            rotateObject.rotationSpeed = -0.1f;
        }
        else if (name.Equals("Obj_nmp"))
        {
            BMD m = FetchArchive("J_Hyosatu", "j_hyousatu");
        }
        else if (name.Equals("thouse")) FetchArchive("U_THouse", "u_tobyhouse_tup");
        else if (name.Equals("tkrDai"))
        {
            BMD m = FetchArchive("M_TakaraD", "m_takaradai_base");
            BMD top = FetchArchive("M_TakaraD", "m_takaradai_top").SetParentJoint(m, "world_root").SetLocalPosition(new Vector3(-1233.1f, 2008, -1234.2f));
            RotateObject r = top.AddComponent<RotateObject>();
            r.rotationAxis = RotateObject.RotationAxis.Y;
            r.rotationSpeed = -1f;
        }
        else if (name.Equals("Ikada")) FetchArchive("M_Ikada", "m_ikada");
        else if (name.Equals("Obj_Tie"))
        {
            BMD m = FetchArchive("J_Necktie", "j_necktie");
            //m.LoadBTK("j_necktie");
            m.AddClothPhysics(2);
        }
        else if (name.Equals("Pumpkin")) FetchArchive("pumpkin", "pumpkin");
        else if (name.Equals("Pleaf")) FetchArchive("J_Hatake", "j_hatake00");
        else if (name.Equals("E_nest")) FetchArchive("E_nest", "o_hachinosu_01");
        else if (name.Equals("bura7A")) FetchArchive("S_bura_7A", "s_bura_swi7a");
        else if (name.Equals("bura7B")) FetchArchive("S_bura_7b", "s_l7bura_swi");
        else if (name.Equals("bura7C")) FetchArchive("S_bura_7c", "s_l7bura_swil");
        else if (name.Equals("hsMato"))
        {
            FetchArchive("L7HsMato", "lv7_hsma00");
        }
        else if (name.Equals("fan"))
        {
            string[] l_arcName = {
                "Obj_prop1",
                "Obj_prop0",
                "Obj_prop2",
            };

            // https://github.com/zeldaret/tp/blob/83875b9c9e9d5a05dd78f1592f92b057fe420c27/src/d/actor/d_a_obj_fan.cpp#L344
            BMD fan = FetchArchive(l_arcName[1], 4);
            
            /*field_0x5ac = new dBgW();
            if (field_0x5ac == NULL ||
                field_0x5ac->Set((cBgD_t*)dComIfG_getObjectRes(l_arcName[field_0xad4], l_dzb3[field_0xad4]),
                    1, &mBgMtx))
            {
                field_0x5ac = NULL;
                return 0;
            }
            return 1;*/
        }
        else if (name.Equals("Obj_knk"))
        {
            BMD[] arms = new BMD[4];
            arms[0] = FetchArchive("J_Kazami", "arm").ToUrpLitShader().Translate(new Vector3(0f, 0.15f, 0f));
            arms[1] = FetchArchive("J_Kazami", "arm").ToUrpLitShader().RotateY(90);
            arms[2] = FetchArchive("J_Kazami", "arm").ToUrpLitShader().RotateY(180);
            arms[3] = FetchArchive("J_Kazami", "arm").ToUrpLitShader().RotateY(270);
            
            foreach (BMD arm in arms)
            {
                RotateObject rotate = arm.GetWorldRoot().AddComponent<RotateObject>();
                rotate.rotationAxis = RotateObject.RotationAxis.Y;
                rotate.rotationSpeed = 1.25f;
            }
            
            FetchArchive("J_Kazami", "pole").ToUrpLitShader().Translate(new Vector3(0f, -0.15f, 0f));
        }
        else if (name.Equals("Obj_nd")) FetchArchive("Obj_ndoor", "m_nekodoor");
        else if (name.Equals("wshield")) FetchArchive("CWShd", "al_shb");
        else if (name.Equals("mvstair")) FetchArchive("K_mvkai00", "k_mvkai00");
        else if (name.Equals("Mbrid15")) FetchArchive("P_Mbridge", "p_mbridge_15");
        else if (name.Equals("hasi00"))
        {
            FetchArchive("K_mbhasi0", "k_hasikage00");
            FetchArchive("K_mbhasi0", "k_mbhasi00");
        }
        else if (name.Equals("stone")) FetchArchive("D_Srock", "d_srock");
        else if (name.Equals("stoneB")) FetchArchive("D_Brock", "d_brock");
        else if (name.Equals("MR_Pole")) FetchArchive("MR-6Pole", "u_mr_6pole");
        //else if (name.Contains("MR_Scrw")) FetchArchive("MR-Screw", "");
        else if (name.Contains("MR_Chin"))
        {
            FetchArchive("MR-Chain", "u_mr_monoana");
            FetchArchive("MR-Chain", "u_mr_hole", "u_mr_hole");
        }
        else if (name.Equals("MR_Sand")) FetchArchive("MR-Sand", "u_mr_sand");
        else if (name.Contains("MR_Tble")) FetchArchive("MR-Table", "u_mr_table").Translate(new Vector3(0, 5.7f, 0));
        else if (name.Equals("PDtile"))
        {
            int type = actor.Parameter & 0xF;
    
            if (type == 0) {
                FetchArchive("P_Dtile", "p_dtile_s");
            } else if (type == 2) {
                FetchArchive("P_Dtile00", "k_dtile00");
            } else if (type == 4) {
                FetchArchive("Lv9_Dtile", "lv9_dtile00");
            } else {
                FetchArchive("P_Dtile", "p_dtile_l");
            }
        }
        else if (name.Equals("L4Gate")) FetchArchive("L4Gate", "p_lv4gate");
        else if (name.Equals("L4Pgate")) FetchArchive("L4R02Gate", "p_lv4r02_gate");
        else if (name.Equals("l4chand"))
        {
            BMD m = FetchArchive("P_Lv4Chan", "lv4_chandelier");
            m.SetPosition(new Vector3(0, -800, 00));
        }
        else if (name.Equals("L7Prop")) FetchArchive("L7Prop", "s_lv7prop_01");
        else if (name.Equals("propy")) FetchArchive("stickwl00", "k_stickwall_00");
        else if (name.Equals("L_RopeB") || name.Equals("L_RopeS"))
        {
            int type = (actor.Parameter >> 0x10) & 3;
            
            if (type == 0) FetchArchive("L_RopeB_S", "l_ropeb_s");
            else if (type == 1) FetchArchive("L_RopeB_L", "l_ropeb_l");
        }
        // Boss doors
        else if (name.Equals("L1Bdoor") || name.Equals("L2Bdoor") || name.Equals("L4Bdoor") || name.Equals("L6Bdoor") || name.Equals("L7Bdoor") || name.Equals("L8Bdoor") || name.Equals("L9Bdoor"))
        {
            string[] l_stageName =
            {
                "D_MN05", "D_MN05A", "D_MN04", "D_MN04A", "D_MN01", "D_MN01A", "D_MN10", "D_MN10A", "D_MN11",
                "D_MN11A", "D_MN06", "D_MN06A", "D_MN07", "D_MN07A", "D_MN08", "D_MN08A", "D_MN09", "D_MN09A"
            };

            double stage_idx = -1;
            for (int i = 0; i < 18; i++) {
                if (StageLoader.Instance.StageName.Equals(l_stageName[i])) {
                    stage_idx = Math.Floor((i / 2f)) + 1;
                    break;
                }
            }

            if (stage_idx != -1) {
                string arcName = "";
                if (stage_idx == 0) {
                    arcName = "L1Bdoor";
                } else {
                    arcName = "L" + stage_idx + "Bdoor";
                }

                FetchArchive(arcName, "door_shutterboss");
            }
        }
        else if (name.Equals("hswitch")) FetchArchive("Hswitch", "p_hswitch");
        else if (name.Equals("hvySw")) FetchArchive("D_Hfsw00", "d_hfswitch");

        else if (name.Equals("Obj_ih"))
        {
            FetchArchive("Obj_ihasi", "i_bajyohasiparts");
            FetchArchive("Obj_ihasi", "i_bajyohasiparts_ef");
        }
        else if (name.Equals("WarpOB2"))
        {
            FetchArchive("Obj_kbrgD", "ni_kakarikobridge").SetPosition(new Vector3(0, -1880, 0));
        }
        else if (name.Equals("R_Gate")) FetchArchive("M_RGate00", "m_ridergate");
        else if (name.Equals("WdStone")) FetchArchive("WindStone", "model0");
        else if (name.Equals("Bhhashi")) FetchArchive("BHBridge", "m_bhbridge");
        else if (name.Equals("tgake")) FetchArchive("A_TGake", "a_touboegake");
        else if (name.Equals("CstaF"))
        {
            if (StageLoader.Instance.StageName.Equals("R_SP209")) FetchArchive("CstaFB", "cs_f_b");
            else FetchArchive("CstaF", "cs_f_a");
        }
        else if (name.Equals("dmele"))
        {
            BMD m = FetchArchive("H_Elevato", "h_elevater");
            FetchArchive("D_Hfsw00", "d_hfswitch").SetParentJoint(m, "elevater").SetPosition(new Vector3(0, 165, -80));
        }
        else if (name.Equals("swHit"))
        {
            int type = Math.Abs(actor.Parameter >> 0x1E);
            if (type > 3) {
                type = 0;
            }
            
            // 0 = yellow
            // 1 = blue
            // 2 = red
            // 3 = green
            int[] colors = {1, 0, 2, 3, 3, 2, 0, 1};
            int c = colors[type];

            UnityEngine.Color color = new UnityEngine.Color(0, 0, 0);
            if (colors[type] == 0) color = new UnityEngine.Color(255, 255, 0);
            if (colors[type] == 1) color = new UnityEngine.Color(0, 0, 255);
            if (colors[type] == 2) color = new UnityEngine.Color(255, 0, 0);
            if (colors[type] == 3) color = new UnityEngine.Color(0, 255, 0);
            
            FetchArchive("S_swHit00", "s_swhit00").ChangeColor(color);
        }
        //else if (name.Contains("Horse")) FetchArchive("", "");
        else if (name.Equals("Bou"))
        {
            BMD m = FetchArchive("Bou", "bou", "bou_wait_a");
            //m.LoadBTK("bou");
        }
        else if (name.Equals("Uri")) FetchArchive("Uri", "uri", "uri_wait_a");
        else if (name.Equals("Hanjo")) FetchArchive("Hanjo", "hanjo", "hanjo_wait_a");
        else if (name.Equals("Jagar")) FetchArchive("Jagar", "jagar", "jagar_wait_a");
        else if (name.Equals("Aru")) FetchArchive("Aru", "aru", "aru_wait_a");
        else if (name.Equals("Seira")) FetchArchive("Sera", "sera").PlayAnimationFromDifferentArchive("Seira", "sera_table_wait");
        else if (name.Equals("Besu")) FetchArchive("Besu", "besu").PlayAnimationFromDifferentArchive("Besu0", "besu_wait_a");
        else if (name.Equals("Taro")) FetchArchive("Taro", "taro").PlayAnimationFromDifferentArchive("Taro0", "taro_wait_a");
        else if (name.Equals("Maro")) FetchArchive("Maro", "maro", "maro_wait_a");
        else if (name.Equals("Kolin")) FetchArchive("Kolin", "kolin", "kolin_wait_a");
        else if (name.Equals("Kkri")) FetchArchive("Kkri", "kkri", "kkri_sleepsit");
        else if (name.Equals("Bans")) FetchArchive("Bans", "bans", "bans_wait_a");
        else if (name.Equals("grD")) FetchArchive("grD", "grd", "grd_wait_a");
        else if (name.Equals("grO"))
        {
            BMD m = FetchArchive("grO", "gro_a", "gro_wait_a");
            FetchArchive("grO", "gro_pipe").SetParentJoint(m, "handR");
        }
        else if (name.Equals("grR")) FetchArchive("grR", "grr", "grr_agura_wait");
        else if (name.Equals("grS"))
        {
            BMD m = FetchArchive("grS", "grs", "grs_wait_a");
            FetchArchive("grS", "grs_stick").SetParentJoint(m, "handL");
        }
        else if (name.Equals("Npc_ks")) FetchArchive("Npc_ks", "saru", "saru_kago_jump");
        else if (name.Equals("Cow")) FetchArchive("Cow", "cow", "cow_wait_a");
        else if (name.Equals("grA") || name.Contains("Obj_grA")) FetchArchive("grA_mdl", "gra_a").PlayAnimationFromDifferentArchive("grA_base", "gra_wait_a");
        else if (name.Equals("Hoz")) FetchArchive("Hoz", "hoz", "hoz_wait_a");
        else if (name.Equals("Rafrel")) FetchArchive("Rafrel", "raf", "raf_wait_a");
        else if (name.Equals("Shad")) FetchArchive("Shad", "shad").PlayAnimationFromDifferentArchive("Shad1", "shad_wait_a");
        else if (name.Equals("Moi")) FetchArchive("Moi", "moi", "moi_wait_a");
        else if (name.Equals("MoiR")) FetchArchive("MoiR", "moir", "moir_wait_a");
        else if (name.Equals("Ash")) FetchArchive("Ash", "ash", "ash_wait_a");
        else if (name.Equals("The")) FetchArchive("The", "the", "the_wait_a");
        else if (name.Equals("Yelia")) FetchArchive("Yelia", "yelia").PlayAnimationFromDifferentArchive("Yelia0", "yelia_wait_a");
        else if (name.Equals("ins")) FetchArchive("ins", "ins").PlayAnimationFromDifferentArchive("Ins1", "ins_wait_a");
        else if (name.Equals("B_yo"))
        {
            FetchArchive("L5_R50", "r50_p1");
            FetchArchive("L5_R50", "t_r50furniture");
        }
        else if (name.Equals("Obj_bm")) FetchArchive("Obj_bm", "bm");
        else if (name.Equals("E_bm6")) FetchArchive("E_bm6", "bm6");
        else if (name.Equals("E_rd"))
        {
            int type = (actor.Parameter >> 8) & 0xF;
            if (type == 0xF) type = 0;
            
            BMD m = FetchArchive("E_rd", "rd", "rd_wait01");
            if (type == 1) {
                FetchArchive("E_rd", "rd_club").SetParentJoint(m, "handR");
            } else if (type >= 2) {
                FetchArchive("E_rd", "rd_bow").SetParentJoint(m, "yubiL").RotateXShort(0x4000);
            }
        }
        else if (name.Equals("E_mm"))
        {
            if (name.Contains("2"))     // Helmasaurus
            {
                BMD m = FetchArchive("E_mm", "dm", "mm_wait").SetBaseScale(new Vector3(2.5f, 2.5f, 2.5f));
                FetchArchive("E_mm_mt", "dm_met").SetParentJoint(m, "helmet");
            }
            else
            {
                BMD m = FetchArchive("E_mm", "mm", "mm_wait").SetBaseScale(new Vector3(1.4f, 1.4f, 1.4f));
                FetchArchive("E_mm_mt", "mt").SetParentJoint(m, "helmet");
            }
        }
        else if (name.Equals("E_tt"))
        {        
            int type = (actor.Parameter >> 8) & 0xFF;
            if (type == 0xFF) type = 0;

            string[] arcNames = {"E_ttr", "E_ttb"};
            string[] bmdNames = {"tt", "tt_b"};
            
            FetchArchive(arcNames[type], bmdNames[type]).PlayAnimationFromDifferentArchive("E_tt", "tt_wait");
        }
        else if (name.Equals("Obj_Uma")) FetchArchive("J_Umak", "j_umakusa");
        else if (name.Equals("Obj_Tbi")) FetchArchive("J_Tobi", "j_tobi");
        else if (name.Equals("BkDoorL") || name.Equals("BkDoorR"))
        {
            int type = actor.Parameter & 1;
            if (type == 1)
            {
                BMD m = FetchArchive("A_BkDoor", "a_bkdoorr");
                m.transform.localEulerAngles = new Vector3(0, 180, 0);
                //MtxTrans(actor.pos!, false);
                //mDoMtx_YrotM(calc_mtx, actor.rot![1]);
                //mDoMtx_ZXYrotM(calc_mtx, vec3.set(scratchVec3a, 0, 700, 0));
                //mat4.copy(m.modelMatrix, calc_mtx);
            }
            else
            {
                BMD m = FetchArchive("A_BkDoor", "a_bkdoorl");
                m.transform.localEulerAngles = new Vector3(0, 180, 0);
                //MtxTrans(actor.pos!, false);
                //mDoMtx_YrotM(calc_mtx, actor.rot![1]);
                //mDoMtx_ZXYrotM(calc_mtx, vec3.set(scratchVec3a, 0, 700, 0));
                //mat4.copy(m.modelMatrix, calc_mtx);
            }
        }
        else if (name.Equals("IGateL"))
        {
            BMD m = FetchArchive("M_IGate", "m_izumigate");
            m.transform.localEulerAngles = new Vector3(0, 180, 0);
        }
        else if (name.Equals("IGateR"))
        {
            BMD m = FetchArchive("M_IGate", "m_izumigate");
            m.transform.localEulerAngles = new Vector3(0, 180, 0);
        }
        else if (name.Equals("HGateL"))
        {
            BMD m = FetchArchive("M_HGate", "m_hashigate");
            m.transform.localEulerAngles = new Vector3(0, 0, 0);
        }
        else if (name.Equals("HGateR"))
        {
            BMD m = FetchArchive("M_HGate", "m_hashigate");
            m.transform.localEulerAngles = new Vector3(0, 180, 0);
        }
        else if (name.Equals("CrvLH")) {
            FetchArchive("CrvLH_Dw", "u_crvlh_down");
            FetchArchive("CrvLH_Up", "u_crvlh_up");
        }
        else if (name.Equals("bmWin")) FetchArchive("H_Window", "h_window").OverwriteMaterial(1, StageLoader.Instance.TransparentSurface);
        else if (name.Equals("SCanCrs")) FetchArchive("SCanCrs", "ni_skycannon_crash_ef");
        else if (name.Equals("goGate")) FetchArchive("P_Ggate", "p_ggate");
        else if (name.Equals("rGate")) FetchArchive("P_Rgate", "p_rgate");
        else if (name.Equals("Obj_ms"))
        {
            FetchArchive("MAGNESIMA", "s_magne_sima");
            //m.bindTRK1(parseBRK(rarc, `brk/s_magne_sima.brk`), animFram//m.bindTTK1(parseBTK(rarc, `btk/s_magne_sima.btk`));
            
            // Lava
            FetchArchive("S_YOGAN", "s_yogan");
            //m.bindTRK1(parseBRK(rarc, `brk/s_yogan.brk`), animFrame(0));
            //m.bindTTK1(parseBTK(rarc, `btk/s_yogan.btk`));
        }
        else if (name.Equals("ObjHasi")) FetchArchive("L_hhashi", "l_hhashi00");
        else if (name.Equals("Cldst00")) FetchArchive("lv1cdl00", "d_lv1candl_00");
        else if (name.Equals("Obj_w0")) FetchArchive("Obj_web0", "k_kum_kabe00");
        else if (name.Equals("Obj_w1")) FetchArchive("Obj_web1", "k_kum_yuka00");
        else if (name.Equals("l3watB"))
        {
            FetchArchive("L3_bwater", "lv3boss_water");
            //
        }
        else if (name.Equals("l3wat02"))
        {
            FetchArchive("Kr03wat04", "k_r03water04").ResetRotation();
            //
        }
        else if (name.Equals("rstair"))
        {
            FetchArchive("K_spkai00", "k_spkaidan_00");
            //
        }
        else if (name.Equals("fence"))
        {
            int type = (actor.Parameter >> 8) & 0xFF;
            string[] arcNames = {"K_tetd", "S_bsaku00", "S_lv7saku"};
            string[] bmdNames = {"j_tetd_00", "s_bura_saku", "s_lv7saku"};

            FetchArchive(arcNames[type], bmdNames[type])/*.ResetRotation()*/;
        }
        else if (name.Equals("Cldst01"))
        {
            FetchArchive("lv1cdl01", "d_lv1candl_01");
            CreateParticle(StageLoader.Instance.Fire, new Vector3(0, 120, 0));
        }
        else if (name.Equals("E_df")) FetchArchive("E_DF", "df", "df_wait");
        else if (name.Equals("szGate"))
        {
            BMD m1 = FetchArchive("L6SzGate", "lv6_obj_skzogate").SetPosition(new Vector3(-200, 0, 0));
            BMD m2 = FetchArchive("L6SzGate", "lv6_obj_skzogate").SetPosition(new Vector3(-200, 0, 0));
        }
        else if (name.Equals("l6SwGt"))
        {
            BMD m1 = FetchArchive("L6SwGate", "lv6_obj_swgate").Translate(new Vector3(-1.50f, 0, 0));
            BMD m2 = FetchArchive("L6SwGate", "lv6_obj_swgate").RotateY(180).Translate(new Vector3(-3.00f, 0, 0));
        }
        else if (name.Equals("Tenbin"))
        {
            BMD m1 = FetchArchive("L6Tenbin", "lv6_obj_tenbin").SetPosition(new Vector3(-480, 0, 0));
            BMD m2 = FetchArchive("L6Tenbin", "lv6_obj_tenbin").SetPosition(new Vector3(-480, 0, 0));
        }
        else if (name.Equals("R50Sand")) FetchArchive("P_L4Sand", "lv4r50_ryusa");
        else if (name.Equals("l4floor")) FetchArchive("P_L4Floor", "lv4r50_floor");
        else if (name.Equals("rwall")) FetchArchive("P_L4Rwall", "lv4r50_ralewall");
        else if (name.Equals("E_ai"))
        {
            FetchArchive("E_ai", "ai");
            //
        }
        else if (name.Equals("E_MD")) FetchArchive("E_md", "md");
        else if (name.Equals("E_dn")) FetchArchive("E_dn", "dn", "dn_wait01");
        else if (name.Equals("E_mf")) FetchArchive("E_mf", "mf", "mf_wait01");
        else if (name.Equals("E_db"))
        {
            FetchArchive("E_db", "db", "db_defaultpose");
            FetchArchive("E_db", "dl");
            FetchArchive("E_db", "dt");
            //mat4.rotateX(m.modelMatrix, m.modelMatrix, -(Math.PI / 2));
        }
        else if (name.Equals("E_gb"))
        {
            FetchArchive("E_gb", "gb", "gb_wait");
            FetchArchive("E_gb", "gf", "gf_wait");
            FetchArchive("E_gb", "gs", "gf_wait");
        }
        else if (name.Equals("E_gi")) FetchArchive("E_gi", "gi", "gi_get_up");        // Loop mode
        else if (name.Equals("B_tn"))
        {
            BMD m = FetchArchive("B_tnp", "tn").PlayAnimationFromDifferentArchive("B_tn", "tnb_wait");
            BMD armL = FetchArchive("B_tnp", "tn_armor_arm_l").SetParentJoint(m, "arm_L_2");
            BMD armR = FetchArchive("B_tnp", "tn_armor_arm_r").SetParentJoint(m, "arm_R_2");
            BMD chestB = FetchArchive("B_tnp", "tn_armor_chest_b").SetParentJoint(m, "backbone_3");
            BMD chestF = FetchArchive("B_tnp", "tn_armor_chest_f").SetParentJoint(m, "backbone_3");
            BMD headB = FetchArchive("B_tnp", "tn_armor_head_b").SetParentJoint(m, "head");
            BMD headF = FetchArchive("B_tnp", "tn_armor_head_f").SetParentJoint(m, "head");
            BMD shoulderL = FetchArchive("B_tnp", "tn_armor_shoulder_l").SetParentJoint(m, "sholder_armor_L");
            BMD shoulderR = FetchArchive("B_tnp", "tn_armor_shoulder_r").SetParentJoint(m, "sholder_armor_R");
            BMD waistB = FetchArchive("B_tnp", "tn_armor_waist_b").SetParentJoint(m, "waist_armor_Back");
            BMD waistF = FetchArchive("B_tnp", "tn_armor_waist_f").SetParentJoint(m, "tn_armor_waist_F");
            BMD waistL = FetchArchive("B_tnp", "tn_armor_waist_l").SetParentJoint(m, "waist_armor_L");
            BMD waistR = FetchArchive("B_tnp", "tn_armor_waist_r").SetParentJoint(m, "waist_armor_R");
            BMD shield = FetchArchive("B_tnp", "tn_shield").SetParentJoint(m, "hand_L");
            BMD sword = FetchArchive("B_tnp", "tn_sword_a").SetParentJoint(m, "hand_R");
        }
        else if (name.Equals("B_gg"))
        {
            BMD m = FetchArchive("B_gg", "gg", "gg_wait");
            BMD sword = FetchArchive("B_gg", "gg_sword").SetParentJoint(m, "hand_L");
            BMD shield = FetchArchive("B_gg", "gg_shield").SetParentJoint(m, "hand_R").TranslateLocal(new Vector3(-30.75f, -19.51f, 4.1f)).RotateY(112).RotateX(-17);
            //BMD armR = FetchArchive("B_gg", "tn_armor_arm_r").SetParentJoint(m, "arm_R_2");
        }
        else if (name.Equals("Cstatue"))
        {        
            int type = (actor.Parameter >> 8) & 0xF;
        
            if (type == 2) {
                type = 1;
            } else if (type == 3) {
                type = 0;
            } else if (type > 2) {
                type -= 2;

                if (type > 4) {
                    type = 0;
                }
            }

            if (type == 0)
            {
                BMD m = FetchArchive("Cstatue", "cs", "cs_stop").SetBaseScale(new Vector3(1.6f, 1.6f, 1.6f));       // LOOP MODE
            } else if (type == 1)
            {
                FetchArchive("Cstatue", "cs_b");
            }
        }
        else if (name.Equals("E_fb"))
        {
            BMD m = FetchArchive("E_fl", "fl_model", "fl_wait");
            
            int prm0 = actor.Parameter & 0xFF;
            float scale = 1.5f;
            if (prm0 == 1)
                scale = 1.3f;

            m.SetBaseScale(new Vector3(scale, scale, scale));
        }
        else if (name.Equals("E_fz")) FetchArchive("E_fz", "fz");
        else if (name.Equals("l5icewl"))
        {
            BMD m = FetchArchive("l5IceWall", "yicewall_01");
            float scale_x = (actor.Parameter >> 0x10) & 0x1F;
            float scale_y = (actor.Parameter >> 0x15) & 0x1F;
            float scale_z = (actor.Parameter >> 0x1A) & 0x1F;

            //m.OverwriteMaterial(StageLoader.Instance.DefaultMaterial);
            m.SetBaseScale(new Vector3(scale_x * 0.1f, scale_y * 0.1f, scale_z * 0.1f));
            m.ChangeAlphaThreshold(0);

        }
        else if (name.Equals("spnGear"))
        {
            FetchArchive("P_Sswitch", "p_sswitch_a");
            FetchArchive("P_Sswitch", "p_sswitch_b");
        }
        else if (name.Equals("swhel00")) FetchArchive("S_wheel00", "s_wheel00");
        else if (name.Equals("kwhel00")) FetchArchive("K_wheel00", "k_wheel00");
        else if (name.Equals("kwhel01")) FetchArchive("K_wheel01", "k_wheel01");
        else if (name.Equals("syRock")) FetchArchive("syourock", "k_syourock_01").ResetRotation();
        else if (name.Equals("buraA")) FetchArchive("S_bura_A", "s_bura_swia");
        else if (name.Equals("buraB")) FetchArchive("S_bura_B", "s_bura_swib");
        else if (name.Equals("bsGate"))
        {
            BMD m = FetchArchive("S_Zgate", "s_zgate");
            if (((actor.Parameter >> 8) & 0xFF) == 1)
            {
                m.SetRotation(new Vector3(0, -180, 0));
            } //else 
            //m.SetRotation(new Vector3(0, 0, 0));
        }
        
        // Enemy
        /*else if (name.StartsWith("E_"))
        {
            //FetchArchive(name, name.Split("_")[1]);
        }*/
        
        else if (name.Equals("Digpl"))
        {
            //FetchArchive("P_DSand", "p_dsand");
        }

        else if (name.Equals("E_wap"))
        {
            BMD m = FetchArchive("ef_Portal", "ef_brportal").SetBaseScale(new Vector3(5, 5, 5));
            //m.ChangeColor(new UnityEngine.Color(255, 0, 0));
        }
        
        else if (name.Equals("gstone")) FetchArchive("H_OsiHaka", "h_oshihaka");
        else if (name.Equals("GrvStn")) FetchArchive("H_Haka", "h_haka");
        else if (name.Equals("MYNA")) FetchArchive("Npc_myna", "myna", "myna_wait_a");
        else if (name.Equals("TGMNLIG")) CreateParticle(StageLoader.Instance.Fire, new Vector3(0, 0, 0), 1);
        else if (name.Contains("carry")) {
            int type = ((int)actor.Rot.z >> 1) & 0x1F;

            string[] arcNames = {"J_tubo_00", "J_tubo_01", "Kkiba_00", "Y_ironbal", "J_taru00", "J_doku00", "Obj_bkl",
                "K_tubo02", "Obj_ballS", "Obj_ballS", "D_aotubo0", "Obj_tama", "O_tuboS", "O_tuboB"};
            string[] bmdNames = {"j_tubo_00", "j_tubo_01", "j_hako_00", "yironball", "j_taru_00", "j_doku_00", "hl",
                "k_tubo02", "lvl8_obj_hikaris", "lvl8_obj_hikaris", "d_aotubo00", "lvl8_tama", "o_tubos_lv8", "o_tubob_lv8"};
            
            string arcName = arcNames[type];

            BMD m = null;
            if (arcNames[type].Equals("Obj_bkl"))
            {
                FetchArchive(arcName, bmdNames[type]);       // Leaves
                m = FetchArchive(arcName, "k_hb00");     // Actual
            }
            else
            {
                //Debug.LogError(arcName + " :: " + bmdNames[type]);
                m = FetchArchive(arcName, bmdNames[type]);
            }

            if (arcName.Equals("Kkiba_00")) m.SetBaseScale(new Vector3(.5f, .5f, .5f));

            //m.transform.AddComponent<Rigidbody>();
        }
        
        // KAKARIKO VILLAGE
        else if (name.Equals("Onsen"))
        {
            int type = actor.Parameter & 0xFF;

            string[] l_arc_names = {"H_Onsen", "H_KaOnsen"};
            string[] l_bmd_names = {"h_onsen", "h_kakaonsen"};
            FetchArchive(l_arc_names[type], l_bmd_names[type]);
        }
        
        // GORON_MINES
        else if (name.Equals("yfire")) FetchArchive("Obj_yogan", "ef_yoganbashira", "ef_yoganbashira");
        else if (name.Equals("candlL2"))
        {
            FetchArchive("L2candl", "l_lv2candl");
            CreateParticle(StageLoader.Instance.Fire, new Vector3(0, 120, 0));
        }
        else if (name.Equals("magLifR"))
        {
            int prm2 = (actor.Parameter >> 8) & 0xFF;
            int type = 0;
            if (prm2 == 0 || prm2 == 0xFF)
                type = (actor.Parameter >> 0x10) & 0xFF;

            string[] l_arc_names = {"MagLiftS", "MagLiftM", "MagLiftL"};
            string[] bmd_ids = {"l_maglift_s", "l_maglift_m", "l_maglift_l"};
            int[] btk_ids = new int[]{-1, -1, 12};
            int[] brk_ids = new int[]{-1, -1, 9};
            
            FetchArchive(l_arc_names[type], bmd_ids[type]);
        } else if (name.Equals("marm"))
        {
            BMD a = FetchArchive("D_Marm", "d_marm_a");
            BMD b = FetchArchive("D_Marm", "d_marm_b").TranslateLocal(new Vector3(0, 1200, -175));
            BMD c = FetchArchive("D_Marm", "d_marm_c");
            BMD d = FetchArchive("D_Marm", "d_marm_d").TranslateLocal(new Vector3(0, 2500, 0));
            BMD e = FetchArchive("D_Marm", "d_marm_e").TranslateLocal(new Vector3(-1450, 2530, 0));
            BMD f = FetchArchive("D_Marm", "d_marm_f").TranslateLocal(new Vector3(-1465, 1980, 0));
        } else if (name.Equals("smgdoor"))
        {
            //BMD a = FetchArchiveSeperate("A_SMGDoor", "a_smgdoor").TranslateLocal(new Vector3(-1.443f, 0, 0));
            //BMD b = FetchArchiveSeperate("A_SMGDoor", "a_smgdoor").RotateY(180).TranslateLocal(new Vector3(290f, 0, 0));
            BMD a = FetchArchiveSeperate("TempleOfTimeRightDoor", "A_SMGDoor", "a_smgdoor").RotateY(180).TranslateLocal(new Vector3(150, 0, 0)).SetLocalScale(new Vector3(1.05f, 1f, 1f));
            BMD b = FetchArchiveSeperate("TempleOfTimeLeftDoor", "A_SMGDoor", "a_smgdoor").TranslateLocal(new Vector3(-150, 0, 0)).SetLocalScale(new Vector3(1.05f, 1f, 1f));
        } else if (name.Equals("smkdoor"))
        {
            BMD a = FetchArchiveSeperate("TempleOfTimeRightDoor", "A_SMKDoor", "a_smkdoor").RotateY(180).TranslateLocal(new Vector3(-150, 0, 0)).SetLocalScale(new Vector3(1.05f, 1f, 1f));
            BMD b = FetchArchiveSeperate("TempleOfTimeLeftDoor", "A_SMKDoor", "a_smkdoor").TranslateLocal(new Vector3(150, 0, 0)).SetLocalScale(new Vector3(1.05f, 1f, 1f));
        } else if (name.Equals("Sekizoa"))
        {
            BMD m = FetchArchive("sekizoA", "sekizoa", "seki_still_l");
            FetchArchive("sekizoA", "yaria").SetParentJoint(m, "fingerL").SetLocalPosition(new Vector3(-15.7f,12f,8.4f)).SetLocalRotation(new Vector3(-11.6f, 0.9f, -3.6f)).SetLocalScale(new Vector3(1, 1, -1));
            if (actor.Parameter == 737) m.gameObject.SetActive(false);           // Puzzle
        } else if (name.Equals("Sekizob"))
        {
            BMD m = FetchArchive("sekizoA", "sekizoa", "seki_still_r");
            FetchArchive("sekizoA", "yarib").SetParentJoint(m, "fingerR").SetLocalPosition(new Vector3(-17.7f, 10.4f, 0)).SetLocalRotation(new Vector3(-12.16f, -16.5f, 0)).SetLocalScale(new Vector3(1, 1, -1));
            if (actor.Parameter == 737) m.gameObject.SetActive(false);           // Puzzle
        } else if (name.Equals("mstrsrd"))
        {
            FetchArchive("MstrSword", "o_al_swm");
        } else if (name.Equals("Ice_l"))
        {
            FetchArchive("V_Ice_l", "ice_l");
        } else if (name.Equals("Ice_s"))
        {
            FetchArchive("V_Ice_s", "ice_s");
        }


        // ENEMIES
        else if (name.Equals("Pikari")) FetchArchive("E_hp", "hp");
        else if (name.Equals("E_oc"))
            FetchArchive("E_oc", "oc").PlayAnimationFromDifferentArchive("E_ocb", "oc_wait");
        else if (name.Equals("E_yr")) FetchArchive("E_yr", "yr", "yr_wait");
        else if (name.Equals("E_hb"))
        {
            FetchArchive("E_hb", "hl");
            FetchArchive("E_hb", "hb");
        }
        else if (name.Equals("E_sm2")) FetchArchive("E_sm2", "sm2");

        // Horsespawn
        else if (name.Equals("Hinit")) CreateTag(actor, TagShape.Cube);
        // Link spawn
        else if (name.Equals("Gstart")) CreateTag(actor, TagShape.Cube);
        // Scene change
        else if (name.Equals("scnChg"))
        {
            BMD tag = CreateTag(actor, TagShape.Cube);

            SceneChange change = tag.AddComponent<SceneChange>();
            change.Type = SceneChangeType.AREA;

            change.Init();
        }
        // Cam change
        else if (name.Equals("CamChg")) CreateTag(actor, TagShape.Cube);
        // Room back
        else if (name.Equals("RMback0")) CreateTag(actor, TagShape.Cube);
        // Midna Stop
        else if (name.Equals("Mstop")) CreateTag(actor, TagShape.Cylinder);
        // Midna Stop
        else if (name.Equals("ClearB")) CreateTag(actor, TagShape.Cube);
        
        else if (name.Equals("item"))
        {
            BMD m = null;
            /*if (actor.ItemNo == (int)ItemNo.GREEN_RUPEE) m = FetchArchive("Always", "o_g_rupy").ChangeColor(new UnityEngine.Color(0, 255, 0));
            if (actor.ItemNo == (int)ItemNo.BLUE_RUPEE) m = FetchArchive("Always", "o_g_rupy").ChangeColor(new UnityEngine.Color(0, 0, 255));
            if (actor.ItemNo == (int)ItemNo.YELLOW_RUPEE) m = FetchArchive("Always", "o_g_rupy").ChangeColor(new UnityEngine.Color(255, 255, 0));
            if (actor.ItemNo == (int)ItemNo.RED_RUPEE) m = FetchArchive("Always", "o_g_rupy").ChangeColor(new UnityEngine.Color(255, 0, 0));
            if (actor.ItemNo == (int)ItemNo.PURPLE_RUPEE) m = FetchArchive("Always", "o_g_rupy").ChangeColor(new UnityEngine.Color(155, 0, 255));
            if (actor.ItemNo == (int)ItemNo.SILVER_RUPEE) m = FetchArchive("Always", "o_g_rupy").ChangeColor(new UnityEngine.Color(182, 180, 184));
            if (actor.ItemNo == (int)ItemNo.ORANGE_RUPEE) m = FetchArchive("Always", "o_g_rupy").ChangeColor(new UnityEngine.Color(237, 154, 0));*/
            
            
            if (actor.ItemNo == (int)ItemNo.GREEN_RUPEE) m = FetchArchive("Always", "o_g_rupy").OverwriteMaterial(StageLoader.Instance.OverrideRupy).ChangeEmission(new UnityEngine.Color(0.005671625f, 0.1545f, 0));
            if (actor.ItemNo == (int)ItemNo.BLUE_RUPEE) m = FetchArchive("Always", "o_g_rupy").OverwriteMaterial(StageLoader.Instance.OverrideRupy).ChangeEmission(new UnityEngine.Color(0, 0, 255));
            if (actor.ItemNo == (int)ItemNo.YELLOW_RUPEE) m = FetchArchive("Always", "o_g_rupy").OverwriteMaterial(StageLoader.Instance.OverrideRupy).ChangeEmission(new UnityEngine.Color(0.2470844f, 0.2470844f, 0));
            if (actor.ItemNo == (int)ItemNo.RED_RUPEE) m = FetchArchive("Always", "o_g_rupy").OverwriteMaterial(StageLoader.Instance.OverrideRupy).ChangeEmission(new UnityEngine.Color(255, 0, 0));
            if (actor.ItemNo == (int)ItemNo.PURPLE_RUPEE) m = FetchArchive("Always", "o_g_rupy").OverwriteMaterial(StageLoader.Instance.OverrideRupy).ChangeEmission(new UnityEngine.Color(155, 0, 255));
            if (actor.ItemNo == (int)ItemNo.SILVER_RUPEE)
            {
                m = FetchArchive("Always", "o_g_rupy").OverwriteMaterial(StageLoader.Instance.OverrideRupy).ChangeEmission(new UnityEngine.Color(182, 182, 182));
                m.SetParticles(0, 0x0C14);
            }

            if (actor.ItemNo == (int)ItemNo.ORANGE_RUPEE)
            {
                m = FetchArchive("Always", "o_g_rupy").OverwriteMaterial(StageLoader.Instance.OverrideRupy).ChangeEmission(new UnityEngine.Color(0.3745098f, 0.2433f, 0));
                m.SetParticles(0, 0x0C14);
            }

            if (m == null)
            {
                Debug.LogError("NO RUPY? " + actor.ItemNo);
                return;
            }
            RotateObject rotateObject = m.AddComponent<RotateObject>();
            rotateObject.rotationAxis = RotateObject.RotationAxis.Y;
            rotateObject.rotationSpeed = 2f;

            int itemNo = actor.Parameter & 0xFF;
            int type = (actor.Parameter >> 0x18) & 0xF;
            int switchNo = (actor.Parameter >> 0x10) & 0xFF;
            
            /*string item_res = 
        
            if (globals.item_info[this.itemNo].flag & 2) {
                this.CreateInit(globals);
            } else {
                const item_res = globals.field_item_resource[this.itemNo];
                const arcName = item_res.arcName;
                console.log(this)
                if (arcName === null)
                    return cPhs__Status.Next;

                const status = dComIfG_resLoad(globals, arcName);
                if (status !== cPhs__Status.Complete)
                    return status;

                this.CreateItemHeap(globals, arcName, item_res.bmdID, -1, -1, item_res.bckID, -1, item_res.brkID, -1);
                this.CreateInit(globals);
            }*/
        }
        
        else if (name.Equals("Grass")) CreateEnvironment(StageLoader.Instance.Grass);
        else if (name.Equals("flwr7")) CreateEnvironment(StageLoader.Instance.Flower7);
        else if (name.Equals("flwr17")) CreateEnvironment(StageLoader.Instance.Flower17);
        else if (name.Equals("pflwrx7")) CreateEnvironment(StageLoader.Instance.FlowerLong);
        else if (name.Equals("pflower")) CreateEnvironment(StageLoader.Instance.FlowerLongSmall);
        
        //else if (name.Equals("E_ga")) FetchArchive("E_ga", "ga");
        
        // Barrikade, Paramater?
        else if (name.Equals("mmvbg"))
        {
            int bg_id = actor.Parameter & 0xFFFF;
            string arcName = string.Format("@bg{0}", bg_id.ToString("X4"));

            temp.name = arcName;
            
            FetchArchive(arcName, "model0");      // Big rock
            //FetchArchive("@bg0011", "model1");      // Small root after explosion
            
            //FetchArchive("@bg0011", "model0");      // Big rock
            //FetchArchive("@bg0011", "model1");      // Small root after explosion
        }
            
            /*
                     // Small Chest
        if (model_type === 0x00) fetchArchive(`Dalways`).then((rarc) => {
            const m = buildModel(rarc, `bmdr/boxa.bmd`);
            m.bindTRK1(parseBRK(rarc, `brk/boxa.brk`), animFrame(0));
            m.bindTTK1(parseBTK(rarc, `btk/boxa.btk`));
            m.lightTevColorType = LightType.UNK_16;
        });
        // Big Chest
        else if (model_type === 0x01) fetchArchive(`Dalways`).then((rarc) => {
            const m = buildModel(rarc, `bmdr/boxb.bmd`);
            m.lightTevColorType = LightType.UNK_16;
        });
        // Boss Chest
        else if (model_type === 0x02) fetchArchive(`BoxC`).then((rarc) => {
            const m = buildModel(rarc, `bmdv/boxc.bmd`);
            m.lightTevColorType = LightType.UNK_16;
        });
             */

        //else Debug.LogWarning("Couldn"t fetch bmd from archive: " + name);
    }

    private static BMD CreateTag(Actor actor, TagShape shape)
    {
        BMD tag = null;
        if (shape == TagShape.Cube)
        {
            tag = FetchArchive("K_cube00", "k_size_cube");
        } else if (shape == TagShape.Cylinder) 
        {
            tag = FetchArchive("K_cyli00", "k_size_cylinder");
        }

        return tag;
    }
    
    public static void AssignFields<T>(T source, T target)
    {
        if (source == null || target == null)
            throw new ArgumentNullException("Source or target object cannot be null");

        var fields = typeof(T).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            var value = field.GetValue(source);
            field.SetValue(target, value);
        }

        var properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        foreach (var property in properties)
        {
            if (property.CanRead && property.CanWrite)
            {
                var value = property.GetValue(source);
                property.SetValue(target, value);
            }
        }
    }
    
    private static void CopyComponentsToParent(Transform source, Transform target)
    {
        // Referenz zum aktuellen GameObject und seinem Parent
        GameObject parent = target.gameObject;
        GameObject child = source.gameObject;

        if (parent == null)
        {
            Debug.LogError("Das Objekt hat keinen Parent.");
            return;
        }

        // Alle Komponenten des aktuellen Objekts holen
        Component[] components = child.GetComponents<Component>();

        foreach (Component component in components)
        {
            // Transform nicht kopieren, da dies automatisch existiert
            if (component is Transform) continue;

            // Komponente klonen und auf den Parent übertragen
            Component copiedComponent = parent.AddComponent(component.GetType());

            // Alle Felder und Eigenschaften kopieren
            System.Reflection.FieldInfo[] fields = component.GetType().GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                field.SetValue(copiedComponent, field.GetValue(component));
            }

            System.Reflection.PropertyInfo[] properties = component.GetType().GetProperties(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (property.CanWrite && property.CanRead)
                {
                    property.SetValue(copiedComponent, property.GetValue(component));
                }
            }
        }

        Debug.Log($"Alle Komponenten von '{child.name}' wurden in den Parent '{parent.name}' kopiert.");
    }


    //private static FetchData<BMD> FetchArchive(string arc, string bmd, string animation = "")
    public static void CreateFromExistingBMD(GameObject parent, Cache cache, Actor newActor)
    {
        // Copy object
        if (cache.Bmd != null)
        {
            GameObject copy = StageLoader.Instantiate(cache.Bmd.gameObject);
            copy.name = cache.Bmd.gameObject.name + "_Cashed";
            
            if (FetchAmount == 0) copy.transform.parent = parent.transform;
            else copy.transform.parent = temp.transform;
            
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localEulerAngles = Vector3.zero;
            copy.transform.localScale = Vector3.one;

            BMD bmd = copy.GetComponent<BMD>();
            bmd.Archive = cache.Bmd.Archive;
            bmd.DialogueId = cache.Bmd.DialogueId;
            bmd.Dialogue3DRotation = cache.Bmd.Dialogue3DRotation;
            bmd.lastAnimationName = cache.Bmd.lastAnimationName;
            bmd.specificAnimations = cache.Bmd.specificAnimations;
            bmd.INF1Tag = cache.Bmd.INF1Tag;
            bmd.VTX1Tag = cache.Bmd.VTX1Tag;
            bmd.EVP1Tag = cache.Bmd.EVP1Tag;
            bmd.DRW1Tag = cache.Bmd.DRW1Tag;
            bmd.JNT1Tag = cache.Bmd.JNT1Tag;
            bmd.SHP1Tag = cache.Bmd.SHP1Tag;
            bmd.MAT3Tag = cache.Bmd.MAT3Tag;
            bmd.TEX1Tag = cache.Bmd.TEX1Tag;
            //bmd.Actor.ChestItem = newActor.ChestItem;
            //bmd.Actor.Parameter = newActor.Parameter;
            //bmd.Actor.HexParameter = newActor.HexParameter;
            AssignFields(newActor, bmd.Actor);
            
            Vector3 localPos = copy.transform.position;
            Vector3 localRot = copy.transform.eulerAngles;
            GameObject previousParent = copy.transform.parent.gameObject;
            
            copy.transform.SetParent(copy.transform.parent.parent);
            copy.transform.position = localPos;
            copy.transform.eulerAngles = localRot;
            
            StageLoader.Destroy(previousParent);

            //CopyComponentsToParent(copy.transform, copy.transform.parent);

            
            // Check if model is animated
            if (cache.Bmd.GetComponent<AnimancerComponent>() != null)
            {
                bmd.UseSameAnimationAsOriginalModelForCopy(cache.Bmd);
            }

            /*BMD model = null;
            if(FetchAmount == 0) model = BMD.CopyModel(existingBmd, parent);
            else
            {
                GameObject child = new GameObject(existingBmd.Name);
                child.transform.parent = temp.transform;
                child.transform.localPosition = Vector3.zero;
                child.transform.localScale = Vector3.one;
                model = BMD.CopyModel(existingBmd, child);
            }

            model.CreateShapes();*/
            FetchAmount++;

            /*GameObject copy = GameObject.Instantiate(existingBmd.gameObject);
            copy.transform.parent = parent.transform;
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localEulerAngles = Vector3.zero;
            copy.transform.localScale = Vector3.one;*/

            /*foreach (Transform c in copy.transform)
            {
                c.transform.parent = parent.transform;
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Vector3.zero;
                c.transform.localScale = Vector3.one;
            }

            GameObject.Destroy(copy);*/
        }
    }   

    //private static FetchData<BMD> FetchArchive(string arc, string bmd, string animation = "")
    public static BMD FetchArchive(string arc, string bmd, string animation = "")
    {
        string arcPath = OBJ_PATH + "/" + arc + ".arc";
        
        Archive archive = null;
        if (ARCHIVES.ContainsKey(arc)) archive = ARCHIVES[arc];
        else
        {
            archive = ArcReader.Read(arcPath);
            ARCHIVES.Add(arc, archive);
        }
        
        BMD model = null;
        if (MODELS.ContainsKey(bmd)) model = MODELS[bmd];
        else
        {
            if (FetchAmount == 0)
            {
                model = BMD.CreateModelFromPath(archive, bmd, null, temp);
            }
            else
            {
                GameObject child = new GameObject(bmd);
                child.transform.parent = temp.transform;
                child.transform.localPosition = Vector3.zero;
                child.transform.localScale = Vector3.one;
                model = BMD.CreateModelFromPath(archive, bmd, null, child);
            }
        }

        if (!animation.Equals(""))
        {
            model.PrepareWeights();
            
            AnimationJobManager job = model.AddComponent<AnimationJobManager>();
            job.PlayAnimation(animation);
        }
        
        CacheData.Add(new Cache()
        {
            BmdName = bmd,
            ObjectName = tempOName,
            Actor = model.GetComponent<Actor>(),
            Bmd = model,
            AnimationName = animation
        });
        
        amount++;
        FetchAmount++;
        CurrentBMD = model;
        return model;
    }   

    //private static FetchData<BMD> FetchArchive(string arc, string bmd, string animation = "")
    public static BMD FetchArchive(string arc, int fileId, string animation = "")
    {
            
        string arcPath = OBJ_PATH + "/" + arc + ".arc";
        
        Archive archive = null;
        if (ARCHIVES.ContainsKey(arc)) archive = ARCHIVES[arc];
        else
        {
            archive = ArcReader.Read(arcPath);
            ARCHIVES.Add(arc, archive);
        }
        
        RARC.File file = ArcReader.GetFileById(archive, fileId);
        
        BMD model = null;
        if (MODELS.ContainsKey(file.Name)) model = MODELS[file.Name];
        else
        {
            if (FetchAmount == 0)
            {
                model = BMD.CreateModelFromBuffer(archive, file.Name, file.FileData, null, temp);
            }
            else
            {
                GameObject child = new GameObject(file.Name);
                child.transform.parent = temp.transform;
                child.transform.localPosition = Vector3.zero;
                child.transform.localScale = Vector3.one;
                model = BMD.CreateModelFromBuffer(archive, file.Name, file.FileData, null, child);
            }
        }

        if (!animation.Equals(""))
        {
            model.PrepareWeights();
            
            AnimationJobManager job = model.AddComponent<AnimationJobManager>();
            job.PlayAnimation(animation);
        }
        
        CacheData.Add(new Cache()
        {
            BmdName = file.Name,
            ObjectName = tempOName,
            Actor = model.GetComponent<Actor>(),
            Bmd = model,
            AnimationName = animation
        });
        
        amount++;
        FetchAmount++;
        CurrentBMD = model;
        return model;
        
    }   
    
    public static BMD FetchArchiveSeperate(string child, string arc, string bmd, string animation = "")
    {
        string arcPath = OBJ_PATH + "/" + arc + ".arc";
        
        Archive archive = null;
        if (ARCHIVES.ContainsKey(arc)) archive = ARCHIVES[arc];
        else
        {
            archive = ArcReader.Read(arcPath);
            ARCHIVES.Add(arc, archive);
        }

        GameObject parent = new GameObject(child);
        parent.transform.parent = temp.transform;
        parent.transform.localPosition = Vector3.zero;
        parent.transform.localScale = Vector3.one;

        BMD model = null;
        if (MODELS.ContainsKey(bmd)) model = MODELS[bmd];
        else
        {
            model = BMD.CreateModelFromPath(archive, bmd, null, parent);
            /*if(FetchAmountSeperate == 0) model = BMD.CreateModelFromPath(archive, bmd, null, parent);
            else
            {
                GameObject child = new GameObject(bmd);
                child.transform.parent = parent.transform;
                child.transform.localPosition = temp.transform.localPosition;
                child.transform.localScale = Vector3.one;
                model = BMD.CreateModelFromPath(archive, bmd, null, child);
                model.transform.localScale = new Vector3(0.01f, 0.01f,  0.01f);
                child.transform.localPosition = new Vector3(temp.transform.localPosition.x / 100, temp.transform.localPosition.y, temp.transform.localPosition.z);
            }*/
        }
        /*if (amount == 1)
        {
            GameObject child = new GameObject(bmd);
            child.transform.parent = temp.transform;
            child.transform.localPosition = Vector3.zero;
            model = BMD.CreateModelFromPath(archive, bmd, null, child);
        }
        else
        {
            model = BMD.CreateModelFromPath(archive, bmd, null, temp);
        }*/
        
        if(!animation.Equals("")) model.LoadAnimation(animation);

        amount++;
        FetchAmountSeperate++;
        CurrentBMD = model;
        return model;
    }
    public static BMD FetchFromStage(Archive stage, string bmd, GameObject child = null)
    {
        if (child == null) child = temp;
        BMD model = BMD.CreateModelFromPath(stage, bmd, null, child);
        CurrentBMD = model;
        return model;
    }
    private static BMD FetchFromStageMultiple(Archive stage, string bmd, GameObject multipleParent)
    {
        BMD model = BMD.CreateModelFromPath(stage, bmd, null, multipleParent);
        CurrentBMD = model;
        return model;
    }

    private static Archive FetchAnimation(string arc)
    {
        string arcPath = OBJ_PATH + "/" + arc + ".arc";
        
        Archive archive = null;
        
        if (ARCHIVES.ContainsKey(arc))
        {
            archive = ARCHIVES[arc];
        }
        else
        {
            archive = ArcReader.Read(arcPath);
            ARCHIVES.Add(arc, archive);
        }
        
        return archive;
    }

    public static void CreateParticle(GameObject o, Vector3 offset, float scale = 2)
    {
        GameObject particle = MonoBehaviour.Instantiate(o);
        particle.transform.parent = temp.transform;
        particle.transform.localScale = o.transform.localScale;
        particle.transform.localPosition = offset;
        particle.transform.localScale = new Vector3(scale, scale, scale);
    }

    public static void CreateParticle(GameObject o, float scale = 2)
    {
        GameObject particle = MonoBehaviour.Instantiate(o);
        particle.transform.parent = temp.transform;
        particle.transform.localScale = o.transform.localScale;
        particle.transform.localPosition = new Vector3(0f, 0f, 0f);
        particle.transform.localScale = new Vector3(scale, scale, scale);

        if (o == StageLoader.Instance.Fire)
        {
            GameObject dist = MonoBehaviour.Instantiate(StageLoader.Instance.FireDistortion);
            dist.transform.parent = temp.transform;
            dist.transform.localScale = o.transform.localScale;
            dist.transform.localPosition = new Vector3(0f, 0f, 0f);
            dist.transform.localScale = new Vector3(1, 1, 1);
            
            GameObject smoke = MonoBehaviour.Instantiate(StageLoader.Instance.FireSmoke);
            smoke.transform.parent = temp.transform;
            smoke.transform.localScale = o.transform.localScale;
            smoke.transform.localPosition = new Vector3(0f, 0f, 0f);
            smoke.transform.localScale = new Vector3(1, 1, 1);
        }
    }

    public static void CreateEnvironment(GameObject envrionment)
    {
        GameObject m = StageLoader.Instantiate(envrionment);
        temp.transform.localScale = new Vector3(1, 1, 1);
        m.transform.parent = temp.transform;
        m.transform.localPosition = Vector3.zero;
        m.transform.localScale = new Vector3(1, 1, 1);

        m.AddComponent<FlowerRotation>();
    }
    
    public static int GetParamBit(int parameters, int shift, int bit) {
        return (parameters >> shift) & ((1 << bit) - 1);
    }
    
    public static string LeftPad(string S, int spaces, char ch = '0')
    {
        while (S.Length < spaces)
            S = ch + S;
        return S;
    }

    public static GameObject CreateGoTo(string name, Transform parent, Vector3 pos, Vector3 rot)
    {
        GameObject gotoPointA = new GameObject(name);
        gotoPointA.transform.SetParent(parent);
        gotoPointA.transform.localPosition = pos;
        gotoPointA.transform.localEulerAngles = rot;

        return gotoPointA;
    }

    public static GameObject CreateCamera(string name, Transform parent, Vector3 pos, Vector3 rot)
    {
        // Create virtual camera
        GameObject openCamera = new GameObject(name);
        openCamera.transform.SetParent(parent);
        openCamera.transform.localPosition = pos;
        openCamera.transform.localEulerAngles = rot;
                
        CinemachineVirtualCamera vOpen = openCamera.AddComponent<CinemachineVirtualCamera>();
        vOpen.Priority = 30;
        vOpen.m_Lens.FieldOfView = 60;
        vOpen.enabled = false;

        return openCamera;
    }
}

public class FetchData<T>
{
    public Archive Archive;
    public T Data;

    public FetchData(Archive archive, T data)
    {
        Archive = archive;
        Data = data;
    }
}

public enum TagShape
{
    Cube,
    Cylinder
}

public class MultipleObject
{
    private Archive Archive;
    private List<string> Archives = new List<string>();
    public List<BMD> BMDs = new List<BMD>();

    public void CreateTransforms()
    {
        foreach (string archive in Archives)
        {
            GameObject child = new GameObject(archive);
            child.transform.parent = BMDFetcher.temp.transform;
            child.transform.localPosition = Vector3.zero;
            child.transform.localScale = Vector3.one;
            
            BMD bmd = BMDFetcher.FetchFromStage(Archive, archive, child);
            bmd.transform.parent = child.transform;
            
            BMDs.Add(bmd);
        }
    }
    
    public MultipleObject(Archive archive, params string[] archives)
    {
        Archive = archive;
        Archives.AddRange(archives);
    }
}