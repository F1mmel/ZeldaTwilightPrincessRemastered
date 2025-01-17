
using System.Collections.Generic;
using UnityEngine;
using System;
using Vector3 = UnityEngine.Vector3;

public class PrimitiveProcessor
{
    private List<Vector3> positions;
    private List<Vector3> normals;
    private List<ColorPP>[] vertexColors;

    public PrimitiveProcessor(List<Vector3> positions, List<Vector3> normals,  List<ColorPP>[] vertexColors)
    {
        this.positions = positions;
        this.normals = normals;
        this.vertexColors = vertexColors;
    }

    public void ProcessPrimitive(int primitiveType, List<PrimitivePoint> points)
    {
        switch (primitiveType)
        {
            case 0x98:
                ProcessTriangleStrip(points);
                break;
            case 0xa0:
                ProcessTriangleFan(points);
                break;
            default:
                break;
        }
    }

    private void ProcessTriangleStrip(List<PrimitivePoint> points)
    {
        for (int i = 0; i < points.Count - 2; i++)
        {
            Vector3 p0 = GetPosition(points[i].PositionIndex);
            Vector3 p1 = GetPosition(points[i + 1].PositionIndex);
            Vector3 p2 = GetPosition(points[i + 2].PositionIndex);


        }
    }

    private void ProcessTriangleFan(List<PrimitivePoint> points)
    {
        Vector3 p0 = GetPosition(points[0].PositionIndex);
        for (int i = 1; i < points.Count - 1; i++)
        {
            Vector3 p1 = GetPosition(points[i].PositionIndex);
            Vector3 p2 = GetPosition(points[i + 1].PositionIndex);


        }
    }

    private Vector3 GetPosition(int index)
    {
        if (index >= 0 && index < positions.Count)
        {
            return positions[index];
        }
        else
        {
            throw new IndexOutOfRangeException("Position index out of range.");
        }
    }
}

public class PrimitivePoint
{
    public int PositionIndex { get; set; }
}

public class ColorPP
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
    public float A { get; set; }
}

public class LoopRepresentation
{
    public int vertex = -1;
    public float[] UVs = new float[8];
    public float[] VColors = new float[2];
    public float[] normal;
    public int mm = -1;

}

public class FaceRepresentation
{
    public int loop_start = -1;
    public object material;
}

public class ModelRepresentation
{
    public List<Vector3> vertices = new List<Vector3>();
    public List<FaceRepresentation> faces = new List<FaceRepresentation>();
    public List<LoopRepresentation> loops = new List<LoopRepresentation>();
    public bool[] hasTexCoords = new bool[8];
    public bool[] hasColors = new bool[2];
    public bool hasMatrixIndices;
    public bool hasNormals;

    public Dictionary<int, List<int>> dedup_verts = new Dictionary<int, List<int>>();


    public float[] ToArray(string type)
    {
        if (type == "co")
        {
            List<float> retList = new List<float>();
            foreach (Vector3 com in vertices)
            {
                retList.Add(com.x);
                retList.Add(com.y);
                retList.Add(com.z);
            }
            return retList.ToArray();
        }
        else if (type == "loop_start")
        {
            List<float> retList = new List<float>();
            foreach (FaceRepresentation com in faces)
            {
                retList.Add(com.loop_start);
            }
            return retList.ToArray();
        }
        else if (type == "normal")
        {
            List<float> retList = new List<float>();
            foreach (LoopRepresentation com in loops)
            {
                retList.Add(com.normal[0]);
                retList.Add(com.normal[1]);
                retList.Add(com.normal[2]);
            }
            return retList.ToArray();
        }
        else if (type == "v_indexes")
        {
            List<float> retList = new List<float>();
            foreach (LoopRepresentation com in loops)
            {
                retList.Add(com.vertex);
            }
            return retList.ToArray();
        }
        else
        {
            throw new ArgumentException("wrong array type");
        }
    }

    public LoopRepresentation GetLoop(int faceidx, int i)
    {
        if (i < 0 || i > 2)
        {
            Debug.LogError("Index must be between 0 and 2");
        }
        return loops[faces[faceidx].loop_start + i];
    }

    public (LoopRepresentation, LoopRepresentation, LoopRepresentation) GetLoops(int faceidx)
    {
        int l1 = faces[faceidx].loop_start;
        return (loops[l1], loops[l1 + 1], loops[l1 + 2]);
    }

    public (int, int, int) GetVerts(int faceidx)
    {
        int l1 = faces[faceidx].loop_start;
        return (loops[l1].vertex, loops[l1 + 1].vertex, loops[l1 + 2].vertex);
    }
}
