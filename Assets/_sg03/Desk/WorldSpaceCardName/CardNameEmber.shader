Shader "SG03/CardNameEmber"
{
    SubShader
    {
        Tags { "Queue"="Overlay+2" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        ZTest Always
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float glow = exp2(-dot(p, p) * 5.0);
                float core = exp2(-dot(p * float2(2.5, 1.0), p * float2(2.5, 1.0)) * 12.0);
                float fade = 1.0 - smoothstep(0.65, 1.0, length(p));
                return half4(input.color.rgb * (1.0 + core), (glow + core) * fade * input.color.a);
            }
            ENDHLSL
        }
    }
}
