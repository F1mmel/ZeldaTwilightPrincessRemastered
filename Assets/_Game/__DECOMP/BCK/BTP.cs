using System;
using System.Collections.Generic;
using System.IO;
using GameFormatReader.Common;
using UnityEngine;

public class BTP
{
    public List<BTPAnimationEntry> AnimationEntries { get; private set; } = new List<BTPAnimationEntry>();
    public string Name { get; private set; }
    public int Duration { get; private set; }
    public byte LoopMode { get; private set; }

    public BTP(string name)
    {
        Name = name;
    }

    public void ReadTPT1Chunk(EndianBinaryReader reader)
    {
        // Read the J3D Header
        string magic = new string(reader.ReadChars(4)); // "J3D1"
        string animType = new string(reader.ReadChars(4)); // "tpt1"

        if (magic != "J3D1" || animType != "btp1")
            throw new InvalidDataException("Invalid BTP file");

        int fileSize = reader.ReadInt32();
        int tagCount = reader.ReadInt32();

        // Skip unused space (16 bytes)
        reader.Skip(16);
        
        long tagStart = reader.BaseStream.Position;
        string tagName = reader.ReadString(4);
        int tagSize = reader.ReadInt32();

        // Read specific TPT1 data
        LoopMode = reader.ReadByte();
        byte angleMultiplier = reader.ReadByte();       // Most likely padding
        Duration = reader.ReadInt16();

        int materialAnimationTableCount = reader.ReadInt16();
        int textureIndexTableCount = reader.ReadInt16();
        int materialAnimationTableOffset = reader.ReadInt32();
        int textureIndexTableOffset = reader.ReadInt32();
        int remapTableOffset = reader.ReadInt32();
        int nameTableOffset = reader.ReadInt32();

        // Read name table
        reader.BaseStream.Position = tagStart + nameTableOffset;
        StringTable nameTable = StringTable.FromStream(reader);

        // Reading the animation entries
        reader.BaseStream.Position = tagStart + materialAnimationTableOffset;
        for (int i = 0; i < materialAnimationTableCount; i++)
        {
            // Material Name
            string materialName = nameTable.Strings[i].String;

            int textureCount = reader.ReadInt16();
            int textureFirstIndex = reader.ReadInt16();
            int texMapIndex = reader.ReadByte();
            reader.Skip(3); // Skip padding

            // Read the texture indices
            int[] textureIndices = new int[textureCount];
            //reader.BaseStream.Seek(textureIndexTableOffset + textureFirstIndex * 2, SeekOrigin.Begin);
            for (int j = 0; j < textureCount; j++)
            {
                reader.BaseStream.Position = tagStart + materialAnimationTableOffset + textureIndexTableOffset + (textureFirstIndex + j) * 0x02;
                textureIndices[j] = reader.ReadInt16(); // Each texture index is 2 bytes
            }

            // Add the animation entry to the list
            AnimationEntries.Add(new BTPAnimationEntry
            {
                MaterialName = materialName,
                TexMapIndex = texMapIndex,
                TextureIndices = textureIndices
            });

            // Move to the next animation entry in the material animation table
            reader.BaseStream.Seek(materialAnimationTableOffset + (i + 1) * 8, SeekOrigin.Begin);
        }
    }
}

public class BTPAnimationEntry
{
    public string MaterialName { get; set; }
    public int TexMapIndex { get; set; }
    public int[] TextureIndices { get; set; }
}