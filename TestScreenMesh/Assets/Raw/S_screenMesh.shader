Shader "Unlit/S_screenMesh"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
			_p00("_p00", Vector) = (0,0,0,0)
			_p10("_p10", Vector) = (0,0,0,0)
			_p01("_p01", Vector) = (0,0,0,0)
			_p11("_p11", Vector) = (0,0,0,0)
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
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

			float4 _p00;
			float4 _p10;
			float4 _p01;
			float4 _p11;

            v2f vert (appdata v)
            {
                v2f o;
                //o.vertex = UnityObjectToClipPos(v.vertex);
				float2 uv = v.vertex.xy;
				float3 px0 = lerp(_p00, _p10, uv.x);
				float3 px1 = lerp(_p01, _p11, uv.x);
				float3 pos = lerp(px0, px1, uv.y);
				o.vertex = UnityObjectToClipPos(mul(unity_WorldToObject, float4(pos,1)));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}