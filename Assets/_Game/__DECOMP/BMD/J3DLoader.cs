using System.Collections;
using System.Collections.Generic;
using System.IO;
using GameFormatReader.Common;
using UnityEngine;

public class J3DLoader
{
    public string Magic { get; set; }
    public int Size { get; set; }
    public int Offset { get; set; }
    public int NumChunks { get; private set; }
    public string Subversion { get; private set; }

    public byte[] Buffer { get; private set; }

    public EndianBinaryReader Reader;

    public J3DLoader(byte[] buffer)
    {
        Buffer = buffer;

        using (EndianBinaryReader reader = new EndianBinaryReader(buffer, Endian.Big))
        { 
            // Read the J3D Header
            Magic = new string(reader.ReadChars(4));
            Magic = new string(reader.ReadChars(4));
            Size = reader.ReadInt32();
            NumChunks = reader.ReadInt32();

            // Skip over an unused tag ("SVR3") which is consistent in all models.
            reader.Skip(16);

            Reader = reader;
        }
    }
}
