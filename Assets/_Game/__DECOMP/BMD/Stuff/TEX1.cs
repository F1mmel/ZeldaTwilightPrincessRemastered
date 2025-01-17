using GameFormatReader.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

/*public class Texture
    {
        public string Name { get; protected set; }
        //public BinaryTextureImage CompressedData { get; protected set; }

        private int m_glTextureIndex;

        // To detect redundant calls
        private bool m_hasBeenDisposed = false;

        public Texture(string name)
        {
            Name = name;
            //CompressedData = compressedData;

            /*m_glTextureIndex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, m_glTextureIndex);
            /*GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GXToOpenGL.GetWrapMode(compressedData.WrapS));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GXToOpenGL.GetWrapMode(compressedData.WrapT));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GXToOpenGL.GetMinFilter(compressedData.MinFilter));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GXToOpenGL.GetMagFilter(compressedData.MagFilter));

            // Border Color
            WLinearColor borderColor = compressedData.BorderColor;
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new[] { borderColor.R, borderColor.G, borderColor.B, borderColor.A });

            // ToDo: Min/Mag LOD & Biases

            // Upload Image Data
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, compressedData.Width, compressedData.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, compressedData.GetData());

            // Generate Mip Maps
            if (compressedData.MipMapCount > 0)
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        public override string ToString()
        {
            return Name;
        }
    }*/

    public class TEX1
    {
        //public BindingList<Texture> Textures;
        private static bool m_allowTextureCache = true;

        public List<BTI> BTIs = new List<BTI>();
        public List<BinaryTextureImage> BinaryTextureImages = new List<BinaryTextureImage>();

        public void LoadTEX1FromStream(EndianBinaryReader reader, long tagStart, List<BTI> externalBTIs)
        {
            ushort numTextures = reader.ReadUInt16();
            int padding = reader.ReadUInt16();
            if(padding != 0xFFFF) return;

            int textureHeaderDataOffset = reader.ReadInt32();
            int stringTableOffset = reader.ReadInt32();

            // Texture Names
            reader.BaseStream.Position = tagStart + stringTableOffset;
            StringTable nameTable = StringTable.FromStream(reader);

            //Textures = new BindingList<Texture>();
            for (int t = 0; t < numTextures; t++)
            {
                // Reset the stream position to the start of this header as loading the actual data of the texture
                // moves the stream head around.
                reader.BaseStream.Position = tagStart + textureHeaderDataOffset + (t * 0x20);

                bool foundExternal = false;
                if (externalBTIs != null)
                {
                    foreach (BTI ex in externalBTIs)
                    {
                        if (ex.Name.Equals(nameTable.Strings[t].String.ToLower()))
                        {
                            BTIs.Add(ex);
                            foundExternal = true;
                        }
                    }
                }

                if (foundExternal)
                {
                    continue;
                }

                BinaryTextureImage compressedTex = new BinaryTextureImage();
                compressedTex.Load(reader, tagStart + 0x20, t);
                
                Texture2D tex = compressedTex.SkiaToTexture();

                BTI bti = new BTI(nameTable.Strings[t].String, tex, compressedTex);
                BTIs.Add(bti);
            }
        }
        
        public void LoadTEX1FromStreamRaw(EndianBinaryReader reader, long tagStart, List<BTI> externalBTIs)
        {
            ushort numTextures = reader.ReadUInt16();
            int padding = reader.ReadUInt16();
            if(padding != 0xFFFF) return;

            int textureHeaderDataOffset = reader.ReadInt32();
            int stringTableOffset = reader.ReadInt32();

            // Texture Names
            reader.BaseStream.Position = tagStart + stringTableOffset;
            StringTable nameTable = StringTable.FromStream(reader);

            //Textures = new BindingList<Texture>();
            for (int t = 0; t < numTextures; t++)
            {
                // Reset the stream position to the start of this header as loading the actual data of the texture
                // moves the stream head around.
                reader.BaseStream.Position = tagStart + textureHeaderDataOffset + (t * 0x20);

                bool foundExternal = false;
                if (externalBTIs != null)
                {
                    foreach (BTI ex in externalBTIs)
                    {
                        if (ex.Name.Equals(nameTable.Strings[t].String.ToLower()))
                        {
                            BinaryTextureImages.Add(ex.Compressed);
                            foundExternal = true;
                        }
                    }
                }

                if (foundExternal)
                {
                    continue;
                }

                BinaryTextureImage compressedTex = new BinaryTextureImage(nameTable.Strings[t].String);
                compressedTex.Load(reader, tagStart + 0x20, t);
                
                BinaryTextureImages.Add(compressedTex);
            }
        }
    }