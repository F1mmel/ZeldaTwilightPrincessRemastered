using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Syroot.BinaryData;
using System.Numerics;

namespace KclLibrary
{
    public static class BinaryDataReaderExtensions
    {

        internal static KclPrism[] ReadPrisms(this BinaryDataReader self, int count, FileVersion version)
        {
            KclPrism[] values = new KclPrism[count];
            for (int i = 0; i < count; i++) {
                values[i] = new KclPrism();
                values[i].Read(self, version);
                if (version != FileVersion.Version2)
                    values[i].GlobalIndex = (ushort)i;
            }
            return values;
        }

        internal static Vector3U ReadVector3U(this BinaryDataReader self) {
            return new Vector3U(self.ReadUInt32(), self.ReadUInt32(), self.ReadUInt32());
        }

        internal static Vector3U[] ReadVector3s(this BinaryDataReader self, int count)
        {
            Vector3U[] values = new Vector3U[count];
            for (int i = 0; i < count; i++) {
                values[i] = ReadVector3U(self);
            }
            return values;
        }

        internal static Vector3 ReadVector3F(this BinaryDataReader self) {
            return new Vector3(self.ReadSingle(), self.ReadSingle(), self.ReadSingle());
        }

        internal static Vector3[] ReadVector3Fs(this BinaryDataReader self, int count)
        {
            Vector3[] values = new Vector3[count];
            for (int i = 0; i < count; i++) {
                values[i] = ReadVector3F(self);
            }
            return values;
        }




        internal static Vector3[] ReadVector3Fx16s(this BinaryDataReader self, int count)
        {
            Vector3[] values = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = ReadVector3Fx16(self);
            }
            return values;
        }

        internal static Vector3[] ReadVector3Fx32s(this BinaryDataReader self, int count)
        {
            Vector3[] values = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = ReadVector3Fx32(self);
            }
            return values;
        }

        internal static Vector3 ReadVector3Fx32(this BinaryDataReader self) {
            return new Vector3(self.ReadFx32(), self.ReadFx32(), self.ReadFx32());
        }

        internal static Vector3 ReadVector3Fx16(this BinaryDataReader self) {
            return new Vector3(self.ReadFx16(), self.ReadFx16(), self.ReadFx16());
        }

        internal static float ReadFx32(this BinaryDataReader self) {
            return self.ReadInt32() / 4096f;
        }

        internal static float ReadFx16(this BinaryDataReader self) {
            return self.ReadInt16() / 4096f;
        }
    }
}
