Shader "KuneKune/HeatHaze"
{
    Properties
    {
        _Heat ("Heat", Range(0, 1)) = 0
        _DistortionStrength ("Distortion Strength", Range(0, 0.01)) = 0.003
        _HazeHeight ("Low Heat Haze Height", Range(0.1, 1)) = 0.55
        _Warmth ("Warmth", Range(0, 0.5)) = 0.5
        _VignetteStrength ("Vignette Strength", Range(0, 0.7)) = 0.7
        _PressureStrength ("Pressure Strength", Range(0, 0.05)) = 0.01
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
            Name "HeatHazePass"

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            // Blit 화면 텍스처를 사용하기 위해 Core를 먼저 포함한다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Heat;
            float _DistortionStrength;
            float _HazeHeight;
            float _Warmth;
            float _VignetteStrength;
            float _PressureStrength;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float heat = saturate(_Heat);
                float time = _Time.y;

                // 더위가 낮을 때는 화면 아래쪽에서만 아지랑이가 보인다.
                float lowHeat = smoothstep(0.05, 0.6, heat);
                float mediumHeat = smoothstep(0.5, 0.8, heat);
                // 따뜻한 색감과 가장자리 어두워짐은 72%부터 강해지고 90%부터 최대 강도를 유지한다.
                float warmthAndDarkness = smoothstep(0.72, 0.9, heat);
                // 화면 압박은 90%부터 별도로 시작해 기존 화면 효과 위에 더해진다.
                float dangerHeat = smoothstep(0.9, 0.96, heat);
                float bottomHaze = 1.0 - smoothstep(0.0, _HazeHeight, uv.y);
                float hazeArea = saturate(bottomHaze + mediumHeat * 0.35 + warmthAndDarkness * 0.3);

                // 서로 다른 속도의 물결을 섞어 일정하게 반복되는 느낌을 줄인다.
                float horizontalWave = sin(uv.y * 58.0 + time * 2.1 + sin(uv.x * 9.0 - time * 0.7));
                horizontalWave += sin(uv.y * 31.0 - time * 1.4 + uv.x * 13.0) * 0.5;
                float verticalWave = sin(uv.x * 26.0 + time * 1.2 + uv.y * 7.0);

                float distortion = _DistortionStrength * lowHeat * hazeArea * lerp(0.35, 1.0, heat);
                float2 heatOffset = float2(horizontalWave, verticalWave * 0.18) * distortion;

                // 90%부터 시야가 천천히 안쪽으로 눌렸다가 원래 크기로 돌아온다.
                float2 fromCenter = uv - 0.5;
                float pressurePulse = sin(time * 1.15) * 0.5 + 0.5;
                heatOffset -= fromCenter * pressurePulse * dangerHeat * _PressureStrength;

                half4 color = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    saturate(uv + heatOffset)
                );

                // 72%부터 붉고 노란빛을 더하고 푸른빛을 줄여 메마른 색감을 만든다.
                half3 warmColor = color.rgb * half3(2.5, 0.98, 0.74);
                warmColor = (warmColor - 0.5) * 1.03 + 0.5;
                color.rgb = lerp(color.rgb, warmColor, _Warmth * warmthAndDarkness);

                // 72%부터 화면 가장자리를 어둡게 하고, 90%부터는 화면 움직임에 맞춰 조금 더 짙어진다.
                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float2 vignettePosition = float2(fromCenter.x * aspect, fromCenter.y);
                float edge = smoothstep(0.32, 0.78, length(vignettePosition));
                float dangerVignette = lerp(1.0, 1.25, pressurePulse * dangerHeat);
                float vignette = edge * _VignetteStrength * warmthAndDarkness * dangerVignette;
                color.rgb *= 1.0 - vignette;

                return color;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
