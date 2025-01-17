using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GameFormatReader.Common;
using UnityEngine;
using WiiExplorer;

public static class IntExtension
{
    public static byte GetByte(this int value, int bytePosition)
    {
        if (bytePosition < 1 || bytePosition > 4)
            throw new ArgumentOutOfRangeException(nameof(bytePosition), "Byte position must be between 1 and 4.");

        return (byte)((value >> ((4 - bytePosition) * 8)) & 0xFF);
    }
    
    public static string PrintCollection<T>(this IEnumerable<T> collection)
    {
        StringBuilder sb = new StringBuilder();

        foreach (var item in collection)
        {
            if (item is byte)
            {
                sb.Append(((byte)(object)item).ToString("X2") + " ");
            }
            else
            {
                sb.Append(item.ToString() + " ");
            }
        }

        return sb.ToString();
    }

    public static BTI ToBTI(this ArcFile file)
    {
        byte[] btiBuffer = file.Buffer;
                    
        MemoryStream FS = new MemoryStream(btiBuffer);
        bool compressed = MemoryStreamEx.ReadString(FS, 4) == "Yaz0";
        FS.Close();

        if (compressed) btiBuffer = YAZ0.Decompress(btiBuffer);

        BinaryTextureImage compressedTex = new BinaryTextureImage();
        using (EndianBinaryReader reader = new EndianBinaryReader(btiBuffer, Endian.Big))
        {
            compressedTex.Load(reader, 0, 0);
        }
        
        return new BTI(file.Name, compressedTex.SkiaToTexture(), compressedTex);
    }

    public static Sprite ToSprite(this Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

}
