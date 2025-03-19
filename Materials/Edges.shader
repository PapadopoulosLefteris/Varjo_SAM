Shader "Custom/CannyEdgeDetection"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LowThreshold ("Low Threshold", Range(0, 1)) = 0.4
        _HighThreshold ("High Threshold", Range(0, 1)) = 0.9
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment FragCannyEdgeDetection
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _LowThreshold;
            float _HighThreshold;

            Varyings vert(appdata_t v)
            {
                Varyings o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float luminance(float3 color)
            {
                return dot(color, float3(0.299, 0.587, 0.114));
            }

            float4 FragCannyEdgeDetection(Varyings i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;

                // Gaussian Blur Kernel (simplified)
                float3 blur = (
                    tex2D(_MainTex, i.uv + texel * float2(-1, -1)).rgb * 1 +
                    tex2D(_MainTex, i.uv + texel * float2( 0, -1)).rgb * 2 +
                    tex2D(_MainTex, i.uv + texel * float2( 1, -1)).rgb * 1 +
                    tex2D(_MainTex, i.uv + texel * float2(-1,  0)).rgb * 2 +
                    tex2D(_MainTex, i.uv).rgb * 4 +
                    tex2D(_MainTex, i.uv + texel * float2( 1,  0)).rgb * 2 +
                    tex2D(_MainTex, i.uv + texel * float2(-1,  1)).rgb * 1 +
                    tex2D(_MainTex, i.uv + texel * float2( 0,  1)).rgb * 2 +
                    tex2D(_MainTex, i.uv + texel * float2( 1,  1)).rgb * 1
                ) / 16.0;

                float lum = luminance(blur);

                // Sobel operator for gradients
                float gx = (
                    -1 * luminance(tex2D(_MainTex, i.uv + texel * float2(-1, -1)).rgb) +
                     1 * luminance(tex2D(_MainTex, i.uv + texel * float2(1, -1)).rgb) +
                    -2 * luminance(tex2D(_MainTex, i.uv + texel * float2(-1, 0)).rgb) +
                     2 * luminance(tex2D(_MainTex, i.uv + texel * float2(1, 0)).rgb) +
                    -1 * luminance(tex2D(_MainTex, i.uv + texel * float2(-1, 1)).rgb) +
                     1 * luminance(tex2D(_MainTex, i.uv + texel * float2(1, 1)).rgb)
                );

                float gy = (
                    -1 * luminance(tex2D(_MainTex, i.uv + texel * float2(-1, -1)).rgb) +
                    -2 * luminance(tex2D(_MainTex, i.uv + texel * float2(0, -1)).rgb) +
                    -1 * luminance(tex2D(_MainTex, i.uv + texel * float2(1, -1)).rgb) +
                     1 * luminance(tex2D(_MainTex, i.uv + texel * float2(-1, 1)).rgb) +
                     2 * luminance(tex2D(_MainTex, i.uv + texel * float2(0, 1)).rgb) +
                     1 * luminance(tex2D(_MainTex, i.uv + texel * float2(1, 1)).rgb)
                );

                // Gradient magnitude and direction
                float gradient = sqrt(gx * gx + gy * gy);

                // Double thresholding
                if (gradient >= _HighThreshold)
                    return float4(1, 1, 1, 1); // Strong edge
                else if (gradient >= _LowThreshold)
                    return float4(0.5, 0.5, 0.5, 1); // Weak edge (visualized differently)
                else
                    return float4(0, 0, 0, 1); // Non-edge
            }
            ENDCG
        }
    }
}
