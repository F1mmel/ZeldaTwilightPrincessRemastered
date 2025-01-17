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
                // Read and verify the Magic Number
                string magicNumber = new string(reader.ReadChars(4));
                if (magicNumber != "Gfx2")
                {
                    Debug.LogError("Invalid GTX file: " + magicNumber);
                    return;
                }

                // Read the Endianness (not used in this example)
                ushort endianness = reader.ReadUInt16();

                // Read the version
                ushort version = reader.ReadUInt16();

                // Read the file size
                uint fileSize = reader.ReadUInt32();

                // Read the header size
                uint headerSize = reader.ReadUInt32();

                // Read the number of textures
                uint textureCount = reader.ReadUInt32();
                Debug.LogError(textureCount);

                // Read the offset to the first texture description
                uint firstTextureOffset = reader.ReadUInt32();

                // Move to the first texture description
                reader.BaseStream.Seek(firstTextureOffset, SeekOrigin.Begin);

                // Load the first texture
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
        // Read the texture format
        uint format = reader.ReadUInt16();
        Debug.LogWarning(format);

        // Read the texture width and height
        ushort width = reader.ReadUInt16();
        ushort height = reader.ReadUInt16();
        
        Debug.LogWarning(width+"x" + height);

        // Read the number of mipmaps
        byte mipmaps = reader.ReadByte();

        // Skip the reserved bytes
        reader.BaseStream.Seek(3, SeekOrigin.Current);

        // Read the offset to the texture data
        uint textureDataOffset = reader.ReadUInt32();

        // Save the current position
        long currentPos = reader.BaseStream.Position;

        // Move to the texture data offset
        reader.BaseStream.Seek(textureDataOffset, SeekOrigin.Begin);

        // Read the texture data
        byte[] textureData = reader.ReadBytes((int)(reader.BaseStream.Length - textureDataOffset));

        // Create the Texture2D
        loadedTexture = new Texture2D(width, height);
        if (loadedTexture.LoadImage(textureData))
        {
            Debug.Log("Texture loaded successfully");
        }
        else
        {
            Debug.LogError("Failed to load texture");
        }

        // Restore the position
        reader.BaseStream.Seek(currentPos, SeekOrigin.Begin);
    }
}
