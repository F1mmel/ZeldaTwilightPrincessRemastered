using System;
using System.Collections.Generic;
using GameFormatReader.Common;
using UnityEngine;

public class PLC : MonoBehaviour
{
    public byte[] Buffer;

    public List<sBgPc> Codes;
    
    public PLC(byte[] buffer)
    {
        Buffer = buffer;
    }

    public void LoadFromStream()
    {
        using (EndianBinaryReader reader = new EndianBinaryReader(Buffer, Endian.Big))
        {
            reader.ReadUInt32();            // SPLC
            ushort codeSize = reader.ReadUInt16();
            ushort num = reader.ReadUInt16();

            Codes = new List<sBgPc>();
            for (int i = 0; i < num; i++)
            {
                sBgPc sBgPc = new sBgPc()
                {
                    code0 = reader.ReadUInt32(),
                    code1 = reader.ReadUInt32(),
                    code2 = reader.ReadUInt32(),
                    code3 = reader.ReadUInt32(),
                    code4 = reader.ReadUInt32(),
                };

                sBgPc.wallCode = sBgPc.WallCode;
                
                Codes.Add(sBgPc);
            }
        }
    }
    
    [Serializable]
    public class sBgPc
        {
            //[JsonIgnore]
            public uint code0;
            //  [JsonIgnore]
            public uint code1;
            //  [JsonIgnore]
            public uint code2;
            //  [JsonIgnore]
            public uint code3;
            //    [JsonIgnore]
            public uint code4;

            public WallCode wallCode;

            public sBgPc()
            {
                code0 = 0xFF7F;
            }


            public uint LinkNo
            {
                get { return code1.DecodeBit(0, 8); }
                set { code1 = code1.EncodeBit(value, 0, 8); }
            }

            public WallCode WallCode
            {
                get { return (WallCode)code1.DecodeBit(8, 4); }
                set { code1 = code1.EncodeBit((uint)value, 8, 4); }
            }

            public Att0Code Att0Code
            {
                get { return (Att0Code)code1.DecodeBit(12, 4); }
                set { code1 = code1.EncodeBit((uint)value, 12, 4); }
            }

            public Att1Code Att1Code
            {
                get { return (Att1Code)code1.DecodeBit(16, 3); }
                set { code1 = code1.EncodeBit((uint)value, 16, 3); }
            }

            public GroundCode GroundCode
            {
                get { return (GroundCode)Get(code1, 0x13, 0x1F); }
                set { Set(ref code1, (uint)value, 0x13, 0x1F); }
            }

            public uint Room
            {
                get { return Get(code4, 0x14); }
                set { Set(ref code4, value, 0x14); }
            }

            public uint Exit
            {
                get { return BitUtility.DecodeBit(code0, 8, 6); }
                set { code0 = BitUtility.EncodeBit(code0, value, 8, 6); }
            }

            public uint PolyColor
            {
                get { return BitUtility.DecodeBit(code0, 0, 8); }
                set { code0 = BitUtility.EncodeBit(code0, value, 0, 8); }
            }

            public SpecialCode Spl
            {
                get { return (SpecialCode)Get(code0, 0x18, 0xF); }
                set { Set(ref code0, (uint)value, 0x18, 0xF); }
            }

            public bool NoHorseEntry
            {
                get { return GetBool(code0, 0x15, (uint)1); }
                set { Set(ref code0, value, (uint)0x15); }
            }

            public uint Magnet
            {
                get { return Get(code0, 0x1C, 0x3); }
                set { Set(ref code0, value, 0x1C, 0x3); }
            }

            public uint MonkeyBars
            {
                get { return Get(code0, 0x1F); }
                set { Set(ref code0, value, 0x1F); }
            }

            public uint RoomCam
            {
                get { return code2.DecodeBit(0, 8); }
                set { code2 = code2.EncodeBit(value, 0, 8); }
            }

            public uint RoomCamPath
            {
                get { return code2.DecodeBit(0x10, 8); }
                set { code2 = code2.EncodeBit(value, 0x10, 8); }
            }

            public uint RoomCamPathPnt
            {
                get { return code2.DecodeBit(0x18, 8); }
                set { code2 = code2.EncodeBit(value, 0x18, 8); }
            }

            public uint RoomInf
            {
                get { return code4.DecodeBit(0, 8); }
                set { code4 = code4.EncodeBit(value, 0, 8); }
            }

            public SoundID SoundID
            {
                get { return (SoundID)code4.DecodeBit(8, 8); }
                set { code4 = code4.EncodeBit((uint)value, 8, 8); }
            }

            public bool ObjThrough
            {
                get { return GetBool(code0, 0, (uint)dBgPc_ECode.CODE_OBJ_THRU); }
                set { Set(ref code0, value, (uint)dBgPc_ECode.CODE_OBJ_THRU); }
            }

            public bool CameraThrough
            {
                get { return GetBool(code0, 0, (uint)dBgPc_ECode.CODE_CAM_THRU); }
                set { Set(ref code0, value, (uint)dBgPc_ECode.CODE_CAM_THRU); }
            }

            public bool LinkThrough
            {
                get { return GetBool(code0, 0, (uint)dBgPc_ECode.CODE_LINK_THRU); }
                set { Set(ref code0, value, (uint)dBgPc_ECode.CODE_LINK_THRU); }
            }

            public bool ArrowThrough
            {
                get { return GetBool(code0, 0, (uint)dBgPc_ECode.CODE_ARROW_THRU); }
                set { Set(ref code0, value, (uint)dBgPc_ECode.CODE_ARROW_THRU); }
            }

            public bool HSStickThrough
            {
                get { return GetBool(code0, 0, (uint)dBgPc_ECode.CODE_HS_STICK); }
                set { Set(ref code0, value, (uint)dBgPc_ECode.CODE_HS_STICK); }
            }

            public bool BoomerangThrough
            {
                get { return GetBool(code0, 0, (uint)dBgPc_ECode.CODE_BOOMERANG_THRU); }
                set { Set(ref code0, value, (uint)dBgPc_ECode.CODE_BOOMERANG_THRU); }
            }

            public bool RopeThrough
            {
                get { return GetBool(code0, 0, (uint)dBgPc_ECode.CODE_ROPE_THRU); }
                set { Set(ref code0, value, (uint)dBgPc_ECode.CODE_ROPE_THRU); }
            }

            public bool BombThrough
            {
                get { return GetBool(code0, 0, (uint)dBgPc_ECode.CODE_BOMB_THRU); }
                set { Set(ref code0, value, (uint)dBgPc_ECode.CODE_BOMB_THRU); }
            }

            public bool IronBallThrough
            {
                get { return GetBool(code1, 0, (uint)dBgPc_ECode.CODE_IRON_BALL_THRU); }
                set { Set(ref code1, value, (uint)dBgPc_ECode.CODE_IRON_BALL_THRU); }
            }

            public bool ShadowThrough
            {
                get { return GetBool(code0, 0, (uint)dBgPc_ECode.CODE_SHDW_THRU); }
                set { Set(ref code0, value, (uint)dBgPc_ECode.CODE_SHDW_THRU); }
            }

            public bool UnderwaterRoof
            {
                get { return GetBool(code0, 0, (uint)dBgPc_ECode.CODE_UNDERWATER_ROOF); }
                set { Set(ref code0, value, (uint)dBgPc_ECode.CODE_UNDERWATER_ROOF); }
            }

            public bool AttackThrough
            {
                get { return GetBool(code1, 0, (uint)dBgPc_ECode.CODE_ATTACK_THRU); }
                set { Set(ref code1, value, (uint)dBgPc_ECode.CODE_ATTACK_THRU); }
            }

            private bool GetBool(uint flag, int shift, uint bits = 1u)
            {
                return Get(flag, shift, bits) != 0;
            }

            private uint Get(uint flag, int shift, uint bits = 1u)
            {
                if (bits == 1)
                    return flag >> shift;

                return flag >> shift & bits;
            }

            private void Set(ref uint flag, bool v, uint bits = 1u)
            {
                if (v) // If the new value is true
                {
                    // Set the bit by ORing it with the flag
                    flag |= bits;
                }
                else
                {
                    // Clear the bit by ANDing its complement with the flag
                    flag &= ~bits;
                }
            }

            private void Set(ref uint flag, uint v, int shift, uint bits = 1u)
            {
                flag &= ~(bits << shift);
                flag |= (v & bits) << shift;
            }
        }

        public enum GroundCode
        {
            Normal = 0x00,
            Unknown_1 = 0x01,
            Unknown_2 = 0x02,
            Force_Ledge_Hang = 0x03,
            Respawn_Generic = 0x04,
            Unknown_5 = 0x05,
            Unknown_6 = 0x06,
            Unknown_7 = 0x07,
            Slope = 0x08,
            Respawn_2 = 0x09,
            Respawn_3 = 0x0A,
            Unknown_11 = 0x0B,
            Unknown_12 = 0x0C,
            Unknown_13 = 0x0D,
            Unknown_14 = 0x0E,
            Unknown_15 = 0x0F,
            Unknown_16 = 0x10,
            Unknown_17 = 0x11,
            Unknown_18 = 0x12,
            Unknown_19 = 0x13,
            Unknown_20 = 0x14,
            Unknown_21 = 0x15,
            Unknown_22 = 0x16,
            Unknown_23 = 0x17,
            Unknown_24 = 0x18,
            Unknown_25 = 0x19,
            Unknown_26 = 0x1A,
            Unknown_27 = 0x1B,
            Unknown_28 = 0x1C,
            Unknown_29 = 0x1D,
            Unknown_30 = 0x1E,
            Unknown_31 = 0x1F,
            Multi,
            None
        }

        [Serializable]
        public enum WallCode
        {
            Normal = 0x0,
            Climbable_Generic = 0x1,
            Wall = 0x2,
            Grabbable = 0x3,
            Climbable_Ladder = 0x4,
            Ladder_Top = 0x5,
            Unknown_6 = 0x6,
            Unknown_7 = 0x7,
            InvisibleWall = 0x8,
            Unknown_9 = 0x9,
            Unknown_10 = 0xA,
            Unknown_11 = 0xB,
            Unknown_12 = 0xC,
            Unknown_13 = 0xD,
            Unknown_14 = 0xE,
            Unknown_15 = 0xF,
            Multi,
            None
        }

        public enum SpecialCode
        {
            Normal = 0x0,
            Force_Slide_1 = 0x1,
            Force_Slide_2 = 0x2,
            No_Sidle = 0x3,
            Unknown_4 = 0x4,
            Diggable_Black_Soil = 0x5,
            HeavySnow = 0x6,
            Unknown_7 = 0x7,
            Slippery = 0x8,
            Unknown_9 = 0x9,
            Unknown_10 = 0xA,
            Unknown_11 = 0xB,
            Unknown_12 = 0xC,
            Unknown_13 = 0xD,
            Unknown_14 = 0xE,
            Unknown_15 = 0xF,
            Multi,
            None
        }

        public enum Att0Code
        {
            Normal = 0x00,
            Type_1, 
            Type_2, 
            Type_3,
            Type_4, 
            Type_5, 
            Damage_Generic, 
            Water,
            Type_8,
            Type_9,
            Type_10,
            Type_11,
            Damage_SlowMovement, 
            SnowEffects,
            Type_14, 
            Type_15,
        }

        public enum Att1Code
        {
            Normal = 0x00,
            Type_1, 
            Type_2,
            Type_3, 
            Type_4, 
            Type_5, 
            Type_6, 
            Type_7,
        }

        public enum SoundID
        {
            Normal = 0x00,
            Water, 
            Type_2,
            Type_3, 
            Type_4, 
            Type_5, 
            Type_6, 
            Type_7,
            Type_8,
            Type_9,
            Type_10,
            Type_11,
            Type_12, 
            Type_13,
            Type_14, 
            Type_15,
        }

        public enum dBgPc_ECode
        {
            CODE_OBJ_THRU = 0x4000,
            CODE_CAM_THRU = 0x8000,
            CODE_LINK_THRU = 0x10000,
            CODE_ARROW_THRU = 0x20000,
            CODE_HS_STICK = 0x40000,
            CODE_BOOMERANG_THRU = 0x80000,
            CODE_ROPE_THRU = 0x100000,
            CODE_HORSE_NO_ENTRY = 0x200000,
            CODE_SHDW_THRU = 0x400000,
            CODE_BOMB_THRU = 0x800000,
            CODE_IRON_BALL_THRU = 0x1000000,
            CODE_ATTACK_THRU = 0x2000000,
            CODE_UNDERWATER_ROOF = 0x40000000,
        }
}