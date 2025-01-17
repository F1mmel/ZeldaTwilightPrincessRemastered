using System.IO;
using GameFormatReader.Common;
using UnityEngine;

public class GTXLoader : MonoBehaviour
{
    public string filePath = @"G:\cemu_1.23.1\cemu_1.23.1\mlc01\usr\title\00050000\1019e600\content\res\Object\@bg000b.pack\model0.bmd.gtx";
    public Texture2D loadedTexture;

    void Start()
    {
        LoadGTX(filePath);
    }

    void LoadGTX(string path)
    {
        if (File.Exists(path))
        {
            byte[] fileData = File.ReadAllBytes(path);

            using (EndianBinaryReader reader = new EndianBinaryReader(new MemoryStream(fileData), Endian.Big))
            {
                string magicNumber = new string(reader.ReadChars(4));
                if (magicNumber != "Gfx2")
                {
                    Debug.LogError("Invalid GTX file: " + magicNumber);
                    return;
                }

                ushort endianness = reader.ReadUInt16();

                ushort version = reader.ReadUInt16();

                uint fileSize = reader.ReadUInt32();

                uint headerSize = reader.ReadUInt32();

                uint textureCount = reader.ReadUInt32();
                Debug.LogError(textureCount);

                uint firstTextureOffset = reader.ReadUInt32();

                reader.BaseStream.Seek(firstTextureOffset, SeekOrigin.Begin);

                LoadTexture(reader);
            }
        }
        else
        {
            Debug.LogError("File not found: " + path);
        }
    }

    void LoadTexture(EndianBinaryReader reader)
    {
        uint format = reader.ReadUInt16();
        Debug.LogWarning(format);

        ushort width = reader.ReadUInt16();
        ushort height = reader.ReadUInt16();
        
        Debug.LogWarning(width+"x" + height);

        byte mipmaps = reader.ReadByte();

        reader.BaseStream.Seek(3, SeekOrigin.Current);

        uint textureDataOffset = reader.ReadUInt32();

        long currentPos = reader.BaseStream.Position;

        reader.BaseStream.Seek(textureDataOffset, SeekOrigin.Begin);

        byte[] textureData = reader.ReadBytes((int)(reader.BaseStream.Length - textureDataOffset));

        loadedTexture = new Texture2D(width, height);
        if (loadedTexture.LoadImage(textureData))
        {
            Debug.Log("Texture loaded successfully");
        }
        else
        {
            Debug.LogError("Failed to load texture");
        }

        reader.BaseStream.Seek(currentPos, SeekOrigin.Begin);
    }
}
