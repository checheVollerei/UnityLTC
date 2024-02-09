Shader "Unlit/BlurSample"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }


            CGINCLUDE
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
            sampler2D _PrevMip;
            sampler2D _BloomTex;
            sampler2D _RenderTarget;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float GaussWeight2D(float x, float y, float sigma)
            {
                float PI = 3.14159265358;
                float E  = 2.71828182846;
                float sigma_2 = pow(sigma, 2);

                float a = -(x*x + y*y) / (2.0 * sigma_2);
                return pow(E, a) / (2.0 * PI * sigma_2);
            }

            float3 Gauss5X5(sampler2D tex,float2 uv,float2 pexleSize)
            {
            //¡À2+0
                int s=2;
                float3 result=float3(0,0,0);
                float weight=0.0;
                for(int i=-s;i<=s;i++)
                {
                    for(int j=-s;j<=s;j++)
                    {
                        float2 texCoord=uv+float2(i*pexleSize.x,j*pexleSize.y);
                        float w=GaussWeight2D(i,j,1.0f);

                        result+=tex2D(tex,texCoord).rgb*w;
                        weight += w;
                    }
                
                }
                result/=weight;
                return result;
            }


            
            float4 Downfrag (v2f i) : SV_Target
            {
                // sample the texture
                float4 col = float4(0,0,0,1);
                //float3 curr_Col=Gauss5X5(_MainTex,i.uv,_MainTex_TexelSize.xy);
                col.rgb=Gauss5X5(_MainTex,i.uv,_MainTex_TexelSize.xy);
                return col;
            }

            float4 UPfrag (v2f i) : SV_Target
            {
                // sample the texture
                float4 col = float4(0,0,0,1);
                float2 prev_PixelSize=0.5*_MainTex_TexelSize.xy;
                float2 curr_PixelSize=_MainTex_TexelSize.xy;
                float3 prev_Col=Gauss5X5(_PrevMip,i.uv,prev_PixelSize);
                float3 curr_Col=Gauss5X5(_MainTex,i.uv,curr_PixelSize);
               col.rgb=curr_Col+prev_Col;
               //float3 curr_Col_2=Gauss5X5(_MainTex,i.uv,0.5*_MainTex_TexelSize.xy);
               //  col.rgb=curr_Col_2;
                return col;
            }


            float3 ACESToneMapping(float3 color, float adapted_lum)
            {
                const float A = 2.51f;
                const float B = 0.03f;
                const float C = 2.43f;
                const float D = 0.59f;
                const float E = 0.14f;

                color *= adapted_lum;
                return (color * (A * color + B)) / (color * (C * color + D) + E);
            }
            float _Lumin;
            float4 MutiplyImage (v2f i) : SV_Target
            {
                float3 Col= tex2D(_RenderTarget, i.uv).rgb;
                float3 BloomCol=tex2D(_BloomTex,i.uv).rgb*_Lumin;

                BloomCol=ACESToneMapping(BloomCol,1.0);

                //BloomCol= saturate(pow(BloomCol,1/2.2));
                // sample the texture
                float4 Result = float4((1-BloomCol)*Col+BloomCol,1);
                return Result;
            }

           float _luminanceThreshole;

            float4 Threshold (v2f i) : SV_Target
            {
                float4 color = tex2D(_MainTex, i.uv);
                float lum = dot(float3(0.2126, 0.7152, 0.0722), color.rgb);
                if(lum>_luminanceThreshole) 
                {
                    return color;
                }
                return float4(0,0,0,1);
            }


            ENDCG
                    Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment Downfrag
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment UPfrag
            ENDCG
        }
                Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment MutiplyImage
            ENDCG
        }
                        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment Threshold
            ENDCG
        }
    }
}
