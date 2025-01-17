using GameFormatReader.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;


    public class TEX1
    {
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

            reader.BaseStream.Position = tagStart + stringTableOffset;
            StringTable nameTable = StringTable.FromStream(reader);

            for (int t = 0; t < numTextures; t++)
            {
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

            reader.BaseStream.Position = tagStart + stringTableOffset;
            StringTable nameTable = StringTable.FromStream(reader);

            for (int t = 0; t < numTextures; t++)
            {
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