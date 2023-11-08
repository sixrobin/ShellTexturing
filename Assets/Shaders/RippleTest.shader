Shader "Ripple Test"
{
    Properties
    {
        _Size ("Size", Float) = 1
        _CircleSmoothing ("Circle Smoothing", Float) = 0
        _RingSmoothing ("Ring Smoothing", Float) = 0
        _Lifetime ("Lifetime", Float) = 1
        _Duration ("Duration", Float) = 2
    }
    
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        
        LOD 100

        Pass
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float _CircleSmoothing;
            float _RingSmoothing;
            float _Size;
            float _Lifetime;
            float _Duration;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uvCentered = (i.uv - 0.5) * 2 / _Lifetime;

                // TMP test.
                float circle = smoothstep(_Lifetime - (_CircleSmoothing/2), _Lifetime + (_CircleSmoothing/2), length(uvCentered));
                float ring = smoothstep(_Lifetime + _Size - (_RingSmoothing/2), _Lifetime + _Size + (_RingSmoothing/2), length(uvCentered));
                return fixed4((circle - ring).xxx, 1);
                
                float rippleLength = (1 - length(uvCentered)) * _Size;

                float rippleRadius = smoothstep(0.65, 0.7, rippleLength);
                float rippleRing = smoothstep(0.7, 1, rippleLength);
                
                float ripple = rippleRadius - (rippleRing * (_Lifetime / _Duration));
                ripple *= ripple;
                ripple *= 1 - (_Lifetime / _Duration);
                
                return fixed4(ripple.xxx, 1);
            }
            ENDCG
        }
    }
}
