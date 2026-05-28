Shader "Custom/SpriteTopWind"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        _WindStrength("Wind Strength", Float) = 0.08
        _WindSpeed("Wind Speed", Float) = 1.5
        _WindFrequency("Wind Frequency", Float) = 2.0
        _BottomLock("Bottom Lock", Range(0,1)) = 0.25
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
            }

            Cull Off
            Lighting Off
            ZWrite Off
            Blend One OneMinusSrcAlpha

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile _ PIXELSNAP_ON
                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex   : POSITION;
                    float4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                };

                struct v2f
                {
                    float4 vertex   : SV_POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                };

                sampler2D _MainTex;
                fixed4 _Color;

                float _WindStrength;
                float _WindSpeed;
                float _WindFrequency;
                float _BottomLock;

                v2f vert(appdata_t IN)
                {
                    v2f OUT;

                    float4 worldPos = mul(unity_ObjectToWorld, IN.vertex);

                    // sprite 안에서 위쪽일수록 더 많이 흔들리게 하는 값
                    float heightMask = saturate((IN.texcoord.y - _BottomLock) / (1.0 - _BottomLock));
                    heightMask = heightMask * heightMask;

                    // 오브젝트 위치마다 흔들림 타이밍 다르게
                    float randomOffset = worldPos.x * 1.37 + worldPos.y * 0.73;

                    float wind = sin(_Time.y * _WindSpeed + randomOffset * _WindFrequency);

                    IN.vertex.x += wind * _WindStrength * heightMask;

                    OUT.vertex = UnityObjectToClipPos(IN.vertex);
                    OUT.texcoord = IN.texcoord;
                    OUT.color = IN.color * _Color;

                    #ifdef PIXELSNAP_ON
                    OUT.vertex = UnityPixelSnap(OUT.vertex);
                    #endif

                    return OUT;
                }

                fixed4 frag(v2f IN) : SV_Target
                {
                    fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                    c.rgb *= c.a;
                    return c;
                }
                ENDCG
            }
        }
}