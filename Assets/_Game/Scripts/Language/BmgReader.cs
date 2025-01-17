using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameFormatReader.Common;
using Newtonsoft.Json;
using UnityEngine;

public class BmgReader
{
    public string inputFilePath;
    public string outputFilePath;

    private void Start()
    {
        inputFilePath = @"C:\Users\finne\Downloads\zel_00.bmg";
        outputFilePath = @"C:\Users\finne\Downloads\zel_00_output.json";
        
        try
        {
            if (string.IsNullOrEmpty(inputFilePath))
            {
                Debug.LogError("Input file path is not set.");
                return;
            }

            if (string.IsNullOrEmpty(outputFilePath))
            {
                outputFilePath = inputFilePath + ".json";
            }

            Debug.Log($"Reading BMG file: {inputFilePath}");
            Debug.Log($"Output JSON file will be saved to: {outputFilePath}");

            Debug.Log("BMG parsing completed successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing BMG: {ex.Message}");
        }
    }

   public void ParseBmgToJson(byte[] buffer)
    {

        using (var reader = new EndianBinaryReader(buffer, Endian.Big))
        {
            var magic = reader.ReadBytes(8);
            bool bigEndian = Encoding.ASCII.GetString(magic.Take(4).ToArray()) == "MESG";

            if (!bigEndian && Encoding.ASCII.GetString(magic) != "MESG1gmb")
            {
                throw new InvalidDataException("Input file is not a valid BMG file.");
            }

            reader.CurrentEndian = bigEndian ? Endian.Big : Endian.Little;

            uint fileSize = reader.ReadUInt32();
            uint sectionCount = reader.ReadUInt32();
            uint encodingVal = reader.ReadUInt32();

            string encoding = encodingVal == 0x03000000 ? "shift-jis" : "latin-1";

            reader.BaseStream.Seek(12, SeekOrigin.Current);

            var sections = new List<Section>();
            for (int i = 0; i < sectionCount; i++)
            {
                long sectionStart = reader.BaseStream.Position;
                string sectionMagic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                uint sectionSize = reader.ReadUInt32();

                byte[] sectionData = reader.ReadBytes((int)sectionSize - 8);
                sections.Add(new Section(sectionStart, sectionMagic, sectionSize, sectionData));
            }

            var inf1 = sections.FirstOrDefault(s => s.Magic == "INF1");
            if (inf1 == null) throw new InvalidDataException("INF1 section not found.");

            reader.BaseStream.Seek(inf1.Start + 8, SeekOrigin.Begin);
            ushort messageCount = reader.ReadUInt16();
            ushort itemLength = reader.ReadUInt16();

            reader.BaseStream.Seek(4, SeekOrigin.Current);

            var infItems = new List<InfItem>();
            for (int i = 0; i < messageCount; i++)
            {
                uint dat1Offset = reader.ReadUInt32();
                byte[] attributes = reader.ReadBytes(itemLength - 4);
                infItems.Add(new InfItem(dat1Offset, attributes));
                
            }

            var dat1 = sections.FirstOrDefault(s => s.Magic == "DAT1");
            var mid1 = sections.FirstOrDefault(s => s.Magic == "MID1");

            if (dat1 == null || mid1 == null)
            {
                throw new InvalidDataException("Required sections (DAT1 or MID1) not found.");
            }

            Language.Messages = new List<Message>();
            for (int i = 0; i < infItems.Count; i++)
            {
                var item = infItems[i];
                reader.BaseStream.Seek(dat1.Start + 8 + item.Dat1Offset, SeekOrigin.Begin);

                var message = new Message
                {
                    Attributes = BitConverter.ToString(item.Attributes).Replace("-", ""),
                    TextParts = new List<string>()
                };

                var currentText = new StringBuilder();
                while (true)
                {
                    byte b = reader.ReadByte();
                    if (b == 0x00) break;

                    if (b == 0x1A)
                    {
                        int argLen = reader.ReadByte();
                        byte[] args = reader.ReadBytes(argLen - 2);

                        if (currentText.Length > 0)
                        {
                            message.TextParts.Add(currentText.ToString());
                            currentText.Clear();
                        }

                        message.TextParts.Add($"{{{BitConverter.ToString(new byte[] { b }).Replace("-", "")}:{BitConverter.ToString(args).Replace("-", "")}}}");
                    }
                    else
                    {
                        currentText.Append((char)b);
                    }
                }

                if (currentText.Length > 0)
                {
                    message.TextParts.Add(currentText.ToString());
                }

                reader.BaseStream.Seek(mid1.Start + 16 + i * 4, SeekOrigin.Begin);
                uint msgId = reader.ReadUInt32();
                message.Id = msgId;
                
                if(message.TextParts.Count >= 1)
                    message.TextParts.RemoveAt(0);
                
                Language.Messages.Add(message);
            }
            
            foreach (var msg in Language.Messages)
            {
                for (int i = 0; i < msg.TextParts.Count; i++)
                {
                    if (msg.TextParts[i] == "{1A:000001}") msg.TextParts[i] = "";
                    else if (msg.TextParts[i] == "{1A:000002}") msg.TextParts[i] = "";
                    else if (msg.TextParts[i] == "{1A:000007000A}") msg.TextParts[i] = "";
                    else if (msg.TextParts[i] == "{1A:000036001E}") msg.TextParts[i] = "";
                    else if (msg.TextParts[i] == "{1A:00000A}") msg.TextParts[i] = "<GC_Button_A>";
                    else if (msg.TextParts[i] == "{1A:00000B}") msg.TextParts[i] = "<sprite name=\"mouse_left\">";
                    else if (msg.TextParts[i] == "{1A:FF000002}") msg.TextParts[i] = "<color=#96C67D>";
                    else if (msg.TextParts[i] == "{1A:FF000003}") msg.TextParts[i] = "<color=#92A0BF>";
                    else if (msg.TextParts[i] == "{1A:FF000004}") msg.TextParts[i] = "<color=#C5C276>";
                    else if (msg.TextParts[i] == "{1A:FF000001}") msg.TextParts[i] = "<color=#CC504B>";
                    else if (msg.TextParts[i] == "{1A:FF000006}") msg.TextParts[i] = "<color=#AF8FC0>";
                    else if (msg.TextParts[i] == "{1A:FF000008}") msg.TextParts[i] = "<color=#C09970>";
                    else if (msg.TextParts[i] == "{1A:FF000007}") msg.TextParts[i] = "<color=#D8D9D9>";
                    else if (msg.TextParts[i] == "{1A:FF000000}") msg.TextParts[i] = "</color>";
                    
                }

                foreach (var text in msg.TextParts)
                {
                    msg.FullText += text;
                }
            }

            var json = new
            {
                AttributeLength = itemLength,
                Encoding = encoding,
                Messages = Language.Messages.Select(m => new
                {
                    ID = m.Id,
                    Attributes = m.Attributes,
                    Text = m.TextParts
                }).ToList()
            };

            File.WriteAllText(@"C:\Users\finne\Downloads\zel_00_output.json", Newtonsoft.Json.JsonConvert.SerializeObject(json, Formatting.Indented));
        }
}
    
    private class Section
    {
        public long Start { get; }
        public string Magic { get; }
        public uint Size { get; }
        public byte[] Data { get; }

        public Section(long start, string magic, uint size, byte[] data)
        {
            Start = start;
            Magic = magic;
            Size = size;
            Data = data;
        }
    }

    private class InfItem
    {
        public uint Dat1Offset { get; }
        public byte[] Attributes { get; }

        public InfItem(uint dat1Offset, byte[] attributes)
        {
            Dat1Offset = dat1Offset;
            Attributes = attributes;
        }
    }

    public class Message
    {
        public uint Id { get; set; }
        public string Attributes { get; set; }
        public List<string> TextParts { get; set; }
        public string FullText { get; set; }
        
        public uint StringOffset { get; set; }
        public short StringID { get; set; }
        public short ItemPrice { get; set; }
        public short NextMessageID { get; set; }
        public short UnknownField3 { get; set; }
        public byte TextBoxType { get; set; }
        public byte DrawType { get; set; }
        public byte BoxPosition { get; set; }
        public byte ItemId { get; set; }
        public byte Unknown1 { get; set; }
        public byte InitialSoundId { get; set; }
        public byte InitialCameraBehaviour { get; set; }
        public byte InitialSpeakerAnim { get; set; }
        public byte Unknown5 { get; set; }
        public short NumLines { get; set; }
        public byte Padding { get; set; }

        public static Message ParseMessage(BinaryReader reader, int attributeLength)
        {
            uint stringOffset = reader.ReadUInt32();

            return new Message
            {
                StringOffset = stringOffset,
                StringID = reader.ReadInt16(),
                ItemPrice = reader.ReadInt16(),
                NextMessageID = reader.ReadInt16(),
                UnknownField3 = reader.ReadInt16(),
                TextBoxType = reader.ReadByte(),
                DrawType = reader.ReadByte(),
                BoxPosition = reader.ReadByte(),
                ItemId = reader.ReadByte(),
                Unknown1 = reader.ReadByte(),
                InitialSoundId = reader.ReadByte(),
                InitialCameraBehaviour = reader.ReadByte(),
                InitialSpeakerAnim = reader.ReadByte()
            };
        }
    }
}