Shader "SG03/ArrowIndicator"
{
    Properties
    {
        _Color ("Blood Color", Color) = (0.65, 0.025, 0.045, 1)
        [HDR] _GlowColor ("Ember Core", Color) = (1.8, 0.55, 0.28, 1)
        _ShadowColor ("Smoke Shadow", Color) = (0.035, 0.008, 0.025, 1)
        _FlowSpeed ("Flow Speed", Float) = 2
        _PulseSpeed ("Pulse Speed", Float) = 3
        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.15
        [HideInInspector] _EffectTime ("Animation Clock", Float) = 0
        [HideInInspector] _PathLength ("Path Length", Float) = 1
        [HideInInspector] _HeadMode ("Spear Head", Float) = 0
        [HideInInspector] _Opacity ("Reveal", Float) = 1
        [HideInInspector] _HeadLength ("Spear Length", Float) = 4.2
        [HideInInspector] _RibbonScale ("Canvas to Tether Width", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay+1" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "BloodTether"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color, _GlowColor, _ShadowColor;
                float _FlowSpeed, _PulseSpeed, _PulseAmount;
                float _EffectTime, _PathLength, _HeadMode, _Opacity;
                float _HeadLength, _RibbonScale;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
                    lerp(Hash(cell + float2(0, 1)), Hash(cell + float2(1, 1)), f.x), f.y);
            }

            float4 frag(Varyings input) : SV_Target
            {
                float t = _EffectTime;
                float x = input.uv.x * _PathLength;
                float across = input.uv.y * 2.0 - 1.0;
                float headU = (x - (_PathLength - _HeadLength)) / max(_HeadLength, 0.001);
                float ringMode = saturate(_HeadMode - 1.0);
                float tipTaper = 1.0 - smoothstep(0.65, 1.0, headU);
                float sourceTaper = lerp(0.3, 1.0, smoothstep(0.0, 0.1, input.uv.x));
                float tetherWidth = lerp(max(0.015, tipTaper * sourceTaper), 1.0, ringMode);
                float y = across * _RibbonScale / tetherWidth;
                float edge = abs(y);
                // All longitudinal motion travels from source (U=0) to target (U=1).
                float flow = x - t * _FlowSpeed * 3.0;
                float smoke = Noise(float2(flow * 0.75, y * 3.0 + t * 0.32));
                float detail = Noise(float2(flow * 2.1, y * 6.0 - t * 0.45));
                float warp = sin(x * 1.8 - t * 4.0) * 0.12 * (1.0 - ringMode);
                float filament = abs(y - warp);
                float core = exp2(-filament * filament * 180.0);
                float vein = exp2(-pow(abs(y + warp * 1.5 - sin(flow) * 0.16), 2.0) * 90.0);
                float halo = exp2(-edge * edge * 5.5);
                float smokeMask = (1.0 - smoothstep(0.45, 1.0, edge)) * (0.35 + smoke * 0.65);

                // Moving tapered crests reinforce direction without breaking the tether into dashes.
                float crestPhase = frac(flow * 0.32 - abs(y) * 0.16);
                float crest = smoothstep(0.55, 0.87, crestPhase) * (1.0 - smoothstep(0.87, 1.0, crestPhase));
                float heartbeat = 1.0 - _PulseAmount * (0.5 + 0.5 * sin(t * _PulseSpeed));
                float energy = core * (0.55 + crest * 0.8) + vein * 0.22;

                // Sparse embers ride inside the outer smoke; no textures or particle allocations.
                float2 sparkUV = float2(flow * 1.4, y * 3.0);
                float2 sparkCell = floor(sparkUV);
                float2 sparkLocal = frac(sparkUV) - 0.5;
                float spark = (1.0 - smoothstep(0.05, 0.2, length(sparkLocal * float2(0.6, 1.8))))
                    * step(0.78, Hash(sparkCell)) * smoothstep(0.2, 0.45, edge)
                    * (1.0 - smoothstep(0.65, 1.0, edge));

                float3 body = lerp(_ShadowColor.rgb, _Color.rgb, halo * (0.5 + detail * 0.5));
                body += _Color.rgb * crest * halo * 0.65;
                body += _GlowColor.rgb * (energy + spark * 0.8) * heartbeat;
                float bodyAlpha = saturate(smokeMask * 0.8 + halo * 0.25 + core * 0.4 + spark);

                // Swept-back barbs and concave heels follow the reference silhouette.
                // These wings grow out of the SAME flowing field, without a UV/color reset.
                float lateral = abs(across);
                float outer = 0.92 * pow(saturate(1.0 - headU), 0.72);
                float heel = 0.36 - 0.3 * saturate(lateral / 0.86);
                float aa = max(0.012, fwidth(across) * 1.25);
                float wing = smoothstep(-aa, aa, outer - lateral)
                    * smoothstep(-aa, aa, headU - heel)
                    * (1.0 - ringMode);
                float wingVein = exp2(-pow(lateral - outer * 0.72 - sin(flow * 1.8) * 0.035, 2.0) * 210.0) * wing;
                float wingEdge = (1.0 - smoothstep(aa, aa * 3.0, abs(outer - lateral))) * wing;
                float wingHeat = (0.35 + detail * 0.45 + crest * 0.25) * wing;
                // Keep the tether's spine and turbulent embers continuous into the point.
                float3 color = body + _Color.rgb * wingHeat;
                color += _GlowColor.rgb * (wingVein * 0.28 + wingEdge * 0.12) * heartbeat;
                float alpha = max(bodyAlpha, wing * (0.4 + detail * 0.4 + crest * 0.12));
                alpha *= lerp(1.0 - smoothstep(0.985, 1.0, headU), 1.0, ringMode);
                // Eight revolving broken seals surround the exact destination.
                float sealPhase = frac(input.uv.x * 8.0 - t * 0.35);
                float seal = smoothstep(0.08, 0.16, sealPhase) * (1.0 - smoothstep(0.7, 0.8, sealPhase));
                float orbit = pow(0.5 + 0.5 * sin(input.uv.x * 12.56637 - t * 2.0), 8.0);
                float3 ring = _Color.rgb * 0.75 + _GlowColor.rgb * (core * (0.3 + orbit) + halo * 0.15);
                color = lerp(color, ring, ringMode);
                alpha = lerp(alpha, seal * halo * heartbeat, ringMode);
                return float4(color * input.color.rgb, alpha * _Color.a * input.color.a * _Opacity);
            }
            ENDHLSL
        }
    }
}
