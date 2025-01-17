public static class BitUtility
    {
        public static bool HasBit(uint self, int index)
        {
            return (self & (1u << index)) != 0;
        }

        public static uint SetBit(uint flags, int val, bool set)
        {
            if (set)
                return flags | (uint)(1u << val);
            else
                return flags &~(uint)(1u << val);
        }

        public static uint DecodeBit(this byte value, int firstBit, int numBits)
        {
            return (uint)((value >> firstBit) & ((1u << numBits) - 1));
        }

        public static uint DecodeBit(this ushort value, int firstBit, int numBits)
        {
            return (uint)((value >> firstBit) & ((1u << numBits) - 1));
        }

        public static uint DecodeBit(this uint value, int firstBit, int numBits)
        {
            return (uint)((value >> firstBit) & ((1u << numBits) - 1));
        }

        public static ushort EncodeBit(this byte self, byte value, int firstBit, int bits)
        {
            ushort mask = (byte)(((1u << bits) - 1) << firstBit);
            self &= (byte)~mask;
            value = (byte)((value << firstBit) & mask);

            return (byte)(self | value);
        }

        public static ushort EncodeBit(this ushort self, ushort value, int firstBit, int bits)
        {
            ushort mask = (ushort)(((1u << bits) - 1) << firstBit);
            self &= (ushort)~mask;
            value = (ushort)((value << firstBit) & mask);

            return (ushort)(self | value);
        }

        public static uint EncodeBit(this uint self, int value, int firstBit, int bits)
        {
            uint mask = ((1u << bits) - 1) << firstBit;
            self &= ~mask;
            value = (value << firstBit) & (int)mask;

            return (uint)(self | value);
        }

        public static uint EncodeBit(this uint self, uint value, int firstBit, int bits)
        {
            uint mask = ((1u << bits) - 1) << firstBit;
            self &= ~mask;
            value = (value << firstBit) & mask;

            return self | value;
        }
    }