// Soap bubble drawn procedurally on a quad that is larger than the bubble (room for the glow):
// clear core that thickens toward the rim (fresnel), iridescent rim that slowly turns, a gentle
// edge wobble, specular highlights, plus an outline and an outer glow whose colors are set from C#.
Shader "Bubble Ascent/Bubble"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
        _Tint ("Body tint", Color) = (0.85, 0.95, 1, 1)
        _CoreAlpha ("Core opacity", Range(0, 1)) = 0.12
        _RimAlpha ("Rim opacity", Range(0, 1)) = 0.85
        _Iridescence ("Iridescence", Range(0, 1)) = 0.6
        _IriMix ("Iridescent color strength", Range(0, 1)) = 0.55
        _IriSpeed ("Iridescence spin (turns/s)", Float) = 0.05
        _Wobble ("Rim wobble", Range(0, 0.1)) = 0.025
        _WobbleSpeed ("Wobble speed", Float) = 2
        _OutlineWidth ("Outline width (fraction of radius)", Range(0, 0.2)) = 0.07
        _GlowReach ("Glow reach (fraction of radius)", Range(0.05, 1)) = 0.35
        [HideInInspector] _Margin ("Quad margin (set from C#)", Float) = 1.5
        [HideInInspector] _OutlineColor ("Outline (set from C#)", Color) = (0, 0, 0, 0)
        [HideInInspector] _GlowColor ("Glow (set from C#)", Color) = (1, 1, 1, 0)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Tint;
                half _CoreAlpha;
                half _RimAlpha;
                half _Iridescence;
                half _IriMix;
                half _IriSpeed;
                half _Wobble;
                half _WobbleSpeed;
                half _OutlineWidth;
                half _GlowReach;
                float _Margin;
                half4 _OutlineColor;
                half4 _GlowColor;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                SetUpSpriteInstanceProperties();
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                o.color = v.color * unity_SpriteColor;
                return o;
            }

            // Straight-alpha "over".
            half4 Over(half4 src, half4 dst)
            {
                half a = src.a + dst.a * (1.0 - src.a);
                half3 rgb = (src.rgb * src.a + dst.rgb * dst.a * (1.0 - src.a)) / max(a, 1e-4);
                return half4(rgb, a);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y;
                // p: bubble space, the collision radius is 1.
                float2 p = (i.uv - 0.5) * 2.0 * _Margin;
                float r = length(p);
                float ang = atan2(p.y, p.x + 1e-5); // atan2(0, 0) is undefined on some mobile GPUs
                float wobble = 1.0 + _Wobble * (0.6 * sin(ang * 3.0 + t * _WobbleSpeed)
                                              + 0.4 * sin(ang * 5.0 - t * _WobbleSpeed * 0.7));
                float d = r / wobble;
                float aa = max(fwidth(d), 1e-3);
                half inside = 1.0 - smoothstep(1.0 - aa, 1.0 + aa, d);

                // Outer glow, then the outline ring just outside the body.
                // The glow must reach zero before the quad's edge, or the quad shows as a square.
                half edgeFade = 1.0 - smoothstep(_Margin * 0.85, _Margin * 0.98, r);
                half falloff = exp(-max(d - 1.0, 0.0) / _GlowReach);
                half glow = falloff * falloff * edgeFade * (1.0 - inside) * _GlowColor.a;
                half4 o = half4(_GlowColor.rgb, glow);
                half outline = (smoothstep(1.0 - aa, 1.0 + aa, d)
                              - smoothstep(1.0 + _OutlineWidth - aa, 1.0 + _OutlineWidth + aa, d)) * _OutlineColor.a;
                o = Over(half4(_OutlineColor.rgb, outline), o);

                // Body: clear core, denser and iridescent toward the rim.
                half dc = saturate(d);
                half fres = dc * dc * dc;
                half3 iri = 0.5 + 0.5 * cos(6.2831853 * (ang / 6.2831853 + t * _IriSpeed + half3(0.0, 0.33, 0.67)));
                half3 body = lerp(_Tint.rgb, lerp(_Tint.rgb, iri, _IriMix), _Iridescence * fres);
                half bodyA = lerp(_CoreAlpha, _RimAlpha, fres) * inside;
                o = Over(half4(body, bodyA), o);

                // Highlights: a soft tilted crescent up-left and a small dot down-right.
                float2 s = p - float2(-0.38, 0.42);
                float2 sr = float2(s.x * 0.766 + s.y * 0.643, -s.x * 0.643 + s.y * 0.766);
                half spec = 1.0 - smoothstep(0.75, 1.0, length(sr / float2(0.30, 0.14)));
                half dot2 = 1.0 - smoothstep(0.6, 1.0, length(p - float2(0.45, -0.42)) / 0.08);
                o = Over(half4(1, 1, 1, saturate(spec * 0.85 + dot2 * 0.6) * inside), o);

                o.a *= i.color.a;
                return o;
            }
            ENDHLSL
        }
    }
}
