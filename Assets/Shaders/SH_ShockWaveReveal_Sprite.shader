Shader "Custom/ShockWaveReveal_Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _EdgeSoftness ("Edge Softness (world units)", Range(0.01, 10)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                float2 positionWS : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;
            float _EdgeSoftness;

            // 이 오브젝트만의 프로퍼티가 아니라, ShockWaveController가
            // Shader.SetGlobalVector/Float로 매 프레임 갱신하는 전역 값입니다.
            // 여러 GameObject가 머티리얼 인스턴스 없이도 동일하게 반응합니다.
            float3 _ShockWaveOriginWS;
            float _ShockWaveRadiusWS;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positions.positionCS;
                OUT.positionWS = positions.positionWS.xy;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                float dist = distance(IN.positionWS, _ShockWaveOriginWS.xy);
                float reveal = smoothstep(_ShockWaveRadiusWS - _EdgeSoftness, _ShockWaveRadiusWS, dist);
                col.a *= reveal;

                clip(col.a - 0.001);
                return col;
            }
            ENDHLSL
        }
    }
}
