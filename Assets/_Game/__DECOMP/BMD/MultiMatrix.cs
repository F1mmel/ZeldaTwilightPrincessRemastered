/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Collections.Generic;
using UnityEngine;

public class MultiMatrix
{
    public List<float> weights = new List<float>();
    public List<int> indices = new List<int>();
}

public class DataHolder
{
    // Define the needed data structures and members here
}

public class YourClass
{
    private DataHolder dataholder;

    public Matrix4x4 LocalMatrix(Joint jnt, int i, bool use_scale)
    {
        Frame f = jnt.frames[i];
        Vector3 scaleVector = new Vector3(f.sx, f.sy, f.sz);
        Matrix4x4 sx = Matrix4x4.Scale(new Vector3(scaleVector.x, 1, 1));
        Matrix4x4 sy = Matrix4x4.Scale(new Vector3(1, scaleVector.y, 1));
        Matrix4x4 sz = Matrix4x4.Scale(new Vector3(1, 1, scaleVector.z));

        if (use_scale)
        {
            return f.matrix * sz * sy * sx;
        }
        else
        {
            return f.matrix;
        }
    }

    public Matrix4x4 FrameMatrix(Frame f)
    {
        Vector3 translation = new Vector3(f.t.x, f.t.y, f.t.z);
        Quaternion rotation = Quaternion.Euler(new Vector3(f.rx, f.ry, f.rz));
        Matrix4x4 t = Matrix4x4.Translate(translation);
        Matrix4x4 r = Matrix4x4.Rotate(rotation);
        return t * r;
    }

    public Matrix4x4 UpdateMatrix(Frame frame, Matrix4x4 parentmatrix)
    {
        return parentmatrix * FrameMatrix(frame);
    }

    public void UpdateMatrixTable(Evp evp, Drw drw, Joint jnt, CurrentPacket currPacket, List<MultiMatrix> multiMatrixTable, List<Matrix4x4> matrixTable, List<bool> isMatrixWeighted, bool use_scale)
    {
        for (int n = 0; n < currPacket.matrixTable.Length; n++)
        {
            int index = currPacket.matrixTable[n];

            // if index is 0xffff, use the last packet's data.
            if (index != 0xffff) // 0xffff this means keep old entry
            {
                if (drw.isWeighted[index])
                {
                    Matrix4x4 m = new Matrix4x4();
                    m.SetColumn(0, Vector4.zero);
                    m.SetColumn(1, Vector4.zero);
                    m.SetColumn(2, Vector4.zero);
                    m.SetColumn(3, Vector4.zero);

                    MultiMatrix mm = evp.weightedIndices[drw.data[index]];
                    MultiMatrix singleMultiMatrixEntry = new MultiMatrix();

                    singleMultiMatrixEntry.weights.AddRange(mm.weights);
                    singleMultiMatrixEntry.indices.AddRange(mm.indices);
                    for (int r = 0; r < mm.weights.Count; r++)
                    {
                        Matrix4x4 sm1 = evp.matrices[mm.indices[r]];
                        Matrix4x4 sm2 = LocalMatrix(jnt, mm.indices[r], use_scale);
                        Matrix4x4 sm3 = sm2 * sm1;

                        for (int j = 0; j < 4; j++)
                        {
                            for (int k = 0; k < 4; k++)
                            {
                                m[j, k] += sm3[j, k] * mm.weights[r];
                            }
                        }
                    }

                    while (multiMatrixTable.Count <= n)
                        multiMatrixTable.Add(null);
                    multiMatrixTable[n] = singleMultiMatrixEntry;
                    m[3, 3] = 1; // fixed
                    while (matrixTable.Count <= n)
                        matrixTable.Add(null);
                    matrixTable[n] = m;
                    while (isMatrixWeighted.Count <= n)
                        isMatrixWeighted.Add(false);
                    isMatrixWeighted[n] = true;
                }
                else
                {
                    while (matrixTable.Count <= n)
                        matrixTable.Add(null);
                    while (isMatrixWeighted.Count <= n)
                        isMatrixWeighted.Add(false);
                    matrixTable[n] = jnt.frames[drw.data[index]].matrix;
                    isMatrixWeighted[n] = false;

                    MultiMatrix singleMultiMatrixEntry = new MultiMatrix();
                    singleMultiMatrixEntry.weights.Add(1);
                    singleMultiMatrixEntry.indices.Add(drw.data[index]);

                    while (multiMatrixTable.Count <= n)
                        multiMatrixTable.Add(null);
                    multiMatrixTable[n] = singleMultiMatrixEntry;
                }
            }
        }
    }

    public Matrix4x4 RotationPart(Matrix4x4 mtx)
    {
        Matrix4x4 ret = mtx;
        for (int i = 0; i < 3; i++)
        {
            ret[i, 3] = 0;
        }
        return ret;
    }

    public bool IsNear(MultiMatrix mm1, MultiMatrix mm2)
    {
        if (mm1.indices.Count != mm2.indices.Count)
            return false;

        Dictionary<int, float> mm2d = new Dictionary<int, float>();
        for (int i = 0; i < mm2.indices.Count; i++)
        {
            mm2d[mm2.indices[i]] = mm2.weights[i];
        }

        for (int i = 0; i < mm1.indices.Count; i++)
        {
            if (!mm2d.ContainsKey(mm1.indices[i]))
                return false;
            if (Mathf.Abs(mm1.weights[i] - mm2d[mm1.indices[i]]) > 1E-3f)
                return false;
        }

        return true;
    }
}
*/