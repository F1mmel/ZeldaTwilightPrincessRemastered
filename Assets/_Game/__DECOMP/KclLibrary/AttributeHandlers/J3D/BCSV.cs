using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Syroot.BinaryData;

namespace KclLibrary.AttributeHandlers
{
    public class BCSV
    {
        public bool IsBigEndian = false;

        public List<Field> Fields = new List<Field>();

        public List<Record> Records = new List<Record>();

        public BCSV() { }

        public BCSV(Stream stream) {
            Read(new BinaryDataReader(stream));
        }

        public void Save(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write)) {
                Save(fileStream);
            }
        }

        public void Save(Stream stream) {
            Write(new BinaryDataWriter(stream));
        }

        private void Read(BinaryDataReader reader)
        {
            using (reader.TemporarySeek(8, SeekOrigin.Begin))
            {
                if (reader.ReadUInt32() != 76) {
                    IsBigEndian = true; 
                    reader.ByteOrder = ByteOrder.BigEndian;
                }
            }

            uint recordCount = reader.ReadUInt32();
            uint fieldCount = reader.ReadUInt32();
            uint recordOffset = reader.ReadUInt32();
            uint recordSize = reader.ReadUInt32();
            uint strTableOffset = recordOffset + (recordCount * recordSize);

            for (int i = 0; i < fieldCount; i++)
                Fields.Add(new Field(reader));

            for (int i = 0; i < recordCount; i++) {
                reader.Seek(recordOffset + (i * recordSize), SeekOrigin.Begin);
                Records.Add(new Record(reader, Fields));
            }
        }

        private void Write(BinaryDataWriter writer)
        {
            if (IsBigEndian)
                writer.ByteOrder = ByteOrder.BigEndian;

            uint recordSize = MaxFieldSize();

            writer.Write(Records.Count);
            writer.Write(Fields.Count);
            writer.Write(16 + (Fields.Count * 12));
            writer.Write(MaxFieldSize());

            for (int i = 0; i < Fields.Count; i++)
                Fields[i].Write(writer);

            long pos = writer.Position;
            for (int i = 0; i < Records.Count; i++) {
                writer.Seek(pos + (i * recordSize), SeekOrigin.Begin);
                Records[i].Write(writer, Fields);
            }
            AlignBytes(writer, 0x20, 0x40);
        }

        private uint MaxFieldSize()
        {
            uint size = 0;
            for (int i = 0; i < Fields.Count; i++)
                size = Math.Max(size, Fields[i].GetDataSize());

            return AlignedSize(size, 4);
        }

        private uint AlignedSize(uint size, uint amount) {
            return ((size + amount - 1) / amount) *amount;
        }

        private void AlignBytes(BinaryDataWriter writer, int alignment, byte value = 0x00)
        {
            var startPos = writer.Position;
            long position = writer.Seek((-writer.Position % alignment + alignment) % alignment, SeekOrigin.Current);

            writer.Seek(startPos, System.IO.SeekOrigin.Begin);
            while (writer.Position != position) {
                writer.Write(value);
            }
        }

        public class Field
        {
            public string Name { get; set; }

            public uint Hash { get; set; }

            public uint Bitmask { get; set; }

            public ushort Offset { get; set; }

            public sbyte Shift { get; set; }

            public FieldType Type { get; set; }

            public Field(string name, FieldType type, ushort offset, uint mask, sbyte shift = 0)
            {
                Name = name;
                Hash = BCSVHashHelper.CalculateV2(name);
                Type = type;
                Offset = offset;
                Bitmask = mask;
                Shift = shift;
            }

            public Field(uint hash, FieldType type, ushort offset, uint mask, sbyte shift = 0)
            {
                Name = BCSVHashHelper.GetHashName(hash);
                Hash = hash;
                Type = type;
                Offset = offset;
                Bitmask = mask;
                Shift = shift;
            }

            internal Field(BinaryDataReader reader)
            {
                Hash = reader.ReadUInt32();
                Bitmask = reader.ReadUInt32();
                Offset = reader.ReadUInt16();
                Shift = reader.ReadSByte();
                Type = (FieldType)reader.ReadByte();
                Name = BCSVHashHelper.GetHashName(Hash);

                Console.WriteLine($"FIELD {Name} Bitmask {Bitmask} Shift {Shift}");
            }

            internal void Write(BinaryDataWriter writer)
            {
                writer.Write(Hash);
                writer.Write(Bitmask);
                writer.Write(Offset);
                writer.Write(Shift);
                writer.Write((byte)Type);
            }

            public uint GetDataSize()
            {
                switch (Type)
                {
                    case FieldType.Byte:   return 1;
                    case FieldType.Int16:  return 2;
                    case FieldType.Float:  return 4;
                    case FieldType.Int32:  return 4;
                    case FieldType.String: return 4;
                    default:
                      return 4;
                }
            }
        }

        public class Record
        {
            public object[] Values { get; set; }

            internal object[] BaseValues { get; set; }

            public Record(object[] values) {
                Values = values;
            }

            internal Record(BinaryDataReader reader, List<Field> fields)
            {
                long pos = reader.Position;

                Values = new object[fields.Count];
                BaseValues = new object[fields.Count];
                for (int i = 0; i < fields.Count; i++)
                {
                    reader.Seek(pos + fields[i].Offset, SeekOrigin.Begin);
                    switch (fields[i].Type)
                    {
                        case FieldType.Int32:
                            Values[i] = (uint)((reader.ReadInt32() & fields[i].Bitmask) >> fields[i].Shift);
                            break;
                        case FieldType.Float:
                            Values[i] = reader.ReadSingle();
                            break;
                        case FieldType.String:
                            Values[i] = reader.ReadString(BinaryStringFormat.ZeroTerminated);
                            break;
                        case FieldType.Int16:
                            Values[i] = (reader.ReadInt16() >> fields[i].Shift) & fields[i].Bitmask;
                            break;
                        case FieldType.Byte:
                            Values[i] = (reader.ReadByte() >> fields[i].Shift) & fields[i].Bitmask;
                            break;
                        case FieldType.StringJIS:
                            Values[i] = reader.ReadString(BinaryStringFormat.ZeroTerminated, Encoding.GetEncoding("shift_jis"));
                            break;
                    }
                }
            }


           internal void Write(BinaryDataWriter writer, List<Field> fields)
            {
                Dictionary<ushort, uint> buffer = new Dictionary<ushort, uint>(fields.Count);

                long pos = writer.Position;
                for (int i = 0; i < fields.Count; i++)
                {
                    writer.Seek(pos + fields[i].Offset, SeekOrigin.Begin);
                    switch (fields[i].Type)
                    {
                        case FieldType.Int32:
                            uint value = (uint)Values[i];
                            Console.WriteLine($"SAVED Fields {fields[i].Name} {Values[i]}");
                            if (fields[i].Bitmask == uint.MaxValue)
                            {
                                writer.Write(value);
                            }
                            else
                            {
                                if (!buffer.ContainsKey(fields[i].Offset))
                                    buffer[fields[i].Offset] = 0u;

                                buffer[fields[i].Offset] |= ((uint)(value << fields[i].Shift) & fields[i].Bitmask);
                            }
                            break;
                        case FieldType.Float:
                            writer.Write((float)Values[i]);
                            break;
                        case FieldType.String:
                            writer.Write((string)Values[i], BinaryStringFormat.ZeroTerminated);
                            break;
                        case FieldType.Int16:
                            writer.Write((short)Values[i]);
                            break;
                        case FieldType.Byte:
                            writer.Write((byte)Values[i]);
                            break;
                        case FieldType.StringJIS:
                            writer.Write((string)Values[i], BinaryStringFormat.ZeroTerminated, Encoding.GetEncoding("shift_jis"));
                            break;
                    }

                    foreach (var val in buffer)
                    {
                        writer.Seek(pos + val.Key, SeekOrigin.Begin);
                        writer.Write(val.Value);
                    }
                }
            }
        }

        public enum FieldType
        {
            Int32 = 0,
            String = 1,
            Float = 2,  
            Int16 = 4,
            Byte = 5,
            StringJIS = 6,
        }
    }
}
