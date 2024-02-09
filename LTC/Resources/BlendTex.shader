Shader "Hidden/BlendTex"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _RenderTarget;
            float4 frag (v2f i) : SV_Target
            {
               float4 dz = tex2D(_RenderTarget, i.uv);

                // �����ֵд�뵽��Ȼ�������
                //clip(i.vertex.z-depth);

                // ����������ɫֵ����Ϊ��ɫ�����������ݲ�����Ҫ
                //return float4(1, 1, 1, 1);
                float4 col_1 = tex2D(_MainTex, i.uv);
                //float4 col_2 = tex2D(_BlendTarget, i.uv);
                float4 result=dz;
                return result;
            }
            ENDCG
        }
    }
}
