Shader "Shell/Ball"
{
    Properties
    {
        [Header(SHELL)]
        [Space(5)]
        _StepMin ("Step Min", Range(0, 1)) = 0
        _StepMax ("Step Max", Range(0, 1)) = 1
        _HeightSpacePercentage ("Height Space Percentage", Range(0, 1)) = 0
        
        [Header(COLOR AND RADIUS)]
        [Space(5)]
        _ColorMin ("Color Min", Color) = (0,0,0,0)
        _ColorMax ("Color Max", Color) = (1,1,1,1)
        _Radius ("Radius", Float) = 1

        [Header(GRAVITY)]
        [Space(5)]
        _Gravity ("Gravity", Float) = 0
                
        [Header(LOCAL WIND)]
        [Space(5)]
        _WindNoise ("Wind Noise", 2D) = "black" {}
        _WindIntensity ("Wind Intensity", Float) = 1
        _WindSpeed ("Wind Speed", Float) = 1
        
        [Header(LOCAL OFFSET)]
        [Space(5)]
        [NoScaleOffset] _LocalOffsetTexture ("Local Offset Texture", 2D) = "black" {}
        _LocalOffsetIntensity ("Local Offset Intensity", Float) = 1
    }
    
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
            #include "Assets/Shaders/Easing.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // Shell data.
            sampler2D _Mask;
            float4 _Mask_TexelSize;
            float _ShellIndex;
            float _ShellsCount;
            float _ShellHeight;
            float _StepMin;
            float _StepMax;
            float _HeightPercentage;
            float _HeightSpacePercentage;
            
            // Gravity.
            float _Gravity;

            // Position smooth follow.
            float3 _SmoothedPosition;
            float3 _CurrentPosition;
            
            // Local wind.
            sampler2D _WindNoise;
            float4 _WindNoise_ST;
            float _WindIntensity;
            float _WindSpeed;

            // Global wind.
            uniform float2 _GlobalWindDirection;
            
            // Local offset.
            sampler2D _LocalOffset;
            float _LocalOffsetIntensity;

            // Color & radius.
            float4 _ColorMin;
            float4 _ColorMax;
            float _Radius;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.uv = v.uv;

                float3 vertex = v.vertex;

                // Shell height.
                float height = _ShellHeight * _HeightPercentage;
                float3 localHeightOffset = v.normal * height;
                float3 globalHeightOffset = float3(0, height, 0);
                vertex.xyz += lerp(localHeightOffset, globalHeightOffset, _HeightSpacePercentage);

                vertex = mul(unity_ObjectToWorld, float4(vertex.xyz, 1));

                // Position smooth follow. Clamp offset length by shell height to avoid shells being pushed inside the ball.
                float3 followOffset = _SmoothedPosition - _CurrentPosition;
                if (length(followOffset) > _ShellHeight)
                    followOffset = normalize(followOffset) * _ShellHeight;
                vertex.xyz += followOffset * (_ShellIndex / _ShellsCount);
                
                // Gravity.
                vertex.y -= _Gravity * _HeightPercentage;
                
                // TODO: Wind displacement should use mesh tangent.
                // Wind.
                float2 localWind = (tex2Dlod(_WindNoise, float4((o.uv * _WindNoise_ST.xy) + _WindNoise_ST.zw + _Time.y * _WindSpeed, 0, 0)).xx - 0.5) * 2 * _WindIntensity;
                float2 wind = localWind + _GlobalWindDirection;
                vertex.xz += wind * (_ShellIndex / _ShellsCount);

                o.vertex = UnityWorldToClipPos(vertex);
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Shell cells local distance.
                float2 centerUV = (frac(i.uv / _Mask_TexelSize.xy) - 0.5) * 2;
                float localOffset = (tex2D(_LocalOffset, floor(i.uv * _Mask_TexelSize.zw) * _Mask_TexelSize.xy) - 0.5) * 2;
                centerUV += localOffset * _LocalOffsetIntensity;
                float centerDistance = length(centerUV);

                // Shell mask value.
                float maskValue = tex2D(_Mask, i.uv).x;
                fixed shellRandomValue = lerp(_StepMin, _StepMax, maskValue);

                // Discard "invalid" pixels.
                if (_ShellIndex > 0 && centerDistance > _Radius * (shellRandomValue - _HeightPercentage))
                    discard;
                
                return lerp(_ColorMin, _ColorMax, _HeightPercentage);
            }
            
            ENDCG
        }
    }
}
