using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using GameFormatReader.Common;
using UnityEngine;

public class BTILoader : MonoBehaviour
{

    void Start()
    {
    }

    private void CollectArcFiles()
    {
        DirectoryInfo dir = new DirectoryInfo("Assets/GameFiles");
        FileInfo[] info = dir.GetFiles("*.bti");
        foreach (FileInfo f in info)
        {
        }
    }

    public static BTI LoadBTI(   string name, byte[] buffer)
    {
        EndianBinaryReader reader = new EndianBinaryReader(buffer, Endian.Big);




        TexFormat format = (TexFormat)reader.ReadByte();

        int transparent = reader.ReadByte();

        int width = reader.ReadInt16();
        int height = reader.ReadInt16();

        WrapMode wrapX = (WrapMode)reader.ReadByte();
        WrapMode wrapY = (WrapMode)reader.ReadByte();

        int palettesEnabled = reader.ReadByte();
        TexPalette paletteFormat = (TexPalette)reader.ReadByte();
        int paletteCount = reader.ReadInt16();
        int paletteOffset = reader.ReadInt32();

        int mipmapEnabled = reader.ReadByte();
        int doEdgeLOD = reader.ReadByte();
        int biasClamped = reader.ReadByte();

        Anisotropy anisotropy = (Anisotropy)reader.ReadByte();
        TexFilter filterMin = (TexFilter)reader.ReadByte();
        TexFilter filterMag = (TexFilter)reader.ReadByte();

        float minLOD = reader.ReadByte() * 1 / 8;
        float maxLOD = reader.ReadByte() * 1 / 8;

        int mipCount = reader.ReadByte();

        int unknown = reader.ReadByte();

        float loadBias = reader.ReadInt16() * 1 / 100;
        int dataOffset = reader.ReadInt32();

        Debug.LogWarning(
            string.Format(
                "Format: {0}, Width: {1}, Height: {2}, WrapX: {3}, WrapY: {4}, PaletteFormat: {5}, PaletteCount: {6}, PaletteOffset: {7}, Anisotropy: {8}, FilterMin: {9}, FilterMag: {10}, MinLOD: {11}, MaxLOD: {12}, MipCount: {13}, LoadBias: {14}, DataOffset: {15}",
                format, width, height, wrapX, wrapY, paletteFormat, paletteCount, paletteOffset, anisotropy, filterMin,
                filterMag, minLOD, maxLOD, mipCount, loadBias, dataOffset
            )
        );

        Debug.LogWarning("Finished: " + name);
        byte[] paletteData = null;
        byte[] data = BufferUtil.Slice(buffer, dataOffset);

        Debug.LogWarning(data.Length);

        reader.Close();

        Texture2D texture = new Texture2D(width, height);



        Texture2D t = new Texture2D(1, 1);

        return new BTI(name, t, null);
    }


    public static List<ushort[]> canvas_dim = new List<ushort[]>();

}

public class BTI
    {
        public string Name;
        public Texture2D Texture;
        public BinaryTextureImage Compressed;

        public BTI(string name, Texture2D texture, BinaryTextureImage compressed)
        {
            Name = name;
            Texture = texture;
            Compressed = compressed;
        }
    }


public enum TexFormat
{
    I4 = 0x0,
    I8 = 0x1,
    IA4 = 0x2,
    IA8 = 0x3,
    RGB565 = 0x4,
    RGB5A3 = 0x5,
    RGBA8 = 0x6,
    C4 = 0x8,
    C8 = 0x9,
    C14X2 = 0xA,
    CMPR = 0xE,
}

public enum WrapMode
{
    CLAMP = 0,
    REPEAT = 1,
    MIRROR = 2,
}

public enum TexPalette
{
    IA8 = 0x00,
    RGB565 = 0x01,
    RGB5A3 = 0x02,
}

public enum TexFilter
{
    NEAR = 0,
    LINEAR = 1,
    NEAR_MIP_NEAR = 2,
    LIN_MIP_NEAR = 3,
    NEAR_MIP_LIN = 4,
    LIN_MIP_LIN = 5,
}

public enum Anisotropy
{
    _1 = 0x00,
    _2 = 0x01,
    _4 = 0x02,
}

public enum BlockWidths
{
    I4 = 8,
    I8 = 8,
    IA4 = 8,
    IA8 = 4,
    RGB565 = 4,
    RGB5A3 = 4,
    RGBA32 = 4,
    C4 = 8,
    C8 = 8,
    C14X2 = 4,
    CMPR = 8
}

public enum BlockHeights
{
    I4 = 8,
    I8 = 4,
    IA4 = 4,
    IA8 = 4,
    RGB565 = 4,
    RGB5A3 = 4,
    RGBA32 = 4,
    C4 = 8,
    C8 = 4,
    C14X2 = 4,
    CMPR = 8
}

public enum BlockDataSizes
{
    I4 = 32,
    I8 = 32,
    IA4 = 32,
    IA8 = 32,
    RGB565 = 32,
    RGB5A3 = 32,
    RGBA32 = 64,
    C4 = 32,
    C8 = 32,
    C14X2 = 32,
    CMPR = 32
}

public enum ImageFormatsThatUsePalettes
{
    C4,
    C8,
    C14X2
}

public enum GreyscaleImageFormats
{
    I4,
    I8,
    IA4,
    IA8
}

public enum GreyscalePaletteFormats
{
    IA8
}

public enum PaletteFormatsWithAlpha
{
    IA8,
    RGB5A3
}

public enum MaxColorsForImageFormat
{
    C4 = 1 << 4,
    C8 = 1 << 8,
    C14X2 = 1 << 14
}