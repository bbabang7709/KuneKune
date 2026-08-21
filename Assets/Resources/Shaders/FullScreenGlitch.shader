Shader "FullScreenGlitch"
{
    Properties
    {
        _Intensity ("Intensity", Range(0, 1)) = 0

        [Header(Glitch Settings)]
        _RgbSplit ("RGB Split", Range(0, 0.05)) = 0.012
        _Jitter ("Line Jitter", Range(0, 0.1)) = 0.02
        _BlockGlitch ("Block Glitch", Range(0, 0.1)) = 0.03
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.15
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "FullScreenGlitchPass"

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            // 반드시 Blit.hlsl보다 먼저 포함해야 한다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Intensity;
            float _RgbSplit;
            float _Jitter;
            float _BlockGlitch;
            float _ScanlineStrength;
            float _NoiseStrength;

            // 시드값 하나로 0~1 사이 의사난수를 만드는 해시 함수 (텍스처/노이즈 없이 GPU에서 값싸게 랜덤을 얻는 용도)
            float Hash11(float value)
            {
                value = frac(value * 0.1031);
                value *= value + 33.33;
                value *= value + value;
                return frac(value);
            }

            // 2차원 좌표를 받는 해시 함수. uv나 (row, time) 같은 쌍을 랜덤 시드로 쓸 때 사용
            float Hash21(float2 value)
            {
                float3 value3 = frac(float3(value.xyx) * 0.1031);
                value3 += dot(value3, value3.yzx + 33.33);
                return frac((value3.x + value3.y) * value3.z);
            }

            float2 GetBlockOffset(float2 uv, float time)
            {
                // 화면을 가로 18줄 블록으로 나누고, 12fps 주기로 블록 번호를 바꿔 "칸별로 끊겨서 밀리는" 느낌을 만든다.
                float2 blockCell = floor(float2(uv.y * 18.0, time * 12.0));
                // 매 블록마다 18% 확률로만 글리치가 발동하도록 트리거를 켠다.
                float trigger = step(0.82, Hash21(blockCell));
                float shift = (Hash21(blockCell + 13.37) - 0.5) * _BlockGlitch * _Intensity * 2.0;
                return float2(shift * trigger, 0.0);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float time = _Time.y;

                // 0.125초(8fps)마다 8% 확률로 글리치 강도를 순간 증폭시킨다 (버스트 노이즈처럼 화면이 튀는 순간을 만든다).
                float burstSeed = floor(time * 8.0);
                float burst = step(0.92, Hash11(burstSeed));
                float burstMultiplier = lerp(1.0, 1.75, burst);

                // uv.y를 240줄로 양자화해 같은 줄에 속한 픽셀들이 같은 양만큼 가로로 밀리게 한다 (한 줄씩 어긋나는 글리치 라인).
                float lineIndex = floor(uv.y * 240.0 + time * 30.0);
                float lineRandom = Hash11(lineIndex);
                float jitterMask = step(0.65, lineRandom);
                float lineShift = (Hash11(lineIndex + 17.0) - 0.5) * _Jitter * _Intensity * burstMultiplier;
                float2 glitchOffset = float2(lineShift * jitterMask, 0.0);

                // 큰 블록 단위 밀림 추가
                glitchOffset += GetBlockOffset(uv, time);

                // 3% 확률로 화면 전체가 세로로 한 번 크게 찢어지도록 한다 (드문 강한 글리치 연출).
                float verticalTear = step(0.97, Hash11(floor(time * 5.0)));
                float verticalShift = (Hash11(floor(time * 60.0)) - 0.5) * 0.015 * _Intensity * verticalTear;
                glitchOffset.y += verticalShift;

                float2 baseUV = saturate(uv + glitchOffset);

                // RGB 채널을 서로 다른 UV에서 샘플링해 색수차(chromatic aberration) 느낌의 RGB 분리를 만든다.
                float chromaticOffset = _RgbSplit * _Intensity * burstMultiplier;
                float2 redOffset = float2(chromaticOffset, 0.0);
                float red = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(baseUV + redOffset)).r;
                float green = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, baseUV).g;
                float blue = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(baseUV - redOffset)).b;
                half4 color = half4(red, green, blue, 1.0);

                // CRT 스캔라인
                // 실제 화면 해상도(_ScreenParams.y)에 비례한 주파수를 쓰면 픽셀 그리드와 간섭(모아레)이 생겨
                // 두꺼운 밝은 띠가 흐르는 것처럼 보이므로, 해상도와 무관한 고정 주파수를 사용한다.
                float scanValue = sin((uv.y + time * 0.5) * 480.0);
                float scanline = 1.0 - ((scanValue * 0.5 + 0.5) * _ScanlineStrength * _Intensity);
                color.rgb *= scanline;

                // 화면 노이즈: 픽셀 좌표 + 60fps로 바뀌는 시드로 매 프레임 다른 백색 노이즈를 만든다.
                float noise = Hash21(uv * _ScreenParams.xy + floor(time * 60.0)) - 0.5;
                noise *= 2.0;
                color.rgb += noise * _NoiseStrength * _Intensity;

                // 순간적으로 나타나는 밝은 가로 간섭선: 14fps 주기로 랜덤한 세로 위치(stripeY)에 얇은 밝은 줄을 띄운다.
                float stripeSeed = floor(time * 14.0);
                float stripeY = Hash11(stripeSeed);
                float stripeDistance = abs(uv.y - stripeY);
                float stripe = 1.0 - smoothstep(0.0, 0.03, stripeDistance);
                color.rgb += stripe * 0.12 * _Intensity * burst;

                color.rgb = saturate(color.rgb);

                return color;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
