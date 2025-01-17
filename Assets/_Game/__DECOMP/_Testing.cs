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
    
    // Start is called before the first frame update
    void Start()
    {
        //ProcessPack("B_ds");
        ProcessPack("Alink");
        
        /*GTXFile f = new GTXFile();
        f.Load(@"G:\cemu_1.23.1\cemu_1.23.1\mlc01\usr\title\00050000\1019e600\content\res\Object\@bg000b.pack\model0.bmd.gtx");
        
        GTXFile.TextureData data = f.textures[0];
        
        /*Bitmap bmp = new Bitmap((int)data.surface.width, (int)data.surface.height);
        Rectangle rect = new Rectangle(0, 0, (int)data.surface.width, (int)data.surface.height);
        BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);*/

        /*Bitmap bmp = f.textures[0].GetBitmap(0, 0);
        
        MemoryStream ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        var b = new byte[ms.Length];
        ms.Position = 0;
        ms.Read(b, 0, b.Length);
        ms.Close();
                    
        Texture2D = new Texture2D(1, 1);
        Texture2D.LoadImage(b);*/
        
        return;
        /*string parentFolder = @"G:\cemu_1.23.1\cemu_1.23.1\mlc01\usr\title\00050000\1019e600\content\res\Object\";
        string fullPath = Path.Combine(parentFolder, packFile);

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
                        using (MemoryStream gtxStream = new MemoryStream(kvp.Value.Data))
                        {
                            using (BinaryReader r = new BinaryReader(gtxStream))
                            {
                                var magic = r.ReadBytes(4);
                                if (!magic.SequenceEqual(Encoding.ASCII.GetBytes("Gfx2")))
                                {
                                    throw new InvalidDataException($"Invalid magic: {Encoding.ASCII.GetString(magic)} (expected 'Gfx2')");
                                }
                
                                gtxStream.Seek(0, SeekOrigin.Begin);
                                byte[] fileData = r.ReadBytes((int)gtxStream.Length);
                                Texture2D = ProcessGTXFile(fileData);
                            }
                        }
                    }
                }
            }
        }*/

        //Texture2D = ProcessGTXFile(@"G:\cemu_1.23.1\cemu_1.23.1\mlc01\usr\title\00050000\1019e600\content\res\Object\@bg000b.pack\model0.bmd.gtx");
        
        //GTXFile f = new GTXFile();
        //f.Load(@"G:\cemu_1.23.1\cemu_1.23.1\mlc01\usr\title\00050000\1019e600\content\res\Object\@bg000b.pack\model0.bmd.gtx");
        //f.textures[0].SaveBitmap(@"G:\cemu_1.23.1\cemu_1.23.1\mlc01\usr\title\00050000\1019e600\content\res\Object\@bg000b.pack\model0.bmd.gtx____________.png");

        /*GTXFile.TextureData data = f.textures[0];
        
        Bitmap bmp = new Bitmap((int)data.surface.width, (int)data.surface.height);
        Rectangle rect = new Rectangle(0, 0, (int)data.surface.width, (int)data.surface.height);
        BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);*/

        /*Bitmap bmp = f.textures[0].GetBitmap(0, 0);
        
        //Lock the bitmap for writing, copy the bits and then unlock for saving.
        /*IntPtr ptr = bmpData.Scan0;
        byte[] imageData = data.surface.data;
        Marshal.Copy(imageData, 0, ptr, imageData.Length);
        bmp.UnlockBits(bmpData);*/
        
        /*MemoryStream ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        var b = new byte[ms.Length];
        ms.Position = 0;
        ms.Read(b, 0, b.Length);
        ms.Close();
                    
        Texture2D = new Texture2D(1, 1);
        Texture2D.LoadImage(b);*/
    }

    // Update is called once per frame
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