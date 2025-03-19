Shader "Hidden/PhospheneShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 1

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            StructuredBuffer<float2> _Positions;
            StructuredBuffer<float> _Sigma;
            int _Count;
            float _Fov;
            int _Offsetx;
            int _Offsety;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                   

                //float2 pixelCoord = i.uv *(2*_Fov)-_Fov;
                float2 pixelCoord = i.uv*512;
                fixed4 color = fixed4(0, 0, 0, 1); // Start with black (or transparent)
                float2 _Offset = float2(_Offsetx,_Offsety);
                float2 adjustedCoord = pixelCoord - _Offset;
           
                    
                    // Loop over each phosphene.
                for (int idx = 0; idx < _Count; idx++)
                {
                    if (_Sigma[idx]==-1){
                        continue; // Skip this iteration
                    }
                    // Calculate the difference from the current phosphene position to the adjusted coordinate.
                    // This is equivalent to (_Positions[idx] + offset) - pixelCoord.
                    float2 diff = _Positions[idx] - adjustedCoord;
        
                    // Compute the squared distance (avoiding the cost of sqrt)
                    float distSq = dot(diff, diff);
        
                    // Use the squared distance in the Gaussian falloff.
                    // Note: This formula replaces:
                    //   exp(-0.5 * (length(diff)^2) / sigma)
                    // with the more efficient dot product calculation.
                    float sigma = _Sigma[idx];
                    float falloff = exp(-0.5 * distSq / sigma);
        
                    // Accumulate the phosphene's contribution.
                    color += fixed4(falloff, falloff, falloff, falloff);
                }
                
                return color;
                
            }
            ENDCG
        }
    }
}