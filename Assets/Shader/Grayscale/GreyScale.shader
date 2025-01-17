Shader "Grayscale"
{
    Properties
    {
        _GrayscaleColor ("Grayscale Color", Color) = (1,1,1,1)
    }

    Subshader
    {
        Cull off
        Pass
        {
            CGPROGRAM
            #pragma vertex vertex_shader
            #pragma fragment pixel_shader
            #pragma target 2.0

            struct custom_type
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _GrayscaleColor;

            custom_type vertex_shader (float4 vertex : POSITION, float2 uv : TEXCOORD0)
            {
                custom_type vs;
                vs.vertex = UnityObjectToClipPos(vertex);
                vs.uv = uv;
                return vs;
            }

            float4 pixel_shader (custom_type ps) : COLOR
            {
                float3 color = _GrayscaleColor.rgb; // Nutze die angepasste Farbe
                float grayscale = dot(color, _GrayscaleColor.rgb);
                return float4(grayscale, grayscale, grayscale, 1.0); // Rückgabe der Graustufe
            }
            ENDCG
        }
    }
}
