
using System.Collections.Generic;
using UnityEngine;
using System;
using Vector3 = UnityEngine.Vector3; // Für Vector3 und Vector2

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
            case 0x98: // GL_TRIANGLE_STRIP
                ProcessTriangleStrip(points);
                break;
            case 0xa0: // GL_TRIANGLE_FAN
                ProcessTriangleFan(points);
                break;
            default:
                // Handle unknown primitive type
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

            // Additional processing for texture coordinates, normals, vertex colors, etc.
            // ...

            // Do something with the vertices p0, p1, p2
            // ...
        }
    }

    private void ProcessTriangleFan(List<PrimitivePoint> points)
    {
        Vector3 p0 = GetPosition(points[0].PositionIndex);
        for (int i = 1; i < points.Count - 1; i++)
        {
            Vector3 p1 = GetPosition(points[i].PositionIndex);
            Vector3 p2 = GetPosition(points[i + 1].PositionIndex);

            // Additional processing for texture coordinates, normals, vertex colors, etc.
            // ...

            // Do something with the vertices p0, p1, p2
            // ...
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
            // Handle index out of range error
            throw new IndexOutOfRangeException("Position index out of range.");
        }
    }
}

public class PrimitivePoint
{
    public int PositionIndex { get; set; }
    // Add other properties like NormalIndex, TexCoordIndex, ColorIndex, etc. as needed
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
    public int mm = -1; // reference to the multimatrix entry used to move the point

    // public bool Equals(LoopRepresentation other)
    // {
    //     return vertex == other.vertex &&
    //            UVs.SequenceEqual(other.UVs) &&
    //            VColors.SequenceEqual(other.VColors) &&
    //            (normal == null && other.normal == null || normal != null && normal.SequenceEqual(other.normal));
    // }
}

public class FaceRepresentation
{
    public int loop_start = -1;
    public object material; // Replace 'object' with the appropriate type for your materials
}

public class ModelRepresentation
{
    public List<Vector3> vertices = new List<Vector3>();
    public List<FaceRepresentation> faces = new List<FaceRepresentation>();
    public List<LoopRepresentation> loops = new List<LoopRepresentation>();
    public bool[] hasTexCoords = new bool[8]; // will be set to true if necessary
    public bool[] hasColors = new bool[2];
    public bool hasMatrixIndices;
    public bool hasNormals;

    public Dictionary<int, List<int>> dedup_verts = new Dictionary<int, List<int>>(); // {original_id: (new ids)}

    // some faces might reference the same vert multiple times:
    // for this (somewhat dumb and corner-case) occasion,
    // "cloned" verts must be kept.

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
