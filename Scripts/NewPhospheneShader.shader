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
                
           
                    
                    for(int idx = 0; idx < _Count; idx++)
                    {
                        float2 position = _Positions[idx]+ _Offset;
                        float sigma = _Sigma[idx];
                        float distanceToCenter = length(position - pixelCoord);
        
                        // Gaussian falloff
                        float falloff = exp(-0.5 * (distanceToCenter * distanceToCenter)/(sigma));//(0.01)); //calculate square in controller

                        // Add the new phosphene's color to the current color
                        color += fixed4(1, 1, 1, 1)* falloff;
                    }
                
                return color;
                
            }
            ENDCG
        }
    }
}