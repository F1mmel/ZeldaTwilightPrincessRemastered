using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GameFormatReader.Common;
using UnityEngine;

public class JPA
{
    public static JPAC Parse(EndianBinaryReader reader)
    {
        string version = Encoding.ASCII.GetString(reader.ReadBytes(0x08));
        if (version == "JPAC2-10" || version == "JPAC2-11")
        {
            return ParseJPAC2(reader);
        }
        
        return null;
    }

    private static JPAC ParseJPAC2(EndianBinaryReader reader)
    {
        uint effectCount = reader.ReadUInt16();
        uint textureCount = reader.ReadUInt16();
        uint textureTableOffset = reader.ReadUInt32();

        List<JPAResourceRaw> effects = new List<JPAResourceRaw>();
        long effectTableIdx = 0x10;
        for (int i = 0; i < effectCount; i++)
        {
            reader.BaseStream.Seek(effectTableIdx, SeekOrigin.Begin);

            ushort resourceId = reader.ReadUInt16();
            ushort blockCount = reader.ReadUInt16();

            long resourceBeginOffs = effectTableIdx;
            effectTableIdx += 0x08;

            // Quickly skim through the blocks
            for (int j = 0; j < blockCount; j++)
            {
                reader.BaseStream.Seek(effectTableIdx + 0x04, SeekOrigin.Begin);
                uint blockSize = reader.ReadUInt32();
                effectTableIdx += blockSize;
            }

            /*byte[] data = new byte[effectTableIdx - resourceBeginOffs];
            reader.BaseStream.Seek(resourceBeginOffs, SeekOrigin.Begin);
            reader.BaseStream.Read(data, 0, data.Length);*/
            byte[] data = BufferUtil.Slice(reader.BaseStream, resourceBeginOffs, effectTableIdx);

            effects.Add(new JPAResourceRaw { ResourceId = resourceId, Data = data, TexIdBase = 0 });
        }

        List<BTI> textures = new List<BTI>();
        long textureTableIdx = textureTableOffset;
        for (int i = 0; i < textureCount; i++)
        {
            reader.BaseStream.Seek(textureTableIdx, SeekOrigin.Begin);

            string header = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (header != "TEX1")
                throw new InvalidOperationException("Invalid texture header");

            uint blockSize = reader.ReadUInt32();
            reader.BaseStream.Seek(textureTableIdx + 0x0C, SeekOrigin.Begin);

            string textureName = Encoding.ASCII.GetString(reader.ReadBytes(0x14)).TrimEnd('\0');
            
            BinaryTextureImage compressedTex = new BinaryTextureImage();
            compressedTex.Load(reader, textureTableIdx + 0x20, 0);
            Texture2D tex = compressedTex.SkiaToTexture();

            BTI texture = new BTI(textureName, tex, compressedTex);
            textures.Add(texture);

            textureTableIdx += blockSize;
        }
        
        return new JPAC()
        {
            Effects = effects,
            Textures = textures,
        };
    }
}


public class JPAResourceRaw
{
    public int ResourceId;
    public byte[] Data;
    public int TexIdBase;
}