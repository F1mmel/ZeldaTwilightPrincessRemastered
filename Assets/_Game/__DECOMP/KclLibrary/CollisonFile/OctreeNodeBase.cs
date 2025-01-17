using System.Collections;
using System.Collections.Generic;

namespace KclLibrary
{
    public abstract class OctreeNodeBase<T> : IEnumerable<T>
        where T : OctreeNodeBase<T>
    {

        public const int ChildCount = 8;

        protected const uint _flagMask = 0b11000000_00000000_00000000_00000000;


        protected OctreeNodeBase(uint key)
        {
            Key = key;
        }


        public uint Key { get; internal set; }

        public T[] Children { get; internal set; }


        public IEnumerator<T> GetEnumerator()
        {
            return Children == null ? null : ((IEnumerable<T>)Children).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Children?.GetEnumerator();
        }


        internal enum Flags : uint
        {
            Divide = 0b00000000_00000000_00000000_00000000,
            Values = 0b10000000_00000000_00000000_00000000,
            NoData = 0b11000000_00000000_00000000_00000000
        }
    }
}
