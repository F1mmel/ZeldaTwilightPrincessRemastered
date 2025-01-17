using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;


    public class VertexDescription
    {
        private List<ShaderAttributeIds> m_enabledAttributes;

        public VertexDescription()
        {
            m_enabledAttributes = new List<ShaderAttributeIds>();
        }

        public bool AttributeIsEnabled(ShaderAttributeIds attribute)
        {
            return m_enabledAttributes.Contains(attribute);
        }

        public int GetAttributeSize(ShaderAttributeIds attribute)
        {
            switch (attribute)
            {
                case ShaderAttributeIds.Position:
                case ShaderAttributeIds.Normal:
                    return 3;
                case ShaderAttributeIds.Color0:
                case ShaderAttributeIds.Color1:
                    return 4;
                case ShaderAttributeIds.Tex0:
                case ShaderAttributeIds.Tex1:
                case ShaderAttributeIds.Tex2:
                case ShaderAttributeIds.Tex3:
                case ShaderAttributeIds.Tex4:
                case ShaderAttributeIds.Tex5:
                case ShaderAttributeIds.Tex6:
                case ShaderAttributeIds.Tex7:
                    return 2;
                case ShaderAttributeIds.PosMtxIndex:
                    return 1;
                default:
                    Console.WriteLine($"Unsupported attribute: {attribute} in GetAttributeSize!");
                    return 0;
            }
        }

        public VertexAttribPointerType GetAttributePointerType(ShaderAttributeIds attribute)
        {
            switch (attribute)
            {
                case ShaderAttributeIds.Position:
                case ShaderAttributeIds.Normal:
                case ShaderAttributeIds.Binormal:
                case ShaderAttributeIds.Color0:
                case ShaderAttributeIds.Color1:
                case ShaderAttributeIds.Tex0:
                case ShaderAttributeIds.Tex1:
                case ShaderAttributeIds.Tex2:
                case ShaderAttributeIds.Tex3:
                case ShaderAttributeIds.Tex4:
                case ShaderAttributeIds.Tex5:
                case ShaderAttributeIds.Tex6:
                case ShaderAttributeIds.Tex7:
                    return VertexAttribPointerType.Float;
                case ShaderAttributeIds.PosMtxIndex:
                    return VertexAttribPointerType.Int;

                default:
                    Console.WriteLine("Unsupported ShaderAttributeId: {0}", attribute);
                    return VertexAttribPointerType.Float;
            }
        }

        public int GetStride(ShaderAttributeIds attribute)
        {
            switch (attribute)
            {
                case ShaderAttributeIds.Position:
                case ShaderAttributeIds.Normal:
                case ShaderAttributeIds.Binormal:
                    return 4 * 3;
                case ShaderAttributeIds.Color0:
                case ShaderAttributeIds.Color1:
                    return 4 * 4;
                case ShaderAttributeIds.Tex0:
                case ShaderAttributeIds.Tex1:
                case ShaderAttributeIds.Tex2:
                case ShaderAttributeIds.Tex3:
                case ShaderAttributeIds.Tex4:
                case ShaderAttributeIds.Tex5:
                case ShaderAttributeIds.Tex6:
                case ShaderAttributeIds.Tex7:
                    return 4 * 2;
                case ShaderAttributeIds.PosMtxIndex:
                    return 4 * 1;
                default:
                    Console.WriteLine("Unsupported ShaderAttributeId: {0}", attribute);
                    return 0;
            }
        }
    }
    
    public enum ShaderAttributeIds
    {
        None = 0,
        Position = 1,
        Normal = 2,
        Binormal = 3,
        Color0 = 4,
        Color1 = 5,
        Tex0 = 6,
        Tex1 = 7,
        Tex2 = 8,
        Tex3 = 9,
        Tex4 = 10,
        Tex5 = 11,
        Tex6 = 12,
        Tex7 = 13,
        PosMtxIndex = 14,
    }