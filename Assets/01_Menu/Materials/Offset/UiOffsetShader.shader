Shader "Custom/UiOffset"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        _OffsetSpeed ("Offset Speed", Vector) = (0, 1, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        // Pass for rendering the object
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha // Standard alpha blending

            Tags { "LightMode" = "UniversalForward" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            float4 _OffsetSpeed;
            sampler2D _MainTex;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate offset based on time
                float2 offsetSpeed = _OffsetSpeed.xy;
                float yOffset = offsetSpeed.y * _Time.y;
                float xOffset = offsetSpeed.x * _Time.y;
                float2 offsetUV = i.uv + float2(xOffset, yOffset);

                // Apply final color to the pixel, considering canvas group alpha
                fixed4 texColor = tex2D(_MainTex, offsetUV);
                fixed4 finalColor = texColor * _Color;

                // Apply canvas group alpha (considering both material and canvas group alpha)
                finalColor.a *= _Color.a;

                return finalColor;
            }
            ENDCG
        }

        // Pass for rendering the object when it's masked by a CanvasGroup
        Pass
        {
            ZWrite On
            ColorMask RGB
            Blend SrcAlpha OneMinusSrcAlpha // Standard alpha blending

            Tags { "LightMode" = "UniversalForward" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            float4 _OffsetSpeed;
            sampler2D _MainTex;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate offset based on time
                float2 offsetSpeed = _OffsetSpeed.xy;
                float yOffset = offsetSpeed.y * _Time.y;
                float xOffset = offsetSpeed.x * _Time.y;
                float2 offsetUV = i.uv + float2(xOffset, yOffset);

                // Apply final color to the pixel, considering canvas group alpha
                fixed4 texColor = tex2D(_MainTex, offsetUV);
                fixed4 finalColor = texColor * _Color;

                // Apply canvas group alpha (considering both material and canvas group alpha)
                finalColor.a *= _Color.a;

                return finalColor;
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}
