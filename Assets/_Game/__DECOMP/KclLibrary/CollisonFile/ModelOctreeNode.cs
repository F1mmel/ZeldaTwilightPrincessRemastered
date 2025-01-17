using System;
using System.Collections.Generic;
using System.Linq;
using Syroot.BinaryData;
using System.Threading.Tasks;
using System.Numerics;

namespace KclLibrary
{
    public class ModelOctreeNode : OctreeNodeBase<ModelOctreeNode>
    {

        internal ModelOctreeNode() : base(0)
        {
        }

        internal ModelOctreeNode(BinaryDataReader reader, uint parentPosition) : base(reader.ReadUInt32())
        {
            switch ((Flags)(Key & _flagMask))
            {
                case Flags.Divide:
                    uint offset = parentPosition + (Key & 0x3FFFFFFF) * sizeof(uint);
                    long pos = reader.Position;

                    reader.Seek(offset, System.IO.SeekOrigin.Begin);

                    Children = new ModelOctreeNode[ChildCount];
                    for (int i = 0; i < ChildCount; i++) {
                        Children[i] = new ModelOctreeNode(reader, offset);
                    }
                    reader.Seek(pos, System.IO.SeekOrigin.Begin);
                    break;
                case Flags.Values:
                    ModelIndex = Key & ~_flagMask;
                    break;
            }
        }


        public uint? ModelIndex { get; internal set; }


        private long keyPos = 0;

        internal void Write(BinaryDataWriter writer, int branchKey = 8)
        {
            keyPos = writer.Position;

            if (Children == null)
            {
                if (ModelIndex.HasValue)
                {
                    Key = (uint)Flags.Values | ModelIndex.Value;
                }
                else
                {
                    Key = (uint)Flags.NoData;
                }
                writer.Write(Key);
            }
            else
            {
                writer.Write(Key);
            }
        }

        internal void WriteChildren(BinaryDataWriter writer, ref int branchKey)
        {
            if (Children != null)
            {
                using (writer.TemporarySeek(keyPos, System.IO.SeekOrigin.Begin)) {
                    writer.Write(branchKey);
                    branchKey += 8;
                }

                foreach (ModelOctreeNode child in Children) 
                    child.Write(writer, branchKey);

                int childBranchKey = 8;
                foreach (ModelOctreeNode child in Children)
                    child.WriteChildren(writer, ref childBranchKey);
            }
        }
    }
}
