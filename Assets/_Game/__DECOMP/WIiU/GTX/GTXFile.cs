using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.IO;
using System.Linq;
using static Toolbox.Library.GX2;
using System.ComponentModel;
using Syroot.BinaryData;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO.Compression;
using System.Runtime.InteropServices;
using DirectXStructs;
using Debug = UnityEngine.Debug;
using Image = System.Drawing.Image;


public enum BlockType : uint
{
    Invalid = 0x00,
    EndOfFile = 0x01,
    AlignData = 0x02,
    VertexShaderHeader = 0x03,
    VertexShaderProgram = 0x05,
    PixelShaderHeader = 0x06,
    PixelShaderProgram = 0x07,
    GeometryShaderHeader = 0x08,
    GeometryShaderProgram = 0x09,
    GeometryShaderProgram2 = 0x10,
    ImageInfo = 0x11,
    ImageData = 0x12,
    MipData = 0x13,
    ComputeShaderHeader = 0x14,
    ComputeShader = 0x15,
    UserBlock = 0x16
}

public class GTXFile
{
    public bool CanSave { get; set; }
    public string[] Description { get; set; } = new string[] { "GTX" };
    public string[] Extension { get; set; } = new string[] { "*.gtx" };
    public string FileName { get; set; }
    public string FilePath { get; set; }

    public Type[] Types
    {
        get
        {
            var types = new List<Type>();
            return types.ToArray();
        }
    }

    public bool DisplayIcons => false;

    private GTXHeader header;

    public List<byte[]> data = new();
    public List<byte[]> mipMaps = new();
    public List<TextureData> textures = new();

    public List<GTXDataBlock> blocks = new();

    public void Load(string file)
    {
        CanSave = true;

        ReadGx2(new FileReader(file));

        Console.WriteLine("Textures: " + textures.Count);
        foreach (var image in textures)
        {
            Console.WriteLine(image.surface.mipData.Length);
            Console.WriteLine(image.surface.data.Length);
        }
    }

    public void Load(byte[] data)
    {
        CanSave = true;

        using (var memoryStream = new MemoryStream(data))
        {
            ReadGx2(new FileReader(memoryStream));
        }

        Console.WriteLine("Textures: " + textures.Count);
        foreach (var image in textures)
        {
            Console.WriteLine(image.surface.mipData.Length);
            Console.WriteLine(image.surface.data.Length);
        }
    }

    public void Save(Stream stream)
    {
        using (var writer = new FileWriter(stream, true))
        {
            writer.ByteOrder = ByteOrder.BigEndian;
            header.Write(writer);

            uint surfBlockType;
            uint dataBlockType;
            uint mipBlockType;

            if (header.MajorVersion == 6 && header.MinorVersion == 0)
            {
                surfBlockType = 0x0A;
                dataBlockType = 0x0B;
                mipBlockType = 0x0C;
            }
            else if (header.MajorVersion == 6 || header.MajorVersion == 7)
            {
                surfBlockType = 0x0B;
                dataBlockType = 0x0C;
                mipBlockType = 0x0D;
            }
            else
            {
                throw new Exception($"Unsupported GTX version {header.MajorVersion}");
            }

            var imageInfoIndex = -1;
            var imageBlockIndex = -1;
            var imageMipBlockIndex = -1;

            writer.Seek(header.HeaderSize, SeekOrigin.Begin);
            foreach (var block in blocks)
                if ((uint)block.BlockType == surfBlockType)
                {
                    imageInfoIndex++;
                    imageBlockIndex++;
                    imageMipBlockIndex++;

                    block.data = textures[imageInfoIndex].surface.Write();
                    block.Write(writer);
                }
                else if ((uint)block.BlockType == dataBlockType)
                {
                    var tex = textures[imageBlockIndex];

                    var pos = writer.Position;
                    var Alignment = tex.surface.alignment;
                    //Create alignment block first
                    var dataAlignment = GetAlignBlockSize((uint)pos + 32, Alignment);
                    var dataAlignBlock = new GTXDataBlock(BlockType.AlignData, dataAlignment, 0, 0);
                    dataAlignBlock.Write(writer);

                    block.data = tex.surface.data;
                    block.Write(writer);
                }
                else if ((uint)block.BlockType == mipBlockType)
                {
                    var tex = textures[imageMipBlockIndex];

                    var pos = writer.Position;
                    var Alignment = tex.surface.alignment;
                    //Create alignment block first
                    var dataAlignment = GetAlignBlockSize((uint)pos + 32, Alignment);
                    var dataAlignBlock = new GTXDataBlock(BlockType.AlignData, dataAlignment, 0, 0);
                    dataAlignBlock.Write(writer);

                    if (tex.surface.mipData == null || tex.surface.mipData.Length <= 0)
                        throw new Exception("Invalid mip data!");

                    block.data = tex.surface.mipData;
                    block.Write(writer);
                }
                else if (block.BlockType != BlockType.AlignData)
                {
                    block.Write(writer);
                }
        }
    }

    private static uint GetAlignBlockSize(uint DataOffset, uint Alignment)
    {
        var alignSize = RoundUp(DataOffset, Alignment) - DataOffset - 32;

        uint z = 1;
        while (alignSize < 0)
            alignSize = RoundUp(DataOffset + Alignment * z, Alignment) - DataOffset - 32;
        z += 1;

        return alignSize;
    }

    private static uint RoundUp(uint X, uint Y)
    {
        return ((X - 1) | (Y - 1)) + 1;
    }

    private void ReadGx2(FileReader reader)
    {
        reader.ByteOrder = ByteOrder.BigEndian;

        header = new GTXHeader();
        header.Read(reader);

        Console.WriteLine("header size " + header.HeaderSize);

        uint surfBlockType;
        uint dataBlockType;
        uint mipBlockType;
        uint vertexShaderHeader = 0x03;
        uint vertexShaderProgram = 0x05;
        uint pixelShaderHeader = 0x06;
        uint pixelShaderProgram = 0x07;
        uint geometryShaderHeader = 0x08;
        uint geometryShaderProgram = 0x09;
        uint userDataBlock = 0x10;

        if (header.MajorVersion == 6 && header.MinorVersion == 0)
        {
            surfBlockType = 0x0A;
            dataBlockType = 0x0B;
            mipBlockType = 0x0C;
        }
        else if (header.MajorVersion == 6 || header.MajorVersion == 7)
        {
            surfBlockType = 0x0B;
            dataBlockType = 0x0C;
            mipBlockType = 0x0D;
        }
        else
        {
            throw new Exception($"Unsupported GTX version {header.MajorVersion}");
        }

        if (header.GpuVersion != 2)
            throw new Exception($"Unsupported GPU version {header.GpuVersion}");

        reader.Position = header.HeaderSize;

        var blockB = false;
        var blockC = false;

        uint ImageInfo = 0;
        uint images = 0;

        while (reader.Position < reader.BaseStream.Length)
        {
            Console.WriteLine("BLOCK POS " + reader.Position + " " + reader.BaseStream.Length);
            var block = new GTXDataBlock();
            block.Read(reader);
            blocks.Add(block);

            var BlockIsEmpty = block.BlockType == BlockType.AlignData ||
                               block.BlockType == BlockType.EndOfFile;

            //Here we use "if" instead of "case" statements as types vary between versions
            if ((uint)block.BlockType == surfBlockType)
            {
                ImageInfo += 1;
                blockB = true;

                var surface = new SurfaceInfoParse();
                surface.Read(new FileReader(block.data));

                if (surface.tileMode == 0 || surface.tileMode > 16)
                    throw new Exception($"Invalid tileMode {surface.tileMode}!");

                if (surface.numMips > 14)
                    throw new Exception($"Invalid number of mip maps {surface.numMips}!");

                var textureData = new TextureData();
                textureData.surface = surface;
                textureData.MipCount = surface.numMips;
                textureData.ArrayCount = surface.depth;
                //textureData.Text = "Texture" + ImageInfo;
                textures.Add(textureData);
            }
            else if ((uint)block.BlockType == dataBlockType)
            {
                images += 1;
                blockC = true;

                data.Add(block.data);
            }
            else if ((uint)block.BlockType == mipBlockType)
            {
                mipMaps.Add(block.data);
            }
        }

        if (textures.Count != data.Count)
            throw new Exception($"Bad size! {textures.Count} {data.Count}");

        var curTex = 0;
        var curMip = 0;

        for (var i = 0; i < textures.Count; i++)
        {
            var tex = textures[i];

            tex.surface.data = data[curTex];
            tex.surface.bpp = surfaceGetBitsPerPixel(tex.surface.format) >> 3;
            tex.Format = ConvertFromGx2Format((Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat)tex.surface.format);
            tex.Width = tex.surface.width;
            tex.Height = tex.surface.height;

            if (tex.surface.numMips > 1)
                tex.surface.mipData = mipMaps[curMip++];
            else
                tex.surface.mipData = new byte[0];

            if (tex.surface.mipData == null)
                tex.surface.numMips = 1;

            curTex++;
        }
    }

    public static TEX_FORMAT ConvertFromGx2Format(Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat GX2Format)
    {
        switch (GX2Format)
        {
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.T_BC1_UNorm: return TEX_FORMAT.BC1_UNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.T_BC1_SRGB: return TEX_FORMAT.BC1_UNORM_SRGB;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.T_BC2_UNorm: return TEX_FORMAT.BC2_UNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.T_BC2_SRGB: return TEX_FORMAT.BC2_UNORM_SRGB;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.T_BC3_UNorm: return TEX_FORMAT.BC3_UNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.T_BC3_SRGB: return TEX_FORMAT.BC3_UNORM_SRGB;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.T_BC4_UNorm: return TEX_FORMAT.BC4_UNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.T_BC4_SNorm: return TEX_FORMAT.BC4_SNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.T_BC5_UNorm: return TEX_FORMAT.BC5_UNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.T_BC5_SNorm: return TEX_FORMAT.BC5_SNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TC_R5_G5_B5_A1_UNorm: return TEX_FORMAT.B5G5R5A1_UNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TC_A1_B5_G5_R5_UNorm: return TEX_FORMAT.B5G5R5A1_UNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TC_R4_G4_B4_A4_UNorm: return TEX_FORMAT.B4G4R4A4_UNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TCS_R5_G6_B5_UNorm: return TEX_FORMAT.B5G6R5_UNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TCS_R8_G8_B8_A8_SRGB:
                return TEX_FORMAT.R8G8B8A8_UNORM_SRGB;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TCS_R8_G8_B8_A8_UNorm: return TEX_FORMAT.R8G8B8A8_UNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TCS_R10_G10_B10_A2_UNorm:
                return TEX_FORMAT.R10G10B10A2_UNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TC_R11_G11_B10_Float: return TEX_FORMAT.R11G11B10_FLOAT;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TCD_R16_UNorm: return TEX_FORMAT.R16_UNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TCD_R32_Float: return TEX_FORMAT.R32_FLOAT;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.T_R4_G4_UNorm: return TEX_FORMAT.R4G4_UNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TC_R8_G8_UNorm: return TEX_FORMAT.R8G8_UNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TC_R8_G8_SNorm: return TEX_FORMAT.R8G8_SNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TC_R8_UNorm: return TEX_FORMAT.R8_UNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TC_R8_SNorm: return TEX_FORMAT.R8_SNORM;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TC_R32_G32_B32_A32_Float:
                return TEX_FORMAT.R32G32B32A32_FLOAT;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TC_R16_G16_B16_A16_Float:
                return TEX_FORMAT.R16G16B16A16_FLOAT;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TC_R32_G32_B32_A32_SInt:
                return TEX_FORMAT.R32G32B32A32_SINT;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.TC_R32_G32_B32_A32_UInt:
                return TEX_FORMAT.R32G32B32A32_UINT;
            case Syroot.NintenTools.Bfres.GX2.GX2SurfaceFormat.Invalid: throw new Exception("Invalid Format");
            default:
                throw new Exception($"Cannot convert format {GX2Format}");
        }
    }


    public class GTXHeader
    {
        private readonly string Magic = "Gfx2";
        public uint HeaderSize;
        public uint MajorVersion;
        public uint MinorVersion;
        public uint GpuVersion;
        public uint AlignMode;

        public void Read(FileReader reader)
        {
            var Signature = reader.ReadString(4, Encoding.ASCII);
            if (Signature != Magic)
                throw new Exception($"Invalid signature {Signature}! Expected Gfx2.");

            HeaderSize = reader.ReadUInt32();
            MajorVersion = reader.ReadUInt32();
            MinorVersion = reader.ReadUInt32();
            GpuVersion = reader.ReadUInt32(); //Ignored in 6.0
            AlignMode = reader.ReadUInt32();
        }

        public void Write(FileWriter writer)
        {
            writer.WriteSignature(Magic);
            writer.Write(HeaderSize);
            writer.Write(MajorVersion);
            writer.Write(MinorVersion);
            writer.Write(GpuVersion);
            writer.Write(AlignMode);
        }
    }

    public class GTXDataBlock
    {
        private readonly string Magic = "BLK{";
        public uint HeaderSize;
        public uint MajorVersion;
        public uint MinorVersion;
        public BlockType BlockType;
        public uint Identifier;
        public uint Index;
        public uint DataSize;
        public byte[] data;

        public GTXDataBlock()
        {
        }

        public GTXDataBlock(BlockType blockType, uint dataSize, uint identifier, uint index)
        {
            HeaderSize = 32;
            MajorVersion = 1;
            MinorVersion = 0;
            BlockType = blockType;
            DataSize = dataSize;
            Identifier = identifier;
            Index = index;
            data = new byte[dataSize];
        }

        public void Read(FileReader reader)
        {
            var blockStart = reader.Position;

            var Signature = reader.ReadString(4, Encoding.ASCII);
            if (Signature != Magic)
                throw new Exception($"Invalid signature {Signature}! Expected BLK.");

            HeaderSize = reader.ReadUInt32();
            MajorVersion = reader.ReadUInt32(); //Must be 0x01 for 6.x
            MinorVersion = reader.ReadUInt32(); //Must be 0x00 for 6.x
            BlockType = reader.ReadEnum<BlockType>(false);
            DataSize = reader.ReadUInt32();
            Identifier = reader.ReadUInt32();
            Index = reader.ReadUInt32();

            reader.Seek(blockStart + HeaderSize, SeekOrigin.Begin);
            data = reader.ReadBytes((int)DataSize);
            Console.WriteLine(data.Length);
        }

        public void Write(FileWriter writer)
        {
            if (data.Length == 0) data = new byte[(int)DataSize];

            Console.WriteLine("DataSize: " + DataSize);
            Console.WriteLine("DataSize: " + data);
            var blockStart = writer.Position;

            writer.WriteSignature(Magic);
            writer.Write(HeaderSize);
            writer.Write(MajorVersion);
            writer.Write(MinorVersion);
            writer.Write(BlockType, false);
            writer.Write(data.Length);
            writer.Write(Identifier);
            writer.Write(Index);
            writer.Seek(blockStart + HeaderSize, SeekOrigin.Begin);

            writer.Write(data);
        }
    }

    public class TextureData : STGenericTexture
    {
        public override TEX_FORMAT[] SupportedFormats
        {
            get
            {
                return new TEX_FORMAT[]
                {
                    TEX_FORMAT.BC1_UNORM,
                    TEX_FORMAT.BC1_UNORM_SRGB,
                    TEX_FORMAT.BC2_UNORM,
                    TEX_FORMAT.BC2_UNORM_SRGB,
                    TEX_FORMAT.BC3_UNORM,
                    TEX_FORMAT.BC3_UNORM_SRGB,
                    TEX_FORMAT.BC4_UNORM,
                    TEX_FORMAT.BC4_SNORM,
                    TEX_FORMAT.BC5_UNORM,
                    TEX_FORMAT.BC5_SNORM,
                    TEX_FORMAT.B5G5R5A1_UNORM,
                    TEX_FORMAT.B5G6R5_UNORM,
                    TEX_FORMAT.B8G8R8A8_UNORM_SRGB,
                    TEX_FORMAT.B8G8R8A8_UNORM,
                    TEX_FORMAT.R10G10B10A2_UNORM,
                    TEX_FORMAT.R16_UNORM,
                    TEX_FORMAT.B4G4R4A4_UNORM,
                    TEX_FORMAT.R8G8B8A8_UNORM_SRGB,
                    TEX_FORMAT.R8G8B8A8_UNORM,
                    TEX_FORMAT.R8_UNORM,
                    TEX_FORMAT.R8G8_UNORM,
                    TEX_FORMAT.R32G8X24_FLOAT
                };
            }
        }

        public override bool CanEdit { get; set; } = true;

        public SurfaceInfoParse surface;

        public TextureData()
        {
        }

        private void ApplySurface(GX2Surface NewSurface)
        {
            surface.aa = NewSurface.aa;
            surface.alignment = NewSurface.alignment;
            surface.bpp = NewSurface.bpp;
            surface.compSel = NewSurface.compSel;
            surface.data = NewSurface.data;
            surface.depth = NewSurface.depth;
            surface.dim = NewSurface.dim;
            surface.firstMip = NewSurface.firstMip;
            surface.firstSlice = NewSurface.firstSlice;
            surface.format = NewSurface.format;
            surface.height = NewSurface.height;
            surface.imageCount = NewSurface.imageCount;
            surface.imageSize = NewSurface.imageSize;
            surface.mipData = NewSurface.mipData;
            surface.mipSize = NewSurface.mipSize;
            surface.mipOffset = NewSurface.mipOffset;
            surface.numArray = NewSurface.numArray;
            surface.numMips = NewSurface.numMips;
            surface.pitch = NewSurface.pitch;
            surface.resourceFlags = NewSurface.resourceFlags;
            surface.swizzle = NewSurface.swizzle;
            surface.tileMode = NewSurface.tileMode;
            surface.use = NewSurface.use;
            surface.width = NewSurface.width;
            surface.texRegs = NewSurface.texRegs;

            SetChannelComponents();
        }

        private STChannelType SetChannel(byte compSel)
        {
            if (compSel == 0) return STChannelType.Red;
            else if (compSel == 1) return STChannelType.Green;
            else if (compSel == 2) return STChannelType.Blue;
            else if (compSel == 3) return STChannelType.Alpha;
            else if (compSel == 4) return STChannelType.Zero;
            else return STChannelType.One;
        }

        private void SetChannelComponents()
        {
            surface.compSel = new byte[4] { 0, 1, 2, 3 };
        }

        public override void SetImageData(Bitmap bitmap, int ArrayLevel)
        {
            if (bitmap == null)
                return; //Image is likely disposed and not needed to be applied

            RedChannel = SetChannel(surface.compSel[0]);
            GreenChannel = SetChannel(surface.compSel[1]);
            BlueChannel = SetChannel(surface.compSel[2]);
            AlphaChannel = SetChannel(surface.compSel[3]);

            //surface.format = (uint)FTEX.ConvertToGx2Format(Format);
            surface.width = (uint)bitmap.Width;
            surface.height = (uint)bitmap.Height;

            if (MipCount != 1)
            {
                MipCount = GenerateMipCount(bitmap.Width, bitmap.Height);
                if (MipCount == 0)
                    MipCount = 1;
            }

            surface.numMips = MipCount;
            surface.mipOffset = new uint[MipCount];

            //Create image block from bitmap first
            //var data = GenerateMipsAndCompress(bitmap, MipCount, Format);

            //Swizzle and create surface
            /*var NewSurface = GX2.CreateGx2Texture(data, "Text",
                (uint)surface.tileMode,
                (uint)surface.aa,
                (uint)surface.width,
                (uint)surface.height,
                (uint)surface.depth,
                (uint)surface.format,
                (uint)0,
                (uint)surface.dim,
                (uint)surface.numMips
                );

            ApplySurface(NewSurface);
            IsEdited = true;*/
        }

        public override byte[] GetImageData(int ArrayLevel = 0, int MipLevel = 0, int DepthLevel = 0)
        {
            RedChannel = SetChannel(surface.compSel[0]);
            GreenChannel = SetChannel(surface.compSel[1]);
            BlueChannel = SetChannel(surface.compSel[2]);
            AlphaChannel = SetChannel(surface.compSel[3]);


            Console.WriteLine("");
            Console.WriteLine("// ----- GX2Surface Info ----- ");
            Console.WriteLine("  dim             = " + surface.dim);
            Console.WriteLine("  width           = " + surface.width);
            Console.WriteLine("  height          = " + surface.height);
            Console.WriteLine("  depth           = " + surface.depth);
            Console.WriteLine("  numMips         = " + surface.numMips);
            Console.WriteLine("  format          = " + surface.format);
            Console.WriteLine("  aa              = " + surface.aa);
            Console.WriteLine("  use             = " + surface.use);
            Console.WriteLine("  imageSize       = " + surface.imageSize);
            Console.WriteLine("  mipSize         = " + surface.mipSize);
            Console.WriteLine("  tileMode        = " + surface.tileMode);
            Console.WriteLine("  swizzle         = " + surface.swizzle);
            Console.WriteLine("  alignment       = " + surface.alignment);
            Console.WriteLine("  pitch           = " + surface.pitch);
            Console.WriteLine("  bits per pixel  = " + (surface.bpp << 3));
            Console.WriteLine("  bytes per pixel = " + surface.bpp);
            Console.WriteLine("  data size       = " + surface.data.Length);
            Console.WriteLine("  mip size        = " + surface.mipData.Length);
            Console.WriteLine("  realSize        = " + surface.imageSize);

            return Decode(surface, ArrayLevel, MipLevel);
        }
    }

    public class SurfaceInfoParse : GX2Surface
    {
        public void Read(FileReader reader)
        {
            reader.ByteOrder = ByteOrder.BigEndian;

            dim = reader.ReadUInt32();
            width = reader.ReadUInt32();
            height = reader.ReadUInt32();
            depth = reader.ReadUInt32();
            numMips = reader.ReadUInt32();
            format = reader.ReadUInt32();
            aa = reader.ReadUInt32();
            use = reader.ReadUInt32();
            imageSize = reader.ReadUInt32();
            imagePtr = reader.ReadUInt32();
            mipSize = reader.ReadUInt32();
            mipPtr = reader.ReadUInt32();
            tileMode = reader.ReadUInt32();
            swizzle = reader.ReadUInt32();
            alignment = reader.ReadUInt32();
            pitch = reader.ReadUInt32();
            mipOffset = reader.ReadUInt32s(13);
            firstMip = reader.ReadUInt32();
            imageCount = reader.ReadUInt32();
            firstSlice = reader.ReadUInt32();
            numSlices = reader.ReadUInt32();
            compSel = reader.ReadBytes(4);
            texRegs = reader.ReadUInt32s(5);
        }


        public byte[] Write()
        {
            var mem = new MemoryStream();

            var writer = new FileWriter(mem);
            writer.ByteOrder = ByteOrder.BigEndian;
            writer.Write(dim);
            writer.Write(width);
            writer.Write(height);
            writer.Write(depth);
            writer.Write(numMips);
            writer.Write(format);
            writer.Write(aa);
            writer.Write(use);
            writer.Write(imageSize);
            writer.Write(imagePtr);
            writer.Write(mipSize);
            writer.Write(mipPtr);
            writer.Write(tileMode);
            writer.Write(swizzle);
            writer.Write(alignment);
            writer.Write(pitch);

            for (var i = 0; i < 13; i++)
                if (mipOffset.Length > i)
                    writer.Write(mipOffset[i]);
                else
                    writer.Write(0);

            writer.Write(firstMip);
            writer.Write(imageCount);
            writer.Write(firstSlice);
            writer.Write(numSlices);

            for (var i = 0; i < 4; i++)
                if (compSel != null && compSel.Length > i)
                    writer.Write(compSel[i]);
                else
                    writer.Write((byte)0);

            for (var i = 0; i < 5; i++)
                if (texRegs != null && texRegs.Length > i)
                    writer.Write(texRegs[i]);
                else
                    writer.Write(0);

            return mem.ToArray();
        }
    }
}

public enum STCompressionMode
{
    Slow,
    Normal,
    Fast
}

public enum STChannelType
{
    Red = 0,
    Green = 1,
    Blue = 2,
    Alpha = 3,
    One = 4,
    Zero = 5
}

public enum PlatformSwizzle
{
    None = 0,
    Platform_3DS = 1,
    Platform_Wii = 2,
    Platform_Gamecube = 3,
    Platform_WiiU = 4,
    Platform_Switch = 5,
    Platform_Ps4 = 6,
    Platform_Ps3 = 7,
    Platform_Ps2 = 8,
    Platform_Ps1 = 9
}

public enum STSurfaceType
{
    Texture1D,
    Texture2D,
    Texture3D,
    TextureCube,
    Texture1D_Array,
    Texture2D_Array,
    Texture2D_Mulitsample,
    Texture2D_Multisample_Array,
    TextureCube_Array
}


public class EditedBitmap
{
    public int ArrayLevel = 0;
    public Bitmap bitmap;
}

public abstract class STGenericTexture
{
    public STGenericTexture()
    {
        RedChannel = STChannelType.Red;
        GreenChannel = STChannelType.Green;
        BlueChannel = STChannelType.Blue;
        AlphaChannel = STChannelType.Alpha;
    }

    public ImageParameters Parameters = new();
    public bool IsCubemap => ArrayCount == 6 || ArrayCount % 6 == 0;

    public STSurfaceType SurfaceType = STSurfaceType.Texture2D;

    /// <summary>
    /// The swizzle method to use when decoding or encoding back a texture.
    /// </summary>
    public PlatformSwizzle PlatformSwizzle;

    public bool IsSwizzled { get; set; } = true;

    /// <summary>
    /// Is the texture edited or not. Used for the image editor for saving changes.
    /// </summary>
    public bool IsEdited { get; set; } = false;

    /// <summary>
    /// An array of <see cref="EditedBitmap"/> from the image editor to be saved back.
    /// </summary>
    public EditedBitmap[] EditedImages { get; set; }

    //If the texture can be edited or not. Disables some functions in image editor if false
    //If true, the editors will call "SetImageData" for setting data back to the original data.
    public abstract bool CanEdit { get; set; }

    public STChannelType RedChannel = STChannelType.Red;
    public STChannelType GreenChannel = STChannelType.Green;
    public STChannelType BlueChannel = STChannelType.Blue;
    public STChannelType AlphaChannel = STChannelType.Alpha;


    public static bool IsCompressed(TEX_FORMAT Format)
    {
        switch (Format)
        {
            case TEX_FORMAT.BC1_UNORM:
            case TEX_FORMAT.BC1_UNORM_SRGB:
            case TEX_FORMAT.BC1_TYPELESS:
            case TEX_FORMAT.BC2_UNORM_SRGB:
            case TEX_FORMAT.BC2_UNORM:
            case TEX_FORMAT.BC2_TYPELESS:
            case TEX_FORMAT.BC3_UNORM_SRGB:
            case TEX_FORMAT.BC3_UNORM:
            case TEX_FORMAT.BC3_TYPELESS:
            case TEX_FORMAT.BC4_UNORM:
            case TEX_FORMAT.BC4_TYPELESS:
            case TEX_FORMAT.BC4_SNORM:
            case TEX_FORMAT.BC5_UNORM:
            case TEX_FORMAT.BC5_TYPELESS:
            case TEX_FORMAT.BC5_SNORM:
            case TEX_FORMAT.BC6H_UF16:
            case TEX_FORMAT.BC6H_SF16:
            case TEX_FORMAT.BC7_UNORM:
            case TEX_FORMAT.BC7_UNORM_SRGB:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// The total length of all the bytes given from GetImageData.
    /// </summary>
    public long DataSizeInBytes
    {
        get
        {
            long totalSize = 0;

            if (PlatformSwizzle == PlatformSwizzle.Platform_3DS)
            {
                for (var arrayLevel = 0; arrayLevel < ArrayCount; arrayLevel++)
                for (var mipLevel = 0; mipLevel < MipCount; mipLevel++)
                {
                    var width = (uint)Math.Max(1, Width >> mipLevel);
                    var height = (uint)Math.Max(1, Height >> mipLevel);

                    //totalSize += CTR_3DS.CalculateLength((int)width, (int)height, CTR_3DS.ConvertToPICAFormat(Format));
                    totalSize += 50;
                }

                return totalSize;
            }

            if (PlatformSwizzle == PlatformSwizzle.Platform_Gamecube)
            {
                for (var arrayLevel = 0; arrayLevel < ArrayCount; arrayLevel++)
                for (var mipLevel = 0; mipLevel < MipCount; mipLevel++)
                {
                    var width = (uint)Math.Max(1, Width >> mipLevel);
                    var height = (uint)Math.Max(1, Height >> mipLevel);

                    //totalSize += Decode_Gamecube.GetDataSize((uint)Decode_Gamecube.FromGenericFormat(Format), width, height);
                    totalSize += 50;
                }

                return totalSize;
            }

            if (FormatTable.ContainsKey(Format))
            {
                var bpp = GetBytesPerPixel(Format);

                for (var arrayLevel = 0; arrayLevel < ArrayCount; arrayLevel++)
                for (var mipLevel = 0; mipLevel < MipCount; mipLevel++)
                {
                    var width = (uint)Math.Max(1, Width >> mipLevel);
                    var height = (uint)Math.Max(1, Height >> mipLevel);

                    var size = width * height * bpp;
                    if (IsCompressed2(Format))
                    {
                        size = ((width + 3) >> 2) * ((Height + 3) >> 2) * bpp;
                        if (size < bpp)
                            size = bpp;
                    }

                    totalSize += size;
                }
            }

            return totalSize;
        }
    }

    public static bool IsCompressed2(TEX_FORMAT Format)
    {
        switch (Format)
        {
            case TEX_FORMAT.BC1_UNORM:
            case TEX_FORMAT.BC1_UNORM_SRGB:
            case TEX_FORMAT.BC1_TYPELESS:
            case TEX_FORMAT.BC2_UNORM_SRGB:
            case TEX_FORMAT.BC2_UNORM:
            case TEX_FORMAT.BC2_TYPELESS:
            case TEX_FORMAT.BC3_UNORM_SRGB:
            case TEX_FORMAT.BC3_UNORM:
            case TEX_FORMAT.BC3_TYPELESS:
            case TEX_FORMAT.BC4_UNORM:
            case TEX_FORMAT.BC4_TYPELESS:
            case TEX_FORMAT.BC4_SNORM:
            case TEX_FORMAT.BC5_UNORM:
            case TEX_FORMAT.BC5_TYPELESS:
            case TEX_FORMAT.BC5_SNORM:
            case TEX_FORMAT.BC6H_UF16:
            case TEX_FORMAT.BC6H_SF16:
            case TEX_FORMAT.BC7_UNORM:
            case TEX_FORMAT.BC7_UNORM_SRGB:
                return true;
            default:
                return false;
        }
    }

    public abstract byte[] GetImageData(int ArrayLevel = 0, int MipLevel = 0, int DepthLevel = 0);

    private byte[] paletteData = new byte[0];

    public virtual byte[] GetPaletteData()
    {
        return paletteData;
    }

    public List<Surface> Get3DSurfaces(int IndexStart = 0, bool GetAllSurfaces = true, int GetSurfaceAmount = 1)
    {
        if (GetAllSurfaces)
            GetSurfaceAmount = (int)Depth;

        var surfaces = new List<Surface>();
        for (var depthLevel = 0; depthLevel < Depth; depthLevel++)
        {
            var IsLower = depthLevel < IndexStart;
            var IsHigher = depthLevel >= IndexStart + GetSurfaceAmount;
            if (!IsLower && !IsHigher)
            {
                var mips = new List<byte[]>();
                for (var mipLevel = 0; mipLevel < MipCount; mipLevel++) mips.Add(GetImageData(0, mipLevel, depthLevel));

                surfaces.Add(new Surface() { mipmaps = mips });
            }
        }

        return surfaces;
    }

    //
    //Gets a list of surfaces given the start index of the array and the amount of arrays to obtain
    //
    public List<Surface> GetSurfaces(int ArrayIndexStart = 0, bool GetAllSurfaces = true, int GetSurfaceAmount = 1)
    {
        if (GetAllSurfaces)
            GetSurfaceAmount = (int)ArrayCount;

        var surfaces = new List<Surface>();
        for (var arrayLevel = 0; arrayLevel < ArrayCount; arrayLevel++)
        {
            var IsLower = arrayLevel < ArrayIndexStart;
            var IsHigher = arrayLevel >= ArrayIndexStart + GetSurfaceAmount;
            if (!IsLower && !IsHigher)
            {
                var mips = new List<byte[]>();
                for (var mipLevel = 0; mipLevel < MipCount; mipLevel++) mips.Add(GetImageData(arrayLevel, mipLevel));

                surfaces.Add(new Surface() { mipmaps = mips });
            }
        }

        return surfaces;
    }

    public abstract void SetImageData(Bitmap bitmap, int ArrayLevel);

    /// <summary>
    /// The total amount of surfaces for the texture.
    /// </summary>
    public uint ArrayCount
    {
        get => arrayCount;
        set => arrayCount = value;
    }

    private uint arrayCount = 1;

    /// <summary>
    /// The total amount of mipmaps for the texture.
    /// </summary>
    public uint MipCount
    {
        get => mipCount;
        set
        {
            if (value == 0)
                mipCount = 1;
            else if (value > 17)
                throw new Exception($"Invalid mip map count! Texture: {"Text"} Value: {value}");
            else
                mipCount = value;
        }
    }

    private uint mipCount = 1;

    /// <summary>
    /// The width of the image in pixels.
    /// </summary>
    public uint Width { get; set; }

    /// <summary>
    /// The height of the image in pixels.
    /// </summary>
    public uint Height { get; set; }

    /// <summary>
    /// The depth of the image in pixels. Used for 3D types.
    /// </summary>
    public uint Depth { get; set; }

    /// <summary>
    /// The <see cref="TEX_FORMAT"/> Format of the image. 
    /// </summary>
    public TEX_FORMAT Format { get; set; } = TEX_FORMAT.R8G8B8A8_UNORM;

    /// <summary>
    /// The <see cref="PALETTE_FORMAT"/> Format of the image. 
    /// </summary>

    public abstract TEX_FORMAT[] SupportedFormats { get; }

    public static uint GetBytesPerPixel(TEX_FORMAT Format)
    {
        return FormatTable[Format].BytesPerPixel;
    }

    public static uint GetBlockHeight(TEX_FORMAT Format)
    {
        return FormatTable[Format].BlockHeight;
    }

    public static uint GetBlockWidth(TEX_FORMAT Format)
    {
        return FormatTable[Format].BlockWidth;
    }

    public static uint GetBlockDepth(TEX_FORMAT Format)
    {
        return FormatTable[Format].BlockDepth;
    }

    // Based on Ryujinx's image table 
    // https://github.com/Ryujinx/Ryujinx/blob/c86aacde76b5f8e503e2b412385c8491ecc86b3b/Ryujinx.Graphics/Graphics3d/Texture/ImageUtils.cs
    // A nice way to get bpp, block data, and buffer types for formats

    private static readonly Dictionary<TEX_FORMAT, FormatInfo> FormatTable =
        new()
        {
            { TEX_FORMAT.R32G32B32A32_FLOAT, new FormatInfo(16, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R32G32B32A32_SINT, new FormatInfo(16, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R32G32B32A32_UINT, new FormatInfo(16, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R32G32B32_FLOAT, new FormatInfo(8, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R16G16B16A16_FLOAT, new FormatInfo(8, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R16G16B16A16_SINT, new FormatInfo(8, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R16G16B16A16_SNORM, new FormatInfo(8, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R32G32_FLOAT, new FormatInfo(8, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R32G32_SINT, new FormatInfo(8, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R32G32_UINT, new FormatInfo(8, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R8G8B8A8_SINT, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R8G8B8A8_SNORM, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R8G8B8A8_UINT, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R8G8B8A8_UNORM, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R8G8B8A8_UNORM_SRGB, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R32G8X24_FLOAT, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R8G8_B8G8_UNORM, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.B8G8R8X8_UNORM, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.B5G5R5A1_UNORM, new FormatInfo(2, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R5G5B5A1_UNORM, new FormatInfo(2, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.B8G8R8A8_UNORM, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.B8G8R8A8_UNORM_SRGB, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R5G5B5_UNORM, new FormatInfo(2, 1, 1, 1, TargetBuffer.Color) },


            { TEX_FORMAT.R10G10B10A2_UINT, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R10G10B10A2_UNORM, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R32_SINT, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R32_UINT, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R32_FLOAT, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.B4G4R4A4_UNORM, new FormatInfo(2, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R16G16_FLOAT, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R16G16_SINT, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R16G16_SNORM, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R16G16_UINT, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R16G16_UNORM, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R8G8_SINT, new FormatInfo(2, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R8G8_SNORM, new FormatInfo(2, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R8G8_UINT, new FormatInfo(2, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R8G8_UNORM, new FormatInfo(2, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R16_SINT, new FormatInfo(2, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R16_SNORM, new FormatInfo(2, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R16_UINT, new FormatInfo(2, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R16_UNORM, new FormatInfo(2, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R8_SINT, new FormatInfo(1, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R8_SNORM, new FormatInfo(1, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R4G4_UNORM, new FormatInfo(1, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R8_UINT, new FormatInfo(1, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R8_UNORM, new FormatInfo(1, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.R11G11B10_FLOAT, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.B5G6R5_UNORM, new FormatInfo(2, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.BC1_UNORM, new FormatInfo(8, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.BC1_UNORM_SRGB, new FormatInfo(8, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.BC2_UNORM, new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.BC2_UNORM_SRGB, new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.BC3_UNORM, new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.BC3_UNORM_SRGB, new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.BC4_UNORM, new FormatInfo(8, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.BC4_SNORM, new FormatInfo(8, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.BC5_UNORM, new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.BC5_SNORM, new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.BC6H_SF16, new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.BC6H_UF16, new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.BC7_UNORM, new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.BC7_UNORM_SRGB, new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },

            { TEX_FORMAT.ASTC_4x4_UNORM, new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_4x4_SRGB, new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_5x4_UNORM, new FormatInfo(16, 5, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_5x4_SRGB, new FormatInfo(16, 5, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_5x5_UNORM, new FormatInfo(16, 5, 5, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_5x5_SRGB, new FormatInfo(16, 5, 5, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_6x5_UNORM, new FormatInfo(16, 6, 5, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_6x5_SRGB, new FormatInfo(16, 6, 5, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_6x6_UNORM, new FormatInfo(16, 6, 6, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_6x6_SRGB, new FormatInfo(16, 6, 6, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_8x5_UNORM, new FormatInfo(16, 8, 5, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_8x5_SRGB, new FormatInfo(16, 8, 5, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_8x6_UNORM, new FormatInfo(16, 8, 6, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_8x6_SRGB, new FormatInfo(16, 8, 6, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_8x8_UNORM, new FormatInfo(16, 8, 8, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_8x8_SRGB, new FormatInfo(16, 8, 8, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_10x5_UNORM, new FormatInfo(16, 10, 5, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_10x5_SRGB, new FormatInfo(16, 10, 5, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_10x6_UNORM, new FormatInfo(16, 10, 6, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_10x6_SRGB, new FormatInfo(16, 10, 6, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_10x8_UNORM, new FormatInfo(16, 10, 8, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_10x8_SRGB, new FormatInfo(16, 10, 8, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_10x10_UNORM, new FormatInfo(16, 10, 10, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_10x10_SRGB, new FormatInfo(16, 10, 10, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_12x10_UNORM, new FormatInfo(16, 12, 10, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_12x10_SRGB, new FormatInfo(16, 12, 10, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_12x12_UNORM, new FormatInfo(16, 12, 12, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ASTC_12x12_SRGB, new FormatInfo(16, 12, 12, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ETC1_UNORM, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ETC1_SRGB, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.ETC1_A4, new FormatInfo(8, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.HIL08, new FormatInfo(16, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.L4, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.LA4, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.L8, new FormatInfo(8, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.LA8, new FormatInfo(16, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.A4, new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { TEX_FORMAT.A8_UNORM, new FormatInfo(8, 1, 1, 1, TargetBuffer.Color) },

            { TEX_FORMAT.D16_UNORM, new FormatInfo(2, 1, 1, 1, TargetBuffer.Depth) },
            { TEX_FORMAT.D24_UNORM_S8_UINT, new FormatInfo(4, 1, 1, 1, TargetBuffer.Depth) },
            { TEX_FORMAT.D32_FLOAT, new FormatInfo(4, 1, 1, 1, TargetBuffer.Depth) },
            { TEX_FORMAT.D32_FLOAT_S8X24_UINT, new FormatInfo(8, 1, 1, 1, TargetBuffer.DepthStencil) },

            { TEX_FORMAT.I4, new FormatInfo(4, 8, 8, 1, TargetBuffer.Color) },
            { TEX_FORMAT.I8, new FormatInfo(8, 8, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.IA4, new FormatInfo(8, 8, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.IA8, new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.RGB565, new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.RGB5A3, new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.RGBA32, new FormatInfo(32, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.C4, new FormatInfo(4, 8, 8, 1, TargetBuffer.Color) },
            { TEX_FORMAT.C8, new FormatInfo(8, 8, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.C14X2, new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { TEX_FORMAT.CMPR, new FormatInfo(4, 8, 8, 1, TargetBuffer.Color) }
        };

    /// <summary>
    /// A Surface contains mip levels of compressed/uncompressed texture data
    /// </summary>
    public class Surface
    {
        public List<byte[]> mipmaps = new();
    }

    public void CreateGenericTexture(uint width, uint height, List<Surface> surfaces, TEX_FORMAT format)
    {
        Width = width;
        Height = height;
        Format = format;
    }

    private enum TargetBuffer
    {
        Color = 1,
        Depth = 2,
        Stencil = 3,
        DepthStencil = 4
    }

    private class FormatInfo
    {
        public uint BytesPerPixel { get; private set; }
        public uint BlockWidth { get; private set; }
        public uint BlockHeight { get; private set; }
        public uint BlockDepth { get; private set; }

        public TargetBuffer TargetBuffer;

        public FormatInfo(uint bytesPerPixel, uint blockWidth, uint blockHeight, uint blockDepth,
            TargetBuffer targetBuffer)
        {
            BytesPerPixel = bytesPerPixel;
            BlockWidth = blockWidth;
            BlockHeight = blockHeight;
            BlockDepth = blockDepth;
            TargetBuffer = targetBuffer;
        }
    }

    /// <summary>
    /// Gets a <see cref="Bitmap"/> given an array and mip index.
    /// </summary>
    /// <param name="ArrayIndex">The index of the surface/array. Cubemaps will have 6</param>
    /// <param name="MipLevel">The index of the mip level.</param>
    /// <returns></returns>
    /*public Bitmap GetBitmap(int ArrayLevel = 0, int MipLevel = 0, int DepthLevel = 0)
    {
        uint width = Math.Max(1, Width >> MipLevel);
        uint height = Math.Max(1, Height >> MipLevel);
        byte[] data = GetImageData(ArrayLevel, MipLevel, DepthLevel);
        byte[] paletteData = GetPaletteData();
        if (Format == TEX_FORMAT.R8G8B8A8_UNORM && PlatformSwizzle == PlatformSwizzle.None)
            return BitmapExtension.GetBitmap(ConvertBgraToRgba(data), (int)width, (int)height);

        try
        {
            if (data == null)
                throw new Exception("Data is null!");

            if (PlatformSwizzle == PlatformSwizzle.Platform_3DS)
            {
                var Image = BitmapExtension.GetBitmap(ConvertBgraToRgba(CTR_3DS.DecodeBlock(data, (int)width, (int)height, Format)),
                  (int)width, (int)height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                return Image;
            }

            if (PlatformSwizzle == PlatformSwizzle.Platform_Gamecube)
            {
                return BitmapExtension.GetBitmap(Decode_Gamecube.DecodeData(data, paletteData, width, height, Format, PaletteFormat),
                      (int)width, (int)height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }

            switch (Format)
            {
                case TEX_FORMAT.R4G4_UNORM:
                    return BitmapExtension.GetBitmap(R4G4.Decompress(data, (int)width, (int)height, false),
                    (int)width, (int)height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                case TEX_FORMAT.BC5_SNORM:
                    return DDSCompressor.DecompressBC5(data, (int)width, (int)height, true);
                case TEX_FORMAT.ETC1_UNORM:
                    return BitmapExtension.GetBitmap(ETC1.ETC1Decompress(data, (int)width, (int)height, false),
                           (int)width, (int)height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                case TEX_FORMAT.ETC1_A4:
                    return BitmapExtension.GetBitmap(ETC1.ETC1Decompress(data, (int)width, (int)height, true),
                          (int)width, (int)height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                case TEX_FORMAT.R5G5B5A1_UNORM:
                case TEX_FORMAT.LA8:
                case TEX_FORMAT.L8:
                    return BitmapExtension.GetBitmap(RGBAPixelDecoder.Decode(data, (int)width, (int)height, Format),
                          (int)width, (int)height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }

            if (Runtime.UseDirectXTexDecoder)
            {
                return BitmapExtension.GetBitmap(DecodeBlock(data, width, height, Format, new byte[0], Parameters),
                  (int)width, (int)height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }
            else
                return DecodeNotDirectXTex(data, width, height, Format);

        }
        catch (Exception ex)
        {
            return null;
        }
    }*/


    /// <summary>
    /// Decodes a byte array of image data given the source image in bytes, width, height, and DXGI format.
    /// </summary>
    /// <param name="byte[]">The byte array of the image</param>
    /// <param name="Width">The width of the image in pixels.</param>
    /// <param name="Height">The height of the image in pixels.</param>
    /// <param name=" DDS.DXGI_FORMAT">The image format.</param>
    /// <returns>Returns a byte array of decoded data. </returns>
    /*public static byte[] DecodeBlock(byte[] data, uint Width, uint Height, TEX_FORMAT Format, byte[] paletteData, ImageParameters parameters, PALETTE_FORMAT PaletteFormat = PALETTE_FORMAT.None, PlatformSwizzle PlatformSwizzle = PlatformSwizzle.None)
    {
        if (data == null) throw new Exception($"Data is null!");
        if (Format <= 0) throw new Exception($"Invalid Format!");
        if (data.Length <= 0) throw new Exception($"Data is empty!");
        if (Width <= 0) throw new Exception($"Invalid width size {Width}!");
        if (Height <= 0) throw new Exception($"Invalid height size {Height}!");

        byte[] imageData = new byte[0];
        bool DontSwapRG = false;

        if (PlatformSwizzle == PlatformSwizzle.Platform_3DS)
        {
            imageData = CTR_3DS.DecodeBlock(data, (int)Width, (int)Height, Format);
            DontSwapRG = true;
        }
        else if (PlatformSwizzle == PlatformSwizzle.Platform_Gamecube)
            imageData = Decode_Gamecube.DecodeData(data, paletteData, Width, Height, Format, PaletteFormat);
        else
        {
            if (Format == TEX_FORMAT.R32G8X24_FLOAT)
                imageData = DDSCompressor.DecodePixelBlock(data, (int)Width, (int)Height, DDS.DXGI_FORMAT.DXGI_FORMAT_R32G8X24_TYPELESS);

            if (Format == TEX_FORMAT.BC5_SNORM)
                imageData = DDSCompressor.DecompressBC5(data, (int)Width, (int)Height, true, true);

            if (Format == TEX_FORMAT.L8)
                return RGBAPixelDecoder.Decode(data, (int)Width, (int)Height, Format);
            if (Format == TEX_FORMAT.LA8)
                return RGBAPixelDecoder.Decode(data, (int)Width, (int)Height, Format);
            if (Format == TEX_FORMAT.R5G5B5A1_UNORM)
                return RGBAPixelDecoder.Decode(data, (int)Width, (int)Height, Format);

            if (IsCompressed(Format))
                imageData = DDSCompressor.DecompressBlock(data, (int)Width, (int)Height, (DDS.DXGI_FORMAT)Format);
            else
            {
                if (IsAtscFormat(Format))
                    imageData = ASTCDecoder.DecodeToRGBA8888(data, (int)GetBlockWidth(Format), (int)GetBlockHeight(Format), 1, (int)Width, (int)Height, 1);
                else
                    imageData = DDSCompressor.DecodePixelBlock(data, (int)Width, (int)Height, (DDS.DXGI_FORMAT)Format);

                //    imageData = RGBAPixelDecoder.Decode(data, (int)Width, (int)Height, Format);
            }
        }

        if (parameters.DontSwapRG || DontSwapRG)
            return imageData;
        else
            return ConvertBgraToRgba(imageData);
    }*/
    public string DebugInfo()
    {
        return $"Texture Info:\n" +
               $"Name:               {"Text"}\n" +
               $"Format:             {Format}\n" +
               $"Height:             {Height}\n" +
               $"Width:              {Width}\n" +
               $"Block Height:       {GetBlockHeight(Format)}\n" +
               $"Block Width:        {GetBlockWidth(Format)}\n" +
               $"Bytes Per Pixel:    {GetBytesPerPixel(Format)}\n" +
               $"Array Count:        {ArrayCount}\n" +
               $"Mip Map Count:      {MipCount}\n" +
               "";
    }

    public uint GenerateMipCount(int Width, int Height)
    {
        return GenerateMipCount((uint)Width, (uint)Height);
    }


    /// <summary>
    /// Decodes a byte array of image data given the source image in bytes, width, height, and DXGI format.
    /// </summary>
    /// <param name="byte[]">The byte array of the image</param>
    /// <param name="Width">The width of the image in pixels.</param>
    /// <param name="Height">The height of the image in pixels.</param>
    /// <param name=" DDS.DXGI_FORMAT">The image format.</param>
    /// <returns>Returns a byte array of decoded data. </returns>
    public static byte[] DecodeBlock(byte[] data, uint Width, uint Height, TEX_FORMAT Format, byte[] paletteData,
        ImageParameters parameters, PALETTE_FORMAT PaletteFormat = PALETTE_FORMAT.None,
        PlatformSwizzle PlatformSwizzle = PlatformSwizzle.None)
    {
        if (data == null) throw new Exception($"Data is null!");
        if (Format <= 0) throw new Exception($"Invalid Format!");
        if (data.Length <= 0) throw new Exception($"Data is empty!");
        if (Width <= 0) throw new Exception($"Invalid width size {Width}!");
        if (Height <= 0) throw new Exception($"Invalid height size {Height}!");

        var imageData = new byte[0];
        var DontSwapRG = false;

        //if (IsCompressed(Format))
            //imageData = DDSCompressor.DecompressBlock(data, (int)Width, (int)Height, (DDS.DXGI_FORMAT)Format);

            if (Format == TEX_FORMAT.R32G8X24_FLOAT)
                imageData = DDSCompressor.DecodePixelBlock(data, (int)Width, (int)Height,
                    DDS.DXGI_FORMAT.DXGI_FORMAT_R32G8X24_TYPELESS);

            if (Format == TEX_FORMAT.BC5_SNORM)
                imageData = DDSCompressor.DecompressBC5(data, (int)Width, (int)Height, true, true);

            if (Format == TEX_FORMAT.L8)
                return RGBAPixelDecoder.Decode(data, (int)Width, (int)Height, Format);
            if (Format == TEX_FORMAT.LA8)
                return RGBAPixelDecoder.Decode(data, (int)Width, (int)Height, Format);
            if (Format == TEX_FORMAT.R5G5B5A1_UNORM)
                return RGBAPixelDecoder.Decode(data, (int)Width, (int)Height, Format);

            if (IsCompressed(Format))
                imageData = DDSCompressor.DecompressBlock(data, (int)Width, (int)Height, (DDS.DXGI_FORMAT)Format);
            else
            {
                if (IsAtscFormat(Format))
                    imageData = ASTCDecoder.DecodeToRGBA8888(data, (int)GetBlockWidth(Format),
                        (int)GetBlockHeight(Format), 1, (int)Width, (int)Height, 1);
                else
                    imageData = DDSCompressor.DecodePixelBlock(data, (int)Width, (int)Height, (DDS.DXGI_FORMAT)Format);

                //    imageData = RGBAPixelDecoder.Decode(data, (int)Width, (int)Height, Format);
            }
            
        if (parameters.DontSwapRG || DontSwapRG)
            return imageData;
        else
            return ConvertBgraToRgba(imageData);

        /*{
            if (Format == TEX_FORMAT.R32G8X24_FLOAT)
                imageData = DDSCompressor.DecodePixelBlock(data, (int)Width, (int)Height, DDS.DXGI_FORMAT.DXGI_FORMAT_R32G8X24_TYPELESS);

            if (Format == TEX_FORMAT.BC5_SNORM)
                imageData = DDSCompressor.DecompressBC5(data, (int)Width, (int)Height, true, true);

            if (Format == TEX_FORMAT.L8)
                return RGBAPixelDecoder.Decode(data, (int)Width, (int)Height, Format);
            if (Format == TEX_FORMAT.LA8)
                return RGBAPixelDecoder.Decode(data, (int)Width, (int)Height, Format);
            if (Format == TEX_FORMAT.R5G5B5A1_UNORM)
                return RGBAPixelDecoder.Decode(data, (int)Width, (int)Height, Format);

            if (IsCompressed2(Format))
                imageData = DDSCompressor.DecompressBlock(data, (int)Width, (int)Height, (DDS.DXGI_FORMAT)Format);
            else
            {
                if (IsAtscFormat(Format))
                    imageData = ASTCDecoder.DecodeToRGBA8888(data, (int)GetBlockWidth(Format), (int)GetBlockHeight(Format), 1, (int)Width, (int)Height, 1);
                else
                    imageData = DDSCompressor.DecodePixelBlock(data, (int)Width, (int)Height, (DDS.DXGI_FORMAT)Format);

                //    imageData = RGBAPixelDecoder.Decode(data, (int)Width, (int)Height, Format);
            }

            if (parameters.DontSwapRG || DontSwapRG)
                return imageData;
            else
                return ConvertBgraToRgba(imageData);
        }*/
    }

    public byte[] GetBitmapData()
    {
        return DecodeBlock(GetImageData(0, 0, 0), Width, Height, Format, new byte[0], Parameters);
    }

    public Bitmap GetBitmapWithChannel()
    {
        Bitmap b = GetBitmap();
        b = BitmapExtension.SetChannel(b, RedChannel, GreenChannel, BlueChannel, AlphaChannel);

        return b;
    }

    /// <summary>
    /// Gets a <see cref="Bitmap"/> given an array and mip index.
    /// </summary>
    /// <param name="ArrayIndex">The index of the surface/array. Cubemaps will have 6</param>
    /// <param name="MipLevel">The index of the mip level.</param>
    /// <returns></returns>
    public Bitmap GetBitmap(int ArrayLevel = 0, int MipLevel = 0, int DepthLevel = 0)
    {
        var width = Math.Max(1, Width >> MipLevel);
        var height = Math.Max(1, Height >> MipLevel);
        var data = GetImageData(ArrayLevel, MipLevel, DepthLevel);
        var paletteData = GetPaletteData();

        if (Format == TEX_FORMAT.R8G8B8A8_UNORM && PlatformSwizzle == PlatformSwizzle.None)
            return BitmapExtension.GetBitmap(ConvertBgraToRgba(data), (int)width, (int)height);

        /*return BitmapExtension.GetBitmap(DecodeBlock(data, width, height, Format, new byte[0], Parameters), (int)width,
            (int)height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);*/

        try
        {
            if (data == null)
                throw new Exception("Data is null!");

            /*if (PlatformSwizzle == PlatformSwizzle.Platform_Gamecube)
            {
                return BitmapExtension.GetBitmap(Decode_Gamecube.DecodeData(data, paletteData, width, height, Format, PaletteFormat),
                      (int)width, (int)height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }*/

            switch (Format)
            {
                case TEX_FORMAT.R4G4_UNORM:
                    return BitmapExtension.GetBitmap(R4G4.Decompress(data, (int)width, (int)height, false),
                        (int)width, (int)height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                case TEX_FORMAT.BC5_SNORM:
                    return DDSCompressor.DecompressBC5(data, (int)width, (int)height, true);
                case TEX_FORMAT.ETC1_UNORM:
                    return BitmapExtension.GetBitmap(ETC1.ETC1Decompress(data, (int)width, (int)height, false),
                        (int)width, (int)height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                case TEX_FORMAT.ETC1_A4:
                    return BitmapExtension.GetBitmap(ETC1.ETC1Decompress(data, (int)width, (int)height, true),
                        (int)width, (int)height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                case TEX_FORMAT.R5G5B5A1_UNORM:
                case TEX_FORMAT.LA8:
                case TEX_FORMAT.L8:
                    return BitmapExtension.GetBitmap(RGBAPixelDecoder.Decode(data, (int)width, (int)height, Format),
                        (int)width, (int)height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }

            return BitmapExtension.GetBitmap(DecodeBlock(data, width, height, Format, new byte[0], Parameters),
                (int)width, (int)height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        }
        catch (Exception ex)
        {
            
        }

        return null;
        //}
    }

    public void SaveBitmap(string path)
    {
        var surfaces = GetSurfaces();

        foreach (var surface in surfaces)
        {
            var bitMap = GetBitmap(surfaces.IndexOf(surface), 0);
            bitMap = BitmapExtension.SetChannel(bitMap, RedChannel, GreenChannel, BlueChannel, AlphaChannel);
            bitMap.Save(path + "_" + surfaces.IndexOf(surface) + ".png");
        }
    }

    public void SaveDDS(string FileName, bool ExportSurfaceLevel = false,
        bool ExportMipMapLevel = false, int SurfaceLevel = 0, int MipLevel = 0)
    {
        List<Surface> surfaces = null;
        if (ExportSurfaceLevel)
            surfaces = GetSurfaces(SurfaceLevel, false, 1);
        else if (Depth > 1)
            surfaces = Get3DSurfaces();
        else
            surfaces = GetSurfaces();

        if (Depth == 0)
            Depth = 1;

        var dds = new DDS();
        dds.header = new DDS.Header();
        dds.header.width = Width;
        dds.header.height = Height;
        dds.header.depth = Depth;
        dds.header.mipmapCount = (uint)MipCount;
        dds.header.pitchOrLinearSize = (uint)surfaces[0].mipmaps[0].Length;

        //Check for components to be different. Then set our channel flags
        if (RedChannel != STChannelType.Red || GreenChannel != STChannelType.Green ||
            BlueChannel != STChannelType.Blue || AlphaChannel != STChannelType.Alpha)
        {
            //R G B A 1 0
            var components = new uint[6]
            {
                0x000000ff, 0x0000ff00,
                0x00ff0000, 0xff000000, 0x00008000, 0
            };

            /*    dds.header.ddspf.RGBBitCount = 4;
                dds.header.ddspf.RBitMask = components[(int)RedChannel];
                dds.header.ddspf.GBitMask = components[(int)GreenChannel];
                dds.header.ddspf.BBitMask = components[(int)BlueChannel];
                dds.header.ddspf.ABitMask = components[(int)AlphaChannel];*/
        }

        /*  if (Runtime.ImageEditor.PreviewGammaFix)
           {
               foreach (var surface in surfaces)
               {
                   Bitmap bitMap = GetBitmap(surfaces.IndexOf(surface), 0);
                   bitMap = BitmapExtension.AdjustGamma(bitMap, 1.0f / 2.2f);
                   if (Runtime.ImageEditor.UseComponetSelector)
                       bitMap = BitmapExtension.SetChannel(bitMap, RedChannel, GreenChannel, BlueChannel, AlphaChannel);

                   var reEncoded = GenerateMipsAndCompress(bitMap, MipCount, Format);
                   //surface.mipmaps = reEncoded;
               }
           }*/

        var isCubeMap = ArrayCount == 6;


        foreach (var surface in surfaces)
        {
            var bitMap = GetBitmap(surfaces.IndexOf(surface), 0);
            bitMap = BitmapExtension.SetChannel(bitMap, RedChannel, GreenChannel, BlueChannel, AlphaChannel);
            bitMap.Save(
                @"G:\cemu_1.23.1\cemu_1.23.1\mlc01\usr\title\00050000\1019e600\content\res\Object\@bg000b.pack\bitmap_" +
                surfaces.IndexOf(surface) + ".png");
        }


        if (surfaces.Count > 1) //Use DX10 format for array surfaces as it can do custom amounts
            dds.SetFlags((DDS.DXGI_FORMAT)Format, true, isCubeMap);
        else
            dds.SetFlags((DDS.DXGI_FORMAT)Format, false, isCubeMap);

        if (dds.IsDX10)
        {
            if (dds.DX10header == null)
                dds.DX10header = new DDS.DX10Header();

            dds.DX10header.ResourceDim = 3;

            if (isCubeMap)
                dds.DX10header.arrayFlag = (uint)(ArrayCount / 6);
            else
                dds.DX10header.arrayFlag = (uint)ArrayCount;
        }


        dds.Save(dds, FileName, surfaces);
    }

    public uint GenerateMipCount(uint Width, uint Height)
    {
        uint MipmapNum = 0;
        var num = Math.Max(Width, Height);

        var width = (int)Width;
        var height = (int)Height;

        while (true)
        {
            num >>= 1;

            width = width / 2;
            height = height / 2;
            if (width <= 0 || height <= 0)
                break;

            if (num > 0)
                ++MipmapNum;
            else
                break;
        }

        return MipmapNum;
    }

    public static bool IsAtscFormat(TEX_FORMAT Format)
    {
        if (Format.ToString().Contains("ASTC"))
            return true;
        else
            return false;
    }

    public static STChannelType[] SetChannelsByFormat(TEX_FORMAT Format)
    {
        var channels = new STChannelType[4];

        switch (Format)
        {
            case TEX_FORMAT.BC5_UNORM:
            case TEX_FORMAT.BC5_SNORM:
                channels[0] = STChannelType.Red;
                channels[1] = STChannelType.Green;
                channels[2] = STChannelType.One;
                channels[3] = STChannelType.One;
                break;
            case TEX_FORMAT.BC4_UNORM:
            case TEX_FORMAT.BC4_SNORM:
                channels[0] = STChannelType.Red;
                channels[1] = STChannelType.Red;
                channels[2] = STChannelType.Red;
                channels[3] = STChannelType.Red;
                break;
            default:
                channels[0] = STChannelType.Red;
                channels[1] = STChannelType.Green;
                channels[2] = STChannelType.Blue;
                channels[3] = STChannelType.Alpha;
                break;
        }

        return channels;
    }

    public static int GenerateTotalMipCount(uint Width, uint Height)
    {
        var mipCount = 1;

        var width = (int)Width;
        var height = (int)Height;

        while (width > 1 || height > 1)
        {
            ++mipCount;

            if (width > 1)
                width /= 2;

            if (height > 1)
                height /= 2;
        }

        return mipCount;
    }

    public static string SetNameFromPath(string path)
    {
        var FileName = Path.GetFileName(path);
        var extension = Path.GetExtension(FileName);
        return FileName.Substring(0, FileName.Length - extension.Length);
    }

    public Bitmap GetComponentBitmap(Bitmap image, bool ShowAlpha = true)
    {
        return image;
    }

    private bool UseRGBA()
    {
        if (RedChannel == STChannelType.Red &&
            GreenChannel == STChannelType.Green &&
            BlueChannel == STChannelType.Blue &&
            AlphaChannel == STChannelType.Alpha)
            return true;
        else
            return false;
    }

    public static byte[] ConvertBgraToRgba(byte[] bytes)
    {
        if (bytes == null)
            throw new Exception("Data block returned null. Make sure the parameters and image properties are correct!");

        for (var i = 0; i < bytes.Length; i += 4)
        {
            var temp = bytes[i];
            bytes[i] = bytes[i + 2];
            bytes[i + 2] = temp;
        }

        return bytes;
    }


    private static byte[] ConvertBgraToRgba(byte[] bytes, string Format, int bpp, int width, int height, byte[] compSel)
    {
        if (bytes == null)
            throw new Exception("Data block returned null. Make sure the parameters and image properties are correct!");

        var size = width * height * 4;
        var NewImageData = new byte[size];

        var comp = new byte[6] { 0, 0xFF, 0, 0, 0, 0xFF };

        for (var y = 0; y < height; y += 1)
        for (var x = 0; x < width; x += 1)
        {
            var pos = (y * width + x) * bpp;
            var pos_ = (y * width + x) * 4;

            var pixel = 0;
            for (var i = 0; i < bpp; i += 1)
                pixel |= bytes[pos + i] << (8 * i);

            comp = GetComponentsFromPixel(Format, pixel, comp);

            NewImageData[pos_ + 3] = comp[compSel[3]];
            NewImageData[pos_ + 2] = comp[compSel[2]];
            NewImageData[pos_ + 1] = comp[compSel[1]];
            NewImageData[pos_ + 0] = comp[compSel[0]];
        }

        return NewImageData;
    }

    private static byte[] GetComponentsFromPixel(string Format, int pixel, byte[] comp)
    {
        switch (Format)
        {
            case "RGBX8":
                comp[2] = (byte)(pixel & 0xFF);
                comp[3] = (byte)((pixel & 0xFF00) >> 8);
                comp[4] = (byte)((pixel & 0xFF0000) >> 16);
                comp[5] = (byte)((pixel & 0xFF000000) >> 24);
                break;
            case "RGBA8":
                comp[2] = (byte)(pixel & 0xFF);
                comp[3] = (byte)((pixel & 0xFF00) >> 8);
                comp[4] = (byte)((pixel & 0xFF0000) >> 16);
                comp[5] = (byte)((pixel & 0xFF000000) >> 24);
                break;
            case "RGBA4":
                comp[2] = (byte)((pixel & 0xF) * 17);
                comp[3] = (byte)(((pixel & 0xF0) >> 4) * 17);
                comp[4] = (byte)(((pixel & 0xF00) >> 8) * 17);
                comp[5] = (byte)(((pixel & 0xF000) >> 12) * 17);
                break;
            case "RGBA5":
                comp[2] = (byte)(((pixel & 0xF800) >> 11) / 0x1F * 0xFF);
                comp[3] = (byte)(((pixel & 0x7E0) >> 5) / 0x3F * 0xFF);
                comp[4] = (byte)((pixel & 0x1F) / 0x1F * 0xFF);
                break;
        }

        return comp;
    }

    public Properties GenericProperties
    {
        get
        {
            var prop = new Properties();
            prop.Height = Height;
            prop.Width = Width;
            prop.Format = Format;
            prop.Depth = Depth;
            prop.MipCount = MipCount;
            prop.ArrayCount = ArrayCount;
            prop.ImageSize = (uint)GetImageData().Length;

            return prop;
        }
    }

    public class Properties
    {
        [Browsable(true)]
        [ReadOnly(true)]
        [Description("Height of the image.")]
        [Category("Image Info")]
        public uint Height { get; set; }

        [Browsable(true)]
        [ReadOnly(true)]
        [Description("Width of the image.")]
        [Category("Image Info")]
        public uint Width { get; set; }

        [Browsable(true)]
        [ReadOnly(true)]
        [Description("Depth of the image (3D type).")]
        [Category("Image Info")]
        public uint Depth { get; set; }

        [Browsable(true)]
        [ReadOnly(true)]
        [Description("Format of the image.")]
        [Category("Image Info")]
        public TEX_FORMAT Format { get; set; }

        [Browsable(true)]
        [ReadOnly(true)]
        [Description("Mip map count of the image.")]
        [Category("Image Info")]
        public uint MipCount { get; set; }

        [Browsable(true)]
        [ReadOnly(true)]
        [Description("Array count of the image for multiple surfaces.")]
        [Category("Image Info")]
        public uint ArrayCount { get; set; }

        [Browsable(true)]
        [ReadOnly(true)]
        [Description("The image size in bytes.")]
        [Category("Image Info")]
        public uint ImageSize { get; set; }

        [Browsable(true)]
        [ReadOnly(true)]
        [Description("The swizzle value.")]
        [Category("Image Info")]
        public uint Swizzle { get; set; }
    }
}

public class FileWriter : BinaryDataWriter
{
    public bool ReverseMagic { get; set; } = false;

    public void CheckByteOrderMark(uint ByteOrderMark)
    {
        if (ByteOrderMark == 0xFEFF)
            ByteOrder = ByteOrder.BigEndian;
        else
            ByteOrder = ByteOrder.LittleEndian;
    }

    public FileWriter(Stream stream, bool leaveOpen = false)
        : base(stream, Encoding.ASCII, leaveOpen)
    {
    }

    public FileWriter(Stream stream, Encoding encoding, bool leaveOpen = false)
        : base(stream, encoding, leaveOpen)
    {
    }

    public FileWriter(string fileName)
        : this(new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read))
    {
    }

    public FileWriter(byte[] data)
        : this(new MemoryStream(data))
    {
    }

    public void WriteSignature(string value)
    {
        if (ReverseMagic)
            Write(Encoding.ASCII.GetBytes(new string(value.Reverse().ToArray())));
        else
            Write(Encoding.ASCII.GetBytes(value));
    }

    public void WriteString(string value, Encoding encoding = null)
    {
        Write(value, BinaryStringFormat.ZeroTerminated, encoding ?? Encoding);
    }

    public void WriteUint64Offset(long target)
    {
        var pos = Position;
        using (TemporarySeek(target, SeekOrigin.Begin))
        {
            Write(pos);
        }
    }

    public void SetByteOrder(bool IsBigEndian)
    {
        if (IsBigEndian)
            ByteOrder = ByteOrder.BigEndian;
        else
            ByteOrder = ByteOrder.LittleEndian;
    }

    public void WriteString(string text, uint fixedSize, Encoding encoding = null)
    {
        var pos = Position;
        WriteString(text, encoding);
        SeekBegin(pos + fixedSize);
    }

    public void Write(object value, long pos)
    {
        using (TemporarySeek(pos, SeekOrigin.Begin))
        {
            if (value is uint) Write((uint)value);
            else if (value is int) Write((int)value);
            else if (value is long) Write((long)value);
            else if (value is ulong) Write((ulong)value);
            else if (value is ushort) Write((ushort)value);
            else if (value is short) Write((short)value);
            else if (value is sbyte) Write((sbyte)value);
            else if (value is byte) Write((byte)value);
        }
    }

    //Writes the total size of a section as a uint. 
    public void WriteSectionSizeU32(long position, long startPosition, long endPosition)
    {
        WriteSectionSizeU32(position, endPosition - startPosition);
    }

    public void WriteSectionSizeU32(long position, long size)
    {
        using (TemporarySeek(position, SeekOrigin.Begin))
        {
            Write((uint)size);
        }
    }

    //
    // RelativeOffsetPosition controls the relative position the offset starts at
    //
    public void WriteUint32Offset(long target, long relativePosition = 0)
    {
        var pos = Position;
        using (TemporarySeek(target, SeekOrigin.Begin))
        {
            Write((uint)(pos - relativePosition));
        }
    }

    public void WriteUint16Offset(long target, long relativePosition)
    {
        var pos = Position;
        using (TemporarySeek(target, SeekOrigin.Begin))
        {
            Write((ushort)(pos - relativePosition));
        }
    }

    public void SeekBegin(uint Offset)
    {
        Seek(Offset, SeekOrigin.Begin);
    }

    public void SeekBegin(int Offset)
    {
        Seek(Offset, SeekOrigin.Begin);
    }

    public void SeekBegin(long Offset)
    {
        Seek(Offset, SeekOrigin.Begin);
    }

    /// <summary>
    /// Aligns the data by writing bytes (rather than seeking)
    /// </summary>
    /// <param name="alignment"></param>
    /// <param name="value"></param>
    public void AlignBytes(int alignment, byte value = 0x00)
    {
        var startPos = Position;
        var position = Seek((-Position % alignment + alignment) % alignment, SeekOrigin.Current);

        Seek(startPos, SeekOrigin.Begin);
        while (Position != position) Write(value);
    }
}


//Data from https://github.com/jam1garner/Smash-Forge/blob/master/Smash%20Forge/Filetypes/Textures/DDS.cs
public class DDS : STGenericTexture
{
    public STGenericTexture IconTexture => this;


    public PixelInternalFormat pixelInternalFormat;
    public PixelFormat pixelFormat;
    public PixelType pixelType;

    public void SetFlags(DXGI_FORMAT Format, bool UseDX10 = false, bool isCubeMap = false)
    {
        header.flags = (uint)(DDSD.CAPS | DDSD.HEIGHT | DDSD.WIDTH | DDSD.PIXELFORMAT | DDSD.MIPMAPCOUNT |
                              DDSD.LINEARSIZE);
        header.caps = (uint)DDSCAPS.TEXTURE;
        if (header.mipmapCount > 1)
            header.caps |= (uint)(DDSCAPS.COMPLEX | DDSCAPS.MIPMAP);

        if (isCubeMap)
            header.caps2 |= (uint)(DDSCAPS2.CUBEMAP | DDSCAPS2.CUBEMAP_POSITIVEX | DDSCAPS2.CUBEMAP_NEGATIVEX |
                                   DDSCAPS2.CUBEMAP_POSITIVEY | DDSCAPS2.CUBEMAP_NEGATIVEY |
                                   DDSCAPS2.CUBEMAP_POSITIVEZ | DDSCAPS2.CUBEMAP_NEGATIVEZ);

        if (UseDX10)
        {
            header.ddspf.flags = (uint)DDPF.FOURCC;
            header.ddspf.fourCC = FOURCC_DX10;
            if (DX10header == null)
                DX10header = new DX10Header();

            IsDX10 = true;
            DX10header.DXGI_Format = Format;
            if (isCubeMap)
            {
                DX10header.arrayFlag = ArrayCount / 6;
                DX10header.miscFlag = 0x4;
            }

            return;
        }

        switch (Format)
        {
            case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
                header.ddspf.flags = (uint)(DDPF.RGB | DDPF.ALPHAPIXELS);
                header.ddspf.RGBBitCount = 0x8 * 4;
                header.ddspf.RBitMask = 0x000000FF;
                header.ddspf.GBitMask = 0x0000FF00;
                header.ddspf.BBitMask = 0x00FF0000;
                header.ddspf.ABitMask = 0xFF000000;
                pixelInternalFormat = PixelInternalFormat.SrgbAlpha;
                pixelFormat = PixelFormat.Rgba;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM:
                header.ddspf.flags = (uint)(DDPF.RGB | DDPF.ALPHAPIXELS);
                header.ddspf.RGBBitCount = 24;
                header.ddspf.RBitMask = (uint)R8G8B8_MASKS[0];
                header.ddspf.GBitMask = (uint)R8G8B8_MASKS[1];
                header.ddspf.BBitMask = (uint)R8G8B8_MASKS[2];
                header.ddspf.ABitMask = (uint)R8G8B8_MASKS[3];
                pixelInternalFormat = PixelInternalFormat.SrgbAlpha;
                pixelFormat = PixelFormat.Rgba;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB:
            case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
                header.ddspf.flags = (uint)DDPF.FOURCC;
                header.ddspf.fourCC = FOURCC_DXT1;
                pixelInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt1Ext;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB:
            case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM:
                header.ddspf.flags = (uint)DDPF.FOURCC;
                header.ddspf.fourCC = FOURCC_DXT3;
                pixelInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt3Ext;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB:
            case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
                header.ddspf.flags = (uint)DDPF.FOURCC;
                header.ddspf.fourCC = FOURCC_DXT5;
                pixelInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt5Ext;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM:
                header.ddspf.flags = (uint)DDPF.FOURCC;
                header.ddspf.fourCC = FOURCC_BC4U;
                pixelInternalFormat = PixelInternalFormat.CompressedRedRgtc1;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM:
                header.ddspf.flags = (uint)DDPF.FOURCC;
                header.ddspf.fourCC = FOURCC_BC4S;
                pixelInternalFormat = PixelInternalFormat.CompressedSignedRedRgtc1;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM:
                header.ddspf.flags = (uint)DDPF.FOURCC;
                header.ddspf.fourCC = FOURCC_BC5U;
                pixelInternalFormat = PixelInternalFormat.CompressedRgRgtc2;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM:
                header.ddspf.flags = (uint)DDPF.FOURCC;
                header.ddspf.fourCC = FOURCC_BC5S;
                pixelInternalFormat = PixelInternalFormat.CompressedSignedRgRgtc2;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16:
            case DXGI_FORMAT.DXGI_FORMAT_BC6H_SF16:
            case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM:
                header.ddspf.flags = (uint)DDPF.FOURCC;
                header.ddspf.fourCC = FOURCC_DX10;
                if (DX10header == null)
                    DX10header = new DX10Header();

                IsDX10 = true;
                DX10header.DXGI_Format = Format;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB:
                header.ddspf.flags = (uint)DDPF.FOURCC;
                header.ddspf.fourCC = FOURCC_DX10;
                if (DX10header == null)
                    DX10header = new DX10Header();

                IsDX10 = true;
                DX10header.DXGI_Format = DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM;
                break;
        }
    }

    public override void SetImageData(Bitmap bitmap, int ArrayLevel)
    {
        SetArrayLevel(GenerateMipsAndCompress(bitmap, MipCount, Format), ArrayLevel);
    }

    public static byte[] GenerateMipsAndCompress(Bitmap bitmap, uint MipCount, TEX_FORMAT Format,
        bool multiThread = false, float alphaRef = 0.5f, STCompressionMode CompressionMode = STCompressionMode.Fast)
    {
        /*byte[] DecompressedData = BitmapExtension.ImageToByte(bitmap);
        DecompressedData = ConvertBgraToRgba(DecompressedData);

        Bitmap Image = BitmapExtension.GetBitmap(DecompressedData, bitmap.Width, bitmap.Height);

        List<byte[]> mipmaps = new List<byte[]>();
        for (int mipLevel = 0; mipLevel < MipCount; mipLevel++)
        {
            int width = Math.Max(1, bitmap.Width >> mipLevel);
            int height = Math.Max(1, bitmap.Height >> mipLevel);

            Image = BitmapExtension.Resize(Image, width, height);
            mipmaps.Add(STGenericTexture.CompressBlock(BitmapExtension.ImageToByte(Image),
                Image.Width, Image.Height, Format, alphaRef, multiThread, CompressionMode));
        }
        Image.Dispose();

        return Utils.CombineByteArray(mipmaps.ToArray());*/

        return new byte[] { 0 };
    }

    public override TEX_FORMAT[] SupportedFormats
    {
        get
        {
            return new TEX_FORMAT[]
            {
                TEX_FORMAT.BC1_UNORM,
                TEX_FORMAT.BC1_UNORM_SRGB,
                TEX_FORMAT.BC2_UNORM,
                TEX_FORMAT.BC2_UNORM_SRGB,
                TEX_FORMAT.BC3_UNORM,
                TEX_FORMAT.BC3_UNORM_SRGB,
                TEX_FORMAT.BC4_UNORM,
                TEX_FORMAT.BC4_SNORM,
                TEX_FORMAT.BC5_UNORM,
                TEX_FORMAT.BC5_SNORM,
                TEX_FORMAT.BC6H_UF16,
                TEX_FORMAT.BC6H_SF16,
                TEX_FORMAT.BC7_UNORM,
                TEX_FORMAT.BC7_UNORM_SRGB,
                TEX_FORMAT.B5G5R5A1_UNORM,
                TEX_FORMAT.B5G6R5_UNORM,
                TEX_FORMAT.B8G8R8A8_UNORM_SRGB,
                TEX_FORMAT.B8G8R8A8_UNORM,
                TEX_FORMAT.R10G10B10A2_UNORM,
                TEX_FORMAT.R16_UNORM,
                TEX_FORMAT.B4G4R4A4_UNORM,
                TEX_FORMAT.R8G8B8A8_UNORM_SRGB,
                TEX_FORMAT.R8G8B8A8_UNORM,
                TEX_FORMAT.R8_UNORM,
                TEX_FORMAT.R8G8_UNORM,
                TEX_FORMAT.R32G8X24_FLOAT
            };
        }
    }

    public override bool CanEdit { get; set; } = true;

    public bool CanSave { get; set; } = false;
    public bool FileIsEdited { get; set; } = false;
    public bool FileIsCompressed { get; set; } = false;
    public string[] Description { get; set; } = new string[] { "Microsoft DDS" };
    public string[] Extension { get; set; } = new string[] { "*.dds" };
    public string FileName { get; set; }
    public bool IsActive { get; set; } = false;
    public bool UseEditMenu { get; set; } = false;
    public int Alignment { get; set; } = 0;
    public string FilePath { get; set; }

    public bool Identify(Stream stream)
    {
        using (var reader = new FileReader(stream, true))
        {
            return reader.CheckSignature(4, "DDS ");
        }
    }

    public Type[] Types
    {
        get
        {
            var types = new List<Type>();
            return types.ToArray();
        }
    }

    public void Unload()
    {
    }

    public void Save(Stream stream)
    {
        Save(this, stream, GetSurfaces());
    }

    public const uint FOURCC_DXT1 = 0x31545844;
    public const uint FOURCC_DXT2 = 0x32545844;
    public const uint FOURCC_DXT3 = 0x33545844;
    public const uint FOURCC_DXT4 = 0x34545844;
    public const uint FOURCC_DXT5 = 0x35545844;
    public const uint FOURCC_ATI1 = 0x31495441;
    public const uint FOURCC_BC4U = 0x55344342;
    public const uint FOURCC_BC4S = 0x53344342;
    public const uint FOURCC_BC5U = 0x55354342;
    public const uint FOURCC_BC5S = 0x53354342;
    public const uint FOURCC_DX10 = 0x30315844;

    public const uint FOURCC_ATI2 = 0x32495441;
    public const uint FOURCC_RXGB = 0x42475852;

    // RGBA Masks
    private static int[] A1R5G5B5_MASKS = { 0x7C00, 0x03E0, 0x001F, 0x8000 };
    private static int[] X1R5G5B5_MASKS = { 0x7C00, 0x03E0, 0x001F, 0x0000 };
    private static int[] A4R4G4B4_MASKS = { 0x0F00, 0x00F0, 0x000F, 0xF000 };
    private static int[] X4R4G4B4_MASKS = { 0x0F00, 0x00F0, 0x000F, 0x0000 };
    private static int[] R5G6B5_MASKS = { 0xF800, 0x07E0, 0x001F, 0x0000 };
    private static int[] R8G8B8_MASKS = { 0xFF0000, 0x00FF00, 0x0000FF, 0x000000 };
    private static uint[] A8B8G8R8_MASKS = { 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000 };
    private static int[] X8B8G8R8_MASKS = { 0x000000FF, 0x0000FF00, 0x00FF0000, 0x00000000 };
    private static uint[] A8R8G8B8_MASKS = { 0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000 };
    private static int[] X8R8G8B8_MASKS = { 0x00FF0000, 0x0000FF00, 0x000000FF, 0x00000000 };

    private static int[] L8_MASKS = { 0x000000FF, 0x0000 };
    private static int[] A8L8_MASKS = { 0x000000FF, 0x0F00 };

    public enum CubemapFace
    {
        PosX,
        NegX,
        PosY,
        NegY,
        PosZ,
        NegZ
    }

    [Flags]
    public enum DDSD : uint
    {
        CAPS = 0x00000001,
        HEIGHT = 0x00000002,
        WIDTH = 0x00000004,
        PITCH = 0x00000008,
        PIXELFORMAT = 0x00001000,
        MIPMAPCOUNT = 0x00020000,
        LINEARSIZE = 0x00080000,
        DEPTH = 0x00800000
    }

    [Flags]
    public enum DDPF : uint
    {
        ALPHAPIXELS = 0x00000001,
        ALPHA = 0x00000002,
        FOURCC = 0x00000004,
        RGB = 0x00000040,
        YUV = 0x00000200,
        LUMINANCE = 0x00020000
    }

    [Flags]
    public enum DDSCAPS : uint
    {
        COMPLEX = 0x00000008,
        TEXTURE = 0x00001000,
        MIPMAP = 0x00400000
    }

    [Flags]
    public enum DDSCAPS2 : uint
    {
        CUBEMAP = 0x00000200,
        CUBEMAP_POSITIVEX = 0x00000400 | CUBEMAP,
        CUBEMAP_NEGATIVEX = 0x00000800 | CUBEMAP,
        CUBEMAP_POSITIVEY = 0x00001000 | CUBEMAP,
        CUBEMAP_NEGATIVEY = 0x00002000 | CUBEMAP,
        CUBEMAP_POSITIVEZ = 0x00004000 | CUBEMAP,
        CUBEMAP_NEGATIVEZ = 0x00008000 | CUBEMAP,

        CUBEMAP_ALLFACES = CUBEMAP_POSITIVEX | CUBEMAP_NEGATIVEX |
                           CUBEMAP_POSITIVEY | CUBEMAP_NEGATIVEY |
                           CUBEMAP_POSITIVEZ | CUBEMAP_NEGATIVEZ,
        VOLUME = 0x00200000
    }

    public static bool getFormatBlock(uint fourCC)
    {
        switch (fourCC)
        {
            case FOURCC_DXT1:
            case FOURCC_DXT2:
            case FOURCC_DXT3:
            case FOURCC_DXT4:
            case FOURCC_DXT5:
            case FOURCC_ATI1:
            case FOURCC_ATI2:
            case FOURCC_BC4U:
            case FOURCC_BC4S:
            case FOURCC_BC5U:
            case FOURCC_BC5S:
                return true;
            default:
                return false;
        }
    }

    public void SetFourCC(DXGI_FORMAT Format)
    {
        switch (Format)
        {
            case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB:
                header.ddspf.fourCC = FOURCC_DXT1;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB:
                header.ddspf.fourCC = FOURCC_DXT3;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB:
                header.ddspf.fourCC = FOURCC_DXT5;
                break;
        }
    }

    public bool IsDX10;

    public Header header;
    public DX10Header DX10header;

    public class Header
    {
        public uint size = 0x7C;
        public uint flags = 0x00000000;
        public uint height = 0;
        public uint width = 0;
        public uint pitchOrLinearSize = 0;
        public uint depth = 0;
        public uint mipmapCount = 0;
        public uint[] reserved1 = new uint[11];
        public DDS_PixelFormat ddspf = new();

        public class DDS_PixelFormat
        {
            public uint size = 0x20;
            public uint flags = 0x00000000;
            public uint fourCC;
            public uint RGBBitCount = 0;
            public uint RBitMask = 0x00000000;
            public uint GBitMask = 0x00000000;
            public uint BBitMask = 0x00000000;
            public uint ABitMask = 0x00000000;
        }

        public uint caps = 0;
        public uint caps2 = 0;
        public uint caps3 = 0;
        public uint caps4 = 0;
        public uint reserved2 = 0;
    }

    public class DX10Header
    {
        public DXGI_FORMAT DXGI_Format;
        public uint ResourceDim;
        public uint miscFlag;
        public uint arrayFlag;
        public uint miscFlags2;
    }

    public byte[] bdata;
    public List<byte[]> mipmaps = new();

    public enum DXGI_FORMAT : uint
    {
        DXGI_FORMAT_UNKNOWN = 0,
        DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
        DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
        DXGI_FORMAT_R32G32B32A32_UINT = 3,
        DXGI_FORMAT_R32G32B32A32_SINT = 4,
        DXGI_FORMAT_R32G32B32_TYPELESS = 5,
        DXGI_FORMAT_R32G32B32_FLOAT = 6,
        DXGI_FORMAT_R32G32B32_UINT = 7,
        DXGI_FORMAT_R32G32B32_SINT = 8,
        DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
        DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
        DXGI_FORMAT_R16G16B16A16_UNORM = 11,
        DXGI_FORMAT_R16G16B16A16_UINT = 12,
        DXGI_FORMAT_R16G16B16A16_SNORM = 13,
        DXGI_FORMAT_R16G16B16A16_SINT = 14,
        DXGI_FORMAT_R32G32_TYPELESS = 15,
        DXGI_FORMAT_R32G32_FLOAT = 16,
        DXGI_FORMAT_R32G32_UINT = 17,
        DXGI_FORMAT_R32G32_SINT = 18,
        DXGI_FORMAT_R32G8X24_TYPELESS = 19,
        DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
        DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
        DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
        DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
        DXGI_FORMAT_R10G10B10A2_UNORM = 24,
        DXGI_FORMAT_R10G10B10A2_UINT = 25,
        DXGI_FORMAT_R11G11B10_FLOAT = 26,
        DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
        DXGI_FORMAT_R8G8B8A8_UNORM = 28,
        DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
        DXGI_FORMAT_R8G8B8A8_UINT = 30,
        DXGI_FORMAT_R8G8B8A8_SNORM = 31,
        DXGI_FORMAT_R8G8B8A8_SINT = 32,
        DXGI_FORMAT_R16G16_TYPELESS = 33,
        DXGI_FORMAT_R16G16_FLOAT = 34,
        DXGI_FORMAT_R16G16_UNORM = 35,
        DXGI_FORMAT_R16G16_UINT = 36,
        DXGI_FORMAT_R16G16_SNORM = 37,
        DXGI_FORMAT_R16G16_SINT = 38,
        DXGI_FORMAT_R32_TYPELESS = 39,
        DXGI_FORMAT_D32_FLOAT = 40,
        DXGI_FORMAT_R32_FLOAT = 41,
        DXGI_FORMAT_R32_UINT = 42,
        DXGI_FORMAT_R32_SINT = 43,
        DXGI_FORMAT_R24G8_TYPELESS = 44,
        DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
        DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
        DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
        DXGI_FORMAT_R8G8_TYPELESS = 48,
        DXGI_FORMAT_R8G8_UNORM = 49,
        DXGI_FORMAT_R8G8_UINT = 50,
        DXGI_FORMAT_R8G8_SNORM = 51,
        DXGI_FORMAT_R8G8_SINT = 52,
        DXGI_FORMAT_R16_TYPELESS = 53,
        DXGI_FORMAT_R16_FLOAT = 54,
        DXGI_FORMAT_D16_UNORM = 55,
        DXGI_FORMAT_R16_UNORM = 56,
        DXGI_FORMAT_R16_UINT = 57,
        DXGI_FORMAT_R16_SNORM = 58,
        DXGI_FORMAT_R16_SINT = 59,
        DXGI_FORMAT_R8_TYPELESS = 60,
        DXGI_FORMAT_R8_UNORM = 61,
        DXGI_FORMAT_R8_UINT = 62,
        DXGI_FORMAT_R8_SNORM = 63,
        DXGI_FORMAT_R8_SINT = 64,
        DXGI_FORMAT_A8_UNORM = 65,
        DXGI_FORMAT_R1_UNORM = 66,
        DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
        DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
        DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
        DXGI_FORMAT_BC1_TYPELESS = 70,
        DXGI_FORMAT_BC1_UNORM = 71,
        DXGI_FORMAT_BC1_UNORM_SRGB = 72,
        DXGI_FORMAT_BC2_TYPELESS = 73,
        DXGI_FORMAT_BC2_UNORM = 74,
        DXGI_FORMAT_BC2_UNORM_SRGB = 75,
        DXGI_FORMAT_BC3_TYPELESS = 76,
        DXGI_FORMAT_BC3_UNORM = 77,
        DXGI_FORMAT_BC3_UNORM_SRGB = 78,
        DXGI_FORMAT_BC4_TYPELESS = 79,
        DXGI_FORMAT_BC4_UNORM = 80,
        DXGI_FORMAT_BC4_SNORM = 81,
        DXGI_FORMAT_BC5_TYPELESS = 82,
        DXGI_FORMAT_BC5_UNORM = 83,
        DXGI_FORMAT_BC5_SNORM = 84,
        DXGI_FORMAT_B5G6R5_UNORM = 85,
        DXGI_FORMAT_B5G5R5A1_UNORM = 86,
        DXGI_FORMAT_B8G8R8A8_UNORM = 87,
        DXGI_FORMAT_B8G8R8X8_UNORM = 88,
        DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
        DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
        DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
        DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
        DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
        DXGI_FORMAT_BC6H_TYPELESS = 94,
        DXGI_FORMAT_BC6H_UF16 = 95,
        DXGI_FORMAT_BC6H_SF16 = 96,
        DXGI_FORMAT_BC7_TYPELESS = 97,
        DXGI_FORMAT_BC7_UNORM = 98,
        DXGI_FORMAT_BC7_UNORM_SRGB = 99,
        DXGI_FORMAT_AYUV = 100,
        DXGI_FORMAT_Y410 = 101,
        DXGI_FORMAT_Y416 = 102,
        DXGI_FORMAT_NV12 = 103,
        DXGI_FORMAT_P010 = 104,
        DXGI_FORMAT_P016 = 105,
        DXGI_FORMAT_420_OPAQUE = 106,
        DXGI_FORMAT_YUY2 = 107,
        DXGI_FORMAT_Y210 = 108,
        DXGI_FORMAT_Y216 = 109,
        DXGI_FORMAT_NV11 = 110,
        DXGI_FORMAT_AI44 = 111,
        DXGI_FORMAT_IA44 = 112,
        DXGI_FORMAT_P8 = 113,
        DXGI_FORMAT_A8P8 = 114,
        DXGI_FORMAT_B4G4R4A4_UNORM = 115,
        DXGI_FORMAT_P208 = 130,
        DXGI_FORMAT_V208 = 131,
        DXGI_FORMAT_V408 = 132,


        DXGI_FORMAT_ASTC_4X4_UNORM = 134,
        DXGI_FORMAT_ASTC_4X4_UNORM_SRGB = 135,
        DXGI_FORMAT_ASTC_5X4_TYPELESS = 137,
        DXGI_FORMAT_ASTC_5X4_UNORM = 138,
        DXGI_FORMAT_ASTC_5X4_UNORM_SRGB = 139,
        DXGI_FORMAT_ASTC_5X5_TYPELESS = 141,
        DXGI_FORMAT_ASTC_5X5_UNORM = 142,
        DXGI_FORMAT_ASTC_5X5_UNORM_SRGB = 143,
        DXGI_FORMAT_ASTC_6X5_TYPELESS = 145,
        DXGI_FORMAT_ASTC_6X5_UNORM = 146,
        DXGI_FORMAT_ASTC_6X5_UNORM_SRGB = 147,
        DXGI_FORMAT_ASTC_6X6_TYPELESS = 149,
        DXGI_FORMAT_ASTC_6X6_UNORM = 150,
        DXGI_FORMAT_ASTC_6X6_UNORM_SRGB = 151,
        DXGI_FORMAT_ASTC_8X5_TYPELESS = 153,
        DXGI_FORMAT_ASTC_8X5_UNORM = 154,
        DXGI_FORMAT_ASTC_8X5_UNORM_SRGB = 155,
        DXGI_FORMAT_ASTC_8X6_TYPELESS = 157,
        DXGI_FORMAT_ASTC_8X6_UNORM = 158,
        DXGI_FORMAT_ASTC_8X6_UNORM_SRGB = 159,
        DXGI_FORMAT_ASTC_8X8_TYPELESS = 161,
        DXGI_FORMAT_ASTC_8X8_UNORM = 162,
        DXGI_FORMAT_ASTC_8X8_UNORM_SRGB = 163,
        DXGI_FORMAT_ASTC_10X5_TYPELESS = 165,
        DXGI_FORMAT_ASTC_10X5_UNORM = 166,
        DXGI_FORMAT_ASTC_10X5_UNORM_SRGB = 167,
        DXGI_FORMAT_ASTC_10X6_TYPELESS = 169,
        DXGI_FORMAT_ASTC_10X6_UNORM = 170,
        DXGI_FORMAT_ASTC_10X6_UNORM_SRGB = 171,
        DXGI_FORMAT_ASTC_10X8_TYPELESS = 173,
        DXGI_FORMAT_ASTC_10X8_UNORM = 174,
        DXGI_FORMAT_ASTC_10X8_UNORM_SRGB = 175,
        DXGI_FORMAT_ASTC_10X10_TYPELESS = 177,
        DXGI_FORMAT_ASTC_10X10_UNORM = 178,
        DXGI_FORMAT_ASTC_10X10_UNORM_SRGB = 179,
        DXGI_FORMAT_ASTC_12X10_TYPELESS = 181,
        DXGI_FORMAT_ASTC_12X10_UNORM = 182,
        DXGI_FORMAT_ASTC_12X10_UNORM_SRGB = 183,
        DXGI_FORMAT_ASTC_12X12_TYPELESS = 185,
        DXGI_FORMAT_ASTC_12X12_UNORM = 186,
        DXGI_FORMAT_ASTC_12X12_UNORM_SRGB = 187,

        DXGI_FORMAT_FORCE_UINT = 0xFFFFFFFF
    }

    public enum DXGI_ASTC_FORMAT
    {
    }

    public DDS()
    {
    }

    public DDS(byte[] data)
    {
        var reader = new FileReader(new MemoryStream(data));
        reader.ByteOrder = ByteOrder.LittleEndian;
        Load(reader);
    }

    public DDS(string FileName)
    {
        var reader = new FileReader(new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read));

        Load(reader);
    }


    public void Load(BinaryDataReader reader)
    {
        reader.Seek(0);
        var Magic = reader.ReadString(4);
        Console.WriteLine(Magic);

        header = new Header();
        header.size = reader.ReadUInt32();
        header.flags = reader.ReadUInt32();
        header.height = reader.ReadUInt32();
        header.width = reader.ReadUInt32();
        header.pitchOrLinearSize = reader.ReadUInt32();
        header.depth = reader.ReadUInt32();
        header.mipmapCount = reader.ReadUInt32();
        header.reserved1 = new uint[11];
        for (var i = 0; i < 11; ++i)
            header.reserved1[i] = reader.ReadUInt32();

        header.ddspf.size = reader.ReadUInt32();
        header.ddspf.flags = reader.ReadUInt32();
        header.ddspf.fourCC = reader.ReadUInt32();
        header.ddspf.RGBBitCount = reader.ReadUInt32();
        header.ddspf.RBitMask = reader.ReadUInt32();
        header.ddspf.GBitMask = reader.ReadUInt32();
        header.ddspf.BBitMask = reader.ReadUInt32();
        header.ddspf.ABitMask = reader.ReadUInt32();

        header.caps = reader.ReadUInt32();
        header.caps2 = reader.ReadUInt32();
        header.caps3 = reader.ReadUInt32();
        header.caps4 = reader.ReadUInt32();
        header.reserved2 = reader.ReadUInt32();

        if (header.reserved1[9] == 1414813262)
            WiiUSwizzle = true;

        ArrayCount = 1;

        var DX10HeaderSize = 0;
        if (header.ddspf.fourCC == FOURCC_DX10)
        {
            IsDX10 = true;

            DX10HeaderSize = 20;
            ReadDX10Header(reader);
        }

        if (header.caps2 == (uint)DDSCAPS2.CUBEMAP_ALLFACES) ArrayCount = 6;

        var Compressed = false;
        var HasLuminance = false;
        var HasAlpha = false;
        var IsRGB = false;

        if (header.ddspf.flags == 4)
        {
            Compressed = true;
        }
        else if (header.ddspf.flags == (uint)DDPF.LUMINANCE || header.ddspf.flags == 2)
        {
            HasLuminance = true;
        }
        else if (header.ddspf.flags == 0x20001)
        {
            HasLuminance = true;
            HasAlpha = true;
        }
        else if (header.ddspf.flags == (uint)DDPF.RGB)
        {
            IsRGB = true;
        }
        else if (header.ddspf.flags == 0x41)
        {
            IsRGB = true;
            HasAlpha = true;
            HasAlpha = true;
        }

        reader.TemporarySeek((int)(4 + header.size + DX10HeaderSize), SeekOrigin.Begin);
        var UbiExtraData = reader.ReadUInt16();
        reader.TemporarySeek(-2, SeekOrigin.Current);
        if (UbiExtraData == 12816 ||
            (UbiExtraData == 1331 &&
             IsDX10)) //me when ubisoft | for some reason theres some extra data on some mario rabbids textures god knows what it is
        {
            if (header.width == 1024 && header.height == 1024)
                reader.TemporarySeek((int)(4 + 30 + header.size + DX10HeaderSize), SeekOrigin.Begin);
            if (header.width == 512 && header.height == 512)
                reader.TemporarySeek((int)(4 + 26 + header.size + DX10HeaderSize), SeekOrigin.Begin);
        }

        bdata = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));

        Format = GetFormat();
        Width = header.width;
        Height = header.height;
        MipCount = header.mipmapCount;
        Depth = header.depth;
        if (Depth == 0)
            Depth = 1;

        var Components = new byte[4] { 0, 1, 2, 3 };

        if (!IsDX10 && !IsCompressed())
            Format = GetUncompressedType(this, Components, IsRGB, HasAlpha, HasLuminance, header.ddspf);

        RedChannel = (STChannelType)Components[0];
        GreenChannel = (STChannelType)Components[1];
        BlueChannel = (STChannelType)Components[2];
        AlphaChannel = (STChannelType)Components[3];

        reader.Dispose();
        reader.Close();
    }

    private TEX_FORMAT GetUncompressedType(DDS dds, byte[] Components, bool IsRGB, bool HasAlpha, bool HasLuminance,
        Header.DDS_PixelFormat header)
    {
        var bpp = header.RGBBitCount;
        var RedMask = header.RBitMask;
        var GreenMask = header.GBitMask;
        var BlueMask = header.BBitMask;
        var AlphaMask = HasAlpha ? header.ABitMask : 0;

        if (HasLuminance)
        {
            throw new Exception("Luminance not supported!");
        }
        else if (IsRGB)
        {
            if (bpp == 16)
            {
                if (RedMask == A1R5G5B5_MASKS[0] && GreenMask == A1R5G5B5_MASKS[1] && BlueMask == A1R5G5B5_MASKS[2] &&
                    AlphaMask == A1R5G5B5_MASKS[3])
                    return TEX_FORMAT.B5G5R5A1_UNORM;
                else if (RedMask == X1R5G5B5_MASKS[0] && GreenMask == X1R5G5B5_MASKS[1] &&
                         BlueMask == X1R5G5B5_MASKS[2] && AlphaMask == X1R5G5B5_MASKS[3])
                    return TEX_FORMAT.B5G6R5_UNORM;
                else if (RedMask == A4R4G4B4_MASKS[0] && GreenMask == A4R4G4B4_MASKS[1] &&
                         BlueMask == A4R4G4B4_MASKS[2] && AlphaMask == A4R4G4B4_MASKS[3])
                    return TEX_FORMAT.B4G4R4A4_UNORM;
                else if (RedMask == X4R4G4B4_MASKS[0] && GreenMask == X4R4G4B4_MASKS[1] &&
                         BlueMask == X4R4G4B4_MASKS[2] && AlphaMask == X4R4G4B4_MASKS[3])
                    return TEX_FORMAT.B4G4R4A4_UNORM;
                else if (RedMask == R5G6B5_MASKS[0] && GreenMask == R5G6B5_MASKS[1] && BlueMask == R5G6B5_MASKS[2] &&
                         AlphaMask == R5G6B5_MASKS[3])
                    return TEX_FORMAT.B5G6R5_UNORM;
                else
                    throw new Exception("Unsupported 16 bit image!");
            }
            else if (bpp == 24)
            {
                if (RedMask == R8G8B8_MASKS[0] && GreenMask == R8G8B8_MASKS[1] && BlueMask == R8G8B8_MASKS[2] &&
                    AlphaMask == R8G8B8_MASKS[3])
                {
                    dds.bdata = ConvertToRgba(this, "RGB8", 3, new byte[4] { 2, 1, 0, 3 });
                    return TEX_FORMAT.R8G8B8A8_UNORM;
                }
                else
                {
                    throw new Exception("Unsupported 24 bit image!");
                }
            }
            else if (bpp == 32)
            {
                if (RedMask == A8B8G8R8_MASKS[0] && GreenMask == A8B8G8R8_MASKS[1] && BlueMask == A8B8G8R8_MASKS[2] &&
                    AlphaMask == A8B8G8R8_MASKS[3])
                {
                    return TEX_FORMAT.R8G8B8A8_UNORM;
                }
                else if (RedMask == X8B8G8R8_MASKS[0] && GreenMask == X8B8G8R8_MASKS[1] &&
                         BlueMask == X8B8G8R8_MASKS[2] && AlphaMask == X8B8G8R8_MASKS[3])
                {
                    dds.bdata = ConvertToRgba(this, "RGB8X", 4, new byte[4] { 2, 1, 0, 3 });
                    return TEX_FORMAT.B8G8R8X8_UNORM;
                }
                else if (RedMask == A8R8G8B8_MASKS[0] && GreenMask == A8R8G8B8_MASKS[1] &&
                         BlueMask == A8R8G8B8_MASKS[2] && AlphaMask == A8R8G8B8_MASKS[3])
                {
                    dds.bdata = ConvertBgraToRgba(dds.bdata);
                    return TEX_FORMAT.R8G8B8A8_UNORM;
                }
                else if (RedMask == X8R8G8B8_MASKS[0] && GreenMask == X8R8G8B8_MASKS[1] &&
                         BlueMask == X8R8G8B8_MASKS[2] && AlphaMask == X8R8G8B8_MASKS[3])
                {
                    dds.bdata = ConvertToRgba(this, "RGB8X", 4, new byte[4] { 0, 1, 2, 3 });
                    return TEX_FORMAT.B8G8R8X8_UNORM;
                }
                else
                {
                    throw new Exception("Unsupported 32 bit image!");
                }
            }
        }
        else
        {
            throw new Exception("Unknown type!");
        }

        return TEX_FORMAT.UNKNOWN;
    }

    private static byte[] ConvertRgb8ToRgbx8(byte[] bytes)
    {
        var size = bytes.Length / 3;
        var NewData = new byte[size];

        for (var i = 0; i < size; i++)
        {
            NewData[4 * i + 0] = bytes[3 * i + 0];
            NewData[4 * i + 1] = bytes[3 * i + 1];
            NewData[4 * i + 2] = bytes[3 * i + 2];
            NewData[4 * i + 3] = 0xFF;
        }

        return NewData;
    }

    //Thanks abood. Based on https://github.com/aboood40091/BNTX-Editor/blob/master/formConv.py
    private static byte[] ConvertToRgba(DDS dds, string Format, int bpp, byte[] compSel)
    {
        var bytes = dds.bdata;

        if (bytes == null)
            throw new Exception("Data block returned null. Make sure the parameters and image properties are correct!");

        var mipmaps = new List<byte[]>();

        uint Offset = 0;

        for (byte a = 0; a < dds.ArrayCount; ++a)
        for (byte m = 0; m < dds.MipCount; ++m)
        {
            var MipWidth = Math.Max(1, dds.Width >> m);
            var MipHeight = Math.Max(1, dds.Height >> m);

            var NewSize = MipWidth * MipHeight * 4;
            var OldSize = MipWidth * MipHeight * (uint)bpp;

            var NewImageData = new byte[NewSize];
            mipmaps.Add(NewImageData);

            var comp = new byte[4] { 0, 0, 0, 0xFF };

            for (var j = 0; j < MipHeight * MipWidth; j++)
            {
                var pos = Offset + j * bpp;
                var pos_ = j * 4;

                var pixel = 0;
                for (var i = 0; i < bpp; i += 1)
                    pixel |= bytes[pos + i] << (8 * i);

                comp = GetComponentsFromPixel(Format, pixel, comp);
                NewImageData[pos_ + 3] = comp[compSel[3]];
                NewImageData[pos_ + 2] = comp[compSel[2]];
                NewImageData[pos_ + 1] = comp[compSel[1]];
                NewImageData[pos_ + 0] = comp[compSel[0]];
            }

            Offset += OldSize;
        }

        return CombineByteArray(mipmaps.ToArray());
    }

    private static byte[] GetComponentsFromPixel(string Format, int pixel, byte[] comp)
    {
        switch (Format)
        {
            case "RGB8X":
                comp[0] = (byte)(pixel & 0xFF);
                comp[1] = (byte)((pixel & 0xFF00) >> 8);
                comp[2] = (byte)((pixel & 0xFF0000) >> 16);
                comp[3] = (byte)0xFF;
                break;
            case "RGB8":
                comp[0] = (byte)(pixel & 0xFF);
                comp[1] = (byte)((pixel & 0xFF00) >> 8);
                comp[2] = (byte)((pixel & 0xFF0000) >> 16);
                comp[3] = (byte)0xFF;
                break;
            case "RGBA4":
                comp[0] = (byte)((pixel & 0xF) * 17);
                comp[1] = (byte)(((pixel & 0xF0) >> 4) * 17);
                comp[2] = (byte)(((pixel & 0xF00) >> 8) * 17);
                comp[3] = (byte)(((pixel & 0xF000) >> 12) * 17);
                break;
            case "RGBA5":
                comp[0] = (byte)(((pixel & 0xF800) >> 11) / 0x1F * 0xFF);
                comp[1] = (byte)(((pixel & 0x7E0) >> 5) / 0x3F * 0xFF);
                comp[2] = (byte)((pixel & 0x1F) / 0x1F * 0xFF);
                break;
        }

        return comp;
    }

    private static byte[] ConvertBgraToRgba(byte[] bytes)
    {
        if (bytes == null)
            throw new Exception("Data block returned null. Make sure the parameters and image properties are correct!");

        for (var i = 0; i < bytes.Length; i += 4)
        {
            var temp = bytes[i];
            bytes[i] = bytes[i + 2];
            bytes[i + 2] = temp;
        }

        return bytes;
    }

    private void ReadDX10Header(BinaryDataReader reader)
    {
        DX10header = new DX10Header();
        DX10header.DXGI_Format = reader.ReadEnum<DXGI_FORMAT>(true);
        DX10header.ResourceDim = reader.ReadUInt32();
        DX10header.miscFlag = reader.ReadUInt32();
        DX10header.arrayFlag = reader.ReadUInt32();
        DX10header.miscFlags2 = reader.ReadUInt32();

        ArrayCount = DX10header.arrayFlag;
    }

    public bool SwitchSwizzle = false;
    public bool WiiUSwizzle = false;

    public override byte[] GetImageData(int ArrayLevel = 0, int MipLevel = 0, int DepthLevel = 0)
    {
        if (IsAtscFormat(Format))
        {
            SwitchSwizzle = true;
        }

        else if (WiiUSwizzle)
        {
            var bpp = GetBytesPerPixel(Format);

            var surf = new GX2Surface();
            surf.bpp = bpp;
            surf.height = Height;
            surf.width = Width;
            surf.aa = (uint)GX2AAMode.GX2_AA_MODE_1X;
            surf.alignment = 0;
            surf.depth = 1;
            surf.dim = (uint)GX2SurfaceDimension.DIM_2D;
            surf.format = (uint)ConvertToGx2Format(Format);
            surf.use = (uint)GX2SurfaceUse.USE_COLOR_BUFFER;
            surf.pitch = 0;
            surf.data = bdata;
            surf.numMips = MipCount;
            surf.mipOffset = new uint[0];
            surf.mipData = bdata;
            surf.tileMode = (uint)GX2TileMode.MODE_2D_TILED_THIN1;
            surf.swizzle = 0;
            surf.numArray = 1;

            return Decode(surf, ArrayLevel, MipLevel);
        }

        return GetArrayFaces(this, ArrayCount, DepthLevel)[ArrayLevel].mipmaps[MipLevel];
    }
    /*       public STGenericTexture ToGenericTexture()
           {
               STGenericTexture texture = new STGenericTexture();
               texture.Width = header.width;
               texture.Height = header.height;
               texture.Format = GetFormat();
               bool IsCubemap = false;

               if (IsCubemap)
                   texture.Surfaces = GetArrayFaces(this, 6);
               else
                   texture.Surfaces = GetArrayFaces(this, 1);

               return texture;
           }*/

    public void SetArrayLevel(byte[] data, int ArrayIndex, int DepthIndex = 0)
    {
        var formatSize = GetBytesPerPixel(Format);

        uint Offset = 0;
        for (byte d = 0; d < Depth; ++d)
        for (byte i = 0; i < ArrayCount; ++i)
        {
            if (i == ArrayIndex) Array.Copy(data, 0, bdata, Offset, data.Length);

            uint MipWidth = Width, MipHeight = Height;
            for (var j = 0; j < MipCount; ++j)
            {
                MipWidth = (uint)Math.Max(1, Width >> j);
                MipHeight = (uint)Math.Max(1, Height >> j);

                var size = MipWidth * MipHeight; //Total pixels
                if (IsCompressed(Format))
                {
                    size = ((MipWidth + 3) >> 2) * ((MipHeight + 3) >> 2) * formatSize;
                    if (size < formatSize)
                        size = formatSize;
                }
                else
                {
                    size = (uint)(size * GetBytesPerPixel(Format)); //Bytes per pixel
                }

                Offset += size;
            }
        }
    }

    public static List<Surface> GetArrayFaces(STGenericTexture tex, byte[] ImageData, uint Length)
    {
        using (var reader = new FileReader(ImageData))
        {
            var Surfaces = new List<Surface>();

            var formatSize = GetBytesPerPixel(tex.Format);

            uint numDepth = 1;
            if (tex.Depth > 1)
                numDepth = tex.Depth;

            uint Offset = 0;
            for (byte d = 0; d < numDepth; ++d)
            for (byte i = 0; i < Length; ++i)
            {
                var Surface = new Surface();

                uint MipWidth = tex.Width, MipHeight = tex.Height;
                for (var j = 0; j < tex.MipCount; ++j)
                {
                    MipWidth = (uint)Math.Max(1, tex.Width >> j);
                    MipHeight = (uint)Math.Max(1, tex.Height >> j);

                    var size = MipWidth * MipHeight; //Total pixels
                    if (IsCompressed(tex.Format))
                    {
                        size = ((MipWidth + 3) >> 2) * ((MipHeight + 3) >> 2) * formatSize;
                        if (size < formatSize)
                            size = formatSize;
                    }
                    else
                    {
                        size = (uint)(size * GetBytesPerPixel(tex.Format)); //Bytes per pixel
                    }

                    Surface.mipmaps.Add(reader.getSection((int)Offset, (int)size));
                    Offset += size;
                }

                Surfaces.Add(Surface);
            }

            return Surfaces;
        }
    }

    public static List<Surface> GetArrayFaces(DDS dds, uint Length, int DepthLevel = 0)
    {
        using (var reader = new FileReader(dds.bdata))
        {
            var Surfaces = new List<Surface>();

            var formatSize = GetBytesPerPixel(dds.Format);

            var isBlock = dds.IsCompressed();
            if (dds.header.mipmapCount == 0)
                dds.header.mipmapCount = 1;

            uint Offset = 0;

            if (dds.Depth > 1 && dds.header.mipmapCount > 1)
            {
                var Surface = new Surface();

                uint MipWidth = dds.header.width, MipHeight = dds.header.height;
                for (var j = 0; j < dds.header.mipmapCount; ++j)
                {
                    MipWidth = (uint)Math.Max(1, dds.header.width >> j);
                    MipHeight = (uint)Math.Max(1, dds.header.height >> j);
                    for (byte d = 0; d < dds.Depth; ++d)
                    {
                        var size = MipWidth * MipHeight; //Total pixels
                        if (isBlock)
                        {
                            size = ((MipWidth + 3) >> 2) * ((MipHeight + 3) >> 2) * formatSize;
                            if (size < formatSize)
                                size = formatSize;
                        }
                        else
                        {
                            size = (uint)(size * GetBytesPerPixel(dds.Format)); //Bytes per pixel
                        }


                        //Only add mips to the depth level needed
                        if (d == DepthLevel)
                            Surface.mipmaps.Add(reader.getSection((int)Offset, (int)size));

                        Offset += size;

                        //Add the current depth level and only once
                        if (d == DepthLevel && j == 0)
                            Surfaces.Add(Surface);
                    }
                }
            }
            else
            {
                for (byte d = 0; d < dds.Depth; ++d)
                for (byte i = 0; i < Length; ++i)
                {
                    var Surface = new Surface();

                    uint MipWidth = dds.header.width, MipHeight = dds.header.height;
                    for (var j = 0; j < dds.header.mipmapCount; ++j)
                    {
                        MipWidth = (uint)Math.Max(1, dds.header.width >> j);
                        MipHeight = (uint)Math.Max(1, dds.header.height >> j);

                        var size = MipWidth * MipHeight; //Total pixels
                        if (isBlock)
                        {
                            size = ((MipWidth + 3) >> 2) * ((MipHeight + 3) >> 2) * formatSize;
                            if (size < formatSize)
                                size = formatSize;
                        }
                        else
                        {
                            size = (uint)(size * GetBytesPerPixel(dds.Format)); //Bytes per pixel
                        }

                        Surface.mipmaps.Add(reader.getSection((int)Offset, (int)size));
                        Offset += size;
                    }

                    if (d == DepthLevel)
                        Surfaces.Add(Surface);
                }
            }

            return Surfaces;
        }
    }

    public TEX_FORMAT GetFormat()
    {
        if (DX10header != null)
            return (TEX_FORMAT)DX10header.DXGI_Format;

        switch (header.ddspf.fourCC)
        {
            case FOURCC_DXT1:
                return TEX_FORMAT.BC1_UNORM;
            case FOURCC_DXT2:
            case FOURCC_DXT3:
                return TEX_FORMAT.BC2_UNORM;
            case FOURCC_DXT4:
            case FOURCC_DXT5:
                return TEX_FORMAT.BC3_UNORM;
            case FOURCC_ATI1:
            case FOURCC_BC4U:
                return TEX_FORMAT.BC4_UNORM;
            case FOURCC_BC4S:
                return TEX_FORMAT.BC4_SNORM;
            case FOURCC_ATI2:
            case FOURCC_BC5U:
                return TEX_FORMAT.BC5_UNORM;
            case FOURCC_BC5S:
                return TEX_FORMAT.BC5_SNORM;

            case FOURCC_RXGB:
                return TEX_FORMAT.R8G8B8A8_UNORM;
            default:
                return TEX_FORMAT.R8G8B8A8_UNORM;
        }
    }

    public static bool IsAtscFormat(TEX_FORMAT Format)
    {
        if (Format.ToString().Contains("ASTC"))
            return true;
        else
            return false;
    }

    public bool IsCompressed()
    {
        if (header == null)
            return false;

        if (DX10header != null)
            switch (DX10header.DXGI_Format)
            {
                case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC1_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC2_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC3_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC4_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC5_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC6H_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16:
                case DXGI_FORMAT.DXGI_FORMAT_BC6H_SF16:
                case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC7_TYPELESS:
                    return true;
                default:
                    return false;
            }
        else
            switch (header.ddspf.fourCC)
            {
                case FOURCC_DXT1:
                case FOURCC_DXT2:
                case FOURCC_DXT3:
                case FOURCC_DXT4:
                case FOURCC_DXT5:
                case FOURCC_ATI1:
                case FOURCC_BC4U:
                case FOURCC_ATI2:
                case FOURCC_BC5U:
                case FOURCC_BC5S:
                    return true;
                default:
                    return false;
            }
    }

    public void Save(DDS dds, string FileName, List<Surface> data = null)
    {
        Save(dds, new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.Write), data);
    }

    public void Save(DDS dds, Stream stream, List<Surface> data = null)
    {
        var writer = new FileWriter(stream);
        var header = dds.header;
        writer.Write(Encoding.ASCII.GetBytes("DDS "));
        writer.Write(header.size);
        writer.Write(header.flags);
        writer.Write(header.height);
        writer.Write(header.width);
        writer.Write(header.pitchOrLinearSize);
        writer.Write(header.depth);
        writer.Write(header.mipmapCount);
        for (var i = 0; i < 11; ++i)
            writer.Write(header.reserved1[i]);

        writer.Write(header.ddspf.size);
        writer.Write(header.ddspf.flags);
        writer.Write(header.ddspf.fourCC);
        writer.Write(header.ddspf.RGBBitCount);
        writer.Write(header.ddspf.RBitMask);
        writer.Write(header.ddspf.GBitMask);
        writer.Write(header.ddspf.BBitMask);
        writer.Write(header.ddspf.ABitMask);
        writer.Write(header.caps);
        writer.Write(header.caps2);
        writer.Write(header.caps3);
        writer.Write(header.caps4);
        writer.Write(header.reserved2);

        if (IsDX10) WriteDX10Header(writer);

        if (data != null)
            foreach (var surface in data)
                writer.Write(CombineByteArray(surface.mipmaps.ToArray()));
        else
            writer.Write(bdata);

        writer.Flush();
        writer.Close();
        writer.Dispose();
    }

    private void WriteDX10Header(BinaryDataWriter writer)
    {
        if (DX10header == null)
            DX10header = new DX10Header();

        writer.Write((uint)DX10header.DXGI_Format);
        writer.Write(DX10header.ResourceDim);
        writer.Write(DX10header.miscFlag);
        writer.Write(DX10header.arrayFlag);
        writer.Write(DX10header.miscFlags2);
    }


    public static byte[] CompressBC1Block(byte[] data, int Width, int Height)
    {
        var image = new byte[0];

        return image;
    }

    public static void ToRGBA(byte[] data, int Width, int Height, int bpp, int compSel)
    {
        var Size = Width * Height * 4;

        var result = new byte[Size];

        for (var Y = 0; Y < Height; Y++)
        for (var X = 0; X < Width; X++)
        {
            var pos = (Y * Width + X) * bpp;
            var pos_ = (Y * Width + X) * 4;

            var pixel = 0;
        }
    }
}

public enum PixelInternalFormat
{
    DepthComponent = 6402,
    Alpha = 6406,
    Rgb = 6407,
    Rgba = 6408,
    Luminance = 6409,
    LuminanceAlpha = 6410,
    R3G3B2 = 10768,
    Alpha4 = 32827,
    Alpha8 = 32828,
    Alpha12 = 32829,
    Alpha16 = 32830,
    Luminance4 = 32831,
    Luminance8 = 32832,
    Luminance12 = 32833,
    Luminance16 = 32834,
    Luminance4Alpha4 = 32835,
    Luminance6Alpha2 = 32836,
    Luminance8Alpha8 = 32837,
    Luminance12Alpha4 = 32838,
    Luminance12Alpha12 = 32839,
    Luminance16Alpha16 = 32840,
    Intensity = 32841,
    Intensity4 = 32842,
    Intensity8 = 32843,
    Intensity12 = 32844,
    Intensity16 = 32845,
    Rgb2Ext = 32846,
    Rgb4 = 32847,
    Rgb5 = 32848,
    Rgb8 = 32849,
    Rgb10 = 32850,
    Rgb12 = 32851,
    Rgb16 = 32852,
    Rgba2 = 32853,
    Rgba4 = 32854,
    Rgb5A1 = 32855,
    Rgba8 = 32856,
    Rgb10A2 = 32857,
    Rgba12 = 32858,
    Rgba16 = 32859,
    DualAlpha4Sgis = 33040,
    DualAlpha8Sgis = 33041,
    DualAlpha12Sgis = 33042,
    DualAlpha16Sgis = 33043,
    DualLuminance4Sgis = 33044,
    DualLuminance8Sgis = 33045,
    DualLuminance12Sgis = 33046,
    DualLuminance16Sgis = 33047,
    DualIntensity4Sgis = 33048,
    DualIntensity8Sgis = 33049,
    DualIntensity12Sgis = 33050,
    DualIntensity16Sgis = 33051,
    DualLuminanceAlpha4Sgis = 33052,
    DualLuminanceAlpha8Sgis = 33053,
    QuadAlpha4Sgis = 33054,
    QuadAlpha8Sgis = 33055,
    QuadLuminance4Sgis = 33056,
    QuadLuminance8Sgis = 33057,
    QuadIntensity4Sgis = 33058,
    QuadIntensity8Sgis = 33059,
    DepthComponent16 = 33189,
    DepthComponent16Sgix = 33189,
    DepthComponent24 = 33190,
    DepthComponent24Sgix = 33190,
    DepthComponent32 = 33191,
    DepthComponent32Sgix = 33191,
    CompressedRed = 33317,
    CompressedRg = 33318,
    R8 = 33321,
    R16 = 33322,
    Rg8 = 33323,
    Rg16 = 33324,
    R16f = 33325,
    R32f = 33326,
    Rg16f = 33327,
    Rg32f = 33328,
    R8i = 33329,
    R8ui = 33330,
    R16i = 33331,
    R16ui = 33332,
    R32i = 33333,
    R32ui = 33334,
    Rg8i = 33335,
    Rg8ui = 33336,
    Rg16i = 33337,
    Rg16ui = 33338,
    Rg32i = 33339,
    Rg32ui = 33340,
    CompressedRgbS3tcDxt1Ext = 33776,
    CompressedRgbaS3tcDxt1Ext = 33777,
    CompressedRgbaS3tcDxt3Ext = 33778,
    CompressedRgbaS3tcDxt5Ext = 33779,
    RgbIccSgix = 33888,
    RgbaIccSgix = 33889,
    AlphaIccSgix = 33890,
    LuminanceIccSgix = 33891,
    IntensityIccSgix = 33892,
    LuminanceAlphaIccSgix = 33893,
    R5G6B5IccSgix = 33894,
    R5G6B5A8IccSgix = 33895,
    Alpha16IccSgix = 33896,
    Luminance16IccSgix = 33897,
    Intensity16IccSgix = 33898,
    Luminance16Alpha8IccSgix = 33899,
    CompressedAlpha = 34025,
    CompressedLuminance = 34026,
    CompressedLuminanceAlpha = 34027,
    CompressedIntensity = 34028,
    CompressedRgb = 34029,
    CompressedRgba = 34030,
    DepthStencil = 34041,
    Rgba32f = 34836,
    Rgb32f = 34837,
    Rgba16f = 34842,
    Rgb16f = 34843,
    Depth24Stencil8 = 35056,
    R11fG11fB10f = 35898,
    Rgb9E5 = 35901,
    Srgb = 35904,
    Srgb8 = 35905,
    SrgbAlpha = 35906,
    Srgb8Alpha8 = 35907,
    SluminanceAlpha = 35908,
    Sluminance8Alpha8 = 35909,
    Sluminance = 35910,
    Sluminance8 = 35911,
    CompressedSrgb = 35912,
    CompressedSrgbAlpha = 35913,
    CompressedSluminance = 35914,
    CompressedSluminanceAlpha = 35915,
    CompressedSrgbS3tcDxt1Ext = 35916,
    CompressedSrgbAlphaS3tcDxt1Ext = 35917,
    CompressedSrgbAlphaS3tcDxt3Ext = 35918,
    CompressedSrgbAlphaS3tcDxt5Ext = 35919,
    DepthComponent32f = 36012,
    Depth32fStencil8 = 36013,
    Rgba32ui = 36208,
    Rgb32ui = 36209,
    Rgba16ui = 36214,
    Rgb16ui = 36215,
    Rgba8ui = 36220,
    Rgb8ui = 36221,
    Rgba32i = 36226,
    Rgb32i = 36227,
    Rgba16i = 36232,
    Rgb16i = 36233,
    Rgba8i = 36238,
    Rgb8i = 36239,
    Float32UnsignedInt248Rev = 36269,
    CompressedRedRgtc1 = 36283,
    CompressedSignedRedRgtc1 = 36284,
    CompressedRgRgtc2 = 36285,
    CompressedSignedRgRgtc2 = 36286,
    CompressedRgbaBptcUnorm = 36492,
    CompressedSrgbAlphaBptcUnorm = 36493,
    CompressedRgbBptcSignedFloat = 36494,
    CompressedRgbBptcUnsignedFloat = 36495,
    R8Snorm = 36756,
    Rg8Snorm = 36757,
    Rgb8Snorm = 36758,
    Rgba8Snorm = 36759,
    R16Snorm = 36760,
    Rg16Snorm = 36761,
    Rgb16Snorm = 36762,
    Rgba16Snorm = 36763,
    Rgb10A2ui = 36975,
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4
}

public enum PixelFormat
{
    UnsignedShort = 5123,
    UnsignedInt = 5125,
    ColorIndex = 6400,
    StencilIndex = 6401,
    DepthComponent = 6402,
    Red = 6403,
    RedExt = 6403,
    Green = 6404,
    Blue = 6405,
    Alpha = 6406,
    Rgb = 6407,
    Rgba = 6408,
    Luminance = 6409,
    LuminanceAlpha = 6410,
    AbgrExt = 32768,
    CmykExt = 32780,
    CmykaExt = 32781,
    Bgr = 32992,
    Bgra = 32993,
    Ycrcb422Sgix = 33211,
    Ycrcb444Sgix = 33212,
    Rg = 33319,
    RgInteger = 33320,
    R5G6B5IccSgix = 33894,
    R5G6B5A8IccSgix = 33895,
    Alpha16IccSgix = 33896,
    Luminance16IccSgix = 33897,
    Luminance16Alpha8IccSgix = 33899,
    DepthStencil = 34041,
    RedInteger = 36244,
    GreenInteger = 36245,
    BlueInteger = 36246,
    AlphaInteger = 36247,
    RgbInteger = 36248,
    RgbaInteger = 36249,
    BgrInteger = 36250,
    BgraInteger = 36251
}

public enum PixelType
{
    Byte = 5120,
    UnsignedByte = 5121,
    Short = 5122,
    UnsignedShort = 5123,
    Int = 5124,
    UnsignedInt = 5125,
    Float = 5126,
    HalfFloat = 5131,
    Bitmap = 6656,
    UnsignedByte332 = 32818,
    UnsignedByte332Ext = 32818,
    UnsignedShort4444 = 32819,
    UnsignedShort4444Ext = 32819,
    UnsignedShort5551 = 32820,
    UnsignedShort5551Ext = 32820,
    UnsignedInt8888 = 32821,
    UnsignedInt8888Ext = 32821,
    UnsignedInt1010102 = 32822,
    UnsignedInt1010102Ext = 32822,
    UnsignedByte233Reversed = 33634,
    UnsignedShort565 = 33635,
    UnsignedShort565Reversed = 33636,
    UnsignedShort4444Reversed = 33637,
    UnsignedShort1555Reversed = 33638,
    UnsignedInt8888Reversed = 33639,
    UnsignedInt2101010Reversed = 33640,
    UnsignedInt248 = 34042,
    UnsignedInt10F11F11FRev = 35899,
    UnsignedInt5999Rev = 35902,
    Float32UnsignedInt248Rev = 36269
}

public class BitmapExtension
{
    public BitmapExtension()
    {
    }

    public static Bitmap SetChannel(Bitmap b,
        STChannelType channelR,
        STChannelType channelG,
        STChannelType channelB,
        STChannelType channelA)
    {
        var bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
            ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var stride = bmData.Stride;
        var Scan0 = bmData.Scan0;

        unsafe
        {
            var p = (byte*)(void*)Scan0;

            var nOffset = stride - b.Width * 4;

            byte red, green, blue, alpha;

            for (var y = 0; y < b.Height; ++y)
            {
                for (var x = 0; x < b.Width; ++x)
                {
                    blue = p[0];
                    green = p[1];
                    red = p[2];
                    alpha = p[3];

                    p[2] = SetChannelByte(channelR, red, green, blue, alpha);
                    p[1] = SetChannelByte(channelG, red, green, blue, alpha);
                    p[0] = SetChannelByte(channelB, red, green, blue, alpha);
                    p[3] = SetChannelByte(channelA, red, green, blue, alpha);

                    p += 4;
                }

                p += nOffset;
            }
        }

        b.UnlockBits(bmData);

        return b;
    }

    /*public static Bitmap PaintImage(Brush brush, Bitmap input)
    {
        return input;

        Bitmap colorBitmap = new Bitmap(input.Width, input.Height);
        using (var g = Graphics.FromImage(colorBitmap))
        {
            Rectangle rect = new Rectangle(0, 0, colorBitmap.Width, colorBitmap.Height);
            g.FillRectangle(brush, rect);

            return MultiplyImages(input, colorBitmap);
        }
    }

    public static Bitmap MultiplyImages(Bitmap input1, Bitmap input2)
    {
        BitmapData bmData = input1.LockBits(new Rectangle(0, 0, input1.Width, input1.Height),
ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride = bmData.Stride;
        System.IntPtr Scan0 = bmData.Scan0;



        BitmapData cmData = input2.LockBits(new Rectangle(0, 0, input2.Width, input2.Height),
ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int cstride = cmData.Stride;
        System.IntPtr cScan0 = cmData.Scan0;

        unsafe
        {
            byte* p = (byte*)(void*)Scan0;
            byte* p2 = (byte*)(void*)cScan0;

            int nOffset = stride - input1.Width * 4;

            byte red, green, blue, alpha;

            for (int y = 0; y < input1.Height; ++y)
            {
                for (int x = 0; x < input1.Width; ++x)
                {
                    blue = (byte)(p[0] * p2[0] / 255);
                    green = (byte)(p[1] * p2[1] / 255);
                    red = (byte)(p[2] * p2[2] / 255);
                    alpha = (byte)(p[3] * p2[3] / 255);

                    p += 4;
                    p2 += 4;
                }
                p += nOffset;
                p2 += nOffset;
            }
        }

        input1.UnlockBits(bmData);
        input2.UnlockBits(cmData);

        return input1;
    }*/

    public static Bitmap FillColor(int Width, int Height, Color color)
    {
        var Bmp = new Bitmap(Width, Height);
        using (var gfx = Graphics.FromImage(Bmp))
        using (var brush = new SolidBrush(color))
        {
            gfx.FillRectangle(brush, 0, 0, Width, Height);
        }

        return Bmp;
    }

    public static string FileFilter =>
        "Supported Formats|*.png;*.tga;*.jpg;*.tiff|" +
        "Portable Network Graphics |*.png|" +
        "Joint Photographic Experts Group |*.jpg|" +
        "Bitmap Image |*.bmp|" +
        "Tagged Image File Format |*.tiff|" +
        "All files(*.*)|*.*";

    public static List<byte[]> GenerateMipMaps(Bitmap bitmap)
    {
        var datas = new List<byte[]>();

        datas.Add(ImageToByte(bitmap));
        while (bitmap.Width / 2 > 0 && bitmap.Height / 2 > 0)
        {
            bitmap = Resize(bitmap, bitmap.Width / 2, bitmap.Height / 2);
            datas.Add(ImageToByte(bitmap));
        }

        return datas;
    }

    public static Bitmap Resize(Image original, Size size)
    {
        return ResizeImage(original, size.Width, size.Height);
    }

    public static Bitmap Resize(Image original, uint width, uint height)
    {
        return ResizeImage(original, (int)width, (int)height);
    }

    public static Bitmap Resize(Image original, int width, int height)
    {
        return ResizeImage(original, width, height);
    }

    /*public static Bitmap ReplaceChannel(Image OriginalImage, Image ChannelImage, STChannelType ChannelType)
    {
        Bitmap b = new Bitmap(OriginalImage);
        Bitmap c = new Bitmap(ChannelImage, new Size(b.Width, b.Height)); //Force to be same size
        c = GrayScale(c); //Convert to grayscale 

        BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride = bmData.Stride;
        System.IntPtr Scan0 = bmData.Scan0;



        BitmapData cmData = c.LockBits(new Rectangle(0, 0, c.Width, c.Height),
ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int cstride = cmData.Stride;
        System.IntPtr cScan0 = cmData.Scan0;

        unsafe
        {
            byte* p = (byte*)(void*)Scan0;
            byte* channelPointer = (byte*)(void*)cScan0;

            int nOffset = stride - b.Width * 4;

            byte red, green, blue, alpha;

            for (int y = 0; y < b.Height; ++y)
            {
                for (int x = 0; x < b.Width; ++x)
                {
                    blue = p[0];
                    green = p[1];
                    red = p[2];
                    alpha = p[3];

                    if (ChannelType == STChannelType.Red)
                    {
                        p[2] = channelPointer[2];
                        p[1] = green;
                        p[0] = blue;
                        p[3] = alpha;
                    }
                    else if (ChannelType == STChannelType.Green)
                    {
                        p[2] = red;
                        p[1] = channelPointer[2];
                        p[0] = blue;
                        p[3] = alpha;
                    }
                    else if (ChannelType == STChannelType.Blue)
                    {
                        p[2] = red;
                        p[1] = green;
                        p[0] = channelPointer[2];
                        p[3] = alpha;
                    }
                    else if (ChannelType == STChannelType.Alpha)
                    {
                        p[2] = red;
                        p[1] = green;
                        p[0] = blue;
                        p[3] = channelPointer[2];
                    }

                    p += 4;
                    channelPointer += 4;
                }
                p += nOffset;
                channelPointer += nOffset;
            }
        }

        b.UnlockBits(bmData);
        c.UnlockBits(cmData);

        return b;
    }*/

    /* public static Bitmap SwapBlueRedChannels(Bitmap orig)
     {
         Bitmap b = orig;
         if (orig.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
         {
             b = new Bitmap(orig.Width, orig.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

             using (Graphics gr = Graphics.FromImage(b))
             {
                 gr.DrawImage(orig, new Rectangle(0, 0, b.Width, b.Height));
             }
         }

         BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
                  ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
         int stride = bmData.Stride;
         System.IntPtr Scan0 = bmData.Scan0;

         unsafe
         {
             byte* p = (byte*)(void*)Scan0;

             int nOffset = stride - b.Width * 4;

             byte red, green, blue, alpha;

             for (int y = 0; y < b.Height; ++y)
             {
                 for (int x = 0; x < b.Width; ++x)
                 {
                     blue = p[0];
                     green = p[1];
                     red = p[2];
                     alpha = p[3];

                     p[0] = red;
                     p[1] = green;
                     p[2] = blue;
                     p[3] = alpha;

                     p += 4;
                 }
                 p += nOffset;
             }
         }

         b.UnlockBits(bmData);

         return b;
     }*/

    public static Bitmap ResizeImage(Image image, int width, int height,
        InterpolationMode interpolationMode = InterpolationMode.HighQualityBicubic,
        SmoothingMode smoothingMode = SmoothingMode.HighQuality)
    {
        if (width == 0) width = 1;
        if (height == 0) height = 1;

        var destRect = new Rectangle(0, 0, width, height);
        var destImage = new Bitmap(width, height);

        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

        using (var graphics = Graphics.FromImage(destImage))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = interpolationMode;
            graphics.SmoothingMode = smoothingMode;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (var wrapMode = new ImageAttributes())
            {
                wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
            }
        }

        return destImage;
    }

    public static Bitmap GetBitmap(byte[] Buffer, int Width, int Height,
        System.Drawing.Imaging.PixelFormat pixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppArgb)
    {
        var Rect = new Rectangle(0, 0, Width, Height);

        var Img = new Bitmap(Width, Height, pixelFormat);

        var ImgData = Img.LockBits(Rect, ImageLockMode.WriteOnly, Img.PixelFormat);

        if (Buffer.Length > ImgData.Stride * Img.Height)
            throw new Exception($"Invalid Buffer Length ({Buffer.Length})!!!");

        Marshal.Copy(Buffer, 0, ImgData.Scan0, Buffer.Length);

        Img.UnlockBits(ImgData);

        return Img;
    }

    /*public static Bitmap SetChannel(Bitmap b,
        STChannelType channelR,
        STChannelType channelG,
        STChannelType channelB,
        STChannelType channelA)
    {
        BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride = bmData.Stride;
        System.IntPtr Scan0 = bmData.Scan0;

        unsafe
        {
            byte* p = (byte*)(void*)Scan0;

            int nOffset = stride - b.Width * 4;

            byte red, green, blue, alpha;

            for (int y = 0; y < b.Height; ++y)
            {
                for (int x = 0; x < b.Width; ++x)
                {
                    blue = p[0];
                    green = p[1];
                    red = p[2];
                    alpha = p[3];

                    p[2] = SetChannelByte(channelR, red, green, blue, alpha);
                    p[1] = SetChannelByte(channelG, red, green, blue, alpha);
                    p[0] = SetChannelByte(channelB, red, green, blue, alpha);
                    p[3] = SetChannelByte(channelA, red, green, blue, alpha);

                    p += 4;
                }
                p += nOffset;
            }
        }

        b.UnlockBits(bmData);

        return b;
    }*/

    private static byte SetChannelByte(STChannelType channel, byte r, byte g, byte b, byte a)
    {
        switch (channel)
        {
            case STChannelType.Red: return r;
            case STChannelType.Green: return g;
            case STChannelType.Blue: return b;
            case STChannelType.Alpha: return a;
            case STChannelType.One: return 255;
            case STChannelType.Zero: return 0;
            default:
                throw new Exception("Unknown channel type! " + channel);
        }
    }

    /*public static Bitmap ShowChannel(Bitmap b, STChannelType channel)
    {
        BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride = bmData.Stride;
        System.IntPtr Scan0 = bmData.Scan0;

        unsafe
        {
            byte* p = (byte*)(void*)Scan0;

            int nOffset = stride - b.Width * 4;

            byte red, green, blue, alpha;

            for (int y = 0; y < b.Height; ++y)
            {
                for (int x = 0; x < b.Width; ++x)
                {
                    blue = p[0];
                    green = p[1];
                    red = p[2];
                    alpha = p[3];

                    if (channel == STChannelType.Red)
                    {
                        p[0] = red;
                        p[1] = red;
                        p[2] = red;
                        p[3] = 255;
                    }
                    else if (channel == STChannelType.Green)
                    {
                        p[0] = green;
                        p[1] = green;
                        p[2] = green;
                        p[3] = 255;
                    }
                    else if (channel == STChannelType.Blue)
                    {
                        p[0] = blue;
                        p[1] = blue;
                        p[2] = blue;
                        p[3] = 255;
                    }
                    else if (channel == STChannelType.Alpha)
                    {
                        p[0] = alpha;
                        p[1] = alpha;
                        p[2] = alpha;
                        p[3] = 255;
                    }

                    p += 4;
                }
                p += nOffset;
            }
        }

        b.UnlockBits(bmData);

        return b;
    }


    public static bool SetChannels(Bitmap b, bool UseRed, bool UseBlue, bool UseGreen, bool UseAlpha)
    {
        BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
 ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride = bmData.Stride;
        System.IntPtr Scan0 = bmData.Scan0;

        unsafe
        {
            byte* p = (byte*)(void*)Scan0;

            int nOffset = stride - b.Width * 4;

            byte red, green, blue, alpha;

            for (int y = 0; y < b.Height; ++y)
            {
                for (int x = 0; x < b.Width; ++x)
                {
                    blue = p[0];
                    green = p[1];
                    red = p[2];
                    alpha = p[3];

                    if (!UseRed)
                        red = 0;
                    if (!UseGreen)
                        green = 0;
                    if (!UseBlue)
                        blue = 0;
                    if (!UseAlpha)
                        alpha = 0;

                    p[2] = red;
                    p[1] = green;
                    p[0] = blue;
                    p[3] = alpha;

                    p += 4;
                }
                p += nOffset;
            }
        }

        b.UnlockBits(bmData);

        return true;
    }
    public static Bitmap GrayScale(Image b, bool removeAlpha = false)
    {
        return GrayScale(new Bitmap(b), removeAlpha);
    }

    public static Bitmap GrayScale(Bitmap b, bool removeAlpha = false)
    {
        BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
    ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride = bmData.Stride;
        System.IntPtr Scan0 = bmData.Scan0;

        unsafe
        {
            byte* p = (byte*)(void*)Scan0;

            int nOffset = stride - b.Width * 4;

            byte red, green, blue, alpha;

            for (int y = 0; y < b.Height; ++y)
            {
                for (int x = 0; x < b.Width; ++x)
                {
                    blue = p[0];
                    green = p[1];
                    red = p[2];
                    if (removeAlpha)
                        alpha = 255;
                    else
                        alpha = p[3];

                    p[0] = p[1] = p[2] = (byte)(.299 * red
                        + .587 * green
                        + .114 * blue);

                    p += 4;
                }
                p += nOffset;
            }
        }

        b.UnlockBits(bmData);

        return b;
    }

    public static Bitmap EncodeHDRAlpha(Image image, float gamma = 2.2f)
    {
        var b = new Bitmap(image);

        BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
             ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride = bmData.Stride;
        System.IntPtr Scan0 = bmData.Scan0;

        unsafe
        {
            byte* p = (byte*)(void*)Scan0;

            int nOffset = stride - b.Width * 4;

            for (int y = 0; y < b.Height; ++y)
            {
                for (int x = 0; x < b.Width; ++x)
                {
                    float alpha = p[3] / 255f;
                    for (int i = 0; i < 3; i++)
                    {
                        var col = (p[i] / 255f) * (float)Math.Pow(alpha, 4) * 1024;
                        col = col / (col + 1.0f);
                        col = (float)Math.Pow(col, 1.0f / gamma);

                        p[i] = (byte)(col * 255);
                    }

                    p[3] = 255;

                    p += 4;
                }
                p += nOffset;
            }
        }

        b.UnlockBits(bmData);

        return b;
    }

    public static bool Invert(Bitmap b)
    {
        BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
            ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        int stride = bmData.Stride;
        System.IntPtr Scan0 = bmData.Scan0;
        unsafe
        {
            byte* p = (byte*)(void*)Scan0;
            int nOffset = stride - b.Width * 3;
            int nWidth = b.Width * 3;
            for (int y = 0; y < b.Height; ++y)
            {
                for (int x = 0; x < nWidth; ++x)
                {
                    p[0] = (byte)(255 - p[0]);
                    ++p;
                }
                p += nOffset;
            }
        }

        b.UnlockBits(bmData);

        return true;
    }


    public static Bitmap HueStaturationBrightnessScale(Bitmap image,
        bool EditHue, bool EditSaturation, bool EditBrightness,
        float HueScale = 255, float SaturationScale = 0.5f, float BrightnessScale = 0.5f)
    {
        Bitmap b = new Bitmap(image);

        BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride = bmData.Stride;
        System.IntPtr Scan0 = bmData.Scan0;

        unsafe
        {
            byte* pointer = (byte*)(void*)Scan0;

            int bytesPerPixel = 4;

            int nOffset = stride - b.Width * bytesPerPixel;

            byte red, green, blue, alpha;

            for (int y = 0; y < b.Height; ++y)
            {
                for (int x = 0; x < b.Width; ++x)
                {
                    blue = pointer[0];
                    green = pointer[1];
                    red = pointer[2];
                    alpha = pointer[3];


                    double hue, sat, val;

                    ColorToHSV(Color.FromArgb(alpha, red, green, blue), out hue, out sat, out val);

                    var color = ColorFromHSV(hue * HueScale, sat * SaturationScale, val * BrightnessScale);

                    pointer[2] = color.R;
                    pointer[1] = color.G;
                    pointer[0] = color.B;
                    pointer[3] = alpha;

                    pointer += bytesPerPixel;
                }
                pointer += nOffset;
            }
        }

        b.UnlockBits(bmData);

        return b;
    }*/

    public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
    {
        int max = Math.Max(color.R, Math.Max(color.G, color.B));
        int min = Math.Min(color.R, Math.Min(color.G, color.B));

        hue = color.GetHue();
        saturation = max == 0 ? 0 : 1d - 1d * min / max;
        value = max / 255d;
    }

    public static Color ColorFromHSV(double hue, double saturation, double value)
    {
        var hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        var f = hue / 60 - Math.Floor(hue / 60);

        value = value * 255;
        var v = Convert.ToInt32(value);
        var p = Convert.ToInt32(value * (1 - saturation));
        var q = Convert.ToInt32(value * (1 - f * saturation));
        var t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

        if (hi == 0)
            return Color.FromArgb(255, v, t, p);
        else if (hi == 1)
            return Color.FromArgb(255, q, v, p);
        else if (hi == 2)
            return Color.FromArgb(255, p, v, t);
        else if (hi == 3)
            return Color.FromArgb(255, p, q, v);
        else if (hi == 4)
            return Color.FromArgb(255, t, p, v);
        else
            return Color.FromArgb(255, v, p, q);
    }

    public static void RgbToHls(int r, int g, int b,
        out double h, out double l, out double s)
    {
        // Convert RGB to a 0.0 to 1.0 range.
        var double_r = r / 255.0;
        var double_g = g / 255.0;
        var double_b = b / 255.0;

        // Get the maximum and minimum RGB components.
        var max = double_r;
        if (max < double_g) max = double_g;
        if (max < double_b) max = double_b;

        var min = double_r;
        if (min > double_g) min = double_g;
        if (min > double_b) min = double_b;

        var diff = max - min;
        l = (max + min) / 2;
        if (Math.Abs(diff) < 0.00001)
        {
            s = 0;
            h = 0; // H is really undefined.
        }
        else
        {
            if (l <= 0.5) s = diff / (max + min);
            else s = diff / (2 - max - min);

            var r_dist = (max - double_r) / diff;
            var g_dist = (max - double_g) / diff;
            var b_dist = (max - double_b) / diff;

            if (double_r == max) h = b_dist - g_dist;
            else if (double_g == max) h = 2 + r_dist - b_dist;
            else h = 4 + g_dist - r_dist;

            h = h * 60;
            if (h < 0) h += 360;
        }
    }

    // Convert an HLS value into an RGB value.
    public static void HlsToRgb(double h, double l, double s,
        out int r, out int g, out int b)
    {
        double p2;
        if (l <= 0.5) p2 = l * (1 + s);
        else p2 = l + s - l * s;

        var p1 = 2 * l - p2;
        double double_r, double_g, double_b;
        if (s == 0)
        {
            double_r = l;
            double_g = l;
            double_b = l;
        }
        else
        {
            double_r = QqhToRgb(p1, p2, h + 120);
            double_g = QqhToRgb(p1, p2, h);
            double_b = QqhToRgb(p1, p2, h - 120);
        }

        // Convert RGB to the 0 to 255 range.
        r = (int)(double_r * 255.0);
        g = (int)(double_g * 255.0);
        b = (int)(double_b * 255.0);
    }

    private static double QqhToRgb(double q1, double q2, double hue)
    {
        if (hue > 360) hue -= 360;
        else if (hue < 0) hue += 360;

        if (hue < 60) return q1 + (q2 - q1) * hue / 60;
        if (hue < 180) return q2;
        if (hue < 240) return q1 + (q2 - q1) * (240 - hue) / 60;
        return q1;
    }

    private static void ConvertBgraToRgba(byte[] bytes)
    {
        for (var i = 0; i < bytes.Length; i += 4)
        {
            var temp = bytes[i];
            bytes[i] = bytes[i + 2];
            bytes[i + 2] = temp;
        }
    }

    public static Bitmap CreateImageThumbnail(Bitmap image, int width, int height)
    {
        int tw, th, tx, ty;

        var w = image.Width;
        var h = image.Height;

        var whRatio = (double)w / h;
        if (image.Width >= image.Height)
        {
            tw = width;
            th = (int)(tw / whRatio);
        }
        else
        {
            th = height;
            tw = (int)(th * whRatio);
        }

        tx = (width - tw) / 2;
        ty = (height - th) / 2;

        var thumb = new Bitmap(width, height, image.PixelFormat);

        var g = Graphics.FromImage(thumb);

        //  g.Clear(Color.White);
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.DrawImage(image, new Rectangle(tx, ty, tw, th),
            new Rectangle(0, 0, w, h),
            GraphicsUnit.Pixel);

        return thumb;
    }

    /*public static Bitmap AdjustBrightness(Image image, float level)
    {
        ImageAttributes attributes = new ImageAttributes();

        ColorMatrix cm = new ColorMatrix(new float[][]
        {
        new float[] { level, 0, 0, 0, 0},
        new float[] {0, level, 0, 0, 0},
        new float[] {0, 0, level, 0, 0},
        new float[] {0, 0, 0, 1, 0},
        new float[] {0, 0, 0, 0, 1},
        });
        attributes.SetColorMatrix(cm);

        Point[] points =
        {
        new Point(0, 0),
        new Point(image.Width, 0),
        new Point(0, image.Height),
       };
        Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

        Bitmap bm = new Bitmap(image.Width, image.Height);
        using (Graphics gr = Graphics.FromImage(bm))
        {
            gr.DrawImage(image, points, rect,
                GraphicsUnit.Pixel, attributes);
        }
        return bm;
    }

    public static Bitmap AdjustGamma(Image image, float gamma)
    {
        ImageAttributes attributes = new ImageAttributes();
        attributes.SetGamma(gamma);

        Point[] points =
        {
        new Point(0, 0),
        new Point(image.Width, 0),
        new Point(0, image.Height),
       };
        Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

        Bitmap bm = new Bitmap(image.Width, image.Height);
        using (Graphics gr = Graphics.FromImage(bm))
        {
            gr.DrawImage(image, points, rect,
                GraphicsUnit.Pixel, attributes);
        }
        return bm;
    }*/

    public static byte[] ImageToByte(Bitmap bitmap)
    {
        BitmapData bmpdata = null;

        try
        {
            bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                bitmap.PixelFormat);
            var numbytes = bmpdata.Stride * bitmap.Height;
            var bytedata = new byte[numbytes];
            var ptr = bmpdata.Scan0;

            Marshal.Copy(ptr, bytedata, 0, numbytes);

            return bytedata;
        }
        finally
        {
            if (bmpdata != null)
                bitmap.UnlockBits(bmpdata);
        }
    }
}

public class ImageParameters
{
    //Flip the image on the Y axis
    public bool FlipY { get; set; }

    //Dont swap the red and green channels
    public bool DontSwapRG { get; set; }
}

public class RGBAPixelDecoder
{
    private static byte[] GetComponentsFromPixel(TEX_FORMAT format, int pixel, byte[] comp)
    {
        switch (format)
        {
            case TEX_FORMAT.L8:
                comp[0] = (byte)(pixel & 0xFF);
                break;
            case TEX_FORMAT.LA8:
                comp[0] = (byte)(pixel & 0xFF);
                comp[1] = (byte)((pixel & 0xFF00) >> 8);
                break;
            case TEX_FORMAT.LA4:
                comp[0] = (byte)((pixel & 0xF) * 17);
                comp[1] = (byte)(((pixel & 0xF0) >> 4) * 17);
                break;
            case TEX_FORMAT.B5G6R5_UNORM:
                comp[0] = (byte)(((pixel & 0xF800) >> 11) / 0x1F * 0xFF);
                comp[1] = (byte)(((pixel & 0x7E0) >> 5) / 0x3F * 0xFF);
                comp[2] = (byte)((pixel & 0x1F) / 0x1F * 0xFF);
                break;
            case TEX_FORMAT.R5G5B5_UNORM:
            {
                var R = ((pixel >> 0) & 0x1f) << 3;
                var G = ((pixel >> 5) & 0x1f) << 3;
                var B = ((pixel >> 10) & 0x1f) << 3;

                comp[0] = (byte)(R | (R >> 5));
                comp[1] = (byte)(G | (G >> 5));
                comp[2] = (byte)(B | (B >> 5));
            }
                break;
            case TEX_FORMAT.R5G5B5A1_UNORM:
            {
                var R = ((pixel >> 0) & 0x1f) << 3;
                var G = ((pixel >> 5) & 0x1f) << 3;
                var B = ((pixel >> 10) & 0x1f) << 3;
                var A = ((pixel & 0x8000) >> 15) * 0xFF;

                comp[0] = (byte)(R | (R >> 5));
                comp[1] = (byte)(G | (G >> 5));
                comp[2] = (byte)(B | (B >> 5));
                comp[3] = (byte)A;
            }
                break;
            case TEX_FORMAT.R8G8B8A8_UNORM:
            case TEX_FORMAT.R8G8B8A8_UNORM_SRGB:
                comp[0] = (byte)(pixel & 0xFF);
                comp[1] = (byte)((pixel & 0xFF00) >> 8);
                comp[2] = (byte)((pixel & 0xFF0000) >> 16);
                comp[3] = (byte)((pixel & 0xFF000000) >> 24);
                break;
        }


        return comp;
    }

    //Method from https://github.com/aboood40091/BNTX-Editor/blob/master/formConv.py
    public static byte[] Decode(byte[] data, int width, int height, TEX_FORMAT format)
    {
        var bpp = STGenericTexture.GetBytesPerPixel(format);
        var size = width * height * 4;

        bpp = (uint)(data.Length / (width * height));

        var output = new byte[size];

        var inPos = 0;
        var outPos = 0;

        var comp = new byte[] { 0, 0, 0, 0xFF, 0, 0xFF };
        var compSel = new byte[4] { 0, 1, 2, 3 };

        if (format == TEX_FORMAT.LA8)
        {
            compSel = new byte[4] { 0, 0, 0, 1 };
            bpp = 2;
        }
        else if (format == TEX_FORMAT.L8)
        {
            compSel = new byte[4] { 0, 0, 0, 5 };
        }
        else if (format == TEX_FORMAT.R5G5B5A1_UNORM)
        {
            bpp = 2;
            return DDSCompressor.DecodePixelBlock(data, (int)width, (int)height,
                DDS.DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM);
        }

        for (var Y = 0; Y < height; Y++)
        for (var X = 0; X < width; X++)
        {
            inPos = (Y * width + X) * (int)bpp;
            outPos = (Y * width + X) * 4;

            var pixel = 0;
            for (var i = 0; i < bpp; i++)
                pixel |= data[inPos + i] << (8 * i);

            comp = GetComponentsFromPixel(format, pixel, comp);

            output[outPos + 3] = comp[compSel[3]];
            output[outPos + 2] = comp[compSel[2]];
            output[outPos + 1] = comp[compSel[1]];
            output[outPos + 0] = comp[compSel[0]];
        }

        return output;
    }
}

public class DDSCompressor
{
    //Huge thanks to gdkchan and AbooodXD for the method of decomp BC5/BC4.

    //Todo. Add these to DDS code and add in methods to compress and decode more formats
    //BC7 also needs to be decompressed properly since OpenTK can't decompress those

    //BC4 actually breaks a bit with artifacts so i'll need to go back and fix

    private static byte[] BCnDecodeTile(byte[] Input, int Offset, bool IsBC1)
    {
        var CLUT = new Color[4];

        var c0 = Get16(Input, Offset + 0);
        var c1 = Get16(Input, Offset + 2);

        CLUT[0] = DecodeRGB565(c0);
        CLUT[1] = DecodeRGB565(c1);
        CLUT[2] = CalculateCLUT2(CLUT[0], CLUT[1], c0, c1, IsBC1);
        CLUT[3] = CalculateCLUT3(CLUT[0], CLUT[1], c0, c1, IsBC1);

        var Indices = Get32(Input, Offset + 4);

        var IdxShift = 0;

        var Output = new byte[4 * 4 * 4];

        var OOffset = 0;

        for (var TY = 0; TY < 4; TY++)
        for (var TX = 0; TX < 4; TX++)
        {
            var Idx = (Indices >> IdxShift) & 3;

            IdxShift += 2;

            var Pixel = CLUT[Idx];

            Output[OOffset + 0] = Pixel.B;
            Output[OOffset + 1] = Pixel.G;
            Output[OOffset + 2] = Pixel.R;
            Output[OOffset + 3] = Pixel.A;

            OOffset += 4;
        }

        return Output;
    }

    private static Color DecodeRGB565(int Value)
    {
        var B = ((Value >> 0) & 0x1f) << 3;
        var G = ((Value >> 5) & 0x3f) << 2;
        var R = ((Value >> 11) & 0x1f) << 3;

        return Color.FromArgb(
            R | (R >> 5),
            G | (G >> 6),
            B | (B >> 5));
    }

    private static Color CalculateCLUT2(Color C0, Color C1, int c0, int c1, bool IsBC1)
    {
        if (c0 > c1 || !IsBC1)
            return Color.FromArgb(
                (2 * C0.R + C1.R) / 3,
                (2 * C0.G + C1.G) / 3,
                (2 * C0.B + C1.B) / 3);
        else
            return Color.FromArgb(
                (C0.R + C1.R) / 2,
                (C0.G + C1.G) / 2,
                (C0.B + C1.B) / 2);
    }

    private static Color CalculateCLUT3(Color C0, Color C1, int c0, int c1, bool IsBC1)
    {
        if (c0 > c1 || !IsBC1)
            return
                Color.FromArgb(
                    (2 * C1.R + C0.R) / 3,
                    (2 * C1.G + C0.G) / 3,
                    (2 * C1.B + C0.B) / 3);

        return Color.Transparent;
    }

    public static Bitmap DecompressBC1(byte[] data, int width, int height, bool IsSRGB)
    {
        var W = (width + 3) / 4;
        var H = (height + 3) / 4;

        var Output = new byte[W * H * 64];

        for (var Y = 0; Y < H; Y++)
        for (var X = 0; X < W; X++)
        {
            var IOffs = (Y * W + X) * 8;

            var Tile = BCnDecodeTile(data, IOffs, true);

            var TOffset = 0;

            for (var TY = 0; TY < 4; TY++)
            for (var TX = 0; TX < 4; TX++)
            {
                var OOffset = (X * 4 + TX + (Y * 4 + TY) * W * 4) * 4;

                Output[OOffset + 0] = Tile[TOffset + 0];
                Output[OOffset + 1] = Tile[TOffset + 1];
                Output[OOffset + 2] = Tile[TOffset + 2];
                Output[OOffset + 3] = Tile[TOffset + 3];

                TOffset += 4;
            }
        }

        return BitmapExtension.GetBitmap(Output, W * 4, H * 4);
    }

    public static Bitmap DecompressBC3(byte[] data, int width, int height, bool IsSRGB)
    {
        var W = (width + 3) / 4;
        var H = (height + 3) / 4;

        var Output = new byte[W * H * 64];

        for (var Y = 0; Y < H; Y++)
        for (var X = 0; X < W; X++)
        {
            var IOffs = (Y * W + X) * 16;

            var Tile = BCnDecodeTile(data, IOffs + 8, false);

            var Alpha = new byte[8];

            Alpha[0] = data[IOffs + 0];
            Alpha[1] = data[IOffs + 1];

            CalculateBC3Alpha(Alpha);

            var AlphaLow = Get32(data, IOffs + 2);
            var AlphaHigh = Get16(data, IOffs + 6);

            var AlphaCh = (uint)AlphaLow | ((ulong)AlphaHigh << 32);

            var TOffset = 0;

            for (var TY = 0; TY < 4; TY++)
            for (var TX = 0; TX < 4; TX++)
            {
                var OOffset = (X * 4 + TX + (Y * 4 + TY) * W * 4) * 4;

                var AlphaPx = Alpha[(AlphaCh >> (TY * 12 + TX * 3)) & 7];

                Output[OOffset + 0] = Tile[TOffset + 0];
                Output[OOffset + 1] = Tile[TOffset + 1];
                Output[OOffset + 2] = Tile[TOffset + 2];
                Output[OOffset + 3] = AlphaPx;

                TOffset += 4;
            }
        }

        return BitmapExtension.GetBitmap(Output, W * 4, H * 4);
    }

    public static Bitmap DecompressBC4(byte[] data, int width, int height, bool IsSNORM)
    {
        var W = (width + 3) / 4;
        var H = (height + 3) / 4;

        var Output = new byte[W * H * 64];

        for (var Y = 0; Y < H; Y++)
        for (var X = 0; X < W; X++)
        {
            var IOffs = (Y * W + X) * 8;

            var Red = new byte[8];

            Red[0] = data[IOffs + 0];
            Red[1] = data[IOffs + 1];

            if (IsSNORM)
                CalculateBC3AlphaS(Red);
            else
                CalculateBC3Alpha(Red);

            var RedLow = Get32(data, IOffs + 2);
            var RedHigh = Get16(data, IOffs + 6);

            var RedCh = (uint)RedLow | ((ulong)RedHigh << 32);

            var TOffset = 0;
            var TW = Math.Min(width - X * 4, 4);
            var TH = Math.Min(height - Y * 4, 4);

            for (var TY = 0; TY < 4; TY++)
            for (var TX = 0; TX < 4; TX++)
            {
                var OOffset = (X * 4 + TX + (Y * 4 + TY) * W * 4) * 4;

                var RedPx = Red[(RedCh >> (TY * 12 + TX * 3)) & 7];

                Output[OOffset + 0] = RedPx;
                Output[OOffset + 1] = RedPx;
                Output[OOffset + 2] = RedPx;
                Output[OOffset + 3] = 255;

                TOffset += 4;
            }
        }

        return BitmapExtension.GetBitmap(Output, W * 4, H * 4);
    }

    public static byte[] DecompressBC5(byte[] data, int width, int height, bool IsSNORM, bool IsByteArray)
    {
        var W = (width + 3) / 4;
        var H = (height + 3) / 4;

        var Output = new byte[width * height * 4];

        for (var Y = 0; Y < H; Y++)
        for (var X = 0; X < W; X++)
        {
            var IOffs = (Y * W + X) * 16;
            var Red = new byte[8];
            var Green = new byte[8];

            Red[0] = data[IOffs + 0];
            Red[1] = data[IOffs + 1];

            Green[0] = data[IOffs + 8];
            Green[1] = data[IOffs + 9];

            if (IsSNORM == true)
            {
                CalculateBC3AlphaS(Red);
                CalculateBC3AlphaS(Green);
            }
            else
            {
                CalculateBC3Alpha(Red);
                CalculateBC3Alpha(Green);
            }

            var RedLow = Get32(data, IOffs + 2);
            var RedHigh = Get16(data, IOffs + 6);

            var GreenLow = Get32(data, IOffs + 10);
            var GreenHigh = Get16(data, IOffs + 14);

            var RedCh = (uint)RedLow | ((ulong)RedHigh << 32);
            var GreenCh = (uint)GreenLow | ((ulong)GreenHigh << 32);

            var TW = Math.Min(width - X * 4, 4);
            var TH = Math.Min(height - Y * 4, 4);

            if (IsSNORM == true)
                for (var TY = 0; TY < TH; TY++)
                for (var TX = 0; TX < TW; TX++)
                {
                    var Shift = TY * 12 + TX * 3;
                    var OOffset = ((Y * 4 + TY) * width + X * 4 + TX) * 4;

                    var RedPx = Red[(RedCh >> Shift) & 7];
                    var GreenPx = Green[(GreenCh >> Shift) & 7];

                    if (IsSNORM == true)
                    {
                        RedPx += 0x80;
                        GreenPx += 0x80;
                    }

                    var NX = RedPx / 255f * 2 - 1;
                    var NY = GreenPx / 255f * 2 - 1;
                    var NZ = (float)Math.Sqrt(1 - (NX * NX + NY * NY));

                    Output[OOffset + 0] = Clamp((NX + 1) * 0.5f);
                    Output[OOffset + 1] = Clamp((NY + 1) * 0.5f);
                    Output[OOffset + 2] = Clamp((NZ + 1) * 0.5f);
                    Output[OOffset + 3] = 0xff;
                }
            else
                for (var TY = 0; TY < TH; TY++)
                for (var TX = 0; TX < TW; TX++)
                {
                    var Shift = TY * 12 + TX * 3;
                    var OOffset = ((Y * 4 + TY) * width + X * 4 + TX) * 4;

                    var RedPx = Red[(RedCh >> Shift) & 7];
                    var GreenPx = Green[(GreenCh >> Shift) & 7];

                    Output[OOffset + 0] = RedPx;
                    Output[OOffset + 1] = GreenPx;
                    Output[OOffset + 2] = 255;
                    Output[OOffset + 3] = 255;
                }
        }

        return Output;
    }

    public static Bitmap DecompressBC5(byte[] data, int width, int height, bool IsSNORM)
    {
        var W = (width + 3) / 4;
        var H = (height + 3) / 4;

        var Output = new byte[W * H * 64];

        for (var Y = 0; Y < H; Y++)
        for (var X = 0; X < W; X++)

        {
            var IOffs = (Y * W + X) * 16;
            var Red = new byte[8];
            var Green = new byte[8];

            Red[0] = data[IOffs + 0];
            Red[1] = data[IOffs + 1];

            Green[0] = data[IOffs + 8];
            Green[1] = data[IOffs + 9];

            if (IsSNORM == true)
            {
                CalculateBC3AlphaS(Red);
                CalculateBC3AlphaS(Green);
            }
            else
            {
                CalculateBC3Alpha(Red);
                CalculateBC3Alpha(Green);
            }

            var RedLow = Get32(data, IOffs + 2);
            var RedHigh = Get16(data, IOffs + 6);

            var GreenLow = Get32(data, IOffs + 10);
            var GreenHigh = Get16(data, IOffs + 14);

            var RedCh = (uint)RedLow | ((ulong)RedHigh << 32);
            var GreenCh = (uint)GreenLow | ((ulong)GreenHigh << 32);

            var TW = Math.Min(width - X * 4, 4);
            var TH = Math.Min(height - Y * 4, 4);


            if (IsSNORM == true)
                for (var TY = 0; TY < TH; TY++)
                for (var TX = 0; TX < TW; TX++)
                {
                    var Shift = TY * 12 + TX * 3;
                    var OOffset = ((Y * 4 + TY) * width + X * 4 + TX) * 4;

                    var RedPx = Red[(RedCh >> Shift) & 7];
                    var GreenPx = Green[(GreenCh >> Shift) & 7];

                    if (IsSNORM == true)
                    {
                        RedPx += 0x80;
                        GreenPx += 0x80;
                    }

                    var NX = RedPx / 255f * 2 - 1;
                    var NY = GreenPx / 255f * 2 - 1;
                    var NZ = (float)Math.Sqrt(1 - (NX * NX + NY * NY));

                    Output[OOffset + 0] = Clamp((NZ + 1) * 0.5f);
                    Output[OOffset + 1] = Clamp((NY + 1) * 0.5f);
                    Output[OOffset + 2] = Clamp((NX + 1) * 0.5f);
                    Output[OOffset + 3] = 0xff;
                }
            else
                for (var TY = 0; TY < TH; TY++)
                for (var TX = 0; TX < TW; TX++)
                {
                    var Shift = TY * 12 + TX * 3;
                    var OOffset = ((Y * 4 + TY) * width + X * 4 + TX) * 4;

                    var RedPx = Red[(RedCh >> Shift) & 7];
                    var GreenPx = Green[(GreenCh >> Shift) & 7];

                    Output[OOffset + 0] = 255;
                    Output[OOffset + 1] = GreenPx;
                    Output[OOffset + 2] = RedPx;
                    Output[OOffset + 3] = 255;
                }
        }

        return BitmapExtension.GetBitmap(Output, W * 4, H * 4);
    }

    /*public static unsafe byte[] CompressBlock(Byte[] data, int width, int height, DDS.DXGI_FORMAT format, bool multiThread, float AlphaRef = 0.5f, STCompressionMode CompressionMode = STCompressionMode.Normal)
    {
        long inputRowPitch = width * 4;
        long inputSlicePitch = width * height * 4;

        if (data.Length == inputSlicePitch)
        {
            byte* buf;
            buf = (byte*)Marshal.AllocHGlobal((int)inputSlicePitch);
            Marshal.Copy(data, 0, (IntPtr)buf, (int)inputSlicePitch);

            DirectXTexNet.Image inputImage = new DirectXTexNet.Image(
                width, height, DXGI_FORMAT.R8G8B8A8_UNORM, inputRowPitch,
                inputSlicePitch, (IntPtr)buf, null);

            TexMetadata texMetadata = new TexMetadata(width, height, 1, 1, 1, 0, 0,
                DXGI_FORMAT.R8G8B8A8_UNORM, TEX_DIMENSION.TEXTURE2D);

            ScratchImage scratchImage = TexHelper.Instance.InitializeTemporary(
                new DirectXTexNet.Image[] { inputImage }, texMetadata, null);

            var compFlags = TEX_COMPRESS_FLAGS.DEFAULT;

            if (multiThread)
                compFlags |= TEX_COMPRESS_FLAGS.PARALLEL;

            if (format == DDS.DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM ||
                format == DDS.DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB ||
                format == DDS.DXGI_FORMAT.DXGI_FORMAT_BC7_TYPELESS)
            {
                if (CompressionMode == STCompressionMode.Fast)
                    compFlags |= TEX_COMPRESS_FLAGS.BC7_QUICK;
            }

            if (format == DDS.DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB ||
            format == DDS.DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB ||
            format == DDS.DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB ||
            format == DDS.DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB ||
            format == DDS.DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB)
            {
                compFlags |= TEX_COMPRESS_FLAGS.SRGB;
            }

            using (var comp = scratchImage.Compress((DXGI_FORMAT)format, compFlags, 0.5f))
            {
                long outRowPitch;
                long outSlicePitch;
                TexHelper.Instance.ComputePitch((DXGI_FORMAT)format, width, height, out outRowPitch, out outSlicePitch, CP_FLAGS.NONE);

                byte[] result = new byte[outSlicePitch];
                Marshal.Copy(comp.GetImage(0).Pixels, result, 0, result.Length);

                inputImage = null;
                scratchImage.Dispose();


                return result;
            }
        }
        return null;
    }*/
    public static unsafe byte[] DecompressBlock(byte[] data, int width, int height, DDS.DXGI_FORMAT format)
    {
        long inputRowPitch = 1;
        long inputSlicePitch = 1;

        int r;
        unsafe
        {
            FormatHelper.ComputePitch((DXGI_FORMAT)format, width, height, ref inputRowPitch, ref inputSlicePitch,
                CPFlags.BadDxtnTails);
        }

        DXGI_FORMAT FormatDecompressed;

        if (format.ToString().Contains("SRGB"))
            FormatDecompressed = DXGI_FORMAT.R8G8B8A8_UNORM_SRGB;
        else
            FormatDecompressed = DXGI_FORMAT.R8G8B8A8_UNORM;

        var image = DirectXTexNativeHelper.CreateScratchImage();
        int iResult = DirectXTexNativeHelper.Initialize(image,
            new TexMetadata((ulong)width, (ulong)height, 1, 1, 1, 0,
                0, (int)format, TexDimension.Texture2D), CPFlags.None);

        fixed (byte* pData = data)
        {
            var dstImg = image.GetImages()[0];
            Buffer.MemoryCopy(pData, dstImg.Pixels, (long)dstImg.RowPitch * (long)dstImg.Height, data.Length);
        }

        ScratchImage decompressedImage = DirectXTexNativeHelper.CreateScratchImage();
        int dResult = DirectXTexNativeHelper.Decompress2(image.GetImages(), DirectXTexNativeHelper.GetImageCount(image), DirectXTexNativeHelper.GetMetadata(image), (uint)FormatDecompressed, decompressedImage);
        DirectXTexNativeHelper.ScratchImageRelease(image);

        var outImage = decompressedImage.GetImages()[0];
        byte[] result = new byte[outImage.RowPitch * outImage.Height];
        fixed (byte* pResult = result)
        {
            Buffer.MemoryCopy(outImage.Pixels, pResult, result.Length, result.Length);
        }
        DirectXTexNativeHelper.ScratchImageRelease(decompressedImage);

        return result;
    }

    public static byte[] DecodePixelBlock(byte[] data, int width, int height, DDS.DXGI_FORMAT format,
        float AlphaRef = 0.5f)
    {
        if (format == DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM)
        {
            var result = new byte[data.Length];
            Array.Copy(data, result, data.Length);
            return result;
        }

        return Convert(data, width, height, (DXGI_FORMAT)format, DXGI_FORMAT.R8G8B8A8_UNORM);
    }

    public static byte[] Convert(byte[] data, int width, int height, DXGI_FORMAT inputFormat, DXGI_FORMAT outputFormat)
    {
        long inputRowPitch = 1;
        long inputSlicePitch = 1;

        int r;
        unsafe
        {
            FormatHelper.ComputePitch((DXGI_FORMAT)inputFormat, width, height, ref inputRowPitch, ref inputSlicePitch,
                CPFlags.BadDxtnTails);
        }
        
        if (data.Length == inputSlicePitch)
        {
            var image = DirectXTexNativeHelper.CreateScratchImage();
            int iResult = DirectXTexNativeHelper.Initialize(image,
                new TexMetadata((ulong)width, (ulong)height, 1, 1, 1, 0,
                    0, (int)inputFormat, TexDimension.Texture2D), CPFlags.None);
            
            var convFlags = TEX_FILTER_FLAGS.DEFAULT;
            if (inputFormat == DXGI_FORMAT.B8G8R8A8_UNORM_SRGB ||
                inputFormat == DXGI_FORMAT.B8G8R8X8_UNORM_SRGB ||
                inputFormat == DXGI_FORMAT.R8G8B8A8_UNORM_SRGB)
            {
                convFlags |= TEX_FILTER_FLAGS.SRGB;
            }

            unsafe
            {
                fixed (byte* pData = data)
                {
                    var dstImg = image.GetImages()[0];
                    Buffer.MemoryCopy(pData, dstImg.Pixels, (long)dstImg.RowPitch * (long)dstImg.Height, data.Length);
                }

                ScratchImage convertedImage = DirectXTexNativeHelper.CreateScratchImage();
                int cResult = DirectXTexNativeHelper.Convert2(image.GetImages(), DirectXTexNativeHelper.GetImageCount(image),
                    DirectXTexNativeHelper.GetMetadata(image), outputFormat, convFlags, 0.5f, convertedImage);
                DirectXTexNativeHelper.ScratchImageRelease(image);
                
                long outRowPitch = 0;
                long outSlicePitch = 0;
                unsafe
                {
                    FormatHelper.ComputePitch((DXGI_FORMAT)outputFormat, width, height, ref outRowPitch, ref outSlicePitch,
                        CPFlags.BadDxtnTails);
                }

                var outImage = convertedImage.GetImages()[0];
                byte[] result = new byte[outSlicePitch];
                
                Marshal.Copy((nint) outImage.Pixels, result, 0, result.Length);
                DirectXTexNativeHelper.ScratchImageRelease(convertedImage);

                return result;
            }
        }

        return null;
    }
/*    public static byte[] CompressBlock(Byte[] data, int width, int height, DDS.DXGI_FORMAT format, float AlphaRef)
    {
        return DirectXTex.ImageCompressor.Compress(data, width, height, (int)format, AlphaRef);
    }*/


    /*public static Bitmap DecompressCompLibBlock(Byte[] data, int width, int height, DDS.DXGI_FORMAT format, bool GetBitmap)
    {
        return BitmapExtension.GetBitmap(DecompressBlock(data, width, height, format), (int)width, (int)height);
    }*/

    public static int Get16(byte[] Data, int Address)
    {
        return
            (Data[Address + 0] << 0) |
            (Data[Address + 1] << 8);
    }

    public static int Get32(byte[] Data, int Address)
    {
        return
            (Data[Address + 0] << 0) |
            (Data[Address + 1] << 8) |
            (Data[Address + 2] << 16) |
            (Data[Address + 3] << 24);
    }

    private static byte Clamp(float Value)
    {
        if (Value > 1)
            return 0xff;
        else if (Value < 0)
            return 0;
        else
            return (byte)(Value * 0xff);
    }

    private static void CalculateBC3Alpha(byte[] Alpha)
    {
        if (Alpha[0] > Alpha[1])
        {
            for (var i = 2; i < 8; i++)
                Alpha[i] = (byte)(Alpha[0] + (Alpha[1] - Alpha[0]) * (i - 1) / 7);
        }
        else
        {
            for (var i = 2; i < 6; i++)
                Alpha[i] = (byte)(Alpha[0] + (Alpha[1] - Alpha[0]) * (i - 1) / 5);
            Alpha[6] = 0;
            Alpha[7] = 255;
        }
    }

    private static void CalculateBC3AlphaS(byte[] Alpha)
    {
        if ((sbyte)Alpha[0] > (sbyte)Alpha[1])
        {
            for (var i = 2; i < 8; i++)
                Alpha[i] = (byte)(Alpha[0] + ((sbyte)Alpha[1] - (sbyte)Alpha[0]) * (i - 1) / 7);
        }
        else
        {
            for (var i = 2; i < 6; i++)
                Alpha[i] = (byte)(Alpha[0] + ((sbyte)Alpha[1] - (sbyte)Alpha[0]) * (i - 1) / 5);
            Alpha[6] = 0x80;
            Alpha[7] = 0x7f;
        }
    }
}

public enum TEX_DIMENSION
{
    TEXTURE1D = 2,
    TEXTURE2D,
    TEXTURE3D
}

public enum PALETTE_FORMAT : uint
{
    None,
    IA8,
    RGB565,
    RGB5A3
}

/*public class TexHelperImpl
{
    public static TexHelperImpl Instance;

    public readonly int IndexOutOfRange = (int)(Environment.Is64BitProcess ? (-1L) : 4294967295L);

    public TexHelperImpl()
    {
        if (Instance != null) return;

        Instance = (TexHelperImpl)Activator.CreateInstance(Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Environment.Is64BitProcess ? "x64" : "x86", "DirectXTexNetImpl.dll")).GetType("DirectXTexNet.TexHelperImpl"));

        

        Console.WriteLine("INSTANCE: " + Instance);
    }

    public void ComputePitch(DXGI_FORMAT fmt, int width, int height, out long rowPitch, out long slicePitch, CP_FLAGS flags)
    {
        Instance.ComputePitch(fmt, width, height, out rowPitch, out slicePitch, flags);
    }

    public long ComputeScanlines(DXGI_FORMAT fmt, int height)
    {
        return Instance.ComputeScanlines(fmt, height);
    }

    /*public abstract int ComputeImageIndex(TexMetadata metadata, int mip, int item, int slice);

    public abstract DXGI_FORMAT MakeSRGB(DXGI_FORMAT fmt);

    public abstract DXGI_FORMAT MakeTypeless(DXGI_FORMAT fmt);

    public abstract DXGI_FORMAT MakeTypelessUNORM(DXGI_FORMAT fmt);

    public abstract DXGI_FORMAT MakeTypelessFLOAT(DXGI_FORMAT fmt);

    public abstract TexMetadata GetMetadataFromDDSMemory(IntPtr pSource, long size, DDS_FLAGS flags);

    public abstract TexMetadata GetMetadataFromDDSFile(string szFile, DDS_FLAGS flags);

    public abstract TexMetadata GetMetadataFromHDRMemory(IntPtr pSource, long size);

    public abstract TexMetadata GetMetadataFromHDRFile(string szFile);

    public abstract TexMetadata GetMetadataFromTGAMemory(IntPtr pSource, long size);

    public abstract TexMetadata GetMetadataFromTGAFile(string szFile);

    public abstract TexMetadata GetMetadataFromWICMemory(IntPtr pSource, long size, WIC_FLAGS flags);

    public abstract TexMetadata GetMetadataFromWICFile(string szFile, WIC_FLAGS flags);

    public abstract ScratchImage Initialize(TexMetadata mdata, CP_FLAGS flags);

    public abstract ScratchImage Initialize1D(DXGI_FORMAT fmt, int length, int arraySize, int mipLevels, CP_FLAGS flags);

    public abstract ScratchImage Initialize2D(DXGI_FORMAT fmt, int width, int height, int arraySize, int mipLevels, CP_FLAGS flags);

    public abstract ScratchImage Initialize3D(DXGI_FORMAT fmt, int width, int height, int depth, int mipLevels, CP_FLAGS flags);

    public abstract ScratchImage InitializeCube(DXGI_FORMAT fmt, int width, int height, int nCubes, int mipLevels, CP_FLAGS flags);*/

/* public ScratchImage InitializeTemporary(Image[] images, TexMetadata metadata, params IDisposable[] takeOwnershipOf)
 {
     return Instance.InitializeTemporary(images, metadata, takeOwnershipOf);
 }*/

/*public abstract ScratchImage LoadFromDDSMemory(IntPtr pSource, long size, DDS_FLAGS flags);

public abstract ScratchImage LoadFromDDSFile(string szFile, DDS_FLAGS flags);

public abstract ScratchImage LoadFromHDRMemory(IntPtr pSource, long size);

public abstract ScratchImage LoadFromHDRFile(string szFile);

public abstract ScratchImage LoadFromTGAMemory(IntPtr pSource, long size);

public abstract ScratchImage LoadFromTGAFile(string szFile);

public abstract ScratchImage LoadFromWICMemory(IntPtr pSource, long size, WIC_FLAGS flags);

public abstract ScratchImage LoadFromWICFile(string szFile, WIC_FLAGS flags);

public abstract void CopyRectangle(Image srcImage, int srcX, int srcY, int srcWidth, int srcHeight, Image dstImage, TEX_FILTER_FLAGS filter, int xOffset, int yOffset);

public abstract IntPtr GetWICFactory(bool iswic2);

public abstract void SetWICFactory(IntPtr pWIC);

public abstract bool IsSupportedTexture(IntPtr pDevice, TexMetadata metadata);

public abstract ScratchImage CaptureTexture(IntPtr pDevice, IntPtr pContext, IntPtr pSource);*/
//}

/// <summary>
/// A helper class for working with pixel formats.
/// </summary>
public static class FormatHelper
{
    /// <summary>
    /// Computes the pitch and slice pitch for a given <see cref="Format"/>, width, height, and optional flags.
    /// </summary>
    /// <param name="fmt">The <see cref="Format"/> to compute the pitch for.</param>
    /// <param name="width">The width of the data.</param>
    /// <param name="height">The height of the data.</param>
    /// <param name="rowPitch">Output parameter to store the row pitch.</param>
    /// <param name="slicePitch">Output parameter to store the slice pitch.</param>
    /// <param name="flags">Optional flags to control the pitch computation.</param>
    /// <returns><c>true</c> if the pitch was computed successfully; <c>false</c> if there was an issue.</returns>
    public static bool ComputePitch(DXGI_FORMAT fmt, int width, int height, ref long rowPitch, ref long slicePitch,
        CPFlags flags)
    {
        int pitch;
        int slice;
        switch (fmt)
        {
            case DXGI_FORMAT.BC1_TYPELESS:
            case DXGI_FORMAT.BC1_UNORM:
            case DXGI_FORMAT.BC1_UNORM_SRGB:
            case DXGI_FORMAT.BC4_TYPELESS:
            case DXGI_FORMAT.BC4_UNORM:
            case DXGI_FORMAT.BC4_SNORM:
            {
                if ((flags & CPFlags.BadDxtnTails) != 0)
                {
                    var nbw = width >> 2;
                    var nbh = height >> 2;
                    pitch = Math.Max(1, nbw * 8);
                    slice = Math.Max(1, pitch * nbh);
                }
                else
                {
                    var nbw = Math.Max(1, (width + 3) / 4);
                    var nbh = Math.Max(1, height + 3 / 4);
                    pitch = nbw * 8;
                    slice = pitch * nbh;
                }
            }
                break;

            case DXGI_FORMAT.BC2_TYPELESS:
            case DXGI_FORMAT.BC2_UNORM:
            case DXGI_FORMAT.BC2_UNORM_SRGB:
            case DXGI_FORMAT.BC3_TYPELESS:
            case DXGI_FORMAT.BC3_UNORM:
            case DXGI_FORMAT.BC3_UNORM_SRGB:
            case DXGI_FORMAT.BC5_TYPELESS:
            case DXGI_FORMAT.BC5_UNORM:
            case DXGI_FORMAT.BC5_SNORM:
            case DXGI_FORMAT.BC6H_TYPELESS:
            case DXGI_FORMAT.BC6H_UF16:
            case DXGI_FORMAT.BC6H_SF16:
            case DXGI_FORMAT.BC7_TYPELESS:
            case DXGI_FORMAT.BC7_UNORM:
            case DXGI_FORMAT.BC7_UNORM_SRGB:
            {
                if ((flags & CPFlags.BadDxtnTails) != 0)
                {
                    var nbw = width >> 2;
                    var nbh = height >> 2;
                    pitch = Math.Max(1, nbw * 16);
                    slice = Math.Max(1, pitch * nbh);
                }
                else
                {
                    var nbw = Math.Max(1, (width + 3) / 4);
                    var nbh = Math.Max(1, (height + 3) / 4);
                    pitch = nbw * 16;
                    slice = pitch * nbh;
                }
            }
                break;

            case DXGI_FORMAT.R8G8_B8G8_UNORM:
            case DXGI_FORMAT.G8R8_G8B8_UNORM:
            case DXGI_FORMAT.YUY2:
                pitch = ((width + 1) >> 1) * 4;
                slice = pitch * height;
                break;

            case DXGI_FORMAT.Y210:
            case DXGI_FORMAT.Y216:
                pitch = ((width + 1) >> 1) * 8;
                slice = pitch * height;
                break;

            case DXGI_FORMAT.NV12:
            case DXGI_FORMAT.OPAQUE_420:
                if (height % 2 != 0)
                    // Requires a height alignment of 2.
                    return false;
                pitch = ((width + 1) >> 1) * 2;
                slice = pitch * (height + ((height + 1) >> 1));
                break;

            case DXGI_FORMAT.P010:
            case DXGI_FORMAT.P016:
                if (height % 2 != 0)
                    // Requires a height alignment of 2.
                    return false;

                goto case DXGI_FORMAT.NV11;

            case DXGI_FORMAT.NV11:
                pitch = ((width + 3) >> 2) * 4;
                slice = pitch * height * 2;
                break;

            default:
            {
                int bpp;

                if ((flags & CPFlags.Flags24Bpp) != 0)
                    bpp = 24;
                else if ((flags & CPFlags.Flags16Bpp) != 0)
                    bpp = 16;
                else if ((flags & CPFlags.Flags8Bpp) != 0)
                    bpp = 8;
                else
                    bpp = (int)BitsPerPixel(fmt);

                if (bpp == 0) return false;

                if ((flags & (CPFlags.LegacyDword | CPFlags.Paragraph | CPFlags.Ymm | CPFlags.Zmm | CPFlags.Page4K)) !=
                    0)
                {
                    if ((flags & CPFlags.Page4K) != 0)
                    {
                        pitch = (width * bpp + 32767) / 32768 * 4096;
                        slice = pitch * height;
                    }
                    else if ((flags & CPFlags.Zmm) != 0)
                    {
                        pitch = (width * bpp + 511) / 512 * 64;
                        slice = pitch * height;
                    }
                    else if ((flags & CPFlags.Ymm) != 0)
                    {
                        pitch = (width * bpp + 255) / 256 * 32;
                        slice = pitch * height;
                    }
                    else if ((flags & CPFlags.Paragraph) != 0)
                    {
                        pitch = (width * bpp + 127) / 128 * 16;
                        slice = pitch * height;
                    }
                    else // DWORD alignment
                    {
                        // Special computation for some incorrectly created DDS files based on
                        // legacy DirectDraw assumptions about pitch alignment
                        pitch = (width * bpp + 31) / 32 * sizeof(uint);
                        slice = pitch * height;
                    }
                }
                else
                {
                    // Default byte alignment
                    pitch = (width * bpp + 7) / 8;
                    slice = pitch * height;
                }
            }
                break;
        }

        rowPitch = pitch;
        slicePitch = slice;

        return true;
    }

    /// <summary>
    /// Gets the number of bits per pixel for a given <see cref="Format"/>.
    /// </summary>
    /// <param name="fmt">The <see cref="Format"/> to get the bits per pixel for.</param>
    /// <returns>The number of bits per pixel for the given format.</returns>
    public static int BitsPerPixel(DXGI_FORMAT fmt)
    {
        return fmt switch
        {
            DXGI_FORMAT.R32G32B32A32_TYPELESS or DXGI_FORMAT.R32G32B32A32_FLOAT or DXGI_FORMAT.R32G32B32A32_UINT
                or DXGI_FORMAT.R32G32B32A32_SINT => 128,
            DXGI_FORMAT.R32G32B32_TYPELESS or DXGI_FORMAT.R32G32B32_FLOAT or DXGI_FORMAT.R32G32B32_UINT
                or DXGI_FORMAT.R32G32B32_SINT => 96,
            DXGI_FORMAT.R16G16B16A16_TYPELESS or DXGI_FORMAT.R16G16B16A16_FLOAT or DXGI_FORMAT.R16G16B16A16_UNORM
                or DXGI_FORMAT.R16G16B16A16_UINT or DXGI_FORMAT.R16G16B16A16_SNORM or DXGI_FORMAT.R16G16B16A16_SINT
                or DXGI_FORMAT.R32G32_TYPELESS or DXGI_FORMAT.R32G32_FLOAT or DXGI_FORMAT.R32G32_UINT
                or DXGI_FORMAT.R32G32_SINT or DXGI_FORMAT.R32G8X24_TYPELESS or DXGI_FORMAT.D32_FLOAT_S8X24_UINT
                or DXGI_FORMAT.R32_FLOAT_X8X24_TYPELESS or DXGI_FORMAT.X32_TYPELESS_G8X24_UINT or DXGI_FORMAT.Y416
                or DXGI_FORMAT.Y210 or DXGI_FORMAT.Y216 => 64,
            DXGI_FORMAT.R10G10B10A2_TYPELESS or DXGI_FORMAT.R10G10B10A2_UNORM or DXGI_FORMAT.R10G10B10A2_UINT
                or DXGI_FORMAT.R11G11B10_FLOAT or DXGI_FORMAT.R8G8B8A8_TYPELESS or DXGI_FORMAT.R8G8B8A8_UNORM
                or DXGI_FORMAT.R8G8B8A8_UNORM_SRGB or DXGI_FORMAT.R8G8B8A8_UINT or DXGI_FORMAT.R8G8B8A8_SNORM
                or DXGI_FORMAT.R8G8B8A8_SINT or DXGI_FORMAT.R16G16_TYPELESS or DXGI_FORMAT.R16G16_FLOAT
                or DXGI_FORMAT.R16G16_UNORM or DXGI_FORMAT.R16G16_UINT or DXGI_FORMAT.R16G16_SNORM
                or DXGI_FORMAT.R16G16_SINT or DXGI_FORMAT.R32_TYPELESS or DXGI_FORMAT.D32_FLOAT or DXGI_FORMAT.R32_FLOAT
                or DXGI_FORMAT.R32_UINT or DXGI_FORMAT.R32_SINT or DXGI_FORMAT.R24G8_TYPELESS
                or DXGI_FORMAT.D24_UNORM_S8_UINT or DXGI_FORMAT.R24_UNORM_X8_TYPELESS
                or DXGI_FORMAT.X24_TYPELESS_G8_UINT or DXGI_FORMAT.R9G9B9E5_SHAREDEXP or DXGI_FORMAT.R8G8_B8G8_UNORM
                or DXGI_FORMAT.G8R8_G8B8_UNORM or DXGI_FORMAT.B8G8R8A8_UNORM or DXGI_FORMAT.B8G8R8X8_UNORM
                or DXGI_FORMAT.R10G10B10_XR_BIAS_A2_UNORM or DXGI_FORMAT.B8G8R8A8_TYPELESS
                or DXGI_FORMAT.B8G8R8A8_UNORM_SRGB or DXGI_FORMAT.B8G8R8X8_TYPELESS or DXGI_FORMAT.B8G8R8X8_UNORM_SRGB
                or DXGI_FORMAT.AYUV or DXGI_FORMAT.Y410 or DXGI_FORMAT.YUY2 => 32,
            DXGI_FORMAT.P010 or DXGI_FORMAT.P016 => 24,
            DXGI_FORMAT.R8G8_TYPELESS or DXGI_FORMAT.R8G8_UNORM or DXGI_FORMAT.R8G8_UINT or DXGI_FORMAT.R8G8_SNORM
                or DXGI_FORMAT.R8G8_SINT or DXGI_FORMAT.R16_TYPELESS or DXGI_FORMAT.R16_FLOAT or DXGI_FORMAT.D16_UNORM
                or DXGI_FORMAT.R16_UNORM or DXGI_FORMAT.R16_UINT or DXGI_FORMAT.R16_SNORM or DXGI_FORMAT.R16_SINT
                or DXGI_FORMAT.B5G6R5_UNORM or DXGI_FORMAT.B5G5R5A1_UNORM or DXGI_FORMAT.A8P8
                or DXGI_FORMAT.B4G4R4A4_UNORM => 16,
            DXGI_FORMAT.NV12 or DXGI_FORMAT.OPAQUE_420 or DXGI_FORMAT.NV11 => 12,
            DXGI_FORMAT.R8_TYPELESS or DXGI_FORMAT.R8_UNORM or DXGI_FORMAT.R8_UINT or DXGI_FORMAT.R8_SNORM
                or DXGI_FORMAT.R8_SINT or DXGI_FORMAT.A8_UNORM or DXGI_FORMAT.BC2_TYPELESS or DXGI_FORMAT.BC2_UNORM
                or DXGI_FORMAT.BC2_UNORM_SRGB or DXGI_FORMAT.BC3_TYPELESS or DXGI_FORMAT.BC3_UNORM
                or DXGI_FORMAT.BC3_UNORM_SRGB or DXGI_FORMAT.BC5_TYPELESS or DXGI_FORMAT.BC5_UNORM
                or DXGI_FORMAT.BC5_SNORM or DXGI_FORMAT.BC6H_TYPELESS or DXGI_FORMAT.BC6H_UF16 or DXGI_FORMAT.BC6H_SF16
                or DXGI_FORMAT.BC7_TYPELESS or DXGI_FORMAT.BC7_UNORM or DXGI_FORMAT.BC7_UNORM_SRGB or DXGI_FORMAT.AI44
                or DXGI_FORMAT.IA44 or DXGI_FORMAT.P8 => 8,
            DXGI_FORMAT.R1_UNORM => 1,
            DXGI_FORMAT.BC1_TYPELESS or DXGI_FORMAT.BC1_UNORM or DXGI_FORMAT.BC1_UNORM_SRGB or DXGI_FORMAT.BC4_TYPELESS
                or DXGI_FORMAT.BC4_UNORM or DXGI_FORMAT.BC4_SNORM => 4,
            _ => 0
        };
    }
}

public enum CPFlags
{
    None = 0,
    LegacyDword = 1,
    Paragraph = 2,
    Ymm = 4,
    Zmm = 8,
    Page4K = 0x200,
    BadDxtnTails = 0x1000,
    Flags24Bpp = 0x10000,
    Flags16Bpp = 0x20000,
    Flags8Bpp = 0x40000
}

public enum DXGI_FORMAT
{
    UNKNOWN = 0,
    R32G32B32A32_TYPELESS = 1,
    R32G32B32A32_FLOAT = 2,
    R32G32B32A32_UINT = 3,
    R32G32B32A32_SINT = 4,
    R32G32B32_TYPELESS = 5,
    R32G32B32_FLOAT = 6,
    R32G32B32_UINT = 7,
    R32G32B32_SINT = 8,
    R16G16B16A16_TYPELESS = 9,
    R16G16B16A16_FLOAT = 10,
    R16G16B16A16_UNORM = 11,
    R16G16B16A16_UINT = 12,
    R16G16B16A16_SNORM = 13,
    R16G16B16A16_SINT = 14,
    R32G32_TYPELESS = 15,
    R32G32_FLOAT = 16,
    R32G32_UINT = 17,
    R32G32_SINT = 18,
    R32G8X24_TYPELESS = 19,
    D32_FLOAT_S8X24_UINT = 20,
    R32_FLOAT_X8X24_TYPELESS = 21,
    X32_TYPELESS_G8X24_UINT = 22,
    R10G10B10A2_TYPELESS = 23,
    R10G10B10A2_UNORM = 24,
    R10G10B10A2_UINT = 25,
    R11G11B10_FLOAT = 26,
    R8G8B8A8_TYPELESS = 27,
    R8G8B8A8_UNORM = 28,
    R8G8B8A8_UNORM_SRGB = 29,
    R8G8B8A8_UINT = 30,
    R8G8B8A8_SNORM = 31,
    R8G8B8A8_SINT = 32,
    R16G16_TYPELESS = 33,
    R16G16_FLOAT = 34,
    R16G16_UNORM = 35,
    R16G16_UINT = 36,
    R16G16_SNORM = 37,
    R16G16_SINT = 38,
    R32_TYPELESS = 39,
    D32_FLOAT = 40,
    R32_FLOAT = 41,
    R32_UINT = 42,
    R32_SINT = 43,
    R24G8_TYPELESS = 44,
    D24_UNORM_S8_UINT = 45,
    R24_UNORM_X8_TYPELESS = 46,
    X24_TYPELESS_G8_UINT = 47,
    R8G8_TYPELESS = 48,
    R8G8_UNORM = 49,
    R8G8_UINT = 50,
    R8G8_SNORM = 51,
    R8G8_SINT = 52,
    R16_TYPELESS = 53,
    R16_FLOAT = 54,
    D16_UNORM = 55,
    R16_UNORM = 56,
    R16_UINT = 57,
    R16_SNORM = 58,
    R16_SINT = 59,
    R8_TYPELESS = 60,
    R8_UNORM = 61,
    R8_UINT = 62,
    R8_SNORM = 63,
    R8_SINT = 64,
    A8_UNORM = 65,
    R1_UNORM = 66,
    R9G9B9E5_SHAREDEXP = 67,
    R8G8_B8G8_UNORM = 68,
    G8R8_G8B8_UNORM = 69,
    BC1_TYPELESS = 70,
    BC1_UNORM = 71,
    BC1_UNORM_SRGB = 72,
    BC2_TYPELESS = 73,
    BC2_UNORM = 74,
    BC2_UNORM_SRGB = 75,
    BC3_TYPELESS = 76,
    BC3_UNORM = 77,
    BC3_UNORM_SRGB = 78,
    BC4_TYPELESS = 79,
    BC4_UNORM = 80,
    BC4_SNORM = 81,
    BC5_TYPELESS = 82,
    BC5_UNORM = 83,
    BC5_SNORM = 84,
    B5G6R5_UNORM = 85,
    B5G5R5A1_UNORM = 86,
    B8G8R8A8_UNORM = 87,
    B8G8R8X8_UNORM = 88,
    R10G10B10_XR_BIAS_A2_UNORM = 89,
    B8G8R8A8_TYPELESS = 90,
    B8G8R8A8_UNORM_SRGB = 91,
    B8G8R8X8_TYPELESS = 92,
    B8G8R8X8_UNORM_SRGB = 93,
    BC6H_TYPELESS = 94,
    BC6H_UF16 = 95,
    BC6H_SF16 = 96,
    BC7_TYPELESS = 97,
    BC7_UNORM = 98,
    BC7_UNORM_SRGB = 99,
    AYUV = 100,
    Y410 = 101,
    Y416 = 102,
    NV12 = 103,
    P010 = 104,
    P016 = 105,
    OPAQUE_420 = 106,
    YUY2 = 107,
    Y210 = 108,
    Y216 = 109,
    NV11 = 110,
    AI44 = 111,
    IA44 = 112,
    P8 = 113,
    A8P8 = 114,
    B4G4R4A4_UNORM = 115,
    P208 = 130,
    V208 = 131,
    V408 = 132,
    SAMPLER_FEEDBACK_MIN_MIP_OPAQUE = 189,
    SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE = 190
}

public class FileReader : BinaryDataReader
{
    public bool ReverseMagic { get; set; } = false;

    public FileReader(Stream stream, bool leaveOpen = false)
        : base(stream, Encoding.ASCII, leaveOpen)
    {
        Position = 0;
    }

    public FileReader(Stream stream, Encoding encoding, bool leaveOpen = false)
        : base(stream, encoding, leaveOpen)
    {
        Position = 0;
    }

    public FileReader(string fileName, bool leaveOpen = false)
        : this(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), leaveOpen)
    {
        Position = 0;
    }

    public FileReader(byte[] data)
        : this(new MemoryStream(data))
    {
        Position = 0;
    }

    public bool IsBigEndian => ByteOrder == ByteOrder.BigEndian;

    //Checks signature (no stream advancement)
    public bool CheckSignature(int length, string Identifier, long position = 0)
    {
        if (Position + length + position >= BaseStream.Length || position < 0)
            return false;

        Position = position;
        var signature = ReadString(length, Encoding.ASCII);

        //Reset position
        Position = 0;

        return signature == Identifier;
    }

    //From kuriimu https://github.com/IcySon55/Kuriimu/blob/master/src/Kontract/IO/BinaryReaderX.cs#L40
    //public T ReadStruct<T>() => ReadBytes(Marshal.SizeOf<T>()).BytesToStruct<T>(ByteOrder == ByteOrder.BigEndian);
    //public List<T> ReadMultipleStructs<T>(int count) => Enumerable.Range(0, count).Select(_ => ReadStruct<T>()).ToList();
    //public List<T> ReadMultipleStructs<T>(uint count) => Enumerable.Range(0, (int)count).Select(_ => ReadStruct<T>()).ToList();

    public bool CheckSignature(uint Identifier, long position = 0)
    {
        if (Position + 4 >= BaseStream.Length || position < 0 || position + 4 >= BaseStream.Length)
            return false;

        Position = position;
        var signature = ReadUInt32();

        //Reset position
        Position = 0;

        return signature == Identifier;
    }
    
    public UnityEngine.Vector3 ReadVec3()
    {
        return new UnityEngine.Vector3(ReadSingle(), ReadSingle(), ReadSingle());
    }

    public UnityEngine.Vector4 ReadVec4()
    {
        return new UnityEngine.Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
    }
    public UnityEngine.Vector2 ReadVec2()
    {
        return new UnityEngine.Vector2(ReadSingle(), ReadSingle());
    }

    public int ReadInt32(int position)
    {
        var origin = Position;

        SeekBegin(position);
        var value = ReadInt32();

        SeekBegin(origin + sizeof(int));
        return value;
    }

    public uint ReadUInt32(int position)
    {
        var origin = Position;

        SeekBegin(position);
        var value = ReadUInt32();

        SeekBegin(origin + sizeof(uint));
        return value;
    }

    public string ReadNameOffset(bool IsRelative, Type OffsetType, bool ReadNameLength = false,
        bool IsNameLengthShort = false)
    {
        var pos = Position;
        long offset = 0;

        if (OffsetType == typeof(long))
            offset = ReadInt64();
        if (OffsetType == typeof(ulong))
            offset = (long)ReadUInt64();
        if (OffsetType == typeof(uint))
            offset = ReadUInt32();
        if (OffsetType == typeof(int))
            offset = ReadInt32();

        if (IsRelative && offset != 0)
            offset += pos;

        if (offset != 0)
            using (TemporarySeek(offset, SeekOrigin.Begin))
            {
                uint NameLength = 0;
                if (ReadNameLength)
                {
                    if (IsNameLengthShort)
                        NameLength = ReadUInt16();
                    else
                        NameLength = ReadUInt32();
                }

                return ReadString(BinaryStringFormat.ZeroTerminated);
            }
        else
            return "";
    }

    public List<string> ReadNameOffsets(uint Count, bool IsRelative, Type OffsetType, bool ReadNameLength = false)
    {
        var Names = new List<string>();
        for (var i = 0; i < Count; i++)
            Names.Add(ReadNameOffset(IsRelative, OffsetType, ReadNameLength));

        return Names;
    }

    public string ReadString(int length, bool removeSpaces)
    {
        return ReadString(length).Replace("\0", string.Empty);
    }

    public string ReadZeroTerminatedString(Encoding encoding = null)
    {
        return ReadString(BinaryStringFormat.ZeroTerminated, encoding ?? Encoding);
    }

    public string[] ReadZeroTerminatedStrings(uint count, Encoding encoding = null)
    {
        var str = new string[count];
        for (var i = 0; i < count; i++)
            str[i] = ReadString(BinaryStringFormat.ZeroTerminated, encoding ?? Encoding);
        return str;
    }

    public string ReadUTF16String()
    {
        var chars = new List<byte>();

        while (true)
        {
            var val = ReadUInt16();

            if (val == 0)
                return Encoding.ASCII.GetString(chars.ToArray());
            else
                chars.Add((byte)val); // casting to byte will remove the period, which is a part of UTF-16
        }
    }

    /// <summary>
    /// Checks the byte order mark to determine the endianness of the reader.
    /// </summary>
    /// <param name="ByteOrderMark">The byte order value being read. 0xFFFE = Little, 0xFEFF = Big. </param>
    /// <returns></returns>
    public void CheckByteOrderMark(uint ByteOrderMark)
    {
        SetByteOrder(ByteOrderMark == 0xFEFF);
    }

    public void SetByteOrder(bool IsBigEndian)
    {
        if (IsBigEndian)
            ByteOrder = ByteOrder.BigEndian;
        else
            ByteOrder = ByteOrder.LittleEndian;
    }

    public string ReadSignature(int length)
    {
        var RealSignature = ReadString(length, Encoding.ASCII);
        if (ReverseMagic)
            return new string(RealSignature.Reverse().ToArray());
        else
            return RealSignature;
    }

    public string ReadSignature(int length, string ExpectedSignature, bool TrimEnd = false)
    {
        var RealSignature = ReadString(length, Encoding.ASCII);
        if (ReverseMagic)
            RealSignature = new string(RealSignature.Reverse().ToArray());

        if (TrimEnd) RealSignature = RealSignature.TrimEnd(' ');

        if (RealSignature != ExpectedSignature)
            throw new Exception($"Invalid signature {RealSignature}! Expected {ExpectedSignature}.");

        return RealSignature;
    }

    public float ReadByteAsFloat()
    {
        return ReadByte() / 255.0f;
    }

    public void SeekBegin(uint Offset)
    {
        Seek(Offset, SeekOrigin.Begin);
    }

    public void SeekBegin(int Offset)
    {
        Seek(Offset, SeekOrigin.Begin);
    }

    public void SeekBegin(long Offset)
    {
        Seek(Offset, SeekOrigin.Begin);
    }

    public void SeekBegin(ulong Offset)
    {
        Seek((long)Offset, SeekOrigin.Begin);
    }

    public long ReadOffset(bool IsRelative, Type OffsetType)
    {
        var pos = Position;
        long offset = 0;

        if (OffsetType == typeof(long))
            offset = ReadInt64();
        if (OffsetType == typeof(ulong))
            offset = (long)ReadUInt64();
        if (OffsetType == typeof(uint))
            offset = ReadUInt32();
        if (OffsetType == typeof(int))
            offset = ReadInt32();

        if (IsRelative && offset != 0)
            return pos + offset;
        else
            return offset;
    }

    public string LoadString(bool IsRelative, Type OffsetType, Encoding encoding = null, uint ReadStringLength = 0)
    {
        var pos = Position;

        long offset = 0;
        var size = 0;

        if (OffsetType == typeof(long))
            offset = ReadInt64();
        if (OffsetType == typeof(ulong))
            offset = (long)ReadUInt64();
        if (OffsetType == typeof(uint))
            offset = ReadUInt32();
        if (OffsetType == typeof(int))
            offset = ReadInt32();

        if (offset == 0) return string.Empty;

        if (IsRelative)
            offset = offset + pos;

        encoding = encoding ?? Encoding;
        using (TemporarySeek(offset, SeekOrigin.Begin))
        {
            //Read the size of the string if set
            uint stringLength = 0;

            if (ReadStringLength == 2)
                stringLength = ReadUInt16();
            if (ReadStringLength == 4)
                stringLength = ReadUInt32();

            return ReadString(BinaryStringFormat.ZeroTerminated, encoding);
        }
    }

    public byte[] getSection(uint offset, uint size)
    {
        Position = offset;
        return ReadBytes((int)size);
    }

    public byte[] getSection(int offset, int size)
    {
        Position = offset;
        return ReadBytes(size);
    }

    public static byte[] InflateZLIB(byte[] i)
    {
        var stream = new MemoryStream();
        var ms = new MemoryStream(i);
        ms.ReadByte();
        ms.ReadByte();
        var zlibStream = new DeflateStream(ms, CompressionMode.Decompress);
        var buffer = new byte[4095];
        while (true)
        {
            var size = zlibStream.Read(buffer, 0, buffer.Length);
            if (size > 0)
                stream.Write(buffer, 0, buffer.Length);
            else
                break;
        }

        zlibStream.Close();
        return stream.ToArray();
    }

    public string ReadMagic(int Offset, int Length)
    {
        Seek(Offset, SeekOrigin.Begin);
        return ReadString(Length);
    }
}

public static class DirectXTexNativeHelper
{
    [DllImport("DirectXTex.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern unsafe int Decompress2(DirectXStructs.Image* cImages, ulong nimages, TexMetadata metadata,
        uint format, ScratchImage images);

    [DllImport("DirectXTex.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern unsafe int Convert2(DirectXStructs.Image* cImages, ulong nimages, TexMetadata metadata, DXGI_FORMAT format, TEX_FILTER_FLAGS flags, float threshold, ScratchImage outImage);

    [DllImport("DirectXTex.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern unsafe DirectXStructs.Image* GetImages(this ScratchImage img);

    [DllImport("DirectXTex.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern ulong GetImageCount(ScratchImage img);

    [DllImport("DirectXTex.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern TexMetadata GetMetadata(ScratchImage img);

    [DllImport("DirectXTex.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern ScratchImage CreateScratchImage();

    [DllImport("DirectXTex.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Initialize(ScratchImage img, TexMetadata mdata, CPFlags flags);

    [DllImport("DirectXTex.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void ScratchImageRelease(ScratchImage img);

    [DllImport("DirectXTex.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool IsCompressed(DXGI_FORMAT format);
}

namespace DirectXStructs
{
    /// <summary>
    /// To be documented.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly partial struct ScratchImage : IEquatable<ScratchImage>
    {
        public ScratchImage(nint handle)
        {
            Handle = handle;
        }

        public nint Handle { get; }
        public bool IsNull => Handle == 0;
        public static ScratchImage Null => new(0);

        public static implicit operator ScratchImage(nint handle)
        {
            return new ScratchImage(handle);
        }

        public static bool operator ==(ScratchImage left, ScratchImage right)
        {
            return left.Handle == right.Handle;
        }

        public static bool operator !=(ScratchImage left, ScratchImage right)
        {
            return left.Handle != right.Handle;
        }

        public static bool operator ==(ScratchImage left, nint right)
        {
            return left.Handle == right;
        }

        public static bool operator !=(ScratchImage left, nint right)
        {
            return left.Handle != right;
        }

        public bool Equals(ScratchImage other)
        {
            return Handle == other.Handle;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is ScratchImage handle && Equals(handle);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }

        private string DebuggerDisplay => string.Format("ScratchImage [0x{0}]", Handle.ToString("X"));
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Image
    {
        public ulong Width;
        public ulong Height;
        public int Format;
        public ulong RowPitch;
        public ulong SlicePitch;
        public unsafe byte* Pixels;

        public unsafe Image(ulong width = 0uL, ulong height = 0uL, int format = 0, ulong rowPitch = 0uL,
            ulong slicePitch = 0uL, byte* pixels = null)
        {
            Width = width;
            Height = height;
            Format = format;
            RowPitch = rowPitch;
            SlicePitch = slicePitch;
            Pixels = pixels;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TexMetadata
    {
        public ulong Width;
        public ulong Height;
        public ulong Depth;
        public ulong ArraySize;
        public ulong MipLevels;
        public uint MiscFlags;
        public uint MiscFlags2;
        public int Format;
        public TexDimension Dimension;

        public TexMetadata(ulong width = 0uL, ulong height = 0uL, ulong depth = 0uL, ulong arraySize = 0uL,
            ulong mipLevels = 0uL, uint miscFlags = 0u, uint miscFlags2 = 0u, int format = 0,
            TexDimension dimension = (TexDimension)0)
        {
            Width = width;
            Height = height;
            Depth = depth;
            ArraySize = arraySize;
            MipLevels = mipLevels;
            MiscFlags = miscFlags;
            MiscFlags2 = miscFlags2;
            Format = format;
            Dimension = dimension;
        }

        public ulong ComputeIndex(ulong mip, ulong item, ulong slice)
        {
            return 1;
        }

        public ulong ComputeIndex(nuint mip, ulong item, ulong slice)
        {
            return 1;
        }

        public ulong ComputeIndex(ulong mip, nuint item, ulong slice)
        {
            return 1;
        }

        public ulong ComputeIndex(nuint mip, nuint item, ulong slice)
        {
            return 1;
        }

        public ulong ComputeIndex(ulong mip, ulong item, nuint slice)
        {
            return 1;
        }

        public ulong ComputeIndex(nuint mip, ulong item, nuint slice)
        {
            return 1;
        }

        public ulong ComputeIndex(ulong mip, nuint item, nuint slice)
        {
            return 1;
        }

        public ulong ComputeIndex(nuint mip, nuint item, nuint slice)
        {
            return 1;
        }

        public bool IsCubemap()
        {
            return false;
        }

        public bool IsPMAlpha()
        {
            return false;
        }

        public unsafe void SetAlphaMode(TexAlphaMode mode)
        {
        }

        public TexAlphaMode GetAlphaMode()
        {
            return TexAlphaMode.Unknown;
        }

        public bool IsVolumemap()
        {
            return false;
        }
    }

    public enum TexAlphaMode
    {
        Unknown,
        Straight,
        Premultiplied,
        Opaque,
        Custom
    }

    public enum TexDimension
    {
        Texture1D = 2,
        Texture2D,
        Texture3D
    }
}

public class Tmpk
    {
        public class File
        {
            public int Offset { get; }
            public byte[] Data { get; }

            public File(int offset, byte[] data)
            {
                Offset = offset;
                Data = data;
            }
        }

        private readonly Dictionary<string, File> _files = new Dictionary<string, File>();

        public Tmpk(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                var magic = reader.ReadBytes(4);
                if (!magic.SequenceEqual(Encoding.ASCII.GetBytes("TMPK")))
                {
                    throw new InvalidDataException($"Invalid magic: {Encoding.ASCII.GetString(magic)} (expected 'TMPK')");
                }

                var numFiles = (int)ReadUInt32(reader);
                stream.Seek(0x10, SeekOrigin.Begin);

                for (var i = 0; i < numFiles; i++)
                {
                    var nameOffset = (int)ReadUInt32(reader);
                    var fileOffset = (int)ReadUInt32(reader);
                    var fileSize = (int)ReadUInt32(reader);
                    var _ = ReadUInt32(reader);

                    var name = ReadString(data, nameOffset);
                    var fileData = new byte[fileSize];
                    Array.Copy(data, fileOffset, fileData, 0, fileSize);

                    _files[name] = new File(fileOffset, fileData);
                }
            }
        }

        public Dictionary<string, File> GetFiles()
        {
            return _files;
        }

        private static uint ReadUInt32(BinaryReader reader)
        {
            var data = reader.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        private static string ReadString(byte[] data, int offset)
        {
            var end = Array.IndexOf(data, (byte)0, offset);
            if (end < 0)
            {
                end = data.Length;
            }
            return Encoding.UTF8.GetString(data, offset, end - offset);
        }
    }
    
public class R4G4
{
    public static byte[] Decompress(byte[] Input, int Width, int Height, bool Alpha)
    {
        byte[] Output = new byte[Width * Height * 4];

        byte[] comp = new byte[4] { 0xFF, 0xFF, 0xFF, 0xFF };

        int bpp = (int)STGenericTexture.GetBytesPerPixel(TEX_FORMAT.R4G4_UNORM);

        for (int Y = 0; Y < Height; Y++)
        {
            for (int X = 0; X < Width; X++)
            {   
                int InputOffset = (Y * Width + X) * bpp;
                int OutputOffset = (Y * Width + X) * 4;

                int pixel = 0;
                for (int i = 0; i < bpp; i++)
                    pixel |= Input[InputOffset + i] << (8 * i);

                comp[0] = (byte)((pixel & 0xF) * 17);
                comp[1] = (byte)(((pixel & 0xF0) >> 4) * 17);

                Output[OutputOffset + 0] = comp[0];
                Output[OutputOffset + 1] = comp[1];
                Output[OutputOffset + 2] = comp[2];
                Output[OutputOffset + 3] = comp[3];
            }
        }

        return Output;
    }
}


    //From https://github.com/gdkchan/SPICA/blob/42c4181e198b0fd34f0a567345ee7e75b54cb58b/SPICA/PICA/Converters/TextureCompression.cs
    public class ETC1
    {
        private static byte[] XT = { 0, 4, 0, 4 };
        private static byte[] YT = { 0, 0, 4, 4 };

        private static ulong Swap64(ulong Value)
        {
            Value = ((Value & 0xffffffff00000000ul) >> 32) | ((Value & 0x00000000fffffffful) << 32);
            Value = ((Value & 0xffff0000ffff0000ul) >> 16) | ((Value & 0x0000ffff0000fffful) << 16);
            Value = ((Value & 0xff00ff00ff00ff00ul) >> 8) | ((Value & 0x00ff00ff00ff00fful) << 8);

            return Value;
        }

        public static byte[] ETC1Decompress(byte[] Input, int Width, int Height, bool Alpha)
        {
            byte[] Output = new byte[Width * Height * 4];

            using (MemoryStream MS = new MemoryStream(Input))
            {
                BinaryReader Reader = new BinaryReader(MS);

                for (int TY = 0; TY < Height; TY += 8)
                {
                    for (int TX = 0; TX < Width; TX += 8)
                    {
                        for (int T = 0; T < 4; T++)
                        {
                            ulong AlphaBlock = 0xfffffffffffffffful;

                            if (Alpha) AlphaBlock = Reader.ReadUInt64();

                            ulong ColorBlock = Swap64(Reader.ReadUInt64());

                            byte[] Tile = ETC1Tile(ColorBlock);

                            int TileOffset = 0;

                            for (int PY = YT[T]; PY < 4 + YT[T]; PY++)
                            {
                                for (int PX = XT[T]; PX < 4 + XT[T]; PX++)
                                {
                                    int OOffs = ((Height - 1 - (TY + PY)) * Width + TX + PX) * 4;

                                    Buffer.BlockCopy(Tile, TileOffset, Output, OOffs, 3);

                                    int AlphaShift = ((PX & 3) * 4 + (PY & 3)) << 2;

                                    byte A = (byte)((AlphaBlock >> AlphaShift) & 0xf);

                                    Output[OOffs + 3] = (byte)((A << 4) | A);

                                    TileOffset += 4;
                                }
                            }
                        }
                    }
                }

                return Output;
            }
        }

        public static void EncodeETC1Block(FileWriter writer, int blockX, int blockY, byte[] Input, long IOffset, long OOffset, bool Alpha)
        {

        }

        private static byte[] ETC1Tile(ulong Block)
        {
            uint BlockLow = (uint)(Block >> 32);
            uint BlockHigh = (uint)(Block >> 0);

            bool Flip = (BlockHigh & 0x1000000) != 0;
            bool Diff = (BlockHigh & 0x2000000) != 0;

            uint R1, G1, B1;
            uint R2, G2, B2;

            if (Diff)
            {
                B1 = (BlockHigh & 0x0000f8) >> 0;
                G1 = (BlockHigh & 0x00f800) >> 8;
                R1 = (BlockHigh & 0xf80000) >> 16;

                B2 = (uint)((sbyte)(B1 >> 3) + ((sbyte)((BlockHigh & 0x000007) << 5) >> 5));
                G2 = (uint)((sbyte)(G1 >> 3) + ((sbyte)((BlockHigh & 0x000700) >> 3) >> 5));
                R2 = (uint)((sbyte)(R1 >> 3) + ((sbyte)((BlockHigh & 0x070000) >> 11) >> 5));

                B1 |= B1 >> 5;
                G1 |= G1 >> 5;
                R1 |= R1 >> 5;

                B2 = (B2 << 3) | (B2 >> 2);
                G2 = (G2 << 3) | (G2 >> 2);
                R2 = (R2 << 3) | (R2 >> 2);
            }
            else
            {
                B1 = (BlockHigh & 0x0000f0) >> 0;
                G1 = (BlockHigh & 0x00f000) >> 8;
                R1 = (BlockHigh & 0xf00000) >> 16;

                B2 = (BlockHigh & 0x00000f) << 4;
                G2 = (BlockHigh & 0x000f00) >> 4;
                R2 = (BlockHigh & 0x0f0000) >> 12;

                B1 |= B1 >> 4;
                G1 |= G1 >> 4;
                R1 |= R1 >> 4;

                B2 |= B2 >> 4;
                G2 |= G2 >> 4;
                R2 |= R2 >> 4;
            }

            uint Table1 = (BlockHigh >> 29) & 7;
            uint Table2 = (BlockHigh >> 26) & 7;

            byte[] Output = new byte[4 * 4 * 4];

            if (!Flip)
            {
                for (int Y = 0; Y < 4; Y++)
                {
                    for (int X = 0; X < 2; X++)
                    {
                        Color Color1 = ETC1Pixel(R1, G1, B1, X + 0, Y, BlockLow, Table1);
                        Color Color2 = ETC1Pixel(R2, G2, B2, X + 2, Y, BlockLow, Table2);

                        int Offset1 = (Y * 4 + X) * 4;

                        Output[Offset1 + 0] = Color1.B;
                        Output[Offset1 + 1] = Color1.G;
                        Output[Offset1 + 2] = Color1.R;

                        int Offset2 = (Y * 4 + X + 2) * 4;

                        Output[Offset2 + 0] = Color2.B;
                        Output[Offset2 + 1] = Color2.G;
                        Output[Offset2 + 2] = Color2.R;
                    }
                }
            }
            else
            {
                for (int Y = 0; Y < 2; Y++)
                {
                    for (int X = 0; X < 4; X++)
                    {
                        Color Color1 = ETC1Pixel(R1, G1, B1, X, Y + 0, BlockLow, Table1);
                        Color Color2 = ETC1Pixel(R2, G2, B2, X, Y + 2, BlockLow, Table2);

                        int Offset1 = (Y * 4 + X) * 4;

                        Output[Offset1 + 0] = Color1.B;
                        Output[Offset1 + 1] = Color1.G;
                        Output[Offset1 + 2] = Color1.R;

                        int Offset2 = ((Y + 2) * 4 + X) * 4;

                        Output[Offset2 + 0] = Color2.B;
                        Output[Offset2 + 1] = Color2.G;
                        Output[Offset2 + 2] = Color2.R;
                    }
                }
            }

            return Output;
        }

        private static int[,] ETC1LUT =
        {
            {    2,   8,    -2,   -8 },
            {    5,   17,   -5,  -17 },
            {    9,   29,   -9,  -29 },
            {   13,   42,  -13,  -42 },
            {   18,   60,  -18,  -60 },
            {   24,   80,  -24,  -80 },
            {   33,  106,  -33, -106 },
            {   47,  183,  -47, -183 }
        };

        private static Color ETC1Pixel(uint R, uint G, uint B, int X, int Y, uint Block, uint Table)
        {
            int Index = X * 4 + Y;
            uint MSB = Block << 1;

            int Pixel = Index < 8
                ? ETC1LUT[Table, ((Block >> (Index + 24)) & 1) + ((MSB >> (Index + 8)) & 2)]
                : ETC1LUT[Table, ((Block >> (Index + 8)) & 1) + ((MSB >> (Index - 8)) & 2)];

            R = Saturate((int)(R + Pixel));
            G = Saturate((int)(G + Pixel));
            B = Saturate((int)(B + Pixel));

            return Color.FromArgb((int)R, (int)G, (int)B);
        }

        private static byte Saturate(int Value)
        {
            if (Value > byte.MaxValue) return byte.MaxValue;
            if (Value < byte.MinValue) return byte.MinValue;

            return (byte)Value;
        }
    }
    
    public class ASTCDecoderException : Exception
    {
        public ASTCDecoderException(string ExMsg) : base(ExMsg) {/* Toolbox.Library.Forms.STErrorDialog.Show(ExMsg, "", ExMsg);*/ }
    }

public struct IntegerEncoded
{
    public enum EIntegerEncoding
    {
        JustBits,
        Quint,
        Trit
    }

    EIntegerEncoding Encoding;
    public int NumberBits { get; private set; }
    public int BitValue   { get; private set; }
    public int TritValue  { get; private set; }
    public int QuintValue { get; private set; }

    public IntegerEncoded(EIntegerEncoding _Encoding, int NumBits)
    {
        Encoding   = _Encoding;
        NumberBits = NumBits;
        BitValue   = 0;
        TritValue  = 0;
        QuintValue = 0;
    }

    public bool MatchesEncoding(IntegerEncoded Other)
    {
        return Encoding == Other.Encoding && NumberBits == Other.NumberBits;
    }

    public EIntegerEncoding GetEncoding()
    {
        return Encoding;
    }

    public int GetBitLength(int NumberVals)
    {
        int TotalBits = NumberBits * NumberVals;
        if (Encoding == EIntegerEncoding.Trit)
        {
            TotalBits += (NumberVals * 8 + 4) / 5;
        }
        else if (Encoding == EIntegerEncoding.Quint)
        {
            TotalBits += (NumberVals * 7 + 2) / 3;
        }
        return TotalBits;
    }

    public static IntegerEncoded CreateEncoding(int MaxVal)
    {
        while (MaxVal > 0)
        {
            int Check = MaxVal + 1;

            // Is maxVal a power of two?
            if ((Check & (Check - 1)) == 0)
            {
                return new IntegerEncoded(EIntegerEncoding.JustBits, ASTCDecoder.BitArrayStream.PopCnt(MaxVal));
            }

            // Is maxVal of the type 3*2^n - 1?
            if ((Check % 3 == 0) && ((Check / 3) & ((Check / 3) - 1)) == 0)
            {
                return new IntegerEncoded(EIntegerEncoding.Trit, ASTCDecoder.BitArrayStream.PopCnt(Check / 3 - 1));
            }

            // Is maxVal of the type 5*2^n - 1?
            if ((Check % 5 == 0) && ((Check / 5) & ((Check / 5) - 1)) == 0)
            {
                return new IntegerEncoded(EIntegerEncoding.Quint, ASTCDecoder.BitArrayStream.PopCnt(Check / 5 - 1));
            }

            // Apparently it can't be represented with a bounded integer sequence...
            // just iterate.
            MaxVal--;
        }

        return new IntegerEncoded(EIntegerEncoding.JustBits, 0);
    }

    public static void DecodeTritBlock(
        ASTCDecoder.BitArrayStream       BitStream, 
        List<IntegerEncoded> ListIntegerEncoded, 
        int                  NumberBitsPerValue)
    {
        // Implement the algorithm in section C.2.12
        int[] m = new int[5];
        int[] t = new int[5];
        int T;

        // Read the trit encoded block according to
        // table C.2.14
        m[0] = BitStream.ReadBits(NumberBitsPerValue);
        T    = BitStream.ReadBits(2);
        m[1] = BitStream.ReadBits(NumberBitsPerValue);
        T   |= BitStream.ReadBits(2) << 2;
        m[2] = BitStream.ReadBits(NumberBitsPerValue);
        T   |= BitStream.ReadBits(1) << 4;
        m[3] = BitStream.ReadBits(NumberBitsPerValue);
        T   |= BitStream.ReadBits(2) << 5;
        m[4] = BitStream.ReadBits(NumberBitsPerValue);
        T   |= BitStream.ReadBits(1) << 7;

        int C = 0;

        ASTCDecoder.BitArrayStream Tb = new ASTCDecoder.BitArrayStream(new BitArray(new int[] { T }));
        if (Tb.ReadBits(2, 4) == 7)
        {
            C    = (Tb.ReadBits(5, 7) << 2) | Tb.ReadBits(0, 1);
            t[4] = t[3] = 2;
        }
        else
        {
            C = Tb.ReadBits(0, 4);
            if (Tb.ReadBits(5, 6) == 3)
            {
                t[4] = 2;
                t[3] = Tb.ReadBit(7);
            }
            else
            {
                t[4] = Tb.ReadBit(7);
                t[3] = Tb.ReadBits(5, 6);
            }
        }

        ASTCDecoder.BitArrayStream Cb = new ASTCDecoder.BitArrayStream(new BitArray(new int[] { C }));
        if (Cb.ReadBits(0, 1) == 3)
        {
            t[2] = 2;
            t[1] = Cb.ReadBit(4);
            t[0] = (Cb.ReadBit(3) << 1) | (Cb.ReadBit(2) & ~Cb.ReadBit(3));
        }
        else if (Cb.ReadBits(2, 3) == 3)
        {
            t[2] = 2;
            t[1] = 2;
            t[0] = Cb.ReadBits(0, 1);
        }
        else
        {
            t[2] = Cb.ReadBit(4);
            t[1] = Cb.ReadBits(2, 3);
            t[0] = (Cb.ReadBit(1) << 1) | (Cb.ReadBit(0) & ~Cb.ReadBit(1));
        }

        for (int i = 0; i < 5; i++)
        {
            IntegerEncoded IntEncoded = new IntegerEncoded(EIntegerEncoding.Trit, NumberBitsPerValue)
            {
                BitValue  = m[i],
                TritValue = t[i]
            };
            ListIntegerEncoded.Add(IntEncoded);
        }
    }

    public static void DecodeQuintBlock(
        ASTCDecoder.BitArrayStream       BitStream, 
        List<IntegerEncoded> ListIntegerEncoded, 
        int                  NumberBitsPerValue)
    {
        // Implement the algorithm in section C.2.12
        int[] m = new int[3];
        int[] q = new int[3];
        int Q;

        // Read the trit encoded block according to
        // table C.2.15
        m[0] = BitStream.ReadBits(NumberBitsPerValue);
        Q    = BitStream.ReadBits(3);
        m[1] = BitStream.ReadBits(NumberBitsPerValue);
        Q   |= BitStream.ReadBits(2) << 3;
        m[2] = BitStream.ReadBits(NumberBitsPerValue);
        Q   |= BitStream.ReadBits(2) << 5;

        ASTCDecoder.BitArrayStream Qb = new ASTCDecoder.BitArrayStream(new BitArray(new int[] { Q }));
        if (Qb.ReadBits(1, 2) == 3 && Qb.ReadBits(5, 6) == 0)
        {
            q[0] = q[1] = 4;
            q[2] = (Qb.ReadBit(0) << 2) | ((Qb.ReadBit(4) & ~Qb.ReadBit(0)) << 1) | (Qb.ReadBit(3) & ~Qb.ReadBit(0));
        }
        else
        {
            int C = 0;
            if (Qb.ReadBits(1, 2) == 3)
            {
                q[2] = 4;
                C    = (Qb.ReadBits(3, 4) << 3) | ((~Qb.ReadBits(5, 6) & 3) << 1) | Qb.ReadBit(0);
            }
            else
            {
                q[2] = Qb.ReadBits(5, 6);
                C    = Qb.ReadBits(0, 4);
            }

            ASTCDecoder.BitArrayStream Cb = new ASTCDecoder.BitArrayStream(new BitArray(new int[] { C }));
            if (Cb.ReadBits(0, 2) == 5)
            {
                q[1] = 4;
                q[0] = Cb.ReadBits(3, 4);
            }
            else
            {
                q[1] = Cb.ReadBits(3, 4);
                q[0] = Cb.ReadBits(0, 2);
            }
        }

        for (int i = 0; i < 3; i++)
        {
            IntegerEncoded IntEncoded = new IntegerEncoded(EIntegerEncoding.Quint, NumberBitsPerValue)
            {
                BitValue   = m[i],
                QuintValue = q[i]
            };
            ListIntegerEncoded.Add(IntEncoded);
        }
    }

    public static void DecodeIntegerSequence(
        List<IntegerEncoded> DecodeIntegerSequence, 
        ASTCDecoder.BitArrayStream       BitStream, 
        int                  MaxRange, 
        int                  NumberValues)
    {
        // Determine encoding parameters
        IntegerEncoded IntEncoded = CreateEncoding(MaxRange);

        // Start decoding
        int NumberValuesDecoded = 0;
        while (NumberValuesDecoded < NumberValues)
        {
            switch (IntEncoded.GetEncoding())
            {
                case EIntegerEncoding.Quint:
                {
                    DecodeQuintBlock(BitStream, DecodeIntegerSequence, IntEncoded.NumberBits);
                    NumberValuesDecoded += 3;

                    break;
                }

                case EIntegerEncoding.Trit:
                {
                    DecodeTritBlock(BitStream, DecodeIntegerSequence, IntEncoded.NumberBits);
                    NumberValuesDecoded += 5;

                    break;
                }

                case EIntegerEncoding.JustBits:
                {
                    IntEncoded.BitValue = BitStream.ReadBits(IntEncoded.NumberBits);
                    DecodeIntegerSequence.Add(IntEncoded);
                    NumberValuesDecoded++;

                    break;
                }
            }
        }
    }
}

    //https://github.com/GammaUNC/FasTC/blob/master/ASTCEncoder/src/Decompressor.cpp
    public static class ASTCDecoder
    {
        struct TexelWeightParams
        {
            public int  Width;
            public int  Height;
            public bool DualPlane;
            public int  MaxWeight;
            public bool Error;
            public bool VoidExtentLDR;
            public bool VoidExtentHDR;

            public int GetPackedBitSize()
            {
                // How many indices do we have?
                int Indices = Height * Width;

                if (DualPlane)
                {
                    Indices *= 2;
                }

                IntegerEncoded IntEncoded = IntegerEncoded.CreateEncoding(MaxWeight);

                return IntEncoded.GetBitLength(Indices);
            }

            public int GetNumWeightValues()
            {
                int Ret = Width * Height;

                if (DualPlane)
                {
                    Ret *= 2;
                }

                return Ret;
            }
        }

        public static byte[] DecodeToRGBA8888(
            byte[] InputBuffer, 
            int    BlockX, 
            int    BlockY, 
            int    BlockZ, 
            int    X, 
            int    Y, 
            int    Z)
        {
            using (MemoryStream InputStream = new MemoryStream(InputBuffer))
            {
                BinaryReader BinReader = new BinaryReader(InputStream);

                if (BlockX > 12 || BlockY > 12)
                {
                    throw new Exception("Block size unsupported!");
                }

                if (BlockZ != 1 || Z != 1)
                {
                    throw new Exception("3D compressed textures unsupported!");
                }

                using (MemoryStream OutputStream = new MemoryStream())
                {
                    int BlockIndex = 0;

                    for (int j = 0; j < Y; j += BlockY)
                    {
                        for (int i = 0; i < X; i += BlockX)
                        {
                            int[] DecompressedData = new int[144];

                            DecompressBlock(BinReader.ReadBytes(0x10), DecompressedData, BlockX, BlockY);

                            int DecompressedWidth = Math.Min(BlockX, X - i);
                            int DecompressedHeight = Math.Min(BlockY, Y - j);
                            int BaseOffsets = (j * X + i) * 4;

                            for (int jj = 0; jj < DecompressedHeight; jj++)
                            {
                                OutputStream.Seek(BaseOffsets + jj * X * 4, SeekOrigin.Begin);

                                byte[] OutputBuffer = new byte[DecompressedData.Length * sizeof(int)];
                                Buffer.BlockCopy(DecompressedData, 0, OutputBuffer, 0, OutputBuffer.Length);

                                OutputStream.Write(OutputBuffer, jj * BlockX * 4, DecompressedWidth * 4);
                            }

                            BlockIndex++;
                        }
                    }

                    return OutputStream.ToArray();
                }
            }
        }
        
        public class BitArrayStream
{
    public BitArray BitsArray;
    public int Position { get; private set; }

    public BitArrayStream(BitArray BitArray)
    {
        BitsArray = BitArray;
        Position  = 0;
    }

    public short ReadBits(int Length)
    {
        int RetValue = 0;
        for (int i = Position; i < Position + Length; i++)
        {
            if (BitsArray[i])
            {
                RetValue |= 1 << (i - Position);
            }
        }

        Position += Length;
        return (short)RetValue;
    }

    public int ReadBits(int Start, int End)
    {
        int RetValue = 0;
        for (int i = Start; i <= End; i++)
        {
            if (BitsArray[i])
            {
                RetValue |= 1 << (i - Start);
            }
        }

        return RetValue;
    }

    public int ReadBit(int Index)
    {
        return Convert.ToInt32(BitsArray[Index]);
    }

    public void WriteBits(int Value, int Length)
    {
        for (int i = Position; i < Position + Length; i++)
        {
            BitsArray[i] = ((Value >> (i - Position)) & 1) != 0;
        }

        Position += Length;
    }

    public byte[] ToByteArray()
    {
        byte[] RetArray = new byte[(BitsArray.Length + 7) / 8];
        BitsArray.CopyTo(RetArray, 0);
        return RetArray;
    }

    public static int Replicate(int Value, int NumberBits, int ToBit)
    {
        if (NumberBits == 0) return 0;
        if (ToBit == 0) return 0;

        int TempValue = Value & ((1 << NumberBits) - 1);
        int RetValue  = TempValue;
        int ResLength = NumberBits;

        while (ResLength < ToBit)
        {
            int Comp = 0;
            if (NumberBits > ToBit - ResLength)
            {
                int NewShift = ToBit - ResLength;
                Comp         = NumberBits - NewShift;
                NumberBits   = NewShift;
            }
            RetValue <<= NumberBits;
            RetValue  |= TempValue >> Comp;
            ResLength += NumberBits;
        }
        return RetValue;
    }

    public static int PopCnt(int Number)
    {
        int Counter;
        for (Counter = 0; Number != 0; Counter++)
        {
            Number &= Number - 1;
        }
        return Counter;
    }

    public static void Swap<T>(ref T lhs, ref T rhs)
    {
        T Temp = lhs;
        lhs = rhs;
        rhs = Temp;
    }

    // Transfers a bit as described in C.2.14
    public static void BitTransferSigned(ref int a, ref int b)
    {
        b >>= 1;
        b |= a & 0x80;
        a >>= 1;
        a &= 0x3F;
        if ((a & 0x20) != 0) a -= 0x40;
    }
}

        public static bool DecompressBlock(
            byte[] InputBuffer, 
            int[]  OutputBuffer, 
            int    BlockWidth, 
            int    BlockHeight)
        {
            BitArrayStream    BitStream   = new BitArrayStream(new BitArray(InputBuffer));
            TexelWeightParams TexelParams = DecodeBlockInfo(BitStream);

            if (TexelParams.Error)
            {
                throw new Exception("Invalid block mode");
            }

          //  Console.WriteLine($"BlockWidth {BlockWidth} {BlockHeight} BlockHeight");
          //  Console.WriteLine($"TexelParams {TexelParams.Width} X {TexelParams.Height}");

            if (TexelParams.VoidExtentLDR)
            {
                FillVoidExtentLDR(BitStream, OutputBuffer, BlockWidth, BlockHeight);

                return true;
            }

            if (TexelParams.VoidExtentHDR)
            {
                throw new Exception("HDR void extent blocks are unsupported!");
            }

            if (TexelParams.Width > BlockWidth)
            {
                throw new Exception("Texel weight grid width should be smaller than block width");
            }

            if (TexelParams.Height > BlockHeight)
            {
                throw new Exception("Texel weight grid height should be smaller than block height");
            }

            // Read num partitions
            int NumberPartitions = BitStream.ReadBits(2) + 1;
            Debug.Assert(NumberPartitions <= 4);

            if (NumberPartitions == 4 && TexelParams.DualPlane)
            {
                throw new Exception("Dual plane mode is incompatible with four partition blocks");
            }

            // Based on the number of partitions, read the color endpoint mode for
            // each partition.

            // Determine partitions, partition index, and color endpoint modes
            int    PlaneIndices      = -1;
            int    PartitionIndex;
            uint[] ColorEndpointMode = { 0, 0, 0, 0 };

            BitArrayStream ColorEndpointStream = new BitArrayStream(new BitArray(16 * 8));

            // Read extra config data...
            uint BaseColorEndpointMode = 0;

            if (NumberPartitions == 1)
            {
                ColorEndpointMode[0] = (uint)BitStream.ReadBits(4);
                PartitionIndex       = 0;
            }
            else
            {
                PartitionIndex        = BitStream.ReadBits(10);
                BaseColorEndpointMode = (uint)BitStream.ReadBits(6);
            }

            uint BaseMode = (BaseColorEndpointMode & 3);

            // Remaining bits are color endpoint data...
            int NumberWeightBits = TexelParams.GetPackedBitSize();
            int RemainingBits    = 128 - NumberWeightBits - BitStream.Position;

            // Consider extra bits prior to texel data...
            uint ExtraColorEndpointModeBits = 0;

            if (BaseMode != 0)
            {
                switch (NumberPartitions)
                {
                    case 2:  ExtraColorEndpointModeBits += 2; break;
                    case 3:  ExtraColorEndpointModeBits += 5; break;
                    case 4:  ExtraColorEndpointModeBits += 8; break;
                    default: Debug.Assert(false); break;
                }
            }

            RemainingBits -= (int)ExtraColorEndpointModeBits;

            // Do we have a dual plane situation?
            int PlaneSelectorBits = 0;

            if (TexelParams.DualPlane)
            {
                PlaneSelectorBits = 2;
            }

            RemainingBits -= PlaneSelectorBits;

            // Read color data...
            int ColorDataBits = RemainingBits;

            while (RemainingBits > 0)
            {
                int NumberBits = Math.Min(RemainingBits, 8);
                int Bits = BitStream.ReadBits(NumberBits);
                ColorEndpointStream.WriteBits(Bits, NumberBits);
                RemainingBits -= 8;
            }

            // Read the plane selection bits
            PlaneIndices = BitStream.ReadBits(PlaneSelectorBits);

            // Read the rest of the CEM
            if (BaseMode != 0)
            {
                uint ExtraColorEndpointMode = (uint)BitStream.ReadBits((int)ExtraColorEndpointModeBits);
                uint TempColorEndpointMode  = (ExtraColorEndpointMode << 6) | BaseColorEndpointMode;
                TempColorEndpointMode     >>= 2;

                bool[] C = new bool[4];

                for (int i = 0; i < NumberPartitions; i++)
                {
                    C[i] = (TempColorEndpointMode & 1) != 0;
                    TempColorEndpointMode >>= 1;
                }

                byte[] M = new byte[4];

                for (int i = 0; i < NumberPartitions; i++)
                {
                    M[i] = (byte)(TempColorEndpointMode & 3);
                    TempColorEndpointMode >>= 2;
                    Debug.Assert(M[i] <= 3);
                }

                for (int i = 0; i < NumberPartitions; i++)
                {
                    ColorEndpointMode[i] = BaseMode;
                    if (!(C[i])) ColorEndpointMode[i] -= 1;
                    ColorEndpointMode[i] <<= 2;
                    ColorEndpointMode[i] |= M[i];
                }
            }
            else if (NumberPartitions > 1)
            {
                uint TempColorEndpointMode = BaseColorEndpointMode >> 2;

                for (uint i = 0; i < NumberPartitions; i++)
                {
                    ColorEndpointMode[i] = TempColorEndpointMode;
                }
            }

            // Make sure everything up till here is sane.
            for (int i = 0; i < NumberPartitions; i++)
            {
                Debug.Assert(ColorEndpointMode[i] < 16);
            }
            Debug.Assert(BitStream.Position + TexelParams.GetPackedBitSize() == 128);

            // Decode both color data and texel weight data
            int[] ColorValues = new int[32]; // Four values * two endpoints * four maximum partitions
            DecodeColorValues(ColorValues, ColorEndpointStream.ToByteArray(), ColorEndpointMode, NumberPartitions, ColorDataBits);

            ASTCPixel[][] EndPoints = new ASTCPixel[4][];
            EndPoints[0] = new ASTCPixel[2];
            EndPoints[1] = new ASTCPixel[2];
            EndPoints[2] = new ASTCPixel[2];
            EndPoints[3] = new ASTCPixel[2];

            int ColorValuesPosition = 0;

            for (int i = 0; i < NumberPartitions; i++)
            {
                ComputeEndpoints(EndPoints[i], ColorValues, ColorEndpointMode[i], ref ColorValuesPosition);
            }

            // Read the texel weight data.
            byte[] TexelWeightData = (byte[])InputBuffer.Clone();

            // Reverse everything
            for (int i = 0; i < 8; i++)
            {
                byte a = ReverseByte(TexelWeightData[i]);
                byte b = ReverseByte(TexelWeightData[15 - i]);

                TexelWeightData[i]      = b;
                TexelWeightData[15 - i] = a;
            }

            // Make sure that higher non-texel bits are set to zero
            int ClearByteStart                   = (TexelParams.GetPackedBitSize() >> 3) + 1;
            TexelWeightData[ClearByteStart - 1] &= (byte)((1 << (TexelParams.GetPackedBitSize() % 8)) - 1);

            int cLen = 16 - ClearByteStart;
            for (int i = ClearByteStart; i < ClearByteStart + cLen; i++) TexelWeightData[i] = 0;

            List<IntegerEncoded> TexelWeightValues = new List<IntegerEncoded>();
            BitArrayStream WeightBitStream         = new BitArrayStream(new BitArray(TexelWeightData));

            IntegerEncoded.DecodeIntegerSequence(TexelWeightValues, WeightBitStream, TexelParams.MaxWeight, TexelParams.GetNumWeightValues());
            
            // Blocks can be at most 12x12, so we can have as many as 144 weights
            int[][] Weights = new int[2][];
            Weights[0] = new int[144];
            Weights[1] = new int[144];

            UnquantizeTexelWeights(Weights, TexelWeightValues, TexelParams, BlockWidth, BlockHeight);

            // Now that we have endpoints and weights, we can interpolate and generate
            // the proper decoding...
            for (int j = 0; j < BlockHeight; j++)
            {
                for (int i = 0; i < BlockWidth; i++)
                {
                    int Partition = Select2DPartition(PartitionIndex, i, j, NumberPartitions, ((BlockHeight * BlockWidth) < 32));
                    Debug.Assert(Partition < NumberPartitions);

                    ASTCPixel Pixel = new ASTCPixel(0, 0, 0, 0);
                    for (int Component = 0; Component < 4; Component++)
                    {
                        int Component0 = EndPoints[Partition][0].GetComponent(Component);
                        Component0     = BitArrayStream.Replicate(Component0, 8, 16);
                        int Component1 = EndPoints[Partition][1].GetComponent(Component);
                        Component1     = BitArrayStream.Replicate(Component1, 8, 16);

                        int Plane = 0;

                        if (TexelParams.DualPlane && (((PlaneIndices + 1) & 3) == Component))
                        {
                            Plane = 1;
                        }

                        int Weight = Weights[Plane][j * BlockWidth + i];
                        int FinalComponent = (Component0 * (64 - Weight) + Component1 * Weight + 32) / 64;

                        if (FinalComponent == 65535)
                        {
                            Pixel.SetComponent(Component, 255);
                        }
                        else
                        {
                            double FinalComponentFloat = FinalComponent;
                            Pixel.SetComponent(Component, (int)(255.0 * (FinalComponentFloat / 65536.0) + 0.5));
                        }
                    }

                    OutputBuffer[j * BlockWidth + i] = Pixel.Pack();
                }
            }

            return true;
        }

        private static int Select2DPartition(int Seed, int X, int Y, int PartitionCount, bool IsSmallBlock)
        {
            return SelectPartition(Seed, X, Y, 0, PartitionCount, IsSmallBlock);
        }

        private static int SelectPartition(int Seed, int X, int Y, int Z, int PartitionCount, bool IsSmallBlock)
        {
            if (PartitionCount == 1)
            {
                return 0;
            }

            if (IsSmallBlock)
            {
                X <<= 1;
                Y <<= 1;
                Z <<= 1;
            }

            Seed += (PartitionCount - 1) * 1024;

            int  RightNum = Hash52((uint)Seed);
            byte Seed01   = (byte)(RightNum & 0xF);
            byte Seed02   = (byte)((RightNum >> 4) & 0xF);
            byte Seed03   = (byte)((RightNum >> 8) & 0xF);
            byte Seed04   = (byte)((RightNum >> 12) & 0xF);
            byte Seed05   = (byte)((RightNum >> 16) & 0xF);
            byte Seed06   = (byte)((RightNum >> 20) & 0xF);
            byte Seed07   = (byte)((RightNum >> 24) & 0xF);
            byte Seed08   = (byte)((RightNum >> 28) & 0xF);
            byte Seed09   = (byte)((RightNum >> 18) & 0xF);
            byte Seed10   = (byte)((RightNum >> 22) & 0xF);
            byte Seed11   = (byte)((RightNum >> 26) & 0xF);
            byte Seed12   = (byte)(((RightNum >> 30) | (RightNum << 2)) & 0xF);

            Seed01 *= Seed01; Seed02 *= Seed02;
            Seed03 *= Seed03; Seed04 *= Seed04;
            Seed05 *= Seed05; Seed06 *= Seed06;
            Seed07 *= Seed07; Seed08 *= Seed08;
            Seed09 *= Seed09; Seed10 *= Seed10;
            Seed11 *= Seed11; Seed12 *= Seed12;

            int SeedHash1, SeedHash2, SeedHash3;

            if ((Seed & 1) != 0)
            {
                SeedHash1 = (Seed & 2) != 0 ? 4 : 5;
                SeedHash2 = (PartitionCount == 3) ? 6 : 5;
            }
            else
            {
                SeedHash1 = (PartitionCount == 3) ? 6 : 5;
                SeedHash2 = (Seed & 2) != 0 ? 4 : 5;
            }

            SeedHash3 = (Seed & 0x10) != 0 ? SeedHash1 : SeedHash2;

            Seed01 >>= SeedHash1; Seed02 >>= SeedHash2; Seed03 >>= SeedHash1; Seed04 >>= SeedHash2;
            Seed05 >>= SeedHash1; Seed06 >>= SeedHash2; Seed07 >>= SeedHash1; Seed08 >>= SeedHash2;
            Seed09 >>= SeedHash3; Seed10 >>= SeedHash3; Seed11 >>= SeedHash3; Seed12 >>= SeedHash3;

            int a = Seed01 * X + Seed02 * Y + Seed11 * Z + (RightNum >> 14);
            int b = Seed03 * X + Seed04 * Y + Seed12 * Z + (RightNum >> 10);
            int c = Seed05 * X + Seed06 * Y + Seed09 * Z + (RightNum >> 6);
            int d = Seed07 * X + Seed08 * Y + Seed10 * Z + (RightNum >> 2);

            a &= 0x3F; b &= 0x3F; c &= 0x3F; d &= 0x3F;

            if (PartitionCount < 4) d = 0;
            if (PartitionCount < 3) c = 0;

            if (a >= b && a >= c && a >= d) return 0;
            else if (b >= c && b >= d) return 1;
            else if (c >= d) return 2;
            return 3;
        }

        static int Hash52(uint Val)
        {
            Val ^= Val >> 15; Val -= Val << 17; Val += Val << 7; Val += Val << 4;
            Val ^= Val >> 5;  Val += Val << 16; Val ^= Val >> 7; Val ^= Val >> 3;
            Val ^= Val << 6;  Val ^= Val >> 17;

            return (int)Val;
        }

        static void UnquantizeTexelWeights(
            int[][]              OutputBuffer, 
            List<IntegerEncoded> Weights, 
            TexelWeightParams    TexelParams, 
            int                  BlockWidth, 
            int                  BlockHeight)
        {
            int WeightIndices   = 0;
            int[][] Unquantized = new int[2][];
            Unquantized[0]      = new int[144];
            Unquantized[1]      = new int[144];

            for (int i = 0; i < Weights.Count; i++)
            {
                Unquantized[0][WeightIndices] = UnquantizeTexelWeight(Weights[i]);

                if (TexelParams.DualPlane)
                {
                    i++;
                    Unquantized[1][WeightIndices] = UnquantizeTexelWeight(Weights[i]);

                    if (i == Weights.Count)
                    {
                        break;
                    }
                }

                if (++WeightIndices >= (TexelParams.Width * TexelParams.Height)) break;
            }

            // Do infill if necessary (Section C.2.18) ...
            int Ds = (1024 + (BlockWidth / 2)) / (BlockWidth - 1);
            int Dt = (1024 + (BlockHeight / 2)) / (BlockHeight - 1);

            int PlaneScale = TexelParams.DualPlane ? 2 : 1;

            for (int Plane = 0; Plane < PlaneScale; Plane++)
            {
                for (int t = 0; t < BlockHeight; t++)
                {
                    for (int s = 0; s < BlockWidth; s++)
                    {
                        int cs = Ds * s;
                        int ct = Dt * t;

                        int gs = (cs * (TexelParams.Width - 1) + 32) >> 6;
                        int gt = (ct * (TexelParams.Height - 1) + 32) >> 6;

                        int js = gs >> 4;
                        int fs = gs & 0xF;

                        int jt = gt >> 4;
                        int ft = gt & 0x0F;

                        int w11 = (fs * ft + 8) >> 4;
                        int w10 = ft - w11;
                        int w01 = fs - w11;
                        int w00 = 16 - fs - ft + w11;

                        int v0 = js + jt * TexelParams.Width;

                        int p00 = 0;
                        int p01 = 0;
                        int p10 = 0;
                        int p11 = 0;

                        if (v0 < (TexelParams.Width * TexelParams.Height))
                        {
                            p00 = Unquantized[Plane][v0];
                        }

                        if (v0 + 1 < (TexelParams.Width * TexelParams.Height))
                        {
                            p01 = Unquantized[Plane][v0 + 1];
                        }
                        
                        if (v0 + TexelParams.Width < (TexelParams.Width * TexelParams.Height))
                        {
                            p10 = Unquantized[Plane][v0 + TexelParams.Width];
                        }
                        
                        if (v0 + TexelParams.Width + 1 < (TexelParams.Width * TexelParams.Height))
                        {
                            p11 = Unquantized[Plane][v0 + TexelParams.Width + 1];
                        }

                        OutputBuffer[Plane][t * BlockWidth + s] = (p00 * w00 + p01 * w01 + p10 * w10 + p11 * w11 + 8) >> 4;
                    }
                }
            }
        }

        static int UnquantizeTexelWeight(IntegerEncoded IntEncoded)
        {
            int BitValue  = IntEncoded.BitValue;
            int BitLength = IntEncoded.NumberBits;

            int A = BitArrayStream.Replicate(BitValue & 1, 1, 7);
            int B = 0, C = 0, D = 0;

            int Result = 0;

            switch (IntEncoded.GetEncoding())
            {
                case IntegerEncoded.EIntegerEncoding.JustBits:
                    Result = BitArrayStream.Replicate(BitValue, BitLength, 6);
                    break;

                case IntegerEncoded.EIntegerEncoding.Trit:
                {
                    D = IntEncoded.TritValue;
                    Debug.Assert(D < 3);

                    switch (BitLength)
                    {
                        case 0:
                        {
                            int[] Results = { 0, 32, 63 };
                            Result = Results[D];

                            break;
                        }

                        case 1:
                        {
                            C = 50;
                            break;
                        }

                        case 2:
                        {
                            C = 23;
                            int b = (BitValue >> 1) & 1;
                            B = (b << 6) | (b << 2) | b;

                            break;
                        }

                        case 3:
                        {
                            C = 11;
                            int cb = (BitValue >> 1) & 3;
                            B = (cb << 5) | cb;

                            break;
                        }

                        default:
                            throw new ASTCDecoderException("Invalid trit encoding for texel weight");
                    }

                    break;
                }    

                case IntegerEncoded.EIntegerEncoding.Quint:
                {
                    D = IntEncoded.QuintValue;
                    Debug.Assert(D < 5);

                    switch (BitLength)
                    {
                        case 0:
                        {
                            int[] Results = { 0, 16, 32, 47, 63 };
                            Result = Results[D];

                            break;
                        }

                        case 1:
                        {
                            C = 28;

                            break;
                        }

                        case 2:
                        {
                            C = 13;
                            int b = (BitValue >> 1) & 1;
                            B = (b << 6) | (b << 1);

                            break;
                        }
                                
                        default:
                            throw new ASTCDecoderException("Invalid quint encoding for texel weight");
                    }

                    break;
                }    
            }

            if (IntEncoded.GetEncoding() != IntegerEncoded.EIntegerEncoding.JustBits && BitLength > 0)
            {
                // Decode the value...
                Result  = D * C + B;
                Result ^= A;
                Result  = (A & 0x20) | (Result >> 2);
            }

            Debug.Assert(Result < 64);

            // Change from [0,63] to [0,64]
            if (Result > 32)
            {
                Result += 1;
            }

            return Result;
        }

        static byte ReverseByte(byte b)
        {
            // Taken from http://graphics.stanford.edu/~seander/bithacks.html#ReverseByteWith64Bits
            return (byte)((((b) * 0x80200802L) & 0x0884422110L) * 0x0101010101L >> 32);
        }

        static uint[] ReadUintColorValues(int Number, int[] ColorValues, ref int ColorValuesPosition)
        {
            uint[] Ret = new uint[Number];

            for (int i = 0; i < Number; i++)
            {
                Ret[i] = (uint)ColorValues[ColorValuesPosition++];
            }

            return Ret;
        }

        static int[] ReadIntColorValues(int Number, int[] ColorValues, ref int ColorValuesPosition)
        {
            int[] Ret = new int[Number];

            for (int i = 0; i < Number; i++)
            {
                Ret[i] = ColorValues[ColorValuesPosition++];
            }

            return Ret;
        }

        static void ComputeEndpoints(
            ASTCPixel[] EndPoints, 
            int[]       ColorValues, 
            uint        ColorEndpointMode, 
            ref int     ColorValuesPosition)
        {
            switch (ColorEndpointMode)
            {
                case 0:
                {
                    uint[] Val = ReadUintColorValues(2, ColorValues, ref ColorValuesPosition);

                    EndPoints[0] = new ASTCPixel(0xFF, (short)Val[0], (short)Val[0], (short)Val[0]);
                    EndPoints[1] = new ASTCPixel(0xFF, (short)Val[1], (short)Val[1], (short)Val[1]);

                    break;
                }
                    

                case 1:
                {
                    uint[] Val = ReadUintColorValues(2, ColorValues, ref ColorValuesPosition);
                    int L0     = (int)((Val[0] >> 2) | (Val[1] & 0xC0));
                    int L1     = (int)Math.Min(L0 + (Val[1] & 0x3F), 0xFFU);

                    EndPoints[0] = new ASTCPixel(0xFF, (short)L0, (short)L0, (short)L0);
                    EndPoints[1] = new ASTCPixel(0xFF, (short)L1, (short)L1, (short)L1);

                    break;
                }

                case 4:
                {
                    uint[] Val = ReadUintColorValues(4, ColorValues, ref ColorValuesPosition);

                    EndPoints[0] = new ASTCPixel((short)Val[2], (short)Val[0], (short)Val[0], (short)Val[0]);
                    EndPoints[1] = new ASTCPixel((short)Val[3], (short)Val[1], (short)Val[1], (short)Val[1]);

                    break;
                }

                case 5:
                {
                    int[] Val = ReadIntColorValues(4, ColorValues, ref ColorValuesPosition);

                    BitArrayStream.BitTransferSigned(ref Val[1], ref Val[0]);
                    BitArrayStream.BitTransferSigned(ref Val[3], ref Val[2]);

                    EndPoints[0] = new ASTCPixel((short)Val[2], (short)Val[0], (short)Val[0], (short)Val[0]);
                    EndPoints[1] = new ASTCPixel((short)(Val[2] + Val[3]), (short)(Val[0] + Val[1]), (short)(Val[0] + Val[1]), (short)(Val[0] + Val[1]));

                    EndPoints[0].ClampByte();
                    EndPoints[1].ClampByte();

                    break;
                }

                case 6:
                {
                    uint[] Val = ReadUintColorValues(4, ColorValues, ref ColorValuesPosition);

                    EndPoints[0] = new ASTCPixel(0xFF, (short)(Val[0] * Val[3] >> 8), (short)(Val[1] * Val[3] >> 8), (short)(Val[2] * Val[3] >> 8));
                    EndPoints[1] = new ASTCPixel(0xFF, (short)Val[0], (short)Val[1], (short)Val[2]);

                    break;
                }

                case 8:
                {
                    uint[] Val = ReadUintColorValues(6, ColorValues, ref ColorValuesPosition);

                    if (Val[1] + Val[3] + Val[5] >= Val[0] + Val[2] + Val[4])
                    {
                        EndPoints[0] = new ASTCPixel(0xFF, (short)Val[0], (short)Val[2], (short)Val[4]);
                        EndPoints[1] = new ASTCPixel(0xFF, (short)Val[1], (short)Val[3], (short)Val[5]);
                    }
                    else
                    {
                        EndPoints[0] = ASTCPixel.BlueContract(0xFF, (short)Val[1], (short)Val[3], (short)Val[5]);
                        EndPoints[1] = ASTCPixel.BlueContract(0xFF, (short)Val[0], (short)Val[2], (short)Val[4]);
                    }

                    break;
                }

                case 9:
                {
                    int[] Val = ReadIntColorValues(6, ColorValues, ref ColorValuesPosition);

                    BitArrayStream.BitTransferSigned(ref Val[1], ref Val[0]);
                    BitArrayStream.BitTransferSigned(ref Val[3], ref Val[2]);
                    BitArrayStream.BitTransferSigned(ref Val[5], ref Val[4]);

                    if (Val[1] + Val[3] + Val[5] >= 0)
                    {
                        EndPoints[0] = new ASTCPixel(0xFF, (short)Val[0], (short)Val[2], (short)Val[4]);
                        EndPoints[1] = new ASTCPixel(0xFF, (short)(Val[0] + Val[1]), (short)(Val[2] + Val[3]), (short)(Val[4] + Val[5]));
                    }
                    else
                    {
                        EndPoints[0] = ASTCPixel.BlueContract(0xFF, Val[0] + Val[1], Val[2] + Val[3], Val[4] + Val[5]);
                        EndPoints[1] = ASTCPixel.BlueContract(0xFF, Val[0], Val[2], Val[4]);
                    }

                    EndPoints[0].ClampByte();
                    EndPoints[1].ClampByte();

                    break;
                }

                case 10:
                {
                    uint[] Val = ReadUintColorValues(6, ColorValues, ref ColorValuesPosition);

                    EndPoints[0] = new ASTCPixel((short)Val[4], (short)(Val[0] * Val[3] >> 8), (short)(Val[1] * Val[3] >> 8), (short)(Val[2] * Val[3] >> 8));
                    EndPoints[1] = new ASTCPixel((short)Val[5], (short)Val[0], (short)Val[1], (short)Val[2]);

                    break;
                }

                case 12:
                {
                    uint[] Val = ReadUintColorValues(8, ColorValues, ref ColorValuesPosition);

                    if (Val[1] + Val[3] + Val[5] >= Val[0] + Val[2] + Val[4])
                    {
                        EndPoints[0] = new ASTCPixel((short)Val[6], (short)Val[0], (short)Val[2], (short)Val[4]);
                        EndPoints[1] = new ASTCPixel((short)Val[7], (short)Val[1], (short)Val[3], (short)Val[5]);
                    }
                    else
                    {
                        EndPoints[0] = ASTCPixel.BlueContract((short)Val[7], (short)Val[1], (short)Val[3], (short)Val[5]);
                        EndPoints[1] = ASTCPixel.BlueContract((short)Val[6], (short)Val[0], (short)Val[2], (short)Val[4]);
                    }

                    break;
                }

                case 13:
                {
                    int[] Val = ReadIntColorValues(8, ColorValues, ref ColorValuesPosition);

                    BitArrayStream.BitTransferSigned(ref Val[1], ref Val[0]);
                    BitArrayStream.BitTransferSigned(ref Val[3], ref Val[2]);
                    BitArrayStream.BitTransferSigned(ref Val[5], ref Val[4]);
                    BitArrayStream.BitTransferSigned(ref Val[7], ref Val[6]);

                    if (Val[1] + Val[3] + Val[5] >= 0)
                    {
                        EndPoints[0] = new ASTCPixel((short)Val[6], (short)Val[0], (short)Val[2], (short)Val[4]);
                        EndPoints[1] = new ASTCPixel((short)(Val[7] + Val[6]), (short)(Val[0] + Val[1]), (short)(Val[2] + Val[3]), (short)(Val[4] + Val[5]));
                    }
                    else
                    {
                        EndPoints[0] = ASTCPixel.BlueContract(Val[6] + Val[7], Val[0] + Val[1], Val[2] + Val[3], Val[4] + Val[5]);
                        EndPoints[1] = ASTCPixel.BlueContract(Val[6], Val[0], Val[2], Val[4]);
                    }

                    EndPoints[0].ClampByte();
                    EndPoints[1].ClampByte();

                    break;
                }

                default:
                    throw new ASTCDecoderException("Unsupported color endpoint mode (is it HDR?)");
            }
        }

        static void DecodeColorValues(
            int[]  OutputValues, 
            byte[] InputData, 
            uint[] Modes, 
            int    NumberPartitions, 
            int    NumberBitsForColorData)
        {
            // First figure out how many color values we have
            int NumberValues = 0;

            for (int i = 0; i < NumberPartitions; i++)
            {
                NumberValues += (int)((Modes[i] >> 2) + 1) << 1;
            }

            // Then based on the number of values and the remaining number of bits,
            // figure out the max value for each of them...
            int Range = 256;

            while (--Range > 0)
            {
                IntegerEncoded IntEncoded = IntegerEncoded.CreateEncoding(Range);
                int BitLength             = IntEncoded.GetBitLength(NumberValues);

                if (BitLength <= NumberBitsForColorData)
                {
                    // Find the smallest possible range that matches the given encoding
                    while (--Range > 0)
                    {
                        IntegerEncoded NewIntEncoded = IntegerEncoded.CreateEncoding(Range);
                        if (!NewIntEncoded.MatchesEncoding(IntEncoded))
                        {
                            break;
                        }
                    }

                    // Return to last matching range.
                    Range++;
                    break;
                }
            }

            // We now have enough to decode our integer sequence.
            List<IntegerEncoded> IntegerEncodedSequence = new List<IntegerEncoded>();
            BitArrayStream ColorBitStream               = new BitArrayStream(new BitArray(InputData));

            IntegerEncoded.DecodeIntegerSequence(IntegerEncodedSequence, ColorBitStream, Range, NumberValues);

            // Once we have the decoded values, we need to dequantize them to the 0-255 range
            // This procedure is outlined in ASTC spec C.2.13
            int OutputIndices = 0;

            foreach (IntegerEncoded IntEncoded in IntegerEncodedSequence)
            {
                int BitLength = IntEncoded.NumberBits;
                int BitValue  = IntEncoded.BitValue;

                Debug.Assert(BitLength >= 1);

                int A = 0, B = 0, C = 0, D = 0;
                // A is just the lsb replicated 9 times.
                A = BitArrayStream.Replicate(BitValue & 1, 1, 9);

                switch (IntEncoded.GetEncoding())
                {
                    case IntegerEncoded.EIntegerEncoding.JustBits:
                    {
                        OutputValues[OutputIndices++] = BitArrayStream.Replicate(BitValue, BitLength, 8);

                        break;
                    }

                    case IntegerEncoded.EIntegerEncoding.Trit:
                    {
                        D = IntEncoded.TritValue;

                        switch (BitLength)
                        {
                            case 1:
                            {
                                C = 204;

                                break;
                            }
                                    
                            case 2:
                            {
                                C = 93;
                                // B = b000b0bb0
                                int b = (BitValue >> 1) & 1;
                                B = (b << 8) | (b << 4) | (b << 2) | (b << 1);

                                break;
                            }

                            case 3:
                            {
                                C = 44;
                                // B = cb000cbcb
                                int cb = (BitValue >> 1) & 3;
                                B = (cb << 7) | (cb << 2) | cb;

                                break;
                            }
                                    

                            case 4:
                            {
                                C = 22;
                                // B = dcb000dcb
                                int dcb = (BitValue >> 1) & 7;
                                B = (dcb << 6) | dcb;

                                break;
                            }

                            case 5:
                            {
                                C = 11;
                                // B = edcb000ed
                                int edcb = (BitValue >> 1) & 0xF;
                                B = (edcb << 5) | (edcb >> 2);

                                break;
                            }

                            case 6:
                            {
                                C = 5;
                                // B = fedcb000f
                                int fedcb = (BitValue >> 1) & 0x1F;
                                B = (fedcb << 4) | (fedcb >> 4);

                                break;
                            }

                            default:
                                throw new ASTCDecoderException("Unsupported trit encoding for color values!");
                        }

                        break;
                    }
                        
                    case IntegerEncoded.EIntegerEncoding.Quint:
                    {
                        D = IntEncoded.QuintValue;

                        switch (BitLength)
                        {
                            case 1:
                            {
                                C = 113;

                                break;
                            }
                                    
                            case 2:
                            {
                                C = 54;
                                // B = b0000bb00
                                int b = (BitValue >> 1) & 1;
                                B = (b << 8) | (b << 3) | (b << 2);

                                break;
                            }
                                    
                            case 3:
                            {
                                C = 26;
                                // B = cb0000cbc
                                int cb = (BitValue >> 1) & 3;
                                B = (cb << 7) | (cb << 1) | (cb >> 1);

                                break;
                            }

                            case 4:
                            {
                                C = 13;
                                // B = dcb0000dc
                                int dcb = (BitValue >> 1) & 7;
                                B = (dcb << 6) | (dcb >> 1);

                                break;
                            }
                                  
                            case 5:
                            {
                                C = 6;
                                // B = edcb0000e
                                int edcb = (BitValue >> 1) & 0xF;
                                B = (edcb << 5) | (edcb >> 3);

                                break;
                            }

                            default:
                                throw new ASTCDecoderException("Unsupported quint encoding for color values!");
                        }
                        break;
                    }   
                }

                if (IntEncoded.GetEncoding() != IntegerEncoded.EIntegerEncoding.JustBits)
                {
                    int T = D * C + B;
                    T    ^= A;
                    T     = (A & 0x80) | (T >> 2);

                    OutputValues[OutputIndices++] = T;
                }
            }

            // Make sure that each of our values is in the proper range...
            for (int i = 0; i < NumberValues; i++)
            {
                Debug.Assert(OutputValues[i] <= 255);
            }
        }

        static void FillVoidExtentLDR(BitArrayStream BitStream, int[] OutputBuffer, int BlockWidth, int BlockHeight)
        {
            // Don't actually care about the void extent, just read the bits...
            for (int i = 0; i < 4; ++i)
            {
                BitStream.ReadBits(13);
            }

            // Decode the RGBA components and renormalize them to the range [0, 255]
            ushort R = (ushort)BitStream.ReadBits(16);
            ushort G = (ushort)BitStream.ReadBits(16);
            ushort B = (ushort)BitStream.ReadBits(16);
            ushort A = (ushort)BitStream.ReadBits(16);

            int RGBA = (R >> 8) | (G & 0xFF00) | ((B) & 0xFF00) << 8 | ((A) & 0xFF00) << 16;

            for (int j = 0; j < BlockHeight; j++)
            {
                for (int i = 0; i < BlockWidth; i++)
                {
                    OutputBuffer[j * BlockWidth + i] = RGBA;
                }
            }
        }

        static TexelWeightParams DecodeBlockInfo(BitArrayStream BitStream)
        {
            TexelWeightParams TexelParams = new TexelWeightParams();

            // Read the entire block mode all at once
            ushort ModeBits = (ushort)BitStream.ReadBits(11);

            // Does this match the void extent block mode?
            if ((ModeBits & 0x01FF) == 0x1FC)
            {
                if ((ModeBits & 0x200) != 0)
                {
                    TexelParams.VoidExtentHDR = true;
                }
                else
                {
                    TexelParams.VoidExtentLDR = true;
                }

                // Next two bits must be one.
                if ((ModeBits & 0x400) == 0 || BitStream.ReadBits(1) == 0)
                {
                    TexelParams.Error = true;
                }

                return TexelParams;
            }

            // First check if the last four bits are zero
            if ((ModeBits & 0xF) == 0)
            {
                TexelParams.Error = true;
                return TexelParams;
            }

            // If the last two bits are zero, then if bits
            // [6-8] are all ones, this is also reserved.
            if ((ModeBits & 0x3) == 0 && (ModeBits & 0x1C0) == 0x1C0)
            {
                TexelParams.Error = true;

                return TexelParams;
            }

            // Otherwise, there is no error... Figure out the layout
            // of the block mode. Layout is determined by a number
            // between 0 and 9 corresponding to table C.2.8 of the
            // ASTC spec.
            int Layout = 0;

            if ((ModeBits & 0x1) != 0 || (ModeBits & 0x2) != 0)
            {
                // layout is in [0-4]
                if ((ModeBits & 0x8) != 0)
                {
                    // layout is in [2-4]
                    if ((ModeBits & 0x4) != 0)
                    {
                        // layout is in [3-4]
                        if ((ModeBits & 0x100) != 0)
                        {
                            Layout = 4;
                        }
                        else
                        {
                            Layout = 3;
                        }
                    }
                    else
                    {
                        Layout = 2;
                    }
                }
                else
                {
                    // layout is in [0-1]
                    if ((ModeBits & 0x4) != 0)
                    {
                        Layout = 1;
                    }
                    else
                    {
                        Layout = 0;
                    }
                }
            }
            else
            {
                // layout is in [5-9]
                if ((ModeBits & 0x100) != 0)
                {
                    // layout is in [7-9]
                    if ((ModeBits & 0x80) != 0)
                    {
                        // layout is in [7-8]
                        Debug.Assert((ModeBits & 0x40) == 0);

                        if ((ModeBits & 0x20) != 0)
                        {
                            Layout = 8;
                        }
                        else
                        {
                            Layout = 7;
                        }
                    }
                    else
                    {
                        Layout = 9;
                    }
                }
                else
                {
                    // layout is in [5-6]
                    if ((ModeBits & 0x80) != 0)
                    {
                        Layout = 6;
                    }
                    else
                    {
                        Layout = 5;
                    }
                }
            }

            Debug.Assert(Layout < 10);

            // Determine R
            int R = (ModeBits >> 4) & 1;
            if (Layout < 5)
            {
                R |= (ModeBits & 0x3) << 1;
            }
            else
            {
                R |= (ModeBits & 0xC) >> 1;
            }

            Debug.Assert(2 <= R && R <= 7);

            // Determine width & height
            switch (Layout)
            {
                case 0:
                {
                    int A = (ModeBits >> 5) & 0x3;
                    int B = (ModeBits >> 7) & 0x3;

                    TexelParams.Width  = B + 4;
                    TexelParams.Height = A + 2;

                    break;
                }

                case 1:
                {
                    int A = (ModeBits >> 5) & 0x3;
                    int B = (ModeBits >> 7) & 0x3;

                    TexelParams.Width  = B + 8;
                    TexelParams.Height = A + 2;

                    break;
                }

                case 2:
                {
                    int A = (ModeBits >> 5) & 0x3;
                    int B = (ModeBits >> 7) & 0x3;

                    TexelParams.Width  = A + 2;
                    TexelParams.Height = B + 8;

                    break;
                }

                case 3:
                {
                    int A = (ModeBits >> 5) & 0x3;
                    int B = (ModeBits >> 7) & 0x1;

                    TexelParams.Width  = A + 2;
                    TexelParams.Height = B + 6;

                    break;
                }

                case 4:
                {
                    int A = (ModeBits >> 5) & 0x3;
                    int B = (ModeBits >> 7) & 0x1;

                    TexelParams.Width  = B + 2;
                    TexelParams.Height = A + 2;

                    break;
                }

                case 5:
                {
                    int A = (ModeBits >> 5) & 0x3;

                    TexelParams.Width  = 12;
                    TexelParams.Height = A + 2;

                    break;
                }

                case 6:
                {
                    int A = (ModeBits >> 5) & 0x3;

                    TexelParams.Width  = A + 2;
                    TexelParams.Height = 12;

                    break;
                }

                case 7:
                {
                    TexelParams.Width  = 6;
                    TexelParams.Height = 10;

                    break;
                }

                case 8:
                {
                    TexelParams.Width  = 10;
                    TexelParams.Height = 6;
                    break;
                }

                case 9:
                {
                    int A = (ModeBits >> 5) & 0x3;
                    int B = (ModeBits >> 9) & 0x3;

                    TexelParams.Width  = A + 6;
                    TexelParams.Height = B + 6;

                    break;
                }

                default:
                    //Don't know this layout...
                    TexelParams.Error = true;
                    break;
            }

            // Determine whether or not we're using dual planes
            // and/or high precision layouts.
            bool D = ((Layout != 9) && ((ModeBits & 0x400) != 0));
            bool H = (Layout != 9) && ((ModeBits & 0x200) != 0);

            if (H)
            {
                int[] MaxWeights = { 9, 11, 15, 19, 23, 31 };
                TexelParams.MaxWeight = MaxWeights[R - 2];
            }
            else
            {
                int[] MaxWeights = { 1, 2, 3, 4, 5, 7 };
                TexelParams.MaxWeight = MaxWeights[R - 2];
            }

            TexelParams.DualPlane = D;

            return TexelParams;
        }
    }
    
    class ASTCPixel
{
    public short R { get; set; }
    public short G { get; set; }
    public short B { get; set; }
    public short A { get; set; }

    byte[] BitDepth = new byte[4];

    public ASTCPixel(short _A, short _R, short _G, short _B)
    {
        A = _A;
        R = _R;
        G = _G;
        B = _B;

        for (int i = 0; i < 4; i++)
            BitDepth[i] = 8;
    }

    public void ClampByte()
    {
        R = Math.Min(Math.Max(R, (short)0), (short)255);
        G = Math.Min(Math.Max(G, (short)0), (short)255);
        B = Math.Min(Math.Max(B, (short)0), (short)255);
        A = Math.Min(Math.Max(A, (short)0), (short)255);
    }

    public short GetComponent(int Index)
    {
        switch(Index)
        {
            case 0: return A;
            case 1: return R;
            case 2: return G;
            case 3: return B;
        }

        return 0;
    }

    public void SetComponent(int Index, int Value)
    {
        switch (Index)
        {
            case 0:
                A = (short)Value;
                break;
            case 1:
                R = (short)Value;
                break;
            case 2:
                G = (short)Value;
                break;
            case 3:
                B = (short)Value;
                break;
        }
    }

    public void ChangeBitDepth(byte[] Depth)
    {
        for(int i = 0; i< 4; i++)
        {
            int Value = ChangeBitDepth(GetComponent(i), BitDepth[i], Depth[i]);

            SetComponent(i, Value);
            BitDepth[i] = Depth[i];
        }
    }

    short ChangeBitDepth(short Value, byte OldDepth, byte NewDepth)
    {
        Debug.Assert(NewDepth <= 8);
        Debug.Assert(OldDepth <= 8);

        if (OldDepth == NewDepth)
        {
            // Do nothing
            return Value;
        }
        else if (OldDepth == 0 && NewDepth != 0)
        {
            return (short)((1 << NewDepth) - 1);
        }
        else if (NewDepth > OldDepth)
        {
            return (short)ASTCDecoder.BitArrayStream.Replicate(Value, OldDepth, NewDepth);
        }
        else
        {
            // oldDepth > newDepth
            if (NewDepth == 0)
            {
                return 0xFF;
            }
            else
            {
                byte BitsWasted = (byte)(OldDepth - NewDepth);
                short TempValue = Value;

                TempValue = (short)((TempValue + (1 << (BitsWasted - 1))) >> BitsWasted);
                TempValue = Math.Min(Math.Max((short)0, TempValue), (short)((1 << NewDepth) - 1));

                return (byte)(TempValue);
            }
        }
    }

    public int Pack()
    {
        ASTCPixel NewPixel   = new ASTCPixel(A, R, G, B);
        byte[] eightBitDepth = { 8, 8, 8, 8 };

        NewPixel.ChangeBitDepth(eightBitDepth);

        return (byte)NewPixel.A << 24 |
               (byte)NewPixel.B << 16 |
               (byte)NewPixel.G << 8  |
               (byte)NewPixel.R << 0;
    }

    // Adds more precision to the blue channel as described
    // in C.2.14
    public static ASTCPixel BlueContract(int a, int r, int g, int b)
    {
        return new ASTCPixel((short)(a),
                             (short)((r + b) >> 1),
                             (short)((g + b) >> 1),
                             (short)(b));
    }
}

public enum TEX_FILTER_FLAGS
{
    DEFAULT = 0,
    //
    // Zusammenfassung:
    //     Wrap vs. Mirror vs. Clamp filtering options
    WRAP_U = 1,
    //
    // Zusammenfassung:
    //     Wrap vs. Mirror vs. Clamp filtering options
    WRAP_V = 2,
    //
    // Zusammenfassung:
    //     Wrap vs. Mirror vs. Clamp filtering options
    WRAP_W = 4,
    //
    // Zusammenfassung:
    //     Wrap vs. Mirror vs. Clamp filtering options
    WRAP = 7,
    MIRROR_U = 0x10,
    //
    // Zusammenfassung:
    //     Wrap vs. Mirror vs. Clamp filtering options
    MIRROR_V = 0x20,
    //
    // Zusammenfassung:
    //     Wrap vs. Mirror vs. Clamp filtering options
    MIRROR_W = 0x40,
    //
    // Zusammenfassung:
    //     Wrap vs. Mirror vs. Clamp filtering options
    MIRROR = 0x70,
    //
    // Zusammenfassung:
    //     Resize color and alpha channel independently
    SEPARATE_ALPHA = 0x100,
    //
    // Zusammenfassung:
    //     Enable *2 - 1 conversion cases for unorm to/from float and positive-only float
    //     formats
    FLOAT_X2BIAS = 0x200,
    //
    // Zusammenfassung:
    //     When converting RGB to R, defaults to using grayscale. These flags indicate copying
    //     a specific channel instead When converting RGB to RG, defaults to copying RED
    //     | GREEN. These flags control which channels are selected instead.
    RGB_COPY_RED = 0x1000,
    //
    // Zusammenfassung:
    //     When converting RGB to R, defaults to using grayscale. These flags indicate copying
    //     a specific channel instead When converting RGB to RG, defaults to copying RED
    //     | GREEN. These flags control which channels are selected instead.
    RGB_COPY_GREEN = 0x2000,
    //
    // Zusammenfassung:
    //     When converting RGB to R, defaults to using grayscale. These flags indicate copying
    //     a specific channel instead When converting RGB to RG, defaults to copying RED
    //     | GREEN. These flags control which channels are selected instead.
    RGB_COPY_BLUE = 0x4000,
    //
    // Zusammenfassung:
    //     Use ordered 4x4 dithering for any required conversions
    DITHER = 0x10000,
    //
    // Zusammenfassung:
    //     Use error-diffusion dithering for any required conversions
    DITHER_DIFFUSION = 0x20000,
    //
    // Zusammenfassung:
    //     Filtering mode to use for any required image resizing
    POINT = 0x100000,
    //
    // Zusammenfassung:
    //     Filtering mode to use for any required image resizing
    LINEAR = 0x200000,
    //
    // Zusammenfassung:
    //     Filtering mode to use for any required image resizing
    CUBIC = 0x300000,
    //
    // Zusammenfassung:
    //     Filtering mode to use for any required image resizing
    BOX = 0x400000,
    //
    // Zusammenfassung:
    //     Filtering mode to use for any required image resizing Equiv to Box filtering
    //     for mipmap generation
    FANT = 0x400000,
    //
    // Zusammenfassung:
    //     Filtering mode to use for any required image resizing
    TRIANGLE = 0x500000,
    SRGB_IN = 0x1000000,
    SRGB_OUT = 0x2000000,
    //
    // Zusammenfassung:
    //     sRGB to/from RGB for use in conversion operations if the input format type is
    //     IsSRGB(), then SRGB_IN is on by default if the output format type is IsSRGB(),
    //     then SRGB_OUT is on by default
    SRGB = 0x3000000,
    //
    // Zusammenfassung:
    //     Forces use of the non-WIC path when both are an option
    FORCE_NON_WIC = 0x10000000,
    //
    // Zusammenfassung:
    //     Forces use of the WIC path even when logic would have picked a non-WIC path when
    //     both are an option
    FORCE_WIC = 0x20000000
}