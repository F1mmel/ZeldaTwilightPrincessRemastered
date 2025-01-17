using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using GameFormatReader.Common;
using UnityEngine;

public class BTILoader : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        //CollectArcFiles();
    }

    private void CollectArcFiles()
    {
        DirectoryInfo dir = new DirectoryInfo("Assets/GameFiles");
        //FileInfo[] info = dir.GetFiles("*.arc");
        FileInfo[] info = dir.GetFiles("*.bti");
        //info.Select(f => f.FullName).ToArray();
        foreach (FileInfo f in info)
        {
            //LoadBTI(File.ReadAllBytes(f.FullName));
            /*Archive arc = ArcReader.Read(f.FullName);
            List<ArcFile> files = arc.Files;

            foreach (ArcFile file in files)
            {
                if (file.Name.EndsWith(".bti"))
                {
                    if (file.Name.Contains("kkri_eye.1"))
                    {
                        
                        LoadBTI(file);
                        break;
                    }
                }
            }*/
        }
    }

    public static BTI LoadBTI( /*ArcFile file*/ string name, byte[] buffer)
    {
        EndianBinaryReader reader = new EndianBinaryReader(buffer, Endian.Big);

        //BinaryTextureImage i = new BinaryTextureImage();
        //i.Load(reader);

        //reader.Skip(17);

        /*TexFormat format = (TexFormat)reader.ReadByte();
        //reader.ReadByte();
        
        reader.Skip(1);
        int width = reader.ReadInt16();
        int height = reader.ReadInt16();
        
        WrapMode wrapX = (WrapMode)reader.ReadByte();
        WrapMode wrapY = (WrapMode)reader.ReadByte();
        reader.Skip(1);
        TexPalette paletteFormat = (TexPalette)reader.ReadByte();
        int paletteCount = reader.ReadInt16();
        int paletteOffset = reader.ReadInt32();

        Anisotropy anisotropy = (Anisotropy)reader.ReadByte();
        TexFilter filterMin = (TexFilter)reader.ReadByte();
        TexFilter filterMax = (TexFilter)reader.ReadByte();

        int minLOD = reader.ReadByte() * 1 / 8;
        int maxLOD = reader.ReadByte() * 1 / 8;

        int mipCount = reader.ReadByte();
        reader.Skip(8);
        int loadBias = reader.ReadInt16() * 1 / 100;
        reader.Skip(2);
        int dataOffset = reader.ReadInt32();*/

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
        //if(paletteOffset != 0)
        //paletteData = BufferUtil.Subarray(buffer, paletteOffset, paletteCount * 2);
        byte[] data = BufferUtil.Slice(buffer, dataOffset);

        Debug.LogWarning(data.Length);

        reader.Close();

        // Erstelle eine neue Texture2D
        Texture2D texture = new Texture2D(width, height);
        //texture.

        // Lade das Byte-Array in die Textur
        //texture.LoadImage(data);

        //texture = DecodeImage(data, paletteData, format, paletteFormat, paletteCount, width, height);

        /*Bitmap bitmap = Decode_texture(data);
        MemoryStream ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        var b = new byte[ms.Length];
        ms.Position = 0;
        ms.Read(b, 0, b.Length);*/
        Texture2D t = new Texture2D(1, 1);
        //t.LoadImage(b);

        return new BTI(name, t, null);
    }

    /*private static Texture2D DecodeImage(byte[] data, byte[] paletteData, TexFormat format, TexPalette palette,
        int paletteCount, int width, int height)
    {
        BlockWidths blockWidths = (BlockWidths)format;
        BlockHeights blockHeights = (BlockHeights)format;
        BlockDataSizes blockDataSizes = (BlockDataSizes)format;

        Bitmap bitmap = new Bitmap(width, height);

        int offset = 0;
        int blockX = 0;
        int blockY = 0;

        while (blockY < height)
        {
            //decode_cmpr_block(image_format, image_data, offset, block_data_size, colors)

            Color32[] pixelColor = CMPRDecoder.DecodeCMPRBlock(null, data, offset, blockDataSizes, new Color32[0]);
            //Debug.LogWarning("PIXEL: " + pixelColor.Length);

            for (int i = 0; i < pixelColor.Length; i++)
            {
                Color32 color = pixelColor[i];

                int xInBlock = i % (int)blockWidths;
                int yInBlock = i / (int)blockWidths;

                int x = blockX + xInBlock;
                int y = blockY + yInBlock;

                if (x >= width || y >= height) continue;

                System.Drawing.Color d = new System.Drawing.Color();
                //d.R = color.r;
                //d.G = color.g;
                //d.B = color.b;
                bitmap.SetPixel(x, y, System.Drawing.Color.Aqua);
            }

            offset += (int)blockDataSizes;
            blockX += (int)blockWidths;

            // New row
            if (blockX >= width)
            {
                blockX = 0;
                blockY += (int)blockHeights;
            }
        }

        MemoryStream ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        var buffer = new byte[ms.Length];
        ms.Position = 0;
        ms.Read(buffer, 0, buffer.Length);
        Texture2D t = new Texture2D(1, 1);
        t.LoadImage(buffer);

        return t;
    }*/

    public static List<ushort[]> canvas_dim = new List<ushort[]>();

    /*public static Bitmap Decode_texture(byte[] imageData)
    {

        byte[] real_block_width_array =
            { 8, 8, 8, 4, 4, 4, 4, 255, 8, 8, 4, 255, 255, 255, 8 }; // real one to calculate canvas size.
        byte[] block_width_array =
            { 4, 8, 8, 8, 8, 8, 16, 255, 4, 8, 8, 255, 255, 255, 4 }; // altered to match bit-per pixel size.
        byte[] block_height_array =
            { 8, 4, 4, 4, 4, 4, 4, 255, 8, 4, 4, 255, 255, 255, 8 }; // 255 = unused image format

        ushort colour_number = 0;
        int colour_number_x2 = 0;
        int colour_number_x4 = 0;
        // byte[] index declared in each if below
        ushort[] canvas = { 0, 0, 0, 0 };
        byte[] texture_format_int32 = { 0, 0, 0, 7 };
        byte[] palette_format_int32 = { 0, 0, 0, 0 };
        byte[] data = new byte[0];
        bool has_palette = false;
        byte alpha = 1;
        int data_start_offset = 0;
        int palette_start_offset;
        byte mipmaps_number = 0;
        byte[] colour_palette = null;

        using (System.IO.FileStream file = System.IO.File.Open(
                   @"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\kkri_eye.1.bti", System.IO.FileMode.Open,
                   System.IO.FileAccess.Read))
        {
            Array.Resize(ref data, (int)file.Length);
            file.Read(data, 0, (int)file.Length);
        }
        
        /* CRASH? using (System.IO.MemoryStream stream = new System.IO.MemoryStream(imageData))
        {
            Array.Resize(ref data, imageData.Length);
            stream.Read(data, 0, imageData.Length);
        }

        data = imageData;


        texture_format_int32[3] = data[0];
        if (mipmaps_number > data[0x18] - 1)
        {
            mipmaps_number = (byte)(data[0x18] - 1);
            if (mipmaps_number == 255)
                mipmaps_number = 0;
        }

        canvas[0] = (ushort)((data[2] << 8) + data[3]);
        canvas[1] = (ushort)((data[4] << 8) + data[5]);
        if ((data[8] == 1 || data[0x0a] != 0 || data[0x0b] != 0) && data[9] < 3 &&
            data[0] > 7) // the image has a palette - made so even bad images encoded with an idiot tool setting data[0x08] to zero would be decoded here
        {
            has_palette = true;
            palette_format_int32[3] = data[9];
            colour_number = (ushort)((data[0x0a] << 8) + data[0x0b]);
            colour_number_x2 = colour_number << 1;
            colour_number_x4 = colour_number << 2;
            palette_start_offset = (data[0x0C] << 24) | (data[0x0D] << 16) | (data[0x0E] << 8) | data[0x0F];
            Array.Resize(ref colour_palette, colour_number_x2);
            Array.Copy(data, palette_start_offset, colour_palette, 0, colour_number_x2);
        }

        data_start_offset = (data[0x1C] << 24) | (data[0x1D] << 16) | (data[0x1E] << 8) | data[0x1F];

        // fill canvas_dim first array
        canvas[2] = (ushort)(canvas[0] +
                             ((real_block_width_array[texture_format_int32[3]] -
                               (canvas[0] % real_block_width_array[texture_format_int32[3]])) %
                              real_block_width_array[texture_format_int32[3]]));
        canvas[3] = (ushort)(canvas[1] +
                             ((block_height_array[texture_format_int32[3]] -
                               (canvas[1] % block_height_array[texture_format_int32[3]])) %
                              block_height_array[texture_format_int32[3]]));
        // call fill_index_list
        canvas_dim.Add(canvas);
        Fill_index_list_class f = new Fill_index_list_class();
        object picture = f.Fill_index_list(data, data_start_offset, texture_format_int32[3], mipmaps_number,
            real_block_width_array, block_width_array, block_height_array, false, false);
        return Write_bmp_class.Write_bmp((List<List<byte[]>>)picture, canvas_dim, colour_palette, texture_format_int32,
            palette_format_int32, colour_number, @"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\OUTPUT.BMP",
            false, false, has_palette, false, false, false, false, true, false, false, false, false, false, false,
            false, mipmaps_number, 9, colour_number_x2, colour_number_x4);
    }*/
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