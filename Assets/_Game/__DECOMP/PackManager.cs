using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public class PackManager
{
    public static List<Texture2D> GetTextures(string packFile, string modelName)
    {
        List<Texture2D> result = new List<Texture2D>();

        using (FileStream originalFileStream = new FileStream(packFile, FileMode.Open, FileAccess.Read))
        using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
        using (MemoryStream memoryStream = new MemoryStream())
        {
            decompressionStream.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            string fileName = Path.GetFileNameWithoutExtension(packFile);
            byte[] decompressedData = memoryStream.ToArray();

            using (StreamReader reader = new StreamReader(memoryStream))
            {
                var tmpk = new Tmpk(decompressedData);
                var files = tmpk.GetFiles();

                foreach (var kvp in files)
                {
                    if (kvp.Key.EndsWith("gtx"))
                    {
                        GTXFile f = new GTXFile();
                        string gtxName = Path.GetFileName(kvp.Key);

                        if (!gtxName.Replace(".bmd.gtx", "").Equals(modelName)) continue;

                        using (MemoryStream gtxStream = new MemoryStream(kvp.Value.Data))
                        {
                            f.Load(gtxStream.ToArray());

                            foreach (var t in f.textures)
                            {
                                // Get bitmap
                                Bitmap bitmap = t.GetBitmapWithChannel();

                                Texture2D tex = new Texture2D(1, 1);

                                // Bitmap top bytes
                                using (MemoryStream stream = new MemoryStream())
                                {
                                    bitmap.Save(stream, ImageFormat.Png);
                                    tex.LoadImage(stream.ToArray());
                                }

                                result.Add(tex);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    
    public static GMX GetModel(string packFile, string modelName)
    {
        using (FileStream originalFileStream = new FileStream(packFile, FileMode.Open, FileAccess.Read))
        using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
        using (MemoryStream memoryStream = new MemoryStream())
        {
            decompressionStream.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            string fileName = Path.GetFileNameWithoutExtension(packFile);
            byte[] decompressedData = memoryStream.ToArray();

            using (StreamReader reader = new StreamReader(memoryStream))
            {
                var tmpk = new Tmpk(decompressedData);
                var files = tmpk.GetFiles();

                foreach (var kvp in files)
                {
                    if (kvp.Key.EndsWith("gmx"))
                    {
                        GMX gmx = new GMX();
                        string gtxName = Path.GetFileName(kvp.Key);

                        if (!gtxName.Replace(".bmd.gmx", "").Equals(modelName)) continue;

                        using (MemoryStream gtxStream = new MemoryStream(kvp.Value.Data))
                        {
                            gmx.LoadHeader(gtxStream.ToArray());
                        }

                        return gmx;
                    }
                }
            }
        }

        return null;
    }
}