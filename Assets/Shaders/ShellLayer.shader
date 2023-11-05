Shader "USB/Shell Layer"
{
    Properties
    {
        _Mask ("Mask", 2D) = "white" {}
        _Displacement ("Displacement", 2D) = "black" {}
    }
    
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        
        Cull Off
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
                float2 uv              : TEXCOORD0;
                float4 vertex          : SV_POSITION;
                float heightPercentage : TEXCOORD1;
            };

            sampler2D _Mask;
            float4 _Mask_TexelSize;

            sampler2D _Displacement;
            float _DisplacementIntensity;
            float _DisplacementSpeed;
            float _DisplacementScale;

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
                float displacement = (tex2Dlod(_Displacement, float4((o.uv * _DisplacementScale) + _Time.y * _DisplacementSpeed, 0, 0)).x - 0.5) * 2;
                o.vertex.x += displacement * o.heightPercentage * _DisplacementIntensity;
                o.vertex = UnityObjectToClipPos(o.vertex);
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float centerDistance = length((frac(i.uv / _Mask_TexelSize.xy) - 0.5) * 2);
                fixed shellRandomValue = lerp(_StepMin, _StepMax, tex2D(_Mask, i.uv).x);

                if (centerDistance > _Radius * (shellRandomValue - i.heightPercentage) && _ShellIndex > 0)
                    discard;
                
                return _Color;
            }
            
            ENDCG
        }
    }
}
