Shader "Custom/UI/ShockWaveCameraComposite"
{
    Properties
    {
        _MainTex ("Camera1 Texture", 2D) = "white" {}
        _Camera2Tex ("Camera2 Texture", 2D) = "white" {}
        _FocalPoint ("Focal Point (viewport)", Vector) = (0.5, 0.5, 0, 0)
        _ViewportRadius ("Viewport Radius", Float) = 0
        _EdgeSoftness ("Edge Softness (viewport)", Float) = 0.02
        _AspectRatio ("Aspect Ratio", Float) = 1.777
        _DistortionSize ("Distortion Ring Width (viewport)", Float) = 0.06
        _DistortionStrength ("Distortion Strength (viewport)", Float) = 0.04
        [Toggle] _FlipY ("Flip Camera Textures Vertically", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
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
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_Camera2Tex);
            SAMPLER(sampler_Camera2Tex);

            float4 _FocalPoint;
            float _ViewportRadius;
            float _EdgeSoftness;
            float _AspectRatio;
            float _DistortionSize;
            float _DistortionStrength;
            float _FlipY;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float2 ApplyFlip(float2 uv)
            {
                if (_FlipY > 0.5)
                    uv.y = 1 - uv.y;
                return uv;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 화면(뷰포트) 공간에서 파동 중심(_FocalPoint)까지의 거리/방향을 잰다.
                // _AspectRatio로 X축만 보정해서 화면 비율과 무관하게 원형으로 계산한다
                // (_ViewportRadius는 Y축 기준으로 계산되어 넘어온다 - ShockWaveController 참고).
                float2 diff = IN.uv - _FocalPoint.xy;
                diff.x *= _AspectRatio;
                float dist = length(diff);
                float2 dirAspect = dist > 0.0001 ? diff / dist : float2(0, 0);

                // 파동 "경계"(_ViewportRadius) 바로 근처(_DistortionSize 폭)에서만 샘플 UV를
                // 방사 방향으로 밀어서 화면이 출렁이며 굴절되는 느낌을 만든다. 경계에서 멀어질수록
                // (링 바깥/안쪽 모두) 왜곡이 0으로 잦아든다.
                float radialDiff = dist - _ViewportRadius;
                float distortionMask = smoothstep(_DistortionSize, 0, abs(radialDiff));

                // aspect 보정 공간의 방향을 다시 원래 UV 공간으로 되돌린다(넣을 때 X에 곱했던 것의 역).
                float2 dirUV = float2(dirAspect.x / max(_AspectRatio, 0.0001), dirAspect.y);
                float2 distortedUV = IN.uv + dirUV * distortionMask * _DistortionStrength;

                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, ApplyFlip(distortedUV));
                half4 revealColor = SAMPLE_TEXTURE2D(_Camera2Tex, sampler_Camera2Tex, ApplyFlip(distortedUV));

                // 실제 camera1<->camera2 전환 경계 자체는 왜곡시키지 않은 원래 dist를 기준으로
                // 판정한다 - 화면 내용만 출렁이고, 전환 경계 위치는 매끄럽게 원형으로 확장된다.
                // dist가 _ViewportRadius보다 작으면(=파동이 이미 지나간 자리) camera2 쪽으로 전환한다.
                float revealMask = 1 - smoothstep(_ViewportRadius, _ViewportRadius + _EdgeSoftness, dist);

                half4 col = lerp(baseColor, revealColor, revealMask);
                col.a = 1;
                return col;
            }
            ENDHLSL
        }
    }
}
