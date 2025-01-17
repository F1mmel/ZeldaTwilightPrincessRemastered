using Syroot.BinaryData;
using System.Numerics;

namespace KclLibrary
{
    internal static class BinaryDataWriterExtensions
    {

        internal static void Write(this BinaryDataWriter self, KclPrism[] values, FileVersion version)
        {
            foreach (KclPrism value in values)
                value.Write(self, version);
        }

        internal static void Write(this BinaryDataWriter self, Vector3U value)
        {
            self.Write(value.X);
            self.Write(value.Y);
            self.Write(value.Z);
        }

        internal static void Write(this BinaryDataWriter self, Vector3U[] values)
        {
            foreach (Vector3U value in values) {
                Write(self, value);
            }
        }

        internal static void Write(this BinaryDataWriter self, Vector3 value)
        {
            self.Write(value.X);
            self.Write(value.Y);
            self.Write(value.Z);
        }

        internal static void Write(this BinaryDataWriter self, Vector3[] values)
        {
            foreach (Vector3 value in values) {
                Write(self, value);
            }
        }

        internal static void WriteVector3Fx16s(this BinaryDataWriter self, Vector3[] values) {
            foreach (Vector3 value in values) {
                WriteVector3Fx16(self, value);
            }
        }

        internal static void WriteVector3Fx32s(this BinaryDataWriter self, Vector3[] values)
        {
            foreach (Vector3 value in values) {
                WriteVector3Fx32(self, value);
            }
        }

        internal static void WriteVector3Fx16(this BinaryDataWriter self, Vector3 value) {
            self.WriteFx16(value.X);
            self.WriteFx16(value.Y);
            self.WriteFx16(value.Z);
        }

        internal static void WriteVector3Fx32(this BinaryDataWriter self, Vector3 value) {
            self.WriteFx32(value.X);
            self.WriteFx32(value.Y);
            self.WriteFx32(value.Z);
        }

        internal static void WriteFx32(this BinaryDataWriter self, float value) {
            self.Write((int)(value * 4096f));
        }

        internal static void WriteFx16(this BinaryDataWriter self, float value) {
            self.Write((short)(value * 4096f));
        }
    }
}
