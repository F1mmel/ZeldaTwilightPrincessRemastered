using System;
using System.IO;
using System.Linq;
using System.Text;
using GameFormatReader.Common;
using UnityEngine;

public class JPACData
{
    private JPAC jpac;
    
    public JPACData(JPAC JPAC)
    {
        this.jpac = JPAC;

        foreach (JPAResourceRaw resourceRaw in jpac.Effects)
        {
            Debug.Log("PARSING: " + resourceRaw.ResourceId.ToString("X4"));
            ParseChunks(resourceRaw.Data);
        }
        
    }

    private void ParseChunks(byte[] data)
    {
        // 16 Bytes fehlen vor Data?
        Debug.LogError("SIZE: " + data.Length);
        Debug.LogError(data.ToList().PrintCollection());
        using (EndianBinaryReader reader = new EndianBinaryReader(data, Endian.Big))
        {
            uint blockCount = reader.ReadUInt16();
            uint fieldBlockCount = reader.ReadByte();
            uint keyBlockCount = reader.ReadByte();
            uint tdb1Count = reader.ReadByte();
            Debug.Log(blockCount);

            uint tableIdx = 0x08;
            for (int j = 0; j < blockCount; j++)
            {
                reader.BaseStream.Seek(tableIdx, SeekOrigin.Begin);
                string fourcc = Encoding.ASCII.GetString(reader.ReadBytes(4));
                uint blockSize = reader.ReadUInt32();

                Debug.Log(fourcc);

                tableIdx += blockSize;
            }

        }
    }
    
    public void EnsureTexture()
    {
        
    }
}