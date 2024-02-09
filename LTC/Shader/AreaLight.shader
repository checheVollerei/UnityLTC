Shader "Hidden/AreaLight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
         _LightColor("LightColor",COLOR)=(1,1,1)
        _EmissiveIntensity("EmissiveIntensity",Range(0,10))=0.5
    }
    SubShader
    {
        // No culling or depth
       // Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Assets/LTC/Resources/PBR.cginc"
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _LightColor;
            float _EmissiveIntensity;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
              
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                return o;
            }



            Gbuffer_LTC frag (v2f i)
            {
            Gbuffer_LTC o;
            //注意下dx和GL的纹理差异
            float2 texcoord=i.uv;
            # if UNITY_UV_STARTS_AT_TOP
            if (_MainTex_TexelSize.y < 0)
                    texcoord.y = 1-texcoord.y;
            # endif
             //_EmissiveIntensity*=sin(_Time.z);
                float4 col = tex2D(_MainTex, texcoord);
                float4 result=(_LightColor*col)*_EmissiveIntensity;
                
                // just invert the colors
                //col.rgb = 1 - col.rgb;
                 //col.rgb=LinearToGammaSpace(col.rgb);
                //o.ForwardTarget=col*_EmissiveIntensity;
               	//o.Albedo=0;
	            //o.Normal=1;
	            //o.Depth=0;
                o=LodingLtcGbuffer(result,_LightColor,0,0,0,result,0);
                return o;

                //return float4(result,1);
            }
            ENDCG
        }
    }
}
