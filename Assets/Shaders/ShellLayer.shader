Shader "Shell Layer"
{
    Properties
    {
        [MaterialToggle] _IgnoreRipple ("Ignore Ripple", Float) = 0
        _RippleRingWidth ("Ripple Ring Width", Range(0, 1)) = 0.5
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

            uniform float2 _GlobalWindDirection;

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

            sampler2D _Mask;
            float4 _Mask_TexelSize;

            sampler2D _Displacement;
            float _DisplacementIntensity;
            float _DisplacementSpeed;
            float _DisplacementScale;

            sampler2D _LocalOffset;
            float _LocalOffsetIntensity;

            float _Gravity;
            
            float4 _ColorMin;
            float4 _ColorMax;
            float _Radius;
            float _HeightPercentage;
            float _HeightSpacePercentage;

            float _ShellIndex;
            float _ShellsCount;
            float _ShellHeight;
            float _StepMin;
            float _StepMax;

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

                // Gravity. // TODO: Gravity should be handled in world space.
                vertex.y -= _Gravity * _HeightPercentage;
                
                // TODO: Horizontal displacement (wind and ripple) should use mesh tangent/binormal.

                // Ripple.
                if (_IgnoreRipple == 0)
                {
                    float3 worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
                    vertex.xyz += computeRipple(_Ripple1, worldPosition);
                    vertex.xyz += computeRipple(_Ripple2, worldPosition);
                    vertex.xyz += computeRipple(_Ripple3, worldPosition);
                    vertex.xyz += computeRipple(_Ripple4, worldPosition);
                    vertex.xyz += computeRipple(_Ripple5, worldPosition);
                }

                // Wind.
                float2 horizontalDisplacement = (tex2Dlod(_Displacement, float4(o.uv * _DisplacementScale + _Time.y * _DisplacementSpeed, 0, 0)).xx - 0.5) * 2;
                vertex.xz += horizontalDisplacement * (_ShellIndex / _ShellsCount) * _DisplacementIntensity;
                vertex.xz += _GlobalWindDirection * (_ShellIndex / _ShellsCount);

                o.vertex = UnityObjectToClipPos(vertex);
                
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
