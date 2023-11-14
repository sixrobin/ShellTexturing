Shader "Shell/Grass"
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
        [NoScaleOffset] _WindNoise ("Wind Noise", 2D) = "black" {}
        _WindIntensity ("Wind Intensity", Float) = 1
        _WindSpeed ("Wind Speed", Float) = 1
        _WindScale ("Wind Scale", Range(0, 1)) = 1
        
        [Header(LOCAL OFFSET)]
        [Space(5)]
        _LocalOffsetTexture ("Local Offset Texture", 2D) = "black" {}
        _LocalOffsetIntensity ("Local Offset Intensity", Float) = 1
        
        [Header(RIPPLE)]
        [Space(5)]
        [MaterialToggle] _IgnoreRipple ("Ignore Ripple", Float) = 0
        _RippleRingWidth ("Ripple Ring Width", Range(0, 1)) = 0.5
        _RippleCircleSmoothing ("Ripple Circle Smoothing", Float) = 0.5
        _RippleRingSmoothing ("Ripple Ring Smoothing", Float) = 0.5
        _RippleRadiusMultiplier ("Ripple Radius Multiplier", Float) = 1
        _RippleIntensityMultiplier ("Ripple Intensity Multiplier", Float) = 1
        _RippleDuration ("Ripple Duration", Float) = 1
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
            
            // Ripple.
            float _RippleRingWidth;
            float _RippleCircleSmoothing;
            float _RippleRingSmoothing;
            float _RippleRadiusMultiplier;
            float _RippleIntensityMultiplier;
            float _IgnoreRipple;
            float _RippleDuration;
            float4 _Ripple1;
            float4 _Ripple2;
            float4 _Ripple3;
            float4 _Ripple4;
            float4 _Ripple5;
            
            // Local wind.
            sampler2D _WindNoise;
            float _WindIntensity;
            float _WindSpeed;
            float _WindScale;

            // Global wind.
            uniform float2 _GlobalWindDirection;
            
            // Local offset.
            sampler2D _LocalOffsetTexture;
            float _LocalOffsetIntensity;

            // Color & radius.
            float4 _ColorMin;
            float4 _ColorMax;
            float _Radius;

            float3 computeRipple(float4 ripple, float3 worldPosition)
            {
                float distance = length(worldPosition - ripple.xyz) / max(0.001, _RippleRadiusMultiplier);
                float rippleCircle = smoothstep(ripple.w - _RippleCircleSmoothing * 0.5, ripple.w + _RippleCircleSmoothing * 0.5, distance);
                float rippleRing = rippleCircle - smoothstep(ripple.w + _RippleRingWidth - _RippleRingSmoothing * 0.5, ripple.w + _RippleRingWidth + _RippleRingSmoothing * 0.5, distance);

                return normalize(worldPosition - ripple.xyz)
                       * rippleRing
                       * max(0, 1 - OutExpo(ripple.w / _RippleDuration))
                       * saturate(_ShellIndex)
                       * (_ShellIndex / _ShellsCount)
                       * _RippleIntensityMultiplier;
            }
            
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

                // Gravity.
                vertex.y -= _Gravity * _HeightPercentage;
                
                // TODO: Horizontal displacement (wind and ripple) should use mesh tangent.

                // Ripple.
                if (_IgnoreRipple == 0)
                {
                    float3 worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
                    #define APPLY_RIPPLE(i) vertex.xyz += computeRipple(_Ripple##i, worldPosition);
                    APPLY_RIPPLE(1);
                    APPLY_RIPPLE(2);
                    APPLY_RIPPLE(3);
                    APPLY_RIPPLE(4);
                    APPLY_RIPPLE(5);
                }

                // Wind.
                // TODO: Replace _WindScale by _WindNoise_ST.
                float2 localWind = (tex2Dlod(_WindNoise, float4(o.uv * _WindScale + _Time.y * _WindSpeed, 0, 0)).xx - 0.5) * 2 * _WindIntensity;
                float2 wind = localWind + _GlobalWindDirection;
                vertex.xz += wind * (_ShellIndex / _ShellsCount);

                o.vertex = UnityWorldToClipPos(vertex);
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Shell cells local distance.
                float2 centerUV = (frac(i.uv / _Mask_TexelSize.xy) - 0.5) * 2;
                float localOffset = (tex2D(_LocalOffsetTexture, floor(i.uv * _Mask_TexelSize.zw) * _Mask_TexelSize.xy) - 0.5) * 2;
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
