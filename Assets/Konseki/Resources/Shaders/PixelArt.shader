Shader "Hidden/Konseki/PixelArt"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorSteps ("Color Steps", Float) = 48
        _DitherStrength ("Dither Strength", Range(0, 0.25)) = 0.02
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _ColorSteps;
            float _DitherStrength;

            fixed4 frag(v2f_img input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.uv);
                float2 pixel = floor(input.uv * _ScreenParams.xy);
                float checker = fmod(pixel.x + pixel.y * 2.0, 4.0);
                float dither = (checker - 1.5) / max(_ColorSteps, 1.0);
                color.rgb = saturate(color.rgb + dither * _DitherStrength);
                color.rgb = floor(color.rgb * _ColorSteps + 0.5) / _ColorSteps;
                return color;
            }
            ENDCG
        }
    }
}
