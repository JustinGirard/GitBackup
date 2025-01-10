Shader "Custom/RadialProgressOptimized"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Progress ("Progress", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _Progress;
            half4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.vertex.xy; // Assume UVs align with world-space XY
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // Map UVs to an angle in the range [0, 1]
                float angle = atan2(i.uv.y, i.uv.x) / (2 * UNITY_PI);
                angle = angle < 0 ? 1 + angle : angle;

                // Display only parts of the pie within progress range
                if (angle <= _Progress)
                    return _Color;
                else
                    return half4(0, 0, 0, 0); // Transparent
            }
            ENDCG
        }
    }
}
