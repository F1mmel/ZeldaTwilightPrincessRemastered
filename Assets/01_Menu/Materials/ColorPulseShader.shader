Shader "Custom/URPColorPulseShaderTransparent"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        _PulseColor ("Pulse Color", Color) = (1,0,0,1)
        _PulseFrequency ("Pulse Frequency", Float) = 0.5
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
            float4 _PulseColor;
            float _PulseFrequency;
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
                // Pulse effect based on time
                float pulse = sin(_Time.y * _PulseFrequency * 2 * 3.14159); // Pulsation basierend auf Zeit
                fixed4 finalColor = lerp(_Color, _PulseColor, pulse);

                // Apply final color to the pixel, considering canvas group alpha
                fixed4 texColor = tex2D(_MainTex, i.uv);
                finalColor *= texColor; // Multiply with texture color to preserve texture details

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
            float4 _PulseColor;
            float _PulseFrequency;
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
                // Pulse effect based on time
                float pulse = sin(_Time.y * _PulseFrequency * 2 * 3.14159); // Pulsation basierend auf Zeit
                fixed4 finalColor = lerp(_Color, _PulseColor, pulse);

                // Apply final color to the pixel, considering canvas group alpha
                fixed4 texColor = tex2D(_MainTex, i.uv);
                finalColor *= texColor; // Multiply with texture color to preserve texture details

                // Apply canvas group alpha (considering both material and canvas group alpha)
                finalColor.a *= _Color.a;

                return finalColor;
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}
