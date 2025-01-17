using OpenTK;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ZeldaTesting;


#region GX Enums
    public enum GXCompareType
    {
        Never = 0,
        Less = 1,
        Equal = 2,
        LEqual = 3,
        Greater = 4,
        NEqual = 5,
        GEqual = 6,
        Always = 7
    }

    public enum GXAlphaOp
    {
        And = 0,
        Or = 1,
        XOR = 2,
        XNOR = 3
    }

    public enum GXCullMode
    {
        None = 0,
        Front = 1,
        Back = 2,
        All = 3
    }

    public enum GXBlendModeControl
    {
        Zero = 0,
        One = 1,
        SrcColor = 2,
        InverseSrcColor = 3,
        SrcAlpha = 4,
        InverseSrcAlpha = 5,
        DstAlpha = 6,
        InverseDstAlpha = 7
    }

    public enum GXBlendMode
    {
        None = 0,
        Blend = 1,
        Logic = 2,
        Subtract = 3
    }

    public enum GXTevOp
    {
        Add = 0,
        Sub = 1,
        Comp_R8_GT = 8,
        Comp_R8_EQ = 9,
        Comp_GR16_GT = 10,
        Comp_GR16_EQ = 11,
        Comp_BGR24_GT = 12,
        Comp_BGR24_EQ = 13,
        Comp_RGB8_GT = 14,
        Comp_RGB8_EQ = 15,
        Comp_A8_EQ = Comp_RGB8_EQ,
        Comp_A8_GT = Comp_RGB8_GT
    }

    public enum GXTevBias
    {
        Zero = 0,
        AddHalf = 1,
        SubHalf = 2
    }

    public enum GXTevScale
    {
        Scale_1 = 0,
        Scale_2 = 1,
        Scale_4 = 2,
        Divide_2 = 3
    }

    public enum GXCombineColorInput
    {
        ColorPrev = 0,
        AlphaPrev = 1,
        C0 = 2,
        A0 = 3,
        C1 = 4,
        A1 = 5,
        C2 = 6,
        A2 = 7,
        TexColor = 8,
        TexAlpha = 9,
        RasColor = 10,
        RasAlpha = 11,
        One = 12,
        Half = 13,
        Konst = 14,
        Zero = 15
    }

    public enum GXCombineAlphaInput
    {
        AlphaPrev = 0,
        A0 = 1,
        A1 = 2,
        A2 = 3,
        TexAlpha = 4,
        RasAlpha = 5,
        Konst = 6,
        Zero = 7
    }

    public enum GXKonstColorSel
    {
        KCSel_1 = 0x00,
        KCSel_7_8 = 0x01,
        KCSel_3_4 = 0x02,
        KCSel_5_8 = 0x03,
        KCSel_1_2 = 0x04,
        KCSel_3_8 = 0x05,
        KCSel_1_4 = 0x06,
        KCSel_1_8 = 0x07,
        KCSel_K0 = 0x0C,
        KCSel_K1 = 0x0D,
        KCSel_K2 = 0x0E,
        KCSel_K3 = 0x0F,
        KCSel_K0_R = 0x10,
        KCSel_K1_R = 0x11,
        KCSel_K2_R = 0x12,
        KCSel_K3_R = 0x13,
        KCSel_K0_G = 0x14,
        KCSel_K1_G = 0x15,
        KCSel_K2_G = 0x16,
        KCSel_K3_G = 0x17,
        KCSel_K0_B = 0x18,
        KCSel_K1_B = 0x19,
        KCSel_K2_B = 0x1A,
        KCSel_K3_B = 0x1B,
        KCSel_K0_A = 0x1C,
        KCSel_K1_A = 0x1D,
        KCSel_K2_A = 0x1E,
        KCSel_K3_A = 0x1F
    }

    public enum GXKonstAlphaSel
    {
        KASel_1 = 0x00,
        KASel_7_8 = 0x01,
        KASel_3_4 = 0x02,
        KASel_5_8 = 0x03,
        KASel_1_2 = 0x04,
        KASel_3_8 = 0x05,
        KASel_1_4 = 0x06,
        KASel_1_8 = 0x07,
        KASel_K0_R = 0x10,
        KASel_K1_R = 0x11,
        KASel_K2_R = 0x12,
        KASel_K3_R = 0x13,
        KASel_K0_G = 0x14,
        KASel_K1_G = 0x15,
        KASel_K2_G = 0x16,
        KASel_K3_G = 0x17,
        KASel_K0_B = 0x18,
        KASel_K1_B = 0x19,
        KASel_K2_B = 0x1A,
        KASel_K3_B = 0x1B,
        KASel_K0_A = 0x1C,
        KASel_K1_A = 0x1D,
        KASel_K2_A = 0x1E,
        KASel_K3_A = 0x1F
    }

    public enum GXTexGenSrc
    {
        Position = 0,
        Normal = 1,
        Binormal = 2,
        Tangent = 3,
        Tex0 = 4,
        Tex1 = 5,
        Tex2 = 6,
        Tex3 = 7,
        Tex4 = 8,
        Tex5 = 9,
        Tex6 = 10,
        Tex7 = 11,
        TexCoord0 = 12,
        TexCoord1 = 13,
        TexCoord2 = 14,
        TexCoord3 = 15,
        TexCoord4 = 16,
        TexCoord5 = 17,
        TexCoord6 = 18,
        Color0 = 19,
        Color1 = 20,
    }

    public enum GXTexGenType
    {
        Matrix3x4 = 0,
        Matrix2x4 = 1,
        Bump0 = 2,
        Bump1 = 3,
        Bump2 = 4,
        Bump3 = 5,
        Bump4 = 6,
        Bump5 = 7,
        Bump6 = 8,
        Bump7 = 9,
        SRTG = 10
    }

    public enum GXTexMatrix
    {
        TexMtx0 = 30,
        TexMtx1 = 33,
        TexMtx2 = 36,
        TexMtx3 = 39,
        TexMtx4 = 42,
        TexMtx5 = 45,
        TexMtx6 = 48,
        TexMtx7 = 51,
        TexMtx8 = 54,
        TexMtx9 = 57,
        Identity = 60,
    }

    public enum GXPrimitiveType
    {
        Points = 0xB8,
        Lines = 0xA8,
        LineStrip = 0xB0,
        Triangles = 0x90,
        TriangleStrip = 0x98,
        TriangleFan = 0xA0,
        Quads = 0x80,
    }

    [Flags]
    public enum GXLightMask
    {
        Light0 = 0x001,
        Light1 = 0x002,
        Light2 = 0x004,
        Light3 = 0x008,
        Light4 = 0x010,
        Light5 = 0x020,
        Light6 = 0x040,
        Light7 = 0x080,
        None = 0x000
    }

    public enum GXDiffuseFunction
    {
        None = 0,
        Signed = 1,
        Clamp = 2
    }

    public enum GXAttenuationFunction
    {
        None = 2,

        Spec = 0,

        Spot = 1
    }

    public enum GXColorSrc
    {
        Register = 0,
        Vertex = 1
    }

    public enum GXLogicOp
    {
        Clear = 0,
        And = 1,
        Copy = 3,
        Equiv = 9,
        Inv = 10,
        InvAnd = 4,
        InvCopy = 12,
        InvOr = 13,
        NAnd = 14,
        NoOp = 5,
        NOr = 8,
        Or = 7,
        RevAnd = 2,
        RevOr = 11,
        Set = 15,
        XOr = 6
    }

    public enum GXTexCoordSlot
    {
        TexCoord0 = 0,
        TexCoord1 = 1,
        TexCoord2 = 2,
        TexCoord3 = 3,
        TexCoord4 = 4,
        TexCoord5 = 5,
        TexCoord6 = 6,
        TexCoord7 = 7,
        Null = 0xFF
    }

    public enum GXColorChannelId
    {
        Color0 = 0,
        Color1 = 1,
        Alpha0 = 2,
        Alpha1 = 3,
        Color0A0 = 4,
        Color1A1 = 5,
        ColorZero = 6,
        AlphaBump = 7,
        AlphaBumpN = 8,
        ColorNull = 0xFF,
    }

    public enum GXRegister
    {
        Prev = 0,
        Reg0 = 1,
        Reg1 = 2,
        Reg2 = 3,
    }
    #endregion

    #region Material Classes
    public class ZMode
    {
        public bool Enable;

        public GXCompareType Function;

        public bool UpdateEnable;

        public override string ToString()
        {
            return string.Format("Enabled: {0} Function: {1} UpdateEnable: {2}", Enable, Function, UpdateEnable);
        }
    }

    public class AlphaTest
    {
        public GXCompareType Comp0;

        public byte Reference0;

        public GXAlphaOp Operation;

        public GXCompareType Comp1;

        public byte Reference1;

        public override string ToString()
        {
            return string.Format("Compare: {0} Ref: {1} Op: {2} Compare: {3} Reference: {4}", Comp0, Reference0, Operation, Comp1, Reference1);
        }
    }

    public class BlendMode
    {
        public GXBlendMode Type;

        public GXBlendModeControl SourceFactor;

        public GXBlendModeControl DestinationFactor;

        public GXLogicOp Operation;

        public override string ToString()
        {
            return string.Format("Blend Type: {0} Src: {1} Dest: {2} Op: {3}", Type, SourceFactor, DestinationFactor, Operation);
        }
    }

    public class ColorChannelControl : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool LightingEnabled { get { return m_lightingEnabled; } set { m_lightingEnabled = value; OnPropertyChanged(); } }

        public GXColorSrc MaterialSrc { get { return m_materialSrc; } set { m_materialSrc = value; OnPropertyChanged(); } }

        public GXLightMask LitMask { get { return m_litMask; } set { m_litMask = value; OnPropertyChanged(); } }

        public GXDiffuseFunction DiffuseFunction { get { return m_diffuseFunction; } set { m_diffuseFunction = value; OnPropertyChanged(); } }

        public GXAttenuationFunction AttenuationFunction { get { return m_attenuationFunction; } set { m_attenuationFunction = value; OnPropertyChanged(); } }

        public GXColorSrc AmbientSrc { get { return m_ambientSrc; } set { m_ambientSrc = value; OnPropertyChanged(); } }

        private bool m_lightingEnabled;
        private GXColorSrc m_materialSrc;
        private GXLightMask m_litMask;
        private GXDiffuseFunction m_diffuseFunction;
        private GXAttenuationFunction m_attenuationFunction;
        private GXColorSrc m_ambientSrc;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TexCoordGen
    {
        public GXTexGenType Type;
        public GXTexGenSrc Source;
        public GXTexMatrix TexMatrixSource;
		
		public TexCoordGen()
		{
			Type = GXTexGenType.Matrix3x4;
			Source = GXTexGenSrc.TexCoord0;
			TexMatrixSource = GXTexMatrix.TexMtx0;
		}
    }

    public enum TexMatrixProjection
    {
        TexProj_ST = 0,
        TexProj_STQ = 1
    }

    public class TexMatrix
    {
        public TexMatrixProjection Projection;
        public byte Type;
        public float CenterS;
        public float CenterT;
        public float CenterW;
        public float ScaleS;
        public float ScaleT;
        public float Rotation;
        public float TranslateS;
        public float TranslateT;
        public Matrix4 Matrix;

        public Matrix4 TexMtx
        {
            get
            {
				Matrix4 S = Matrix4.CreateScale(ScaleS, ScaleT, 1f);
                Matrix4 R = Matrix4.CreateRotationX(Rotation);
                Matrix4 T = Matrix4.CreateTranslation(new Vector3(TranslateS, TranslateT, 0));

                Matrix4 C = Matrix4.CreateTranslation(CenterS, CenterT, CenterW);
                Matrix4 invC = Matrix4.CreateTranslation(-CenterS, -CenterT, -CenterW);

                S = invC * S * C;
                R = invC * R * C;

                var mat = Matrix * (S * R * T);

                return mat;
			}
        }

        private Matrix4 GetTextureMatrixOld()
        {
            float sine = (float)Math.Sin(Rotation);
            float cosine = (float)Math.Cos(Rotation);

            Matrix4 outMat = new Matrix4();

            outMat.M11 = (ScaleS * cosine);
            outMat.M12 = (-ScaleS * sine);
            outMat.M13 = (0.0f);
            outMat.M14 = (TranslateS + CenterS + CenterS * -ScaleS * cosine + CenterT * ScaleS * sine);

            outMat.M21 = (ScaleT * sine);
            outMat.M22 = (ScaleT * cosine);
            outMat.M23 = 0.0f;
            outMat.M24 = (TranslateT + CenterT + CenterS * -ScaleT * sine - CenterT * ScaleT * cosine);

            outMat.M31 = 0.0f;
            outMat.M32 = 0.0f;
            outMat.M33 = 1.0f;
            outMat.M34 = 0.0f;

            outMat.M41 = 0.0f;
            outMat.M42 = 0.0f;
            outMat.M43 = 0.0f;
            outMat.M44 = 1.0f;
            outMat.Transpose();

            return outMat;
        }
    }

    public class TevIn
    {
        public byte A;
        public byte B;
        public byte C;
        public byte D;
    }

    public class TevOp
    {
        public byte Operation;
        public byte Bias;
        public byte Scale;
        public byte Clamp;
        public byte Out;
    }

    public class TevOrder
    {
        public GXTexCoordSlot TexCoordId;
        public byte TexMap;
        public GXColorChannelId ChannelId;

        public override string ToString()
        {
            return string.Format("TexCoord: {0} TexMap: {1} ColorChan: {2}", TexCoordId, TexMap, ChannelId);
        }
    }

    public class TevIndirect
    {
        public byte IndStage;
        public byte Format;
        public byte Bias;
        public byte IndMatrix;
        public byte WrapS;
        public byte WrapT;
        public byte AddPrev;
        public byte Utclod;
        public byte Alpha;
    }

    public class NBTScale
    {
        public byte Unknown1;
        public Vector3 Scale;

        public NBTScale() { }

        public NBTScale(byte unknown, Vector3 scale)
        {
            Unknown1 = unknown;
            Scale = scale;
        }
    }

    public class TevSwapMode
    {
        public byte RasSel;
        public byte TexSel;
    }

    [Serializable]
    public class TevSwapModeTable
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public override string ToString()
        {
            return string.Format("[{0}, {1}, {2}, {3}]", R, G, B, A);
        }
    }

[Serializable]
    public class IndTexOrder
    {
        public byte TexCoord;
        public byte TexMap;
    }

    public class IndTexCoordScale
    {
        public byte ScaleS;
        public byte ScaleT;
    }

    public class TevStage
    {
        public byte Unknown0;
        public GXCombineColorInput[] ColorIn;
        public GXTevOp ColorOp;
        public GXTevBias ColorBias;
        public GXTevScale ColorScale;
        public bool ColorClamp;
        public GXRegister ColorRegister;
        public GXCombineAlphaInput[] AlphaIn;
        public GXTevOp AlphaOp;
        public GXTevBias AlphaBias;
        public GXTevScale AlphaScale;
        public bool AlphaClamp;
        public GXRegister AlphaRegister;
        public byte Unknown1;

        public TevStage()
        {
            ColorIn = new GXCombineColorInput[4];
            AlphaIn = new GXCombineAlphaInput[4];
        }
    }

    public class FogInfo
    {
        public byte Type;
        public bool Enable;
        public ushort Center;
        public float StartZ;
        public float EndZ;
        public float NearZ;
        public float FarZ;
        public WLinearColor Color;

        public float[] RangeAdjustmentTable;
    }

    public class IndirectTexture
    {
        public bool HasLookup;

        public byte IndTexStageNum;

        public ushort Unknown0;

        public byte Unknown1;

        public byte Unknown2;

        public ushort[] Unknown3;

        public IndirectTextureMatrix[] Matrices;

        public IndirectTextureScale[] Scales;

        public IndirectTevOrder[] TevOrders;

        public IndirectTexture()
        {
            Matrices = new IndirectTextureMatrix[3];
            Scales = new IndirectTextureScale[4];
            TevOrders = new IndirectTevOrder[16];
            Unknown3 = new ushort[7];
        }
    }

    public class IndirectTextureMatrix
    {
        public Matrix2x3 Matrix;
        public byte ScaleExponent;

        public IndirectTextureMatrix(Matrix2x3 matrix, byte scaleExponent)
        {
            Matrix = matrix;
            ScaleExponent = scaleExponent;
        }
    }

    public class IndirectTextureScale
    {
        public byte ScaleS;

        public byte ScaleT;

        public IndirectTextureScale(byte scaleS, byte scaleT)
        {
            ScaleS = scaleS;
            ScaleT = scaleT;
        }
    }

    public class IndirectTevOrder
    {
        public byte TevStageID;
        public byte IndTexFormat;
        public byte IndTexBiasSel;
        public byte IndTexMtxId;
        public byte IndTexWrapS;
        public byte IndTexWrapT;
        public bool AddPrev;
        public bool UtcLod;
        public byte AlphaSel;
    }
    #endregion