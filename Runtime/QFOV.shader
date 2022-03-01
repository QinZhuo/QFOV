Shader "QTool/QFOV"
{
    Properties
    {
        _ColorA ("Texture", Color)=(0,0,0,0.5)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+20" }
		ZTest Off
		Blend SrcAlpha OneMinusSrcAlpha
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

            float4 _ColorA;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o; 
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
             
                return _ColorA;
            }
            ENDCG
        }
    }
}
