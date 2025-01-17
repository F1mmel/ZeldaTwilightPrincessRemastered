using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using GameFormatReader.Common;
using UltEvents;
using Unity.VisualScripting;
using UnityEngine;
using WiiExplorer;
using Color = UnityEngine.Color;
using Graphics = UnityEngine.Graphics;

public class TextureFetcher : MonoBehaviour
{
    public string ArchiveName;
    public string BtiName;

    public int ResizeWidth = 0;
    public int ResizeHeight = 0;

    public Texture2D Texture2D;
    public Texture2D BlurredTexture2D;
    public Sprite Sprite;

    public bool AutomaticallyAttachToUi = true;
    [Header("Transparency")]
    public bool Transparent = true;
    [Range(0, 255)] public float AlphaThreshhold = 0;

    [Header("Blur")] public bool Blur = false;
    public int Radius = 1;
    public int Iterations = 1;
    public UltEvent OnComplete;

    [Header("Material")] public Material CustomMaterial;
    
    private static Dictionary<string, Archive> LoadedArchives = new Dictionary<string, Archive>();

    public static Sprite LoadSprite(string arc, string bti)
    {
        GameObject o = new GameObject("textureFetch_dummy");
        TextureFetcher fetcher = o.AddComponent<TextureFetcher>();
        fetcher.ArchiveName = arc;
        fetcher.BtiName = bti;
        fetcher.AutomaticallyAttachToUi = false;
        UnityEngine.Sprite sprite = fetcher.LoadTexture();
        
        Destroy(o);

        return sprite;
    }

    public static Sprite LoadDialogueSprite(BMD bmd)
    {
        /*if (bmd.Dialogue3DRotation == Vector3.zero)
        {
            return null;
        }
        GameObject o = new GameObject("textureFetch_dummy");
        TextureFetcher fetcher = o.AddComponent<TextureFetcher>();
        fetcher.ArchiveName = "itemicon";
        fetcher.BtiName = bmd.SpriteTexture;
        fetcher.AutomaticallyAttachToUi = false;
        Sprite sprite = fetcher.LoadTexture();
        
        Destroy(o);

        return sprite;*/

        return null;
    }

    // Start is called before the first frame update
    void Start()
    {
        LoadTexture();

        if (AutomaticallyAttachToUi)
        {
            UnityEngine.UI.Image image = GetComponent<UnityEngine.UI.Image>();
            image.sprite = Sprite;
            
            if (CustomMaterial != null)
            {
                image.material = CustomMaterial;
                image.material.mainTexture = Sprite.texture;
                image.material.color = image.color;
            }
        }
    }

    public Sprite LoadTexture()
    {
        Archive archive = null;
        if (LoadedArchives.ContainsKey(ArchiveName)) archive = LoadedArchives[ArchiveName];
        else
        {
            archive = ArcReader.Read("Assets/GameFiles/res/Layout/" + ArchiveName + ".arc");
            LoadedArchives.Add(ArchiveName, archive);
        }
        
        byte[] btiBuffer = ArcReader.GetBuffer(archive, BtiName + ".bti");
        
        MemoryStream FS = new MemoryStream(btiBuffer);
        bool compressed = MemoryStreamEx.ReadString(FS, 4) == "Yaz0";
        FS.Close();

        if (compressed) btiBuffer = YAZ0.Decompress(btiBuffer);

        BinaryTextureImage compressedTex = new BinaryTextureImage();
        using (EndianBinaryReader reader = new EndianBinaryReader(btiBuffer, Endian.Big))
        {
            compressedTex.Load(reader, 0, 0);
        }
        
        /*Bitmap bitmap = compressedTex.CreateBitmap();
                
        MemoryStream ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        var b = new byte[ms.Length];
        ms.Position = 0;
        ms.Read(b, 0, b.Length);
        ms.Close();*/

        Texture2D = compressedTex.SkiaToTexture();
        //Texture2D = new Texture2D(1, 1);
        //Texture2D.LoadImage(b);
                
        //Texture2D = new Texture2D(1, 1);
        //Texture2D.LoadImage(bitmap.Bytes);

        if (ResizeWidth == 0) ResizeWidth = Texture2D.width;
        if (ResizeHeight == 0) ResizeHeight = Texture2D.height;

        if (Texture2D.width != ResizeWidth && Texture2D.height != ResizeHeight)
        {
            Texture2D = TextureUtils.ScaleTexture(Texture2D, ResizeWidth, ResizeHeight);
        }
        
        if (!Transparent) Texture2D = TextureUtils.MakeTransparentBlack(Texture2D, AlphaThreshhold);
        if (Blur) Texture2D = TextureUtils.Blur(Texture2D, Radius, Iterations);
        
        Sprite = UnityEngine.Sprite.Create(Texture2D, new Rect(0, 0, ResizeWidth, ResizeHeight), new Vector2(0.5f, 0.5f));

        /*GameObject o = new GameObject("BackgroundTiler");
        BackgroundTiler backgroundTiler = o.AddComponent<BackgroundTiler>();

        backgroundTiler.backgroundSprite = Sprite;
        backgroundTiler.parentCanvas = GetComponent<RectTransform>();*/
        
        if(OnComplete != null) OnComplete.Invoke();

        return Sprite;
    }
    
    public void CreateBackgroundTiler(Color c)
    {
        GameObject newBackground = new GameObject("BackgroundTiler");
        BackgroundTiler tiler = newBackground.AddComponent<BackgroundTiler>();
        tiler.backgroundSprite = Sprite;
        tiler.parentCanvas = GetComponent<RectTransform>();
        tiler.backgroundColor = c;
    }
    
    public void CreateLine(Color lineColor, int count)
    {
        LineTiler tiler = gameObject.AddComponent<LineTiler>();
        tiler.Initialize(Sprite, lineColor, count);
    }
    

}

public static class TextureUtils
{
    public static Texture2D Blur(Texture2D image, int blurSize, int iterations)
    {
        Texture2D blurred = new Texture2D(image.width, image.height, image.format, image.mipmapCount > 1);
        Graphics.CopyTexture(image, blurred);

        for (int i = 0; i < iterations; i++)
        {
            blurred = BlurImage(blurred, blurSize);
        }

        return blurred;
    }

    private static Texture2D BlurImage(Texture2D image, int blurSize)
    {
        int w = image.width;
        int h = image.height;
        Texture2D blurred = new Texture2D(w, h, image.format, image.mipmapCount > 1);

        // Apply horizontal pass
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color avgColor = Color.clear;
                float total = 0f;

                for (int i = -blurSize; i <= blurSize; i++)
                {
                    int tx = x + i;
                    if (tx >= 0 && tx < w)
                    {
                        Color pixelColor = image.GetPixel(tx, y);
                        avgColor += new Color(pixelColor.r, pixelColor.g, pixelColor.b, 0); // Only add RGB
                        total++;
                    }
                }

                avgColor /= total;
                Color originalColor = image.GetPixel(x, y);
                blurred.SetPixel(x, y, new Color(avgColor.r, avgColor.g, avgColor.b, originalColor.a)); // Preserve Alpha
            }
        }

        blurred.Apply();

        // Apply vertical pass
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Color avgColor = Color.clear;
                float total = 0f;

                for (int i = -blurSize; i <= blurSize; i++)
                {
                    int ty = y + i;
                    if (ty >= 0 && ty < h)
                    {
                        Color pixelColor = blurred.GetPixel(x, ty);
                        avgColor += new Color(pixelColor.r, pixelColor.g, pixelColor.b, 0); // Only add RGB
                        total++;
                    }
                }

                avgColor /= total;
                Color originalColor = blurred.GetPixel(x, y);
                blurred.SetPixel(x, y, new Color(avgColor.r, avgColor.g, avgColor.b, originalColor.a)); // Preserve Alpha
            }
        }

        blurred.Apply();

        return blurred;
    }

    public static Texture2D MakeTransparentBlack(Texture2D texture, float threshhold)
    {
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            if ((int) (pixels[i].a * 255) == threshhold)
            {
                pixels[i] = Color.clear;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        
        return texture;
    }

    public static Texture2D ScaleTexture(Texture2D source, int newWidth, int newHeight)
    {
        // Create a new empty texture with the desired dimensions
        Texture2D scaledTexture = new Texture2D(newWidth, newHeight, source.format, false);
        Color[] pixels = scaledTexture.GetPixels();

        // Calculate scale factor
        float scaleX = (float)source.width / newWidth;
        float scaleY = (float)source.height / newHeight;

        // Fill the pixels of the new texture with interpolated values from the original texture
        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                float gx = Mathf.Floor(x * scaleX);
                float gy = Mathf.Floor(y * scaleY);
                pixels[y * newWidth + x] = source.GetPixel((int)gx, (int)gy);
            }
        }

        // Apply the changes to the new texture
        scaledTexture.SetPixels(pixels);
        scaledTexture.Apply();

        return scaledTexture;
    }
}
