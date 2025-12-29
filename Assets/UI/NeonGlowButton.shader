Shader "UI/NeonOutlineOnly"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 1, 1, 1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.5)) = 0.05
        _GlowStrength ("Glow Strength", Range(0.0, 5.0)) = 2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _GlowStrength;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float2 dist = abs(i.uv - center);
                float d = max(dist.x, dist.y);

                float edgeStart = 0.5 - _OutlineWidth;
                float edgeEnd = 0.5;

                float alpha = smoothstep(edgeEnd, edgeStart, d) * _GlowStrength;

                return fixed4(_OutlineColor.rgb, alpha * _OutlineColor.a);
            }
            ENDCG
        }
    }
}
