using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BufferUtil
{
    public static byte[] Slice(byte[] buffer, int start)
    {
        int length = buffer.Length - start;
        byte[] slicedBytes = new byte[length];
        Array.Copy(buffer, start, slicedBytes, 0, length);

        return slicedBytes;
    }
    public static byte[] Slice(byte[] buffer, int start, int end)
    {
        int length = end - start;
        byte[] slicedBytes = new byte[length];
        Array.Copy(buffer, start, slicedBytes, 0, length);

        return slicedBytes;
    }
    
    public static byte[] Slice(Stream stream, long start, long end)
    {
        stream.Position = start;
        long length = end - start;
        byte[] buffer = new byte[length];
        stream.Read(buffer, 0, (int)length);
        return buffer;
    }

    public static byte[] Subarray(byte[] buffer, int start, int size)
    {
        byte[] subarray = new byte[size];
        Array.Copy(buffer, start, subarray, 0, size);
        return subarray;
    }
}
