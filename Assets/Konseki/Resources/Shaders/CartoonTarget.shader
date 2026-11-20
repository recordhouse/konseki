Shader "Konseki/CartoonTarget"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Tint", Color) = (0.45,0.5,0.65,1)
        _RimColor ("Rim Color", Color) = (0.65,0.75,1,1)
        _RimStrength ("Rim Strength", Range(0,1)) = 0.18
        _RimPower ("Rim Power", Range(0.5,8)) = 3
        _ShadowThreshold ("Shadow Threshold", Range(-1,1)) = 0.05
        _ShadowSoftness ("Shadow Softness", Range(0.001,0.4)) = 0.08
        _OutlineColor ("Outline Color", Color) = (0.035,0.025,0.06,1)
        _OutlineWidth ("Outline Width", Range(0,0.03)) = 0.008
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode"="Always" }
            Cull Front
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
            };

            fixed4 _OutlineColor;
            float _OutlineWidth;

            v2f vert(appdata input)
            {
                v2f output;
                float3 expanded = input.vertex.xyz + normalize(input.normal) * _OutlineWidth;
                output.position = UnityObjectToClipPos(float4(expanded, 1.0));
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPosition : TEXCOORD1;
                SHADOW_COORDS(2)
            };

            fixed4 _Color;
            fixed4 _ShadowColor;
            fixed4 _RimColor;
            float _RimStrength;
            float _RimPower;
            float _ShadowThreshold;
            float _ShadowSoftness;

            v2f vert(appdata input)
            {
                v2f output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                TRANSFER_SHADOW(output)
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float3 normal = normalize(input.worldNormal);
                float3 lightDirection = normalize(UnityWorldSpaceLightDir(input.worldPosition));
                float diffuse = dot(normal, lightDirection);
                float band = smoothstep(
                    _ShadowThreshold - _ShadowSoftness,
                    _ShadowThreshold + _ShadowSoftness,
                    diffuse);

                float shadow = SHADOW_ATTENUATION(input);
                band *= lerp(0.42, 1.0, shadow);

                float3 shadowed = _Color.rgb * _ShadowColor.rgb;
                float3 lit = lerp(shadowed, _Color.rgb, band);
                float3 ambient = ShadeSH9(float4(normal, 1.0)).rgb * _Color.rgb * 0.34;

                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - input.worldPosition);
                float rim = pow(saturate(1.0 - dot(normal, viewDirection)), _RimPower) * _RimStrength;
                float3 color = lit * max(_LightColor0.rgb, 0.3) + ambient + _Color.rgb * 0.35 + _RimColor.rgb * rim;
                return fixed4(color, _Color.a);
            }
            ENDCG
        }

        Pass
        {
            Name "SHADOWCASTER"
            Tags { "LightMode"="ShadowCaster" }
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f
            {
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f input) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(input)
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}
