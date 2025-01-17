using System;
using System.Collections.Generic;
using System.Linq;

class Fill_index_list_class
{
    public List<List<byte[]>> Fill_index_list(byte[] data, int start, byte texture_format3, byte mipmaps_number, byte[] real_block_width_array, byte[] block_width_array, byte[] block_height_array, bool reverse_x, bool reverse_y)
    {
        int blocks_wide;
        int blocks_tall;
        int cursor = start;
        int count = 0;
        List<byte[]> index_list = new List<byte[]>();
        List<List<byte[]>> index_list_list = new List<List<byte[]>>();
        ushort[] canvas = { BTILoader.canvas_dim[0][0], BTILoader.canvas_dim[0][1], BTILoader.canvas_dim[0][2], BTILoader.canvas_dim[0][3] };
        switch (texture_format3)
        {
            case 6:
                {
                    byte[] index = new byte[0];
                    for (byte m = 0; m <= mipmaps_number; m++)
                    {
                        Array.Resize(ref index, BTILoader.canvas_dim[m][2] << 2);
                        blocks_wide = BTILoader.canvas_dim[m][2] >> 2;
                        blocks_tall = BTILoader.canvas_dim[m][3] >> 2;
                        if (reverse_x)
                        {
                            for (int t = 0; t < blocks_tall; t++)
                            {
                                for (int h = 0; h < 4; h++)
                                {
                                    for (int b = 0; b < blocks_wide; b++)
                                    {
                                        for (int w = 0; w < 8; w++)
                                        {
                                            index[count] = data[cursor];
                                            cursor++;
                                            count++;
                                        }
                                        cursor += 24;
                                        for (int w = 0; w < 8; w++)
                                        {
                                            index[count] = data[cursor];
                                            cursor++;
                                            count++;
                                        }
                                        cursor += 24;
                                    }
                                    index_list.Add(index.Reverse().ToArray());
                                    count = 0;
                                    cursor -= (blocks_wide << 6) - 8;
                                }
                                cursor += ((blocks_wide - 1) << 6) + 32;
                            }
                        }
                        else
                        {
                            for (int t = 0; t < blocks_tall; t++)
                            {
                                for (int h = 0; h < 4; h++)
                                {
                                    for (int b = 0; b < blocks_wide; b++)
                                    {
                                        for (int w = 0; w < 8; w++)
                                        {
                                            index[count] = data[cursor];
                                            cursor++;
                                            count++;
                                        }
                                        cursor += 24;
                                        for (int w = 0; w < 8; w++)
                                        {
                                            index[count] = data[cursor];
                                            cursor++;
                                            count++;
                                        }
                                        cursor += 24;
                                    }
                                    index_list.Add(index.ToArray());
                                    count = 0;
                                    cursor -= (blocks_wide << 6) - 8;
                                }
                                cursor += ((blocks_wide - 1) << 6) + 32;
                            }
                        }
                        if (!reverse_y)
                        {
                            index_list.Reverse();
                        }
                        index_list_list.Add(index_list.ToList());
                        if (m + 1 <= mipmaps_number)
                        {
                            canvas[0] >>= 1;
                            canvas[1] >>= 1;
                            canvas[2] = (ushort)(canvas[0] + (4 - (canvas[0] % 4) % 4));
                            canvas[3] = (ushort)(canvas[1] + (4 - (canvas[1] % 4) % 4));
                            BTILoader.canvas_dim.Add(canvas.ToArray());
                            index_list.Clear();
                        }
                    }
                    break;
                }
            case 0xe:
                {
                    byte[] index = new byte[8];
                    for (byte m = 0; m <= mipmaps_number; m++)
                    {
                        blocks_wide = BTILoader.canvas_dim[m][2] >> 3;
                        blocks_tall = BTILoader.canvas_dim[m][3] >> 3;
                        if (reverse_x)
                        {
                            for (int b = 0; b < (blocks_tall * blocks_wide) << 2; b++)
                            {
                                for (count = 0; count < 8; count++)
                                {
                                    index[count] = data[cursor];
                                    cursor++;
                                }
                                index_list.Add(index.Reverse().ToArray());
                            }
                        }
                        else
                        {
                            for (int b = 0; b < (blocks_tall * blocks_wide) << 2; b++)
                            {
                                for (count = 0; count < 8; count++)
                                {
                                    index[count] = data[cursor];
                                    cursor++;
                                }
                                index_list.Add(index.ToArray());
                            }
                        }
                        if (!reverse_y)
                        {
                            index_list.Reverse();
                        }
                        index_list_list.Add(index_list.ToList());
                        if (m + 1 <= mipmaps_number)
                        {
                            canvas[0] >>= 1;
                            canvas[1] >>= 1;
                            canvas[2] = (ushort)(canvas[0] + (8 - (canvas[0] % 8) % 8));
                            canvas[3] = (ushort)(canvas[1] + (8 - (canvas[1] % 8) % 8));
                            BTILoader.canvas_dim.Add(canvas.ToArray());
                            index_list.Clear();
                        }
                    }
                    break;
                }
            default:
                {
                    int len;
                    byte[] index = new byte[0];
                    for (byte m = 0; m <= mipmaps_number; m++)
                    {
                        len = BTILoader.canvas_dim[m][2];
                        switch (texture_format3)
                        {
                            case 0:
                            case 8:
                                {
                                    len >>= 1;
                                    break;
                                }
                            case 3:
                            case 4:
                            case 5:
                            case 10:
                                {
                                    len <<= 1;
                                    break;
                                }
                        }
                        Array.Resize(ref index, len);
                        blocks_wide = BTILoader.canvas_dim[m][2] / real_block_width_array[texture_format3];
                        blocks_tall = BTILoader.canvas_dim[m][3] / block_height_array[texture_format3];
                        if (reverse_x)
                        {
                            for (int t = 0; t < blocks_tall; t++)
                            {
                                for (byte h = 0; h < block_height_array[texture_format3]; h++)
                                {

                                    for (int b = 0; b < blocks_wide; b++)
                                    {
                                        for (byte w = 0; w < block_width_array[texture_format3]; w++)
                                        {
                                            index[count] = data[cursor];
                                            count++;
                                            cursor++;
                                        }
                                        cursor += (block_height_array[texture_format3] - 1) * block_width_array[texture_format3];
                                    }
                                    index_list.Add(index.Reverse().ToArray());
                                    count = 0;
                                    cursor -= (blocks_wide * block_height_array[texture_format3] * block_width_array[texture_format3]) - block_width_array[texture_format3];
                                }
                                cursor += ((blocks_wide - 1) * block_height_array[texture_format3] * block_width_array[texture_format3]);
                            }
                        }
                        else
                        {
                            for (int t = 0; t < blocks_tall; t++)
                            {
                                for (byte h = 0; h < block_height_array[texture_format3]; h++)
                                {

                                    for (int b = 0; b < blocks_wide; b++)
                                    {
                                        for (byte w = 0; w < block_width_array[texture_format3]; w++)
                                        {
                                            index[count] = data[cursor];
                                            count++;
                                            cursor++;
                                        }
                                        cursor += (block_height_array[texture_format3] - 1) * block_width_array[texture_format3];
                                    }
                                    index_list.Add(index.ToArray());
                                    count = 0;
                                    cursor -= (blocks_wide * block_height_array[texture_format3] * block_width_array[texture_format3]) - block_width_array[texture_format3];
                                }
                                cursor += ((blocks_wide - 1) * block_height_array[texture_format3] * block_width_array[texture_format3]);
                            }
                        }
                        if (!reverse_y)
                        {
                            index_list.Reverse();
                        }
                        index_list_list.Add(index_list.ToList());
                        if (m + 1 <= mipmaps_number)
                        {
                            canvas[0] >>= 1;
                            canvas[1] >>= 1;
                            canvas[2] = (ushort)(canvas[0] + ((real_block_width_array[texture_format3] - (canvas[0] % real_block_width_array[texture_format3])) % real_block_width_array[texture_format3]));
                            canvas[3] = (ushort)(canvas[1] + ((block_height_array[texture_format3] - (canvas[1] % block_height_array[texture_format3])) % block_height_array[texture_format3]));
                            BTILoader.canvas_dim.Add(canvas.ToArray());
                            index_list.Clear();
                        }
                    }
                    break;
                }
        }
        return index_list_list;
    }
}