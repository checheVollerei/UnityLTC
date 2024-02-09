Shader "Unlit/PBRLTC"
{
    Properties
    {
        _MainTex ("AlbedoMap", 2D) = "white" {}
        _color("Color",color)=(1,1,1)
        _Roughness("Roughness",Range(0,1))=1
        _AmbStrength("AmbientStrength",Range(0,1))=1
        _Metallic("Metallic",Range(0,1))=0.5

        _difCube("difCube",CUBE)="white"{}
        _speCube("speCube",CUBE)="white"{}
        _IBL("IBL",2D)="white"{}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }




        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

           #include "Assets/LTC/Resources/PBR.cginc"
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            struct a2v
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 Normal:NORMAL;
                float4 Tangent:TANGENT;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 T2W_1:TEXCOORD1;
                float4 T2W_2:TEXCOORD2;
                float4 T2W_3:TEXCOORD3;
                float2 depth:TEXCOORD4;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float3 _color;
            float _Metallic;
            float _Roughness;

            float _AmbStrength;
           // sampler2D _CameraDepthTexture;
            samplerCUBE _difCube;
            samplerCUBE _speCube;
            sampler2D _IBL;
            sampler2D _ShadowTex;

            v2f vert (a2v v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.depth=o.vertex.zw;
                float3 worldPos=mul(unity_ObjectToWorld,v.vertex).xyz;
                float3 worldNor=UnityObjectToWorldNormal(v.Normal);
                float3 worldTag=UnityObjectToWorldDir(v.Tangent.xyz);
                float3 worldBiTag=cross(worldNor,worldTag)*v.Tangent.w;

                o.T2W_1=float4(worldTag.x,worldBiTag.x,worldNor.x,worldPos.x);
                o.T2W_2=float4(worldTag.y,worldBiTag.y,worldNor.y,worldPos.y);
                o.T2W_3=float4(worldTag.z,worldBiTag.z,worldNor.z,worldPos.z);
                o.uv = v.uv;

                return o;
            }
            sampler2D _lut;
            Gbuffer_LTC frag (v2f i)
            {

                Gbuffer_LTC o;

                float3 wPos=float3(i.T2W_1.w,i.T2W_2.w,i.T2W_3.w);
                float3 LightDir=normalize(_WorldSpaceLightPos0).xyz;
                float3 ViewDir=normalize(_WorldSpaceCameraPos.xyz-wPos.xyz);
                float2 ScreenUV=i.vertex.xy*(_ScreenParams.zw-1);

                float3 Normal=normalize(float3((i.T2W_1.z),(i.T2W_2.z),(i.T2W_3.z)));
                //
                //float3 Nor=normalize(i.normal);


                float3 LightCol=_LightColor0.rgb;//灯光颜色
                float4 texCol = tex2D(_MainTex, i.uv);

                float4 albedo = float4(_color.rgb,1);
                albedo*=texCol;
                float3 col=SunShine( Normal,LightDir , ViewDir,LightCol, albedo,1-_Roughness, _Metallic);//直接光
                //间接光
                float3 amb=Ambient(Normal,ViewDir,albedo,1-_Roughness,_Metallic,_difCube,_speCube,_IBL);//间接光
                float4 result=float4(amb*_AmbStrength+col,1);
                float depth= i.vertex.z;

                o=LodingLtcGbuffer(result,albedo,float4(Normal,1),1-_Roughness,_Metallic,0,depth);

                return o;
            }
            ENDCG
        }
    }
}
