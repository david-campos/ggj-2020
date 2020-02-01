Shader "Unlit/DepthShader"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 position : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.position);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				half depth = i.position.z;
				return half4(depth,depth,depth, 1.0f);
            }
            ENDCG
        }
    }
}
