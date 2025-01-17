using GameFormatReader.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JStudio.J3D
{
    public enum HierarchyDataType
    {
        Finish = 0x0,
        NewNode = 0x01,
        EndNode = 0x02,
        Joint = 0x10,
        Material = 0x11,
        Batch = 0x12,
    }

    public sealed class HierarchyNode
    {
        public List<HierarchyNode> Children { get; private set; }
        public HierarchyDataType Type { get; set; }
        public ushort Value { get; set; }

        public HierarchyNode(HierarchyDataType type, ushort value)
        {
            Children = new List<HierarchyNode>();
            Type = type;
            Value = value;
        }

        public HierarchyNode()
        {
            Children = new List<HierarchyNode>();
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}", Type, Value);
        }
    }


    public class INF1
    {
        public ushort Unknown1 { get; set; }
        public uint PacketCount { get; set; }
        public uint VertexCount { get; set; }
        public HierarchyNode HierarchyRoot { get { return m_hierarchy; } }


        private HierarchyNode m_hierarchy;

        public byte[] HierarchyBuffer;

        public List<InfoNode> InfoNodes = new List<InfoNode>();

        public sealed class InfoNode
        {
            public HierarchyDataType Type { get; set; }

            public ushort Value { get; set; }

            public InfoNode(HierarchyDataType type, ushort value)
            {
                Type = type;
                Value = value;
            }
        }

        public void AssignMaterialIndexes(SHP1 shp1)
        {
            int currentMaterialIndex = -1;
            foreach (INF1.InfoNode node in InfoNodes)
            {
                if (node.Type == HierarchyDataType.Finish) break;
                else if (node.Type == HierarchyDataType.Material)
                {
                    currentMaterialIndex = node.Value;
                }
                else if (node.Type == HierarchyDataType.Batch)
                {
                    SHP1.Shape shape = shp1.Shapes[node.Value];

                    if (shape.MaterialIndex != -1) continue;

                    shape.MaterialIndex = currentMaterialIndex;
                }
            }
        }

        public void LoadINF1FromStream(EndianBinaryReader reader, long chunkStart)
        {
            Unknown1 = reader.ReadUInt16();
            reader.Skip(2);
            PacketCount = reader.ReadUInt32();
            VertexCount = reader.ReadUInt32();
            uint hierarchyDataOffset = reader.ReadUInt32();

            reader.BaseStream.Position = chunkStart + hierarchyDataOffset;
            InfoNode curNode = null;

            
            do
            {
                curNode = new InfoNode((HierarchyDataType)reader.ReadUInt16(), reader.ReadUInt16());
                InfoNodes.Add(curNode);
            }
            while (curNode.Type != HierarchyDataType.Finish);

            m_hierarchy = new HierarchyNode();
            BuildSceneGraphFromInfoNodes(ref m_hierarchy, InfoNodes, 0);

        }

		private void PrintHierarchy(HierarchyNode hierarchy, int depth)
		{
			for (int i = 0; i < depth; i++)
				Console.Write("\t");
			Console.WriteLine($"Depth: {depth} Type: {hierarchy.Type} Value: {hierarchy.Value} ChildCount: {hierarchy.Children.Count}");
			foreach (var child in hierarchy.Children)
				PrintHierarchy(child, depth + 1);
		}

		private int BuildSceneGraphFromInfoNodes(ref HierarchyNode parent, List<InfoNode> allNodes, int currentListIndex)
        {
            for (int i = currentListIndex; i < allNodes.Count; i++)
            {
                InfoNode curNode = allNodes[i];
                HierarchyNode newNode = null;

                switch (curNode.Type)
                {
                    case HierarchyDataType.NewNode:
                        HierarchyNode latestChild = parent.Children[parent.Children.Count - 1];
                        i += BuildSceneGraphFromInfoNodes(ref latestChild, allNodes, i + 1);
                        break;

                    case HierarchyDataType.EndNode:
                        return i - currentListIndex + 1;

                    case HierarchyDataType.Material:
                    case HierarchyDataType.Joint:
                    case HierarchyDataType.Batch:
                    case HierarchyDataType.Finish:

                        InfoNode thisNode = allNodes[i];
                        newNode = new HierarchyNode(thisNode.Type, thisNode.Value);
                        parent.Children.Add(newNode);
                        break;

                    default:
                        Console.WriteLine("Unsupported HierarchyDataType \"{0}\" in model!", curNode.Type);
                        break;
                }
            }

            return 0;
        }
    }
}
