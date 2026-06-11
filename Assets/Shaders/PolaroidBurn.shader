Shader "Custom/PolaroidBurn"
{
    Properties
    {
        _MainTex      ("Albedo (RGB)", 2D)        = "white" {}
        _NoiseTex     ("Noise Texture", 2D)       = "white" {}
        _BurnAmount   ("Burn Amount", Range(0,1)) = 0.0
        _BurnWidth    ("Burn Edge Width", Range(0, 0.15)) = 0.05
        _BurnColor    ("Burn Edge Color", Color)  = (1, 0.3, 0, 1)
        _EmissionStrength ("Emission Strength", Float) = 3.0
    }

    SubShader
    {
        // URP: Transparent queue so alpha clip works correctly in passthrough
        Tags
        {
            "RenderType"      = "TransparentCutout"
            "Queue"           = "AlphaTest"
            "RenderPipeline"  = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off          // render both sides of the polaroid quad

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                float  _BurnAmount;
                float  _BurnWidth;
                float4 _BurnColor;
                float  _EmissionStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 col   = SAMPLE_TEXTURE2D(_MainTex,  sampler_MainTex,  IN.uv);
                half  noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.uv).r;

                // Pixels where noise < burnAmount are dissolved away
                clip(noise - _BurnAmount);

                // Orange glow at the burn edge
                float edgeFactor = 1.0 - smoothstep(0.0, _BurnWidth, noise - _BurnAmount);
                half3 burnGlow   = _BurnColor.rgb * edgeFactor * _EmissionStrength;

                col.rgb += burnGlow;
                return col;
            }
            ENDHLSL
        }

        // Shadow caster pass – dissolve also affects shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   vertShadow
            #pragma fragment fragShadow
            #pragma target   2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct AttrShadow
            {
                float4 positionOS  : POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalOS    : NORMAL;
            };

            struct VarShadow
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                float  _BurnAmount;
                float  _BurnWidth;
                float4 _BurnColor;
                float  _EmissionStrength;
            CBUFFER_END

            VarShadow vertShadow(AttrShadow IN)
            {
                VarShadow OUT;
                float3 posWS   = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS= TransformObjectToWorldNormal(IN.normalOS);
                float4 posCS   = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, _MainLightPosition.xyz));
                OUT.positionCS = posCS;
                OUT.uv         = IN.uv;
                return OUT;
            }

            half4 fragShadow(VarShadow IN) : SV_Target
            {
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.uv).r;
                clip(noise - _BurnAmount);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
