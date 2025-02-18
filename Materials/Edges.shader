Shader "Custom/EdgeDetection"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EdgeThreshold ("Edge Threshold", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment FragEdgeDetection
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
            float4 _MainTex_TexelSize; // (1/width, 1/height, width, height)
            float _EdgeThreshold;

            Varyings vert(appdata_t v)
            {
                Varyings o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 FragEdgeDetection(Varyings i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;

                // Sample surrounding pixels
                float3 sampleTL = tex2D(_MainTex, i.uv + texelSize * float2(-1, -1)).rgb;
                float3 sampleTC = tex2D(_MainTex, i.uv + texelSize * float2( 0, -1)).rgb;
                float3 sampleTR = tex2D(_MainTex, i.uv + texelSize * float2( 1, -1)).rgb;

                float3 sampleML = tex2D(_MainTex, i.uv + texelSize * float2(-1,  0)).rgb;
                float3 sampleMC = tex2D(_MainTex, i.uv).rgb; // Center pixel
                float3 sampleMR = tex2D(_MainTex, i.uv + texelSize * float2( 1,  0)).rgb;

                float3 sampleBL = tex2D(_MainTex, i.uv + texelSize * float2(-1,  1)).rgb;
                float3 sampleBC = tex2D(_MainTex, i.uv + texelSize * float2( 0,  1)).rgb;
                float3 sampleBR = tex2D(_MainTex, i.uv + texelSize * float2( 1,  1)).rgb;

                // Convert to grayscale (luminance)
                float lumTL = dot(sampleTL, float3(0.299, 0.587, 0.114));
                float lumTC = dot(sampleTC, float3(0.299, 0.587, 0.114));
                float lumTR = dot(sampleTR, float3(0.299, 0.587, 0.114));

                float lumML = dot(sampleML, float3(0.299, 0.587, 0.114));
                float lumMC = dot(sampleMC, float3(0.299, 0.587, 0.114));
                float lumMR = dot(sampleMR, float3(0.299, 0.587, 0.114));

                float lumBL = dot(sampleBL, float3(0.299, 0.587, 0.114));
                float lumBC = dot(sampleBC, float3(0.299, 0.587, 0.114));
                float lumBR = dot(sampleBR, float3(0.299, 0.587, 0.114));

                // Sobel operator
                float gx = (-1.0 * lumTL) + ( 1.0 * lumTR) +
                           (-2.0 * lumML) + ( 2.0 * lumMR) +
                           (-1.0 * lumBL) + ( 1.0 * lumBR);

                float gy = (-1.0 * lumTL) + (-2.0 * lumTC) + (-1.0 * lumTR) +
                           ( 1.0 * lumBL) + ( 2.0 * lumBC) + ( 1.0 * lumBR);

                // Compute edge magnitude
                float edge = sqrt(gx * gx + gy * gy);

                // Apply threshold
                return edge > _EdgeThreshold ? float4(1,1,1,1) : float4(0,0,0,1);
            }
            ENDCG
        }
    }
}
