﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using GameFormatReader.Common;
using Newtonsoft.Json;
using UnityEngine;

    /// <summary>
    /// The BinaryTextureImage (or BTI) format is used by Wind Waker (and several other Nintendo
    /// games) to store texture images. There are a variety of encoding methods, some of which
    /// are supported right now for decoding. This does not currently support encoding BTI files
    /// but will at some point in time. It does not load mipmaps from the file currently.
    /// 
    /// Image data can be retrieved by calling GetData() which will return an ARGB array of bytes
    /// containing the information. For files without alpha data their values will be set to 0xFF.
    /// 
    /// BTI files are stored both individually on disk and embedded within other file formats. 
    /// </summary>
    public class BinaryTextureImage
    {
        #region Data Types
        /// <summary>
        /// ImageFormat specifies how the data within the image is encoded.
        /// Included is a chart of how many bits per pixel there are, 
        /// the width/height of each block, how many bytes long the
        /// actual block is, and a description of the type of data stored.
        /// </summary>
        public enum TextureFormats
        {
            //Bits per Pixel | Block Width | Block Height | Block Size | Type / Description
            I4 = 0x00,      //  4 | 8 | 8 | 32 | grey
            I8 = 0x01,      //  8 | 8 | 8 | 32 | grey
            IA4 = 0x02,     //  8 | 8 | 4 | 32 | grey + alpha
            IA8 = 0x03,     // 16 | 4 | 4 | 32 | grey + alpha
            RGB565 = 0x04,  // 16 | 4 | 4 | 32 | color
            RGB5A3 = 0x05,  // 16 | 4 | 4 | 32 | color + alpha
            RGBA32 = 0x06,  // 32 | 4 | 4 | 64 | color + alpha
            C4 = 0x08,      //  4 | 8 | 8 | 32 | palette choices (IA8, RGB565, RGB5A3)
            C8 = 0x09,      //  8 | 8 | 4 | 32 | palette choices (IA8, RGB565, RGB5A3)
            C14X2 = 0x0a,   // 16 | 4 | 4 | 32 | palette (IA8, RGB565, RGB5A3) NOTE: only 14 bits are used per pixel
            CMPR = 0x0e,    //  4 | 8 | 8 | 32 | mini palettes in each block, RGB565 or transparent.
        }

        /// <summary>
        /// Defines how textures handle going out of [0..1] range for texcoords.
        /// </summary>
        public enum WrapModes
        {
            ClampToEdge = 0,
            Repeat = 1,
            MirroredRepeat = 2,
        }

        /// <summary>
        /// PaletteFormat specifies how the data within the palette is stored. An
        /// image uses a single palette (except CMPR which defines its own
        /// mini-palettes within the Image data). Only C4, C8, and C14X2 use
        /// palettes. For all other formats the type and count is zero.
        /// </summary>
        public enum PaletteFormats
        {
            IA8 = 0x00,
            RGB565 = 0x01,
            RGB5A3 = 0x02,
        }

        /// <summary>
        /// FilterMode specifies what type of filtering the file should use for min/mag.
        /// </summary>
        public enum FilterMode
        {
            /* Valid in both Min and Mag Filter */
            Nearest = 0x0,                  // Point Sampling, No Mipmap
            Linear = 0x1,                   // Bilinear Filtering, No Mipmap

            /* Valid in only Min Filter */
            NearestMipmapNearest = 0x2,     // Point Sampling, Discrete Mipmap
            NearestMipmapLinear = 0x3,      // Bilinear Filtering, Discrete Mipmap
            LinearMipmapNearest = 0x4,      // Point Sampling, Linear MipMap
            LinearMipmapLinear = 0x5,       // Trilinear Filtering
        }

        /// <summary>
        /// The Palette simply stores the color data as loaded from the file.
        /// It does not convert the files based on the Palette type to RGBA8.
        /// </summary>
        private sealed class Palette
        {
            private byte[] _paletteData;

            public void Load(EndianBinaryReader reader, uint paletteEntryCount)
            {
                //Files that don't have palettes have an entry count of zero.
                if (paletteEntryCount == 0)
                {
                    _paletteData = new byte[0];
                    return;
                }

                //All palette formats are 2 bytes per entry.
                _paletteData = reader.ReadBytes((int)paletteEntryCount * 2);
            }

            public byte[] GetBytes()
            {
                return _paletteData;
            }
        }
        #endregion

        public string Name { get; set; }
        public TextureFormats Format { get; set; }
        public byte AlphaSetting { get; set; } // 0 for no alpha, 0x02 and other values seem to indicate yes alpha.

        [JsonIgnore]
        public ushort Width { get; private set; }

        [JsonIgnore]
        public ushort Height { get; private set; }

        public WrapModes WrapS { get; set; }
        public WrapModes WrapT { get; set; }

        [JsonIgnore]
        public bool PalettesEnabled { get; set; }

        public PaletteFormats PaletteFormat { get; set; }

        [JsonIgnore]
        public ushort PaletteCount { get; set; }

        [JsonIgnore]
        public int EmbeddedPaletteOffset { get; private set; } // This is a guess. It seems to be 0 in most things, but it fits with min/mag filters.

        public FilterMode MinFilter { get; set; }
        public FilterMode MagFilter { get; set; }
        public sbyte MinLOD { get; set; } // Fixed point number, 1/8 = conversion (ToDo: is this multiply by 8 or divide...)
        public sbyte MagLOD { get; set; } // Fixed point number, 1/8 = conversion (ToDo: is this multiply by 8 or divide...)
        [JsonIgnore]
        public byte MipMapCount { get; private set; }
        public short LodBias { get; set; } // Fixed point number, 1/100 = conversion

        [JsonIgnore]
        private Palette m_imagePalette;
        [JsonIgnore]
        private byte[] m_rgbaImageData;
        [JsonIgnore]
        public byte[] RGBAImageData
        {
            get { return m_rgbaImageData; }
            set { m_rgbaImageData = value; }
        }
        
        public short unknown2 = 0;
        public byte unknown3 = 0;

        public BinaryTextureImage()
        {
            MipMapCount = 1;
        }

        public BinaryTextureImage(string name)
        {
            Name = name;
        }

        // headerStart seems to be chunkStart + 0x20 and I don't know why.
        /// <summary>
        /// Load a BinaryTextureImage from a stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="headerStart"></param>
        /// <param name="imageIndex">Optional additional offset used by J3D models. Multiplied by 0x20.</param>
        public void Load(EndianBinaryReader stream, long headerStart, int imageIndex = 0)
        {
            Format = (TextureFormats)stream.ReadByte();
            AlphaSetting = stream.ReadByte();
            Width = stream.ReadUInt16();
            Height = stream.ReadUInt16();
            WrapS = (WrapModes)stream.ReadByte();
            WrapT = (WrapModes)stream.ReadByte();
            PalettesEnabled = Convert.ToBoolean(stream.ReadByte());
            PaletteFormat = (PaletteFormats)stream.ReadByte();
            PaletteCount = stream.ReadUInt16();
            int paletteDataOffset = stream.ReadInt32();
            EmbeddedPaletteOffset = stream.ReadInt32();
            MinFilter = (FilterMode)stream.ReadByte();
            MagFilter = (FilterMode)stream.ReadByte();
            unknown2 = stream.ReadInt16();
            MipMapCount = stream.ReadByte();
            unknown3 = stream.ReadByte();
            LodBias = stream.ReadInt16();
            
            /*Debug.Log("PalettesEnabled:" + PalettesEnabled);
            Debug.Log("PaletteCount:" + PaletteCount);
            Debug.Log("EmbeddedPaletteOffset:" + EmbeddedPaletteOffset);
            Debug.Log("MinFilter:" + MinFilter);
            Debug.Log("MagFilter:" + MagFilter);
            Debug.Log("unknown2:" + unknown2);
            Debug.Log("MipMapCount:" + MipMapCount);
            Debug.Log("unknown3:" + unknown3);
            Debug.Log("LodBias:" + LodBias);*/

            int imageDataOffset = stream.ReadInt32();

            // Load the Palette data 
            stream.BaseStream.Position = headerStart + paletteDataOffset + (0x20 * imageIndex);
            m_imagePalette = new Palette();
            m_imagePalette.Load(stream, PaletteCount);

            // Now load and decode image data into an ARGB array.
            stream.BaseStream.Position = headerStart + imageDataOffset + (0x20 * imageIndex);
            m_rgbaImageData = DecodeData(stream, Width, Height, Format, m_imagePalette, PaletteFormat);
        }

        public void ReplaceHeaderInfo(BinaryTextureImage other) {
            Format = other.Format;
            AlphaSetting = other.AlphaSetting;
            WrapS = other.WrapS;
            WrapT = other.WrapT;
            MinFilter = other.MinFilter;
            MagFilter = other.MagFilter;
            MinLOD = other.MinLOD;
            MagLOD = other.MagLOD;
            LodBias = other.LodBias;
            unknown2 = other.unknown2;
            unknown3 = other.unknown3;
        }

        // We analyze the image data and check 
        public void DetectAndSetFittingFormat()
        {
            bool is_gray = true;
            bool complex_alpha = false;
            bool has_alpha = false;

            List<byte> alphavals = new List<byte>();

            for (int i = 0; i < m_rgbaImageData.Length / 4; i++)
            {
                byte r = m_rgbaImageData[i * 4 + 0];
                byte g = m_rgbaImageData[i * 4 + 1];
                byte b = m_rgbaImageData[i * 4 + 2];
                byte a = m_rgbaImageData[i * 4 + 3];

                if (is_gray && (r != g || g != b || b != r)) {
                    is_gray = false;
                }

                if (a != 255) {
                    has_alpha = true;
                    if (a != 0) {
                        complex_alpha = true;
                    }
                }

            }
            
            if (is_gray) {
                Format = TextureFormats.IA8;
            }
            else if (complex_alpha) {
                Format = TextureFormats.RGB5A3;
            }
            else {
                Format = TextureFormats.CMPR;
            }
            if (has_alpha) {
                AlphaSetting = 0x1;
            }
        }

        public void SaveImageToDisk(string outputFile)
        {
            /*using (Bitmap bmp = CreateBitmap())
            {
                // Bitmaps will throw an exception if the output folder doesn't exist so...
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                bmp.Save(outputFile);
            }*/
            
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
            File.WriteAllBytes(outputFile, SkiaToTexture().EncodeToPNG());
        }

        /*public void SaveImageToDisk(string outputFile, byte[] imageData, int width, int height)
        {
            using (Bitmap bmp = new Bitmap(width, height))
            {
                Rectangle rect = new Rectangle(0, 0, width, height);
                BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                //Lock the bitmap for writing, copy the bits and then unlock for saving.
                IntPtr ptr = bmpData.Scan0;
                Marshal.Copy(imageData, 0, ptr, imageData.Length);
                bmp.UnlockBits(bmpData);

                // Bitmaps will throw an exception if the output folder doesn't exist so...
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                bmp.Save(outputFile);
            }
        }*/

        /*public Bitmap CreateBitmap()
        {
            Bitmap bmp = new Bitmap(Width, Height);
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            //Lock the bitmap for writing, copy the bits and then unlock for saving.
            IntPtr ptr = bmpData.Scan0;
            byte[] imageData = m_rgbaImageData;
            Marshal.Copy(imageData, 0, ptr, imageData.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }*/
        
        /*public Texture2D SkiaToTexture()
        {
            Texture2D tex = new Texture2D(Width, Height, BMD.Instance.TextureFormatTag, false);
            tex.LoadRawTextureData(RGBAImageData);
            tex.Apply();
            return tex;
        }*/
        
        public Texture2D SkiaToTexture()
        {
            // Erstelle ein neues Array für die umgeordneten Daten
            /*byte[] rgbaImageData = new byte[RGBAImageData.Length];

            // BGRA nach RGBA konvertieren
            for (int i = 0; i < RGBAImageData.Length; i += 4)
            {
                rgbaImageData[i] = RGBAImageData[i + 2];     // R
                rgbaImageData[i + 1] = RGBAImageData[i + 1]; // G
                rgbaImageData[i + 2] = RGBAImageData[i];     // B
                rgbaImageData[i + 3] = RGBAImageData[i + 3]; // A
            }

            // Erstelle die Texture2D und lade die konvertierten Daten
            Texture2D tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            tex.LoadRawTextureData(rgbaImageData);
            tex.Apply();*/
            
            // Lade direkt in BGRA-Format
            Texture2D tex = new Texture2D(Width, Height, TextureFormat.BGRA32, false);
            tex.LoadRawTextureData(RGBAImageData);
            tex.Apply();

            return tex;
        }


        /// <summary>
        /// Loads image data from disk into a byte array.
        /// </summary>
        /*public void LoadImageFromDisk(string filePath)
        {
            Bitmap bmp = new Bitmap(filePath);
            byte[] data = new byte[bmp.Width * bmp.Height * 4];

            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(bmpData.Scan0, data, 0, data.Length);
            bmp.UnlockBits(bmpData);

            m_rgbaImageData = data;

            Width = (ushort)bmp.Width;
            Height = (ushort)bmp.Height;
        }*/

        public void WriteHeader(EndianBinaryWriter writer)
        {
            writer.Write((byte)Format);
            writer.Write(AlphaSetting);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write((byte)WrapS);
            writer.Write((byte)WrapT);

            writer.Write(Convert.ToByte(PalettesEnabled));

            writer.Write((byte)PaletteFormat);
            writer.Write((short)PaletteCount);

            // This is a placeholder for PaletteDataOffset
            writer.Write((int)0);

            writer.Write(EmbeddedPaletteOffset);

            writer.Write((byte)MinFilter);
            writer.Write((byte)MagFilter);

            // This is an unknown
            writer.Write((short)unknown2);

            writer.Write((byte)MipMapCount);

            // This is an unknown
            writer.Write((byte)unknown3);

            writer.Write((short)LodBias);

            // This is a placeholder for ImageDataOffset
            writer.Write((int)0);
        }

        #region Decoding
        private static byte[] DecodeData(EndianBinaryReader stream, uint width, uint height, TextureFormats format, Palette imagePalette, PaletteFormats paletteFormat)
        {
            switch (format)
            {
                case TextureFormats.I4:
                    return DecodeI4(stream, width, height);
                case TextureFormats.I8:
                    return DecodeI8(stream, width, height);
                case TextureFormats.IA4:
                    return DecodeIA4(stream, width, height);
                case TextureFormats.IA8:
                    return DecodeIA8(stream, width, height);
                case TextureFormats.RGB565:
                    return DecodeRgb565(stream, width, height);
                case TextureFormats.RGB5A3:
                    return DecodeRgb5A3(stream, width, height);
                case TextureFormats.RGBA32:
                    return DecodeRgba32(stream, width, height);
                case TextureFormats.C4:
                    return DecodeC4(stream, width, height, imagePalette, paletteFormat);
                case TextureFormats.C8:
                    return DecodeC8(stream, width, height, imagePalette, paletteFormat);
                case TextureFormats.CMPR:
                    return DecodeCmpr(stream, width, height);
                case TextureFormats.C14X2:
                default:
                    Console.WriteLine("Unsupported Binary Texture Image format {0}, unable to decode!", format);
                    return new byte[0];
            }
        }
        // For font
        public static byte[] DecodeData(EndianBinaryReader stream, uint width, uint height, TextureFormats format)
        {
            switch (format)
            {
                case TextureFormats.I4:
                    return DecodeI4(stream, width, height);
                case TextureFormats.I8:
                    return DecodeI8(stream, width, height);
                case TextureFormats.IA4:
                    return DecodeIA4(stream, width, height);
                case TextureFormats.IA8:
                    return DecodeIA8(stream, width, height);
                case TextureFormats.RGB565:
                    return DecodeRgb565(stream, width, height);
                case TextureFormats.RGB5A3:
                    return DecodeRgb5A3(stream, width, height);
                case TextureFormats.RGBA32:
                    return DecodeRgba32(stream, width, height);
                //case TextureFormats.C4:
                    //return DecodeC4(stream, width, height, imagePalette, paletteFormat);
                //case TextureFormats.C8:
                    //return DecodeC8(stream, width, height, imagePalette, paletteFormat);
                case TextureFormats.CMPR:
                    return DecodeCmpr(stream, width, height);
                case TextureFormats.C14X2:
                default:
                    Console.WriteLine("Unsupported Binary Texture Image format {0}, unable to decode!", format);
                    return new byte[0];
            }
        }

        private static byte[] DecodeRgba32(EndianBinaryReader stream, uint width, uint height)
        {
            uint numBlocksW = (width + 3) / 4; //4 byte block width
            uint numBlocksH = (height + 3) / 4; //4 byte block height 

            byte[] decodedData = new byte[width * height * 4];

            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    //For each block, we're going to examine block width / block height number of 'pixels'
                    for (int pY = 0; pY < 4; pY++)
                    {
                        for (int pX = 0; pX < 4; pX++)
                        {
                            //Ensure the pixel we're checking is within bounds of the image.
                            if ((xBlock * 4 + pX >= width) || (yBlock * 4 + pY >= height))
                            {
                                stream.SkipByte();
                                stream.SkipByte();
                                continue;
                            }

                            //Now we're looping through each pixel in a block, but a pixel is four bytes long. 
                            uint destIndex = (uint)(4 * (width * ((yBlock * 4) + pY) + (xBlock * 4) + pX));
                            decodedData[destIndex + 3] = stream.ReadByte(); //Alpha
                            decodedData[destIndex + 2] = stream.ReadByte(); //Red
                        }
                    }

                    //...but we have to do it twice, because RGBA32 stores two sub-blocks per block. (AR, and GB)
                    for (int pY = 0; pY < 4; pY++)
                    {
                        for (int pX = 0; pX < 4; pX++)
                        {
                            //Ensure the pixel we're checking is within bounds of the image.
                            if ((xBlock * 4 + pX >= width) || (yBlock * 4 + pY >= height))
                            {
                                stream.SkipByte();
                                stream.SkipByte();
                                continue;
                            }

                            //Now we're looping through each pixel in a block, but a pixel is four bytes long. 
                            uint destIndex = (uint)(4 * (width * ((yBlock * 4) + pY) + (xBlock * 4) + pX));
                            decodedData[destIndex + 1] = stream.ReadByte(); //Green
                            decodedData[destIndex + 0] = stream.ReadByte(); //Blue
                        }
                    }

                }
            }

            return decodedData;
        }

        private static byte[] DecodeC4(EndianBinaryReader stream, uint width, uint height, Palette imagePalette, PaletteFormats paletteFormat)
        {
            //4 bpp, 8 block width/height, block size 32 bytes, possible palettes (IA8, RGB565, RGB5A3)
            uint numBlocksW = (width + 7) / 8;
            uint numBlocksH = (height + 7) / 8;

            byte[] decodedData = new byte[width * height * 8];

            //Read the indexes from the file
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    //Inner Loop for pixels
                    for (int pY = 0; pY < 8; pY++)
                    {
                        for (int pX = 0; pX < 8; pX += 2)
                        {
                            //Ensure we're not reading past the end of the image.
                            if ((xBlock * 8 + pX >= width) || (yBlock * 8 + pY >= height))
                            {
                                stream.SkipByte();
                                continue;
                            }

                            byte data = stream.ReadByte();
                            byte t = (byte)(data & 0xF0);
                            byte t2 = (byte)(data & 0x0F);

                            decodedData[width * ((yBlock * 8) + pY) + (xBlock * 8) + pX + 0] = (byte)(t >> 4);
                            decodedData[width * ((yBlock * 8) + pY) + (xBlock * 8) + pX + 1] = t2;
                        }
                    }
                }
            }

            //Now look them up in the palette and turn them into actual colors.
            byte[] finalDest = new byte[decodedData.Length / 2];

            int pixelSize = paletteFormat == PaletteFormats.IA8 ? 2 : 4;
            int destOffset = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    UnpackPixelFromPalette(decodedData[y * width + x], ref finalDest, destOffset, imagePalette.GetBytes(), paletteFormat);
                    destOffset += pixelSize;
                }
            }

            return finalDest;
        }

        private static byte[] DecodeC8(EndianBinaryReader stream, uint width, uint height, Palette imagePalette, PaletteFormats paletteFormat)
        {
            //4 bpp, 8 block width/4 block height, block size 32 bytes, possible palettes (IA8, RGB565, RGB5A3)
            uint numBlocksW = (width + 7) / 8;
            uint numBlocksH = (height + 3) / 4;

            byte[] decodedData = new byte[width * height * 8];

            //Read the indexes from the file
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    //Inner Loop for pixels
                    for (int pY = 0; pY < 4; pY++)
                    {
                        for (int pX = 0; pX < 8; pX++)
                        {
                            //Ensure we're not reading past the end of the image.
                            if ((xBlock * 8 + pX >= width) || (yBlock * 4 + pY >= height))
                            {
                                stream.SkipByte();
                                continue;
                            }

                            byte data = stream.ReadByte();
                            decodedData[width * ((yBlock * 4) + pY) + (xBlock * 8) + pX] = data;
                        }
                    }
                }
            }

            //Now look them up in the palette and turn them into actual colors.
            byte[] finalDest = new byte[decodedData.Length / 2];

            int pixelSize = paletteFormat == PaletteFormats.IA8 ? 2 : 4;
            int destOffset = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    UnpackPixelFromPalette(decodedData[y * width + x], ref finalDest, destOffset, imagePalette.GetBytes(), paletteFormat);
                    destOffset += pixelSize;
                }
            }

            return finalDest;
        }

        private static byte[] DecodeRgb565(EndianBinaryReader stream, uint width, uint height)
        {
            //16 bpp, 4 block width/height, block size 32 bytes, color.
            uint numBlocksW = (width + 3) / 4;
            uint numBlocksH = (height + 3) / 4;

            byte[] decodedData = new byte[width * height * 4];

            //Read the indexes from the file
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    //Inner Loop for pixels
                    for (int pY = 0; pY < 4; pY++)
                    {
                        for (int pX = 0; pX < 4; pX++)
                        {
                            //Ensure we're not reading past the end of the image.
                            if ((xBlock * 4 + pX >= width) || (yBlock * 4 + pY >= height))
                            {
                                stream.SkipUInt16();
                                continue;
                            }

                            ushort sourcePixel = stream.ReadUInt16();
                            RGB565ToRGBA8(sourcePixel, ref decodedData,
                                (int)(4 * (width * ((yBlock * 4) + pY) + (xBlock * 4) + pX)));
                        }
                    }
                }
            }

            return decodedData;
        }

        private static byte[] DecodeCmpr(EndianBinaryReader stream, uint width, uint height)
        {
            //4 bpp, 8 block width/height, block size 32 bytes, mini palettes in each block, RGB565 or transparent.
            uint numBlocksW = (width + 7) / 8;
            uint numBlocksH = (height + 7) / 8;

            byte[] decodedData = new byte[width * height * 4];

            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int ySubBlock = 0; ySubBlock < 2; ySubBlock++)
                    {
                        for (int xSubBlock = 0; xSubBlock < 2; xSubBlock++)
                        {
                            int subBlockWidth = (int)Math.Max(0, Math.Min(4, width - (xSubBlock * 4 + xBlock * 8)));
                            int subBlockHeight = (int)Math.Max(0, Math.Min(4, height - (ySubBlock * 4 + yBlock * 8)));

                            byte[] subBlockData = DecodeCmprSubBlock(stream);

                            for (int pY = 0; pY < subBlockHeight; pY++)
                            {
                                int destX = xBlock * 8 + xSubBlock * 4;
                                int destY = yBlock * 8 + ySubBlock * 4 + pY;
                                if (destX >= width || destY >= height)
                                    continue;

                                int destOffset = (int)(destY * width + destX) *4;
                                Buffer.BlockCopy(subBlockData, (int)(pY * 4 * 4), decodedData, destOffset, (int)(subBlockWidth*4));
                            }
                        }
                    }
                }
            }

            return decodedData;
        }

        private static byte[] DecodeCmprSubBlock(EndianBinaryReader stream)
        {
            byte[] decodedData = new byte[4 * 4 * 4];
            try
            {

                ushort color1 = stream.ReadUInt16();
                ushort color2 = stream.ReadUInt16();
                uint bits = stream.ReadUInt32();

                byte[][] ColorTable = new byte[4][];
                for (int i = 0; i < 4; i++)
                    ColorTable[i] = new byte[4];

                RGB565ToRGBA8(color1, ref ColorTable[0], 0);
                RGB565ToRGBA8(color2, ref ColorTable[1], 0);

                if (color1 > color2)
                {
                    ColorTable[2][0] = (byte)((2 * ColorTable[0][0] + ColorTable[1][0]) / 3);
                    ColorTable[2][1] = (byte)((2 * ColorTable[0][1] + ColorTable[1][1]) / 3);
                    ColorTable[2][2] = (byte)((2 * ColorTable[0][2] + ColorTable[1][2]) / 3);
                    ColorTable[2][3] = 0xFF;

                    ColorTable[3][0] = (byte)((ColorTable[0][0] + 2 * ColorTable[1][0]) / 3);
                    ColorTable[3][1] = (byte)((ColorTable[0][1] + 2 * ColorTable[1][1]) / 3);
                    ColorTable[3][2] = (byte)((ColorTable[0][2] + 2 * ColorTable[1][2]) / 3);
                    ColorTable[3][3] = 0xFF;
                }
                else
                {
                    ColorTable[2][0] = (byte)((ColorTable[0][0] + ColorTable[1][0]) / 2);
                    ColorTable[2][1] = (byte)((ColorTable[0][1] + ColorTable[1][1]) / 2);
                    ColorTable[2][2] = (byte)((ColorTable[0][2] + ColorTable[1][2]) / 2);
                    ColorTable[2][3] = 0xFF;

                    ColorTable[3][0] = (byte)((ColorTable[0][0] + 2 * ColorTable[1][0]) / 3);
                    ColorTable[3][1] = (byte)((ColorTable[0][1] + 2 * ColorTable[1][1]) / 3);
                    ColorTable[3][2] = (byte)((ColorTable[0][2] + 2 * ColorTable[1][2]) / 3);
                    ColorTable[3][3] = 0x00;
                }

                for (int iy = 0; iy < 4; ++iy)
                {
                    for (int ix = 0; ix < 4; ++ix)
                    {
                        int i = iy * 4 + ix;
                        int bitOffset = (15 - i) * 2;
                        int di = i * 4;
                        int si = (int)((bits >> bitOffset) & 0x3);
                        decodedData[di + 0] = ColorTable[si][0];
                        decodedData[di + 1] = ColorTable[si][1];
                        decodedData[di + 2] = ColorTable[si][2];
                        decodedData[di + 3] = ColorTable[si][3];
                    }
                }


            return decodedData;
            
            }
            catch (EndOfStreamException)
            {
                Debug.LogWarning("Skipped decoding texture. EndOfStream");
                // Ignoriere den Fehler und setze den Standardwert, falls nötig
                return decodedData;
            }
        }

        private static void Swap(ref byte b1, ref byte b2)
        {
            byte tmp = b1; b1 = b2; b2 = tmp;
        }

        private static ushort Read16Swap(byte[] data, uint offset)
        {
            return (ushort)((Buffer.GetByte(data, (int)offset + 1) << 8) | Buffer.GetByte(data, (int)offset));
        }

        private static uint Read32Swap(byte[] data, uint offset)
        {
            return (uint)((Buffer.GetByte(data, (int)offset + 3) << 24) | (Buffer.GetByte(data, (int)offset + 2) << 16) | (Buffer.GetByte(data, (int)offset + 1) << 8) | Buffer.GetByte(data, (int)offset));
        }

        private static byte S3TC1ReverseByte(byte b)
        {
            byte b1 = (byte)(b & 0x3);
            byte b2 = (byte)(b & 0xC);
            byte b3 = (byte)(b & 0x30);
            byte b4 = (byte)(b & 0xC0);

            return (byte)((b1 << 6) | (b2 << 2) | (b3 >> 2) | (b4 >> 6));
        }

        private static byte[] DecodeIA8(EndianBinaryReader stream, uint width, uint height)
        {
            uint numBlocksW = (width + 3) / 4; //4 byte block width
            uint numBlocksH = (height + 3) / 4; //4 byte block height 

            byte[] decodedData = new byte[width * height * 4];

            try
            {
                for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
                {
                    for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                    {
                        //For each block, we're going to examine block width / block height number of 'pixels'
                        for (int pY = 0; pY < 4; pY++)
                        {
                            for (int pX = 0; pX < 4; pX++)
                            {
                                //Ensure the pixel we're checking is within bounds of the image.
                                if ((xBlock * 4 + pX >= width) || (yBlock * 4 + pY >= height))
                                {
                                    stream.SkipByte();
                                    stream.BaseStream.Position++;
                                    //stream.SkipByte();
                                    continue;
                                }

                                //Now we're looping through each pixel in a block, but a pixel is four bytes long. 
                                uint destIndex = (uint)(4 * (width * ((yBlock * 4) + pY) + (xBlock * 4) + pX));
                                byte byte0 = stream.ReadByte();
                                byte byte1 = stream.ReadByte();
                                decodedData[destIndex + 3] = byte0;
                                decodedData[destIndex + 2] = byte1;
                                decodedData[destIndex + 1] = byte1;
                                decodedData[destIndex + 0] = byte1;
                            }
                        }
                    }
                }
            }
            catch (EndOfStreamException)
            {
                Debug.LogWarning("Skipped decoding texture. EndOfStream");
            }

            return decodedData;
        }

        private static byte[] DecodeIA4(EndianBinaryReader stream, uint width, uint height)
        {
            uint numBlocksW = (width + 7) / 8;
            uint numBlocksH = (height + 3) / 4;

            byte[] decodedData = new byte[width * height * 4];

            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    //For each block, we're going to examine block width / block height number of 'pixels'
                    for (int pY = 0; pY < 4; pY++)
                    {
                        for (int pX = 0; pX < 8; pX++)
                        {
                            //Ensure the pixel we're checking is within bounds of the image.
                            if ((xBlock * 8 + pX >= width) || (yBlock * 4 + pY >= height))
                            {
                                stream.BaseStream.Position++;
                                //stream.SkipByte();
                                continue;
                            }

                            byte value = stream.ReadByte();

                            byte alpha = (byte)((value & 0xF0) >> 4);
                            byte lum = (byte)(value & 0x0F);

                            uint destIndex = (uint)(4 * (width * ((yBlock * 4) + pY) + (xBlock * 8) + pX));

                            decodedData[destIndex + 0] = (byte)(lum * 0x11);
                            decodedData[destIndex + 1] = (byte)(lum * 0x11);
                            decodedData[destIndex + 2] = (byte)(lum * 0x11);
                            decodedData[destIndex + 3] = (byte)(alpha * 0x11);
                        }
                    }
                }
            }

            return decodedData;
        }

        private static byte[] DecodeI4(EndianBinaryReader stream, uint width, uint height)
        {
            uint numBlocksW = (width + 7) / 8; //8 byte block width
            uint numBlocksH = (height + 7) / 8; //8 byte block height 

            byte[] decodedData = new byte[width * height * 4];

            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    //For each block, we're going to examine block width / block height number of 'pixels'
                    for (int pY = 0; pY < 8; pY++)
                    {
                        for (int pX = 0; pX < 8; pX += 2)
                        {
                            //Ensure the pixel we're checking is within bounds of the image.
                            if ((xBlock * 8 + pX >= width) || (yBlock * 8 + pY >= height))
                            {
                                stream.BaseStream.Position++;
                                //stream.SkipByte();
                                continue;
                            }

                            byte data = stream.ReadByte();
                            byte t = (byte)((data & 0xF0) >> 4);
                            byte t2 = (byte)(data & 0x0F);
                            uint destIndex = (uint)(4 * (width * ((yBlock * 8) + pY) + (xBlock * 8) + pX));

                            decodedData[destIndex + 0] = (byte)(t * 0x11);
                            decodedData[destIndex + 1] = (byte)(t * 0x11);
                            decodedData[destIndex + 2] = (byte)(t * 0x11);
                            decodedData[destIndex + 3] = (byte)(t * 0x11);

                            decodedData[destIndex + 4] = (byte)(t2 * 0x11);
                            decodedData[destIndex + 5] = (byte)(t2 * 0x11);
                            decodedData[destIndex + 6] = (byte)(t2 * 0x11);
                            decodedData[destIndex + 7] = (byte)(t2 * 0x11);
                        }
                    }
                }
            }

            return decodedData;
        }

        private static byte[] DecodeI8(EndianBinaryReader stream, uint width, uint height)
        {
            uint numBlocksW = (width + 7) / 8; //8 pixel block width
            uint numBlocksH = (height + 3) / 4; //4 pixel block height 

            byte[] decodedData = new byte[width * height * 4];

            try
            {
                for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
                {
                    for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                    {
                        //For each block, we're going to examine block width / block height number of 'pixels'
                        for (int pY = 0; pY < 4; pY++)
                        {
                            for (int pX = 0; pX < 8; pX++)
                            {
                                //Ensure the pixel we're checking is within bounds of the image.
                                if ((xBlock * 8 + pX >= width) || (yBlock * 4 + pY >= height))
                                {
                                    stream.BaseStream.Position++;
                                    //stream.SkipByte();
                                    continue;
                                }

                                byte data = stream.ReadByte();
                                uint destIndex = (uint)(4 * (width * ((yBlock * 4) + pY) + (xBlock * 8) + pX));

                                decodedData[destIndex + 0] = data;
                                decodedData[destIndex + 1] = data;
                                decodedData[destIndex + 2] = data;
                                decodedData[destIndex + 3] = data;
                            }
                        }
                    }
                }
            } catch (EndOfStreamException) {
                Debug.LogWarning("Skipped decoding texture. EndOfStream");
            }

            return decodedData;
        }

        private static byte[] DecodeRgb5A3(EndianBinaryReader stream, uint width, uint height)
        {
            uint numBlocksW = (width + 3) / 4; //4 byte block width
            uint numBlocksH = (height + 3) / 4; //4 byte block height 

            byte[] decodedData = new byte[width * height * 4];

            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    //For each block, we're going to examine block width / block height number of 'pixels'
                    for (int pY = 0; pY < 4; pY++)
                    {
                        for (int pX = 0; pX < 4; pX++)
                        {
                            //Ensure the pixel we're checking is within bounds of the image.
                            if ((xBlock * 4 + pX >= width) || (yBlock * 4 + pY >= height))
                            {
                                stream.BaseStream.Position++;
                                //stream.SkipUInt16();
                                continue;
                            }

                            ushort sourcePixel = stream.ReadUInt16();
                            RGB5A3ToRGBA8(sourcePixel, ref decodedData,
                                (int)(4 * (width * ((yBlock * 4) + pY) + (xBlock * 4) + pX)));
                        }
                    }
                }
            }

            return decodedData;
        }

        private static void UnpackPixelFromPalette(int paletteIndex, ref byte[] dest, int offset, byte[] paletteData, PaletteFormats format)
        {
            switch (format)
            {
                case PaletteFormats.IA8:
                    dest[0] = paletteData[2 * paletteIndex + 1];
                    dest[1] = paletteData[2 * paletteIndex + 0];
                    break;
                case PaletteFormats.RGB565:
                    {
                        ushort palettePixelData = (ushort)((Buffer.GetByte(paletteData, 2 * paletteIndex) << 8) | Buffer.GetByte(paletteData, 2 * paletteIndex + 1));
                        RGB565ToRGBA8(palettePixelData, ref dest, offset);
                    }
                    break;
                case PaletteFormats.RGB5A3:
                    {
                        ushort palettePixelData = (ushort)((Buffer.GetByte(paletteData, 2 * paletteIndex) << 8) | Buffer.GetByte(paletteData, 2 * paletteIndex + 1));
                        RGB5A3ToRGBA8(palettePixelData, ref dest, offset);
                    }
                    break;
            }
        }



        /// <summary>
        /// Convert a RGB565 encoded pixel (two bytes in length) to a RGBA (4 byte in length)
        /// pixel.
        /// </summary>
        /// <param name="sourcePixel">RGB565 encoded pixel.</param>
        /// <param name="dest">Destination array for RGBA pixel.</param>
        /// <param name="destOffset">Offset into destination array to write RGBA pixel.</param>
        private static void RGB565ToRGBA8(ushort sourcePixel, ref byte[] dest, int destOffset)
        {
            byte r, g, b;
            r = (byte)((sourcePixel & 0xF800) >> 11);
            g = (byte)((sourcePixel & 0x7E0) >> 5);
            b = (byte)((sourcePixel & 0x1F));

            r = (byte)((r << (8 - 5)) | (r >> (10 - 8)));
            g = (byte)((g << (8 - 6)) | (g >> (12 - 8)));
            b = (byte)((b << (8 - 5)) | (b >> (10 - 8)));

            dest[destOffset] = b;
            dest[destOffset + 1] = g;
            dest[destOffset + 2] = r;
            dest[destOffset + 3] = 0xFF; //Set alpha to 1
        }

        /// <summary>
        /// Convert a RGB5A3 encoded pixel (two bytes in length) to an RGBA (4 byte in length)
        /// pixel.
        /// </summary>
        /// <param name="sourcePixel">RGB5A3 encoded pixel.</param>
        /// <param name="dest">Destination array for RGBA pixel.</param>
        /// <param name="destOffset">Offset into destination array to write RGBA pixel.</param>
        private static void RGB5A3ToRGBA8(ushort sourcePixel, ref byte[] dest, int destOffset)
        {
            byte r, g, b, a;

            //No alpha bits
            if ((sourcePixel & 0x8000) == 0x8000)
            {
                a = 0xFF;
                r = (byte)((sourcePixel & 0x7C00) >> 10);
                g = (byte)((sourcePixel & 0x3E0) >> 5);
                b = (byte)(sourcePixel & 0x1F);

                r = (byte)((r << (8 - 5)) | (r >> (10 - 8)));
                g = (byte)((g << (8 - 5)) | (g >> (10 - 8)));
                b = (byte)((b << (8 - 5)) | (b >> (10 - 8)));
            }
            //Alpha bits
            else
            {
                a = (byte)((sourcePixel & 0x7000) >> 12);
                r = (byte)((sourcePixel & 0xF00) >> 8);
                g = (byte)((sourcePixel & 0xF0) >> 4);
                b = (byte)(sourcePixel & 0xF);

                a = (byte)((a << (8 - 3)) | (a << (8 - 6)) | (a >> (9 - 8)));
                r = (byte)((r << (8 - 4)) | r);
                g = (byte)((g << (8 - 4)) | g);
                b = (byte)((b << (8 - 4)) | b);
            }

            dest[destOffset + 0] = b;
            dest[destOffset + 1] = g;
            dest[destOffset + 2] = r;
            dest[destOffset + 3] = a;
        }
        #endregion

        #region Encoding

        private Tuple<byte[], ushort[]> EncodeC4()
        {
            List<Color32> palColors = new List<Color32>();

            uint numBlocksW = (uint)(Width + 7) / 8;
            uint numBlocksH = (uint)(Height + 7) / 8;

            byte[] pixIndices = new byte[numBlocksH * numBlocksW * 8 * 8];

            for (int i = 0; i < (Width * Height) * 4; i += 4)
                palColors.Add(new Color32(m_rgbaImageData[i + 2], m_rgbaImageData[i + 1], m_rgbaImageData[i + 0], m_rgbaImageData[i + 3]));
            
            List<ushort> rawColorData = new List<ushort>();
            Dictionary<Color32, byte> pixelColorIndexes = new Dictionary<Color32, byte>();
            foreach (Color32 col in palColors)
            {
                EncodeColor(col, rawColorData, pixelColorIndexes);
            }

            int pixIndex = 0;
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int pY = 0; pY < 8; pY++)
                    {
                        for (int pX = 0; pX < 8; pX += 2)
                        {
                            byte color1 = (byte)(pixelColorIndexes[palColors[Width * ((yBlock * 8) + pY) + (xBlock * 8) + pX]] & 0xF);
                            byte color2 = (byte)(pixelColorIndexes[palColors[Width * ((yBlock * 8) + pY) + (xBlock * 8) + pX + 1]] & 0xF);
                            pixIndices[pixIndex] = (byte)(color1 << 4);
                            pixIndices[pixIndex++] |= color2;
                        }
                    }
                }
            }

            PaletteCount = (ushort)rawColorData.Count;
            PalettesEnabled = true;

            return new Tuple<byte[], ushort[]>(pixIndices, rawColorData.ToArray());
        }

        private Tuple<byte[], ushort[]> EncodeC8()
        {
            List<Color32> palColors = new List<Color32>();

            uint numBlocksW = (uint)(Width + 7) / 8;
            uint numBlocksH = (uint)(Height + 3) / 4;

            byte[] pixIndices = new byte[numBlocksH * numBlocksW * 8 * 4];

            for (int i = 0; i < (Width * Height) * 4; i += 4)
                palColors.Add(new Color32(m_rgbaImageData[i + 2], m_rgbaImageData[i + 1], m_rgbaImageData[i + 0], m_rgbaImageData[i + 3]));

            List<ushort> rawColorData = new List<ushort>();
            Dictionary<Color32, byte> pixelColorIndexes = new Dictionary<Color32, byte>();
            foreach (Color32 col in palColors)
            {
                EncodeColor(col, rawColorData, pixelColorIndexes);
            }

            int pixIndex = 0;
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int pY = 0; pY < 4; pY++)
                    {
                        for (int pX = 0; pX < 8; pX++)
                        {
                            pixIndices[pixIndex++] = pixelColorIndexes[palColors[Width * ((yBlock * 4) + pY) + (xBlock * 8) + pX]];
                        }
                    }
                }
            }

            PaletteCount = (ushort)rawColorData.Count;
            PalettesEnabled = true;

            return new Tuple<byte[], ushort[]>(pixIndices, rawColorData.ToArray());
        }

        private void EncodeColor(Color32 col, List<ushort> rawColorData, Dictionary<Color32, byte> pixelColorIndexes)
        {
            switch (PaletteFormat)
            {
                case PaletteFormats.IA8:
                    byte i = (byte)((col.r * 0.2126) + (col.g * 0.7152) + (col.b * 0.0722));

                    ushort fullIA8 = (ushort)((i << 8) | (col.a));
                    if (!rawColorData.Contains(fullIA8))
                        rawColorData.Add(fullIA8);
                    if (!pixelColorIndexes.ContainsKey(col))
                        pixelColorIndexes.Add(col, (byte)rawColorData.IndexOf(fullIA8));
                    break;
                case PaletteFormats.RGB565:
                    ushort r_565 = (ushort)(col.r >> 3);
                    ushort g_565 = (ushort)(col.g >> 2);
                    ushort b_565 = (ushort)(col.b >> 3);

                    ushort fullColor565 = 0;
                    fullColor565 |= b_565;
                    fullColor565 |= (ushort)(g_565 << 5);
                    fullColor565 |= (ushort)(r_565 << 11);

                    if (!rawColorData.Contains(fullColor565))
                        rawColorData.Add(fullColor565);
                    if (!pixelColorIndexes.ContainsKey(col))
                        pixelColorIndexes.Add(col, (byte)rawColorData.IndexOf(fullColor565));
                    break;
                case PaletteFormats.RGB5A3:
                    ushort fullColor53 = 0;

                    if (col.a == 255)
                    {
                        fullColor53 |= 0x8000;
                        
                        ushort r_53 = (ushort)(col.r >> 3);
                        ushort g_53 = (ushort)(col.g >> 3);
                        ushort b_53 = (ushort)(col.b >> 3);

                        fullColor53 |= b_53;
                        fullColor53 |= (ushort)(g_53 << 5);
                        fullColor53 |= (ushort)(r_53 << 10);
                    }
                    else
                    {
                        ushort r_53 = (ushort)(col.r >> 4);
                        ushort g_53 = (ushort)(col.g >> 4);
                        ushort b_53 = (ushort)(col.b >> 4);
                        ushort a_53 = (ushort)(col.a >> 5);

                        fullColor53 |= b_53;
                        fullColor53 |= (ushort)(g_53 << 4);
                        fullColor53 |= (ushort)(r_53 << 8);
                        fullColor53 |= (ushort)(a_53 << 12);
                    }

                    if (!rawColorData.Contains(fullColor53))
                        rawColorData.Add(fullColor53);
                    if (!pixelColorIndexes.ContainsKey(col))
                        pixelColorIndexes.Add(col, (byte)rawColorData.IndexOf(fullColor53));
                    break;
            }
        }
        #endregion
    }