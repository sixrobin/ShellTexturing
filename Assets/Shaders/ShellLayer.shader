Shader "Shell Layer"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        
        Cull Off

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
                float2 uv              : TEXCOORD0;
                float4 vertex          : SV_POSITION;
                float heightPercentage : TEXCOORD1;
            };

            uniform float2 _GlobalWindDirection;

            sampler2D _Mask;
            float4 _Mask_TexelSize;

            sampler2D _Displacement;
            float _DisplacementIntensity;
            float _DisplacementSpeed;
            float _DisplacementScale;

            sampler2D _LocalOffset;
            float _LocalOffsetIntensity;
            
            float4 _Color;
            float _Radius;

            float _ShellIndex;
            float _ShellsCount;
            float _StepMin;
            float _StepMax;

            v2f vert(appdata v)
            {
                v2f o;

                o.heightPercentage = _ShellIndex / _ShellsCount;
                o.uv = v.uv;

                o.vertex = v.vertex;
                float2 displacement = (tex2Dlod(_Displacement, float4((o.uv * _DisplacementScale) + _Time.y * _DisplacementSpeed, 0, 0)).xx - 0.5) * 2;
                o.vertex.xz += displacement * o.heightPercentage * _DisplacementIntensity;
                o.vertex.xz += _GlobalWindDirection * o.heightPercentage;
                o.vertex = UnityObjectToClipPos(o.vertex);
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centerUV = (frac(i.uv / _Mask_TexelSize.xy) - 0.5) * 2;
                float localOffset = (tex2D(_LocalOffset, floor(i.uv * _Mask_TexelSize.zw) * _Mask_TexelSize.xy) - 0.5) * 2;
                centerUV += localOffset * _LocalOffsetIntensity;
                
                float centerDistance = length(centerUV);
                
                float maskValue = tex2D(_Mask, i.uv).x;
                fixed shellRandomValue = lerp(_StepMin, _StepMax, maskValue);

                if (centerDistance > _Radius * (shellRandomValue - i.heightPercentage) && _ShellIndex > 0)
                    discard;
                
                return _Color;
            }
            
            ENDCG
        }
    }
}
