using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using TMPro;
using UnityEngine.TextCore;
using GameFormatReader.Common;
using WiiExplorer;

[Serializable]
public class BFNData
{
    public TMP_FontAsset Asset;
    public float Scale;
    public float Spacing;
}

public class BFNLoader
{
    public CodepointEncoding Encoding { get; set; }
    public int Ascent { get; set; }
    public int Descent { get; set; }
    public int CharacterWidth { get; set; }
    public int Leading { get; set; }
    public int ReplacementCode { get; set; }

    public List<Sheet> Sheets = new List<Sheet>();
    public List<GlyphBlock> GlyphBlocks = new List<GlyphBlock>();

    public TMP_FontAsset tmpFont;
    public Texture2D atlasTexture;

    public BFNData LoadBFN(string arcName, float fontScale, float spacing)
    {
        // Get bfn font buffer
        Archive archive = ArcReader.Read("Assets/GameFiles/res/Fonteu/" + arcName + ".arc");
        byte[] buffer = archive.Files[0].Buffer;

        using (EndianBinaryReader reader = new EndianBinaryReader(buffer, Endian.Big))
        {
            LoadBFN(reader);
        }
        
        return new BFNData()
        {
            Asset = CreateTMPFont(),
            Scale = fontScale,
            Spacing = spacing
        };
    }

private TMP_FontAsset CreateTMPFont()
{
    tmpFont = ScriptableObject.CreateInstance<TMP_FontAsset>();

    // Create the Texture2D for the atlas
    atlasTexture = new Texture2D(1024, 1024, TextureFormat.Alpha8, false);

    Texture2D[] atlasArray = new Texture2D[8];

    List<UnityEngine.TextCore.Glyph> glyphTable = new List<UnityEngine.TextCore.Glyph>();
    List<TMP_Character> characterTable = new List<TMP_Character>();

    // Generate texture atlas
    int atlasX = 0;
    int atlasY = 0;
    int maxHeight = 0;
    int imageCount = 0;

    Dictionary<int, int> imageIndexToAtlasIndex = new Dictionary<int, int>();
    int atlasIndex = 0;

    // Loop through sheets, should always be a single time
    foreach (Sheet sheet in Sheets)
    {
        // Loop through images, should always be 8
        foreach (var image in sheet.Images)
        {
            Texture2D sheetTexture = LoadTextureWithSkia(image, sheet.SheetWidth, sheet.SheetHeight);

            // Check if the texture needs to wrap to the next row
            if (atlasX + sheetTexture.width > atlasTexture.width)
            {
                atlasX = 0;
                atlasY += maxHeight;
                maxHeight = 0;
            }

            // Copy the pixels from the sheet texture to the atlas
            UnityEngine.Color[] pixels = sheetTexture.GetPixels();
            atlasTexture.SetPixels(atlasX, atlasY, sheetTexture.width, sheetTexture.height, pixels);

            // Update atlas position and max height
            imageIndexToAtlasIndex[imageCount] = atlasIndex;
            atlasX += sheetTexture.width;
            maxHeight = Mathf.Max(maxHeight, sheetTexture.height);
            atlasIndex++;

            atlasArray[imageCount] = sheetTexture;
        }
    }

    atlasTexture.Apply();

    // Loop through all blocks, should always be a single time
    foreach (GlyphBlock glyphBlock in GlyphBlocks)
    {
        int gCount = 0;
        // Loop through all glyphs
        foreach (Glyph glyph in glyphBlock.Glyphs)
        {
            int sheetIndex = glyph.CodePoint / (Sheets[0].RowCount * Sheets[0].ColumnCount);
            Sheet sheet = Sheets[0];

            int localIndex = glyph.CodePoint % (sheet.RowCount * sheet.ColumnCount);
            int x = (localIndex % sheet.ColumnCount) * sheet.CellWidth;
            int y = (localIndex / sheet.ColumnCount) * sheet.CellHeight;

            int yOff = 0;

            if (sheetIndex != 0 && y <= 128-sheet.CellHeight) yOff = 2;

            // Create a Glyph
            UnityEngine.TextCore.Glyph tmpGlyph = new UnityEngine.TextCore.Glyph()
            {
                index = (uint)glyph.CodePoint,
                metrics = new GlyphMetrics(glyph.Width, sheet.CellHeight, 0, 0, glyph.Width),
                glyphRect = new GlyphRect(sheetIndex * 128 + x, 128 - y - sheet.CellHeight - yOff, sheet.CellWidth, sheet.CellHeight),
                scale = 1.0f,
                atlasIndex = 0
            };

            // Create TMP_Character
            TMP_Character tmpCharacter = new TMP_Character((uint)glyph.CharacterValue, tmpGlyph);

            glyphTable.Add(tmpGlyph);
            characterTable.Add(tmpCharacter);

            gCount++;
        }
    }

    // Save the atlas texture for debugging purposes
    //File.WriteAllBytes(@"C:\Users\finne\Desktop\ExportedUI\atlas.png", atlasTexture.EncodeToPNG());

    // Set the atlas texture and glyphs to the TMP_FontAsset
    tmpFont.atlas = atlasTexture;
    tmpFont.atlasTextures = new Texture2D[] { atlasTexture };
    typeof(TMP_FontAsset).GetField("m_AtlasWidth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(tmpFont, 1024);
    typeof(TMP_FontAsset).GetField("m_AtlasHeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(tmpFont, 1024);
    typeof(TMP_FontAsset).GetField("m_GlyphTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(tmpFont, glyphTable);
    typeof(TMP_FontAsset).GetField("m_CharacterTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(tmpFont, characterTable);

    FaceInfo faceInfo = new FaceInfo
    {
        familyName = "CustomFont",
        pointSize = 16,
        lineHeight = 20,
        ascentLine = 0,
        descentLine = Descent
    };

    // Initialisiere das m_fontInfo-Feld
    FaceInfo_Legacy legacy = new FaceInfo_Legacy();
    legacy.Name = "CustomFont";
    
    typeof(TMP_FontAsset).GetField("m_Version", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(tmpFont, "customVersion");
    typeof(TMP_FontAsset).GetField("m_FaceInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(tmpFont, faceInfo);


    // Create a new material for the TMP_FontAsset
    tmpFont.material = new Material(Shader.Find("TextMeshPro/Distance Field"))
    {
        mainTexture = atlasTexture
    };

    // Assign the TMP_FontAsset to a TMP_Text component
    /*TMP_Text textComponent = GetComponent<TMP_Text>();
    if (textComponent != null)
    {
        textComponent.font = tmpFont;
        textComponent.fontSize = Scale;
    }*/

    return tmpFont;
}



private Texture2D LoadTextureWithSkia(byte[] imageData, int width, int height)
{
    Texture2D tex = new Texture2D(width, height, TextureFormat.BGRA32, false);
    tex.LoadRawTextureData(imageData);
    tex.Apply();

    return tex;
}

    private void SetCharacterAndGlyphTables(TMP_FontAsset tmpFont, List<TMP_Character> characterTable, List<UnityEngine.TextCore.Glyph> glyphTable)
    {
        var characterTableField = typeof(TMP_FontAsset).GetField("m_CharacterTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var glyphTableField = typeof(TMP_FontAsset).GetField("m_GlyphTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (characterTableField != null)
        {
            characterTableField.SetValue(tmpFont, characterTable);
        }

        if (glyphTableField != null)
        {
            glyphTableField.SetValue(tmpFont, glyphTable);
        }
    }

    private void LoadBFN(EndianBinaryReader reader)
    {
        // Get the block count
        reader.Skip(12);
        int block_count = reader.ReadInt32();
        reader.Skip(16);

        // There can be multiple INF1 blocks in a file, but only the last one is used.
        do
        {
            block_count--;
            reader.Skip(8);

            Encoding = (CodepointEncoding)reader.ReadInt16();
            Ascent = reader.ReadInt16();
            Descent = reader.ReadInt16();
            CharacterWidth = reader.ReadInt16();
            Leading = reader.ReadInt16();
            ReplacementCode = reader.ReadInt16();

            reader.Skip(12);

        } while (reader.PeekReadInt32() == 0x494E4631); // INF1

        for (int i = 0; i < block_count; i++)
        {
            int fourcc = reader.ReadInt32();

            switch (fourcc)
            {
                // GLY1
                case 0x474C5931:
                    Sheets.Add(new Sheet(reader));
                    break;
                // MAP1
                case 0x4D415031:
                    GlyphBlocks.Add(new GlyphBlock(reader));
                    break;
                // WID1
                case 0x57494431:
                    LoadWidths(reader);
                    break;
            }
        }

        /*int count = 0;
        foreach (Sheet sheet in Sheets)
        {
            sheet.SaveImage(count);

            count++;
        }*/
    }

    private void LoadWidths(EndianBinaryReader reader)
    {
        reader.SkipInt32();

        int first_code = reader.ReadInt16();
        int last_code = reader.ReadInt16();

        byte[] width_data = new byte[(last_code - first_code) * 2];
        for (int i = 0; i < (last_code - first_code) * 2; i += 2)
        {
            width_data[i] = reader.ReadByte();
            width_data[i + 1] = reader.ReadByte();
        }

        foreach (GlyphBlock b in GlyphBlocks)
        {
            if (b.IsCodeInBlock(first_code))
            {
                foreach (Glyph g in b.Glyphs)
                {
                    g.Kerning = width_data[g.CodePoint * 2];
                    g.Width = width_data[(g.CodePoint * 2) + 1];
                }
            }
        }
    }

    /*private void SaveBFN(string FileName)
    {
        using(FileStream strm = new FileStream(FileName, FileMode.Create, FileAccess.Write))
        {
            EndianBinaryWriter writer = new EndianBinaryWriter(strm, Endian.Big);

            WriteHeader(writer);
            WriteINF1(writer);

            foreach (Sheet s in Sheets)
            {
                WriteGLY1(writer, s);
            }

            foreach (GlyphBlock b in GlyphBlocks)
            {
                WriteMAP1(writer, b);
            }

            WriteWID1(writer);

            writer.BaseStream.Seek(8, SeekOrigin.Begin);
            writer.Write((int)writer.BaseStream.Length);
        }
    }

    private void WriteHeader(EndianBinaryWriter writer)
    {
        int block_count = 2 + GlyphBlocks.Count + Sheets.Count;

        writer.Write(0x42464E34);
        writer.Write(24);
        writer.Write((int)0);
        writer.Write((int)0x00000000);
        writer.Write((int)block_count);
        writer.Write((int)0);
        writer.Write((int)0);
        writer.Write((int)0);
        writer.Write((int)0);
        writer.Write((int)0x42464E34);
        writer.Write((int)24);
        writer.Write((int)0x00000000);
        writer.Write((int)0x00000000);
    }

    private void WriteINF1(EndianBinaryWriter writer)
    {
        writer.Write(0x494E4631);
        writer.Write(24);

        writer.Write((short)Encoding);
        writer.Write((short)Ascent);
        writer.Write((short)Descent);
        writer.Write((short)CharacterWidth);
        writer.Write((short)Leading);
        writer.Write((short)ReplacementCode);

        writer.Write((int)0x00000000);
        writer.Write((int)0x00000000);
        writer.Write((int)0x00000000);
    }

    private void WriteGLY1(EndianBinaryWriter writer, Sheet sheet)
    {
        writer.Write(0x474C5931);
        writer.Write((int)sheet.Data.Length + 20);

        writer.Write((int)sheet.SheetWidth);
        writer.Write((int)sheet.SheetHeight);

        writer.Write((int)sheet.Format);
        writer.Write((int)0);

        writer.Write(sheet.Data);
    }

    private void WriteMAP1(EndianBinaryWriter writer, GlyphBlock block)
    {
        writer.Write(0x4D415031);
        writer.Write(0x00000010);

        writer.Write(block.FirstCode);
        writer.Write(block.LastCode);

        foreach (Glyph g in block.Glyphs)
        {
            writer.Write(g.CodePoint);
            writer.Write(g.CharacterValue);
        }
    }

    private void WriteWID1(EndianBinaryWriter writer)
    {
        writer.Write(0x57494431);
        writer.Write(0x00000008);

        writer.Write(GlyphBlocks[0].FirstCode);
        writer.Write(GlyphBlocks[0].LastCode);

        foreach (GlyphBlock b in GlyphBlocks)
        {
            foreach (Glyph g in b.Glyphs)
            {
                writer.Write(g.Kerning);
                writer.Write(g.Width);
            }
        }
    }*/
}


public enum CodepointEncoding
{
    Byte,
    Short,
    Mixed
}

public enum GlyphMapping
{
    Linear,
    KanjiLinear,
    Table,
    Map
}

public class Glyph
{
    public string Name { get; set; }

    public int CharacterValue { get; set; }
    public int CodePoint { get; set; }

    public int Kerning { get; set; }
    public int Width { get; set; }

    public Glyph(int character_value, int codepoint)
    {
        CharacterValue = character_value;
        CodePoint = codepoint;
    }
}

public class GlyphBlock
{
    public int FirstCharacter { get; set; }
    public int LastCharacter { get; set; }

    public int FirstCode { get; set; }
    public int LastCode { get; set; }

    public GlyphMapping Mapping { get; set; }

    public List<Glyph> Glyphs { get; set; }

    public GlyphBlock()
    {
        Glyphs = new List<Glyph>();
    }

    public GlyphBlock(EndianBinaryReader reader)
    {
        Glyphs = new List<Glyph>();

        reader.SkipInt32();

        Mapping = (GlyphMapping)reader.ReadInt16();
        FirstCharacter = reader.ReadInt16();
        LastCharacter = reader.ReadInt16();

        int entry_count = reader.ReadInt16();

        reader.Skip(16);

        switch (Mapping)
        {
            case GlyphMapping.Linear:
                MapLinear();
                break;
            case GlyphMapping.KanjiLinear:
                int base_code = 796;
                if (entry_count == 1)
                {
                    base_code = reader.ReadInt16();
                }

                MapKanjiLinear(base_code);
                break;
            case GlyphMapping.Table:
                MapTable(entry_count);
                break;
            case GlyphMapping.Map:
                MapMap();
                break;
        }
    }

    private void MapLinear()
    {
        FirstCode = 0;
        LastCode = (LastCharacter - FirstCharacter);

        for (int i = FirstCharacter; i <= LastCharacter; i++)
        {
            Glyphs.Add(new Glyph(i, i - FirstCharacter));
        }
    }

    private void MapKanjiLinear(int base_code)
    {
        for (int i = FirstCharacter; i <= LastCharacter; i++)
        {
            int lead_byte = ((i >> 8) & 255);
            int trail_byte = ((i) & 255);

            int index = (trail_byte - 64);

            if (index >= 64)
            {
                index--;
            }

            int final_code = base_code + index + ((lead_byte - 136) * 188 - 94);
            Glyphs.Add(new Glyph(i, final_code));
        }
    }

    private void MapTable(int entry_count)
    {

    }
    
    private void MapMap()
    {

    }

    public void ReadGlyphData(EndianBinaryReader reader)
    {
        foreach (Glyph g in Glyphs)
        {
            g.Kerning = reader.ReadByte();
            g.Width = reader.ReadByte();
        }
    }

    public bool IsCodeInBlock(int codepoint)
    {
        if (codepoint >= FirstCode && codepoint <= LastCode)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}

public class Sheet
{
    public List<byte[]> Images { get; set; }

    public int FirstCode { get; set; }
    public int LastCode { get; set; }

    public int SheetWidth { get; set; }
    public int SheetHeight { get; set; }

    public int CellWidth { get; set; }
    public int CellHeight { get; set; }

    public int RowCount { get; set; }
    public int ColumnCount { get; set; }

    public BinaryTextureImage.TextureFormats TextureFormat { get; set; }

    public Sheet()
    {
        Images = new List<byte[]>();
    }

    public Sheet(EndianBinaryReader reader)
    {
        Images = new List<byte[]>();

        reader.Skip(4);

        FirstCode = reader.ReadInt16();
        LastCode = reader.ReadInt16();

        CellWidth = reader.ReadInt16();
        CellHeight = reader.ReadInt16();

        int total_sheet_size = reader.ReadInt32();

        TextureFormat = (BinaryTextureImage.TextureFormats)reader.ReadInt16();

        RowCount = reader.ReadInt16();
        ColumnCount = reader.ReadInt16();

        SheetWidth = reader.ReadInt16();
        SheetHeight = reader.ReadInt16();

        reader.SkipInt16();

        int num_sheets = (LastCode - FirstCode) / (RowCount * ColumnCount) + 1;

        for (int i = 0; i < num_sheets; i++)
        {
            Images.Add(BinaryTextureImage.DecodeData(reader, (uint)SheetWidth, (uint)SheetHeight, TextureFormat));
        }
    }

    /*public void LoadImages(int index, string directory)
    {
        IEnumerable<string> files = Directory.EnumerateFiles(directory, $"sheet_{ index }_*.png");

        for (int i = 0; i < files.Count(); i++)
        {
            string img_name = files.ElementAt(i);
            Bitmap img = new Bitmap(img_name);

            Images.Add(BinaryTextureImage.EncodeData(img, (uint)SheetWidth, (uint)SheetHeight, TextureFormat));
        }
    }*/

    public void SaveImage(int index)
    {
        BinaryTextureImage aa = new BinaryTextureImage();
        
        for (int i = 0; i < Images.Count; i++)
        {
            //aa.SaveImageToDisk(Path.Combine(@"E:\_UserLinks\Downloads\nintyfont-win32-19032022-38af946\nintyfont-win32\py", $"sheet_{ index }_{ i }.png"), Images[i], SheetWidth, SheetHeight);
        }
    }

    public bool IsCodeInSheet(int codepoint)
    {
        if (codepoint >= FirstCode && codepoint <= LastCode)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}