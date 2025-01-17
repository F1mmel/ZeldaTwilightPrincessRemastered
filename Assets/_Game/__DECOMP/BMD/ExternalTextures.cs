using System.Collections;
using System.Collections.Generic;
using System.IO;
using GameFormatReader.Common;
using Unity.VisualScripting;
using UnityEngine;
using WiiExplorer;

public class ExternalTextures
{
    public static List<BTI> GetExternalTexturesFromStage(Archive archive)
    {
        List<BTI> BTIs = new List<BTI>();

        int imageIndex = 0;
        foreach(ArcFile file in archive.Files)
        {
            if (file.ParentDir.Equals("texc"))
            {
                //Debug.LogError("Loading external: " +file.Name);
                
                BinaryTextureImage compressedTex = new BinaryTextureImage(file.Name.Replace(".bti", ""));
                
                EndianBinaryReader reader = new EndianBinaryReader(file.Buffer, Endian.Big);
                compressedTex.Load(reader, 0);
                reader.Close();
                
                /*Bitmap bitmap = compressedTex.CreateBitmap();
                
                MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                var b = new byte[ms.Length];
                ms.Position = 0;
                ms.Read(b, 0, b.Length);
                
                Texture2D tex = new Texture2D(1, 1);
                tex.LoadImage(b);*/

                Texture2D tex = compressedTex.SkiaToTexture();

                BTI bti = new BTI(file.Name.Replace(".bti", ""), tex, compressedTex);
                BTIs.Add(bti);

                imageIndex++;
            }
        }

        return BTIs;
    }
}
