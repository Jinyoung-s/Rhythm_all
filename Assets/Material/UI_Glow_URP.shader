Shader "Custom/UI_NeonGlow_URP"
{
    Properties
    {
        [MainColor] _GlowColor("Glow Color", Color) = (0, 1, 1, 1)
        _GlowIntensity("Glow Intensity", Range(0,10)) = 3.0
        _GlowSoftness("Glow Softness", Range(0.0, 1.0)) = 0.2
        _Opacity("Opacity", Range(0,1)) = 1.0
        _CoreColor("Core Color", Color) = (0, 0, 0, 1)
        _CoreFade("Core Fade", Range(0,10)) = 8.0
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Pass
        {
            Name "UI_NeonGlow"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float4 _CoreColor;
                float _GlowIntensity;
                float _GlowSoftness;
                float _Opacity;
                float _CoreFade;
                float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 중심 좌표 기준
                float2 uvCenter = float2(0.5, 0.5);

                // 가로/세로 비율 보정
                float2 uvScaled = IN.uv - uvCenter;
                uvScaled.x *= (_ScreenParams.y / _ScreenParams.x);

                // 거리 계산 (사각형 형태 falloff)
                float2 dist = abs(uvScaled) * 2.0;
                float2 edge = smoothstep(1.0 - _GlowSoftness, 1.0, dist);
                float glowMask = 1.0 - max(edge.x, edge.y);

                // 중심부 색상 (CoreColor + Fade로 어둡게)
                float coreDark = pow(glowMask, _CoreFade);
                float3 coreColor = _CoreColor.rgb * coreDark;

                // Glow 테두리 (soft rim)
                float rim = smoothstep(0.7, 1.0, glowMask);
                float3 glowColor = _GlowColor.rgb * _GlowIntensity * rim;

                // 최종 색 조합
                float3 finalColor = coreColor + glowColor;

                // 투명도
                float alpha = rim * _Opacity;

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Unlit/Transparent"
}
