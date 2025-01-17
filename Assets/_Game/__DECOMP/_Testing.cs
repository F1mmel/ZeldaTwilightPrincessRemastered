using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class _Testing : MonoBehaviour
{
    private string processorPath = @"C:\Users\FinnsPC\source\repos\ZeldaTPTesting\bin\Debug\net8.0\ZeldaTPTesting.exe";

    public string packFile = "";

    public List<GTX> GTX = new List<GTX>();

    private void ProcessPack(string pack)
    {
        string parentFolder = @"G:\cemu_1.23.1\cemu_1.23.1\mlc01\usr\title\00050000\1019e600\content\res\Object\";
        string fullPath = Path.Combine(parentFolder, pack + ".pack.gz");

        using (FileStream originalFileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
        using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
        using (MemoryStream memoryStream = new MemoryStream())
        {
            decompressionStream.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            string fileName = Path.GetFileNameWithoutExtension(fullPath);
            byte[] decompressedData = memoryStream.ToArray();

            using (StreamReader reader = new StreamReader(memoryStream))
            {
                string content = reader.ReadToEnd();

                var tmpk = new Tmpk(decompressedData);
                var files = tmpk.GetFiles();

                foreach (var kvp in files)
                {
                    if (kvp.Key.EndsWith("gtx"))
                    {
                        string gtxName = Path.GetFileName(kvp.Key);
                        Debug.LogWarning(gtxName);
                        
                        GTXFile f = new GTXFile();
                        
                        global::GTX g = new GTX();
                        g.FileName = gtxName;
                        GTX.Add(g);
                        
                        using (MemoryStream gtxStream = new MemoryStream(kvp.Value.Data))
                        {
                            f.Load(gtxStream.ToArray());

                            foreach (var t in f.textures)
                            {
                                Bitmap bmp = t.GetBitmapWithChannel();
        
                                MemoryStream ms = new MemoryStream();
                                bmp.Save(ms, ImageFormat.Png);
                                var b = new byte[ms.Length];
                                ms.Position = 0;
                                ms.Read(b, 0, b.Length);
                                ms.Close();
                    
                                Texture2D Texture2D = new Texture2D(1, 1);
                                Texture2D.LoadImage(b);
                                
                                g.Images.Add(new GTXImage()
                                {
                                    Index = f.textures.IndexOf(t),
                                    Texture = Texture2D
                                });
                            }
                        }
                    }
                }
            }
        }
    }
    
    void Start()
    {
        ProcessPack("Alink");
        

        
        return;

        


        
    }

    void Update()
    {
        
    }
}

[Serializable]
public class GTX
{
    public string FileName;
    public List<GTXImage> Images = new List<GTXImage>();
}

[Serializable]
public class GTXImage
{
    public int Index;
    public Texture2D Texture;
}