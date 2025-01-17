using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KclLibrary
{
    public struct KclPrism
    {

        public float Length;

        public ushort PositionIndex;

        public ushort DirectionIndex;

        public ushort Normal1Index;

        public ushort Normal2Index;

        public ushort Normal3Index;

        public ushort CollisionFlags;

        public uint GlobalIndex;

        internal void Read(BinaryDataReader reader, FileVersion version)
        {
            if (version == FileVersion.VersionDS)
                Length = reader.ReadFx32();
            else
                Length = reader.ReadSingle();
            PositionIndex = reader.ReadUInt16();
            DirectionIndex = reader.ReadUInt16();
            Normal1Index = reader.ReadUInt16();
            Normal2Index = reader.ReadUInt16();
            Normal3Index = reader.ReadUInt16();
            CollisionFlags = reader.ReadUInt16();
            if (version >= FileVersion.Version2)
                GlobalIndex = reader.ReadUInt32();
        }

        internal void Write(BinaryDataWriter writer, FileVersion version)
        {
            if (version == FileVersion.VersionDS)
                writer.Write((int)(Length * 4096f));
            else
                writer.Write(Length);
            writer.Write(PositionIndex);
            writer.Write(DirectionIndex);
            writer.Write(Normal1Index);
            writer.Write(Normal2Index);
            writer.Write(Normal3Index);
            writer.Write(CollisionFlags);
            if (version >= FileVersion.Version2)
                writer.Write(GlobalIndex);
        }
    }
}
