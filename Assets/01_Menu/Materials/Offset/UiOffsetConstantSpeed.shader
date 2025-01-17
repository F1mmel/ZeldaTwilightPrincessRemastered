Shader "UI/UiOffsetFixedSpeed"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        _Speed ("Offset Speed", Float) = 1.0
        _BlurSize ("Blur Size", Range(0, 10)) = 5.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            Tags { "LightMode" = "UniversalForward" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _Speed;
            float _BlurSize;
            float2 _MainTex_TexelSize;
            
            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 offset = float2(sin(_Time.y * _Speed), cos(_Time.x * _Speed)) * 0.3;

                float2 finalOffset = offset.x * float2(.3, 0) + offset.y * float2(.3, 1);

                float4 blurredColor = float4(0, 0, 0, 0);
                float blurPixels = _BlurSize;
                float blurSize = _BlurSize * _MainTex_TexelSize.x;

                for (float x = -blurPixels; x <= blurPixels; x++)
                {
                    float2 blurOffset = float2(x, 0) * blurSize;
                    blurredColor += tex2D(_MainTex, i.texcoord + finalOffset + blurOffset);
                }


                for (float y = -blurPixels; y <= blurPixels; y++)
                {
                    float2 blurOffset = float2(0, y) * blurSize;
                    blurredColor += tex2D(_MainTex, i.texcoord + finalOffset + blurOffset);
                }

                blurredColor /= (blurPixels * 2.0 + 1.0);

                float4 col = blurredColor * _Color;
                return col;
            }
            ENDCG
        }
    }
}