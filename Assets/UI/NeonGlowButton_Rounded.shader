Shader "UI/NeonGlowButton_Rounded"
{
    Properties
    {
        _Color ("Color", Color) = (0, 1, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1
        _CornerRadius ("Corner Radius", Range(0, 0.5)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _GlowIntensity;
            float _CornerRadius;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv * 2.0 - 1.0; // [-1,1] 범위로 변환
                float2 dist = abs(uv) - float2(1.0 - _CornerRadius, 1.0 - _CornerRadius);
                float d = length(max(dist, 0.0)) + min(max(dist.x, dist.y), 0.0);

                if (d > 0.0)
                    discard;

                float glow = 1.0 - saturate(length(uv));
                glow = pow(glow, 2.0) * _GlowIntensity;

                return _Color * glow;
            }
            ENDCG
        }
    }
}
