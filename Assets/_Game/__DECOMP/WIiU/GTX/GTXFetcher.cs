using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

public class GTXFetcher
{
    private static string objectPath = "/GameFiles_HD/res/Object/";
    private static string stagePath = "/GameFiles_HD/res/Stage/";

    public static List<BTI> LoadHDTextures(string arcName, bool isModel)
    {
        List<BTI> hd = new List<BTI>();

        string file = "";
        if (isModel)
        {
            file = Path.Combine(Application.dataPath + objectPath, arcName.Replace(".arc", "") + ".pack.gz");
        }
        else
        {
            file = Path.Combine(Application.dataPath + stagePath, StageLoader.Instance.StageName + "/" + arcName.Replace(".arc", "") + ".pack.gz");
        }

        if (file.Contains("STG_00.pack.gz"))
        {
            file = Path.Combine(Application.dataPath + stagePath, StageLoader.Instance.StageName + "/" + arcName.Replace(".arc", "") + ".pack.gz");
        }

            using (FileStream originalFileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
            using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                decompressionStream.CopyTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                string fileName = Path.GetFileNameWithoutExtension(file);
                byte[] decompressedData = memoryStream.ToArray();

                using (StreamReader reader = new StreamReader(memoryStream))
                {
                    var tmpk = new Tmpk(decompressedData);
                    var files = tmpk.GetFiles();

                    foreach (var kvp in files)
                    {
                        //Console.WriteLine($"File: {kvp.Key}, Offset: {kvp.Value.Offset}, Size: {kvp.Value.Data.Length}");

                        // Texture
                        if (kvp.Key.EndsWith("gtx"))
                        {
                            GTXFile f = new GTXFile();

                            using (MemoryStream gtxStream = new MemoryStream(kvp.Value.Data))
                            {
                                f.Load(gtxStream.ToArray());
                                string gtxName = Path.GetFileName(kvp.Key);

                                foreach (var t in f.textures)
                                {
                                    Bitmap bitmap = t.GetBitmapWithChannel();

                                    //bitmap.Save(@"C:\Users\finne\Desktop\_Testing\HD\" + f.textures.IndexOf(t) + ".png");

                                    MemoryStream ms = new MemoryStream();
                                    bitmap.Save(ms, ImageFormat.Png);
                                    var b = new byte[ms.Length];
                                    ms.Position = 0;
                                    ms.Read(b, 0, b.Length);
                                    ms.Close();

                                    Texture2D tex = new Texture2D(1, 1);
                                    tex.LoadImage(b);

                                    hd.Add(new BTI(gtxName, tex, null));
                                }
                            }

                            //return hd;
                        }
                    }
                }
            }

        return hd;
    }

    public static Dictionary<string, byte[]> TextureDatas = new Dictionary<string, byte[]>();

    public static IEnumerator LoadBinFile()
    {
        // Relative
        string filePath = @"C:\Users\finne\RiderProjects\GTXExtractor\GTXExtractor\bin\Debug\net8.0\ZeldaFiles\Stages\D_MN10.bin";
        
        using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
        {
            int fileCount = reader.ReadInt32();
            List<TextureData> textures = new List<TextureData>();

            for (int i = 0; i < fileCount; i++)
            {
                string fileName = reader.ReadString();
                int dataLength = reader.ReadInt32();
                byte[] fileData = reader.ReadBytes(dataLength);

                //Texture2D texture = new Texture2D(2, 2);
                //texture.LoadImage(fileData);
                
                Debug.LogError(fileName);
                TextureDatas.Add(fileName, fileData);
                
                //Texture2D.Destroy(texture);
                
                yield return null;

                //textures.Add(new TextureData { Name = fileName, Texture = texture });
            }
        }
    }
}