Shader "Unlit/CoolWater"
{
	Properties
	{
		_Top("Top", Float) = 0
		_Bottom("Bottom", Float) = -20
		_Color("Color", Color) = (1,1,1,1)
		_ColorTop("Color Top", Color) = (1,1,1,1)
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			half4 _Color;
			half4 _ColorTop;
			float _Top;
			float _Bottom;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
				float2 worldY : TEXCOORD0;
				UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldY = float2(mul(UNITY_MATRIX_M, float4(v.vertex)).y, 0);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
				float factor = (i.worldY - _Bottom) / (_Top - _Bottom);
				fixed4 col = lerp(_Color, _ColorTop, factor);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
