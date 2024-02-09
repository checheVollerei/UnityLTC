#define PI 3.14159265358

float GGX_D(float NdotH,float a)
{
	
	float ndoth=(NdotH*NdotH)*(a*a-1)+1;
	float ndoth_1=ndoth*ndoth*PI;
	return (a*a)/ndoth_1;
}

float3 Schlick_F(float3 F0,float HdotV)
{
	
		float3 F=(1-F0)*pow( 1-HdotV,5)+F0;

		return F;
}
float GGX_G(float a,float NdotV,float NdotL)
{
		float k=((a+1)*(a+1))/8;

		float GGX_V = NdotV / (NdotV*(1-k)+k);
		float GGX_L = NdotL / (NdotL*(1-k)+k);

		float G=GGX_V*GGX_L;
		return G;
}

float3 BRDF_specular(float a,float NL,float NV,float NH,float HV,float F0)
{
	float D=GGX_D(NH,a);//lamber
	float3 F=Schlick_F(F0,HV);//fresnel
	float G=GGX_G(a,NV,NL);
	float3 GGX=D*F*G;
	float3 f_Specular=GGX/(4*max(NV*NL,0.001));
	return f_Specular;

}

float3 SunShine(float3 N,float3 L ,float3 V,float3 LightColor,float3 Albedo, float roughness,float metallic)
{
		roughness=max(roughness,0.05);
		float3 H=normalize(L+V);
		float NdotL=max(0,dot(N,L));
		float NdotV=max(0,dot(N,V));
		float HdotV=max(0,dot(H,V));
		float NdotH=max(0,dot(N,H));
		float a= roughness*roughness;

		float3 F0 = lerp(0.04,Albedo,metallic);
		float3 F=Schlick_F(F0,HdotV);
		float3 kd=(1-F)*(1-metallic);
		
		float3 f_diffuse=kd*(Albedo);//Albedo*(1/PI)



		float3 f_Specular=BRDF_specular( a, NdotL,NdotV,NdotH,HdotV,F0);
		//(漫反射+镜面反射)*cosin*lightColor
		float3 outColor=(f_diffuse+ f_Specular)*NdotL*LightColor;
		return ( outColor);
}
//间接光的F项需要单独计算
float3 Schlick(float NdotV, float3 f0, float roughness)
{
    float r1 = 1.0f - roughness;
    return f0 + (max(float3(r1, r1, r1), f0) - f0) * pow(1 - NdotV, 5);
	//return f0+(max(float3(1.0, 1.0, 1.0)* (1-roughness), f0) - f0) * pow(1.0-NdotV ,5.0);
}
float3 SchlickFrR(float3 wo, float3 norm, float3 F0, float roughness)
{
	roughness=1-roughness;
	float cosTheta = max(dot(wo, norm), 0);
    return F0 + (max(float3(roughness,roughness,roughness), F0) - F0) * pow(1.0 - cosTheta, 5.0);
}

float3 Ambient
(
float3 N,
float3 V ,
float3 Albedo, 
float roughness ,
float metallic,
samplerCUBE _diffuseCube,
samplerCUBE _SpecularCube,
sampler2D _BRDFLut)
{
	roughness=clamp(roughness,0.01,0.99);
								//用法线采样CubeMap
	float NdotV=max(0.1,(dot(N,V)));

	float3 Reflection=2*dot(V,N)*N-V;

	float3 F0=lerp(0.04,Albedo,metallic);

	float3 F=Schlick( NdotV,F0,roughness);

	float kd= (1-F)*(1-metallic);

	float3 IBLdif=texCUBE(_diffuseCube,N).rgb;
	float3 dif=kd*IBLdif*Albedo;


	float rgh=roughness*roughness;
	float Lod=6*rgh;
	float3 IBLspe=texCUBElod(_SpecularCube,float4(Reflection,Lod)).rgb;
	float2 brdf=tex2D(_BRDFLut,float2 (NdotV,roughness)).rg;
	float3 specular=IBLspe*(F*brdf.x+brdf.y);

	float3 ambient=( dif+specular);
	float3 outDebug=IBLdif;
	
	return ambient;


}

//=============BRDF Main Function=======================

//sampler2D _lut;
samplerCUBE _DiffuseNode;
samplerCUBE _SpecularNode;
float3 SurfaceFunction(float3 N,float3 V ,float3 Albedo, float roughness ,float metallic)
{
	//float3 

}



//======================================平均采样 随机数============================================
float RadicalInverse(uint bits )
{
          //reverse bit
          //高低16位换位置
          bits = (bits << 16u) | (bits >> 16u); 
          //A是5的按位取反
          bits = ((bits & 0x55555555) << 1u) | ((bits & 0xAAAAAAAA) >> 1u);
          //C是3的按位取反
          bits = ((bits & 0x33333333) << 2u) | ((bits & 0xCCCCCCCC) >> 2u);
          bits = ((bits & 0x0F0F0F0F) << 4u) | ((bits & 0xF0F0F0F0) >> 4u);
          bits = ((bits & 0x00FF00FF) << 8u) | ((bits & 0xFF00FF00) >> 8u);
          return  float(bits) * 2.3283064365386963e-10;
}

float2 Hammersley(uint i,uint N)
{
          return float2(float(i) / float(N), RadicalInverse(i));
}
//======================================平均采样 随机数============================================
//半球采样，获取随机灯光向量（roughness越大，束越小，roughness为1
float3 importSamplerGGX(float2 Xi ,float roughness,float3 N)
{
	float a=roughness*roughness;
	float Phi=2*3.1415926*Xi.x;
	float cosTheta=sqrt((1-Xi.y)/(1+(a*a-1)*Xi.y));
	float sinTheta=sqrt(1-cosTheta*cosTheta);
	float3 H;
	H.x=sinTheta*cos(Phi);
	H.y=sinTheta*sin(Phi);
	H.z=cosTheta;
	float3 upVector=abs(N.z)<0.9999 ? float3(0,0,1):float3(1,0,0);
	float3 TangentX=normalize(cross(upVector,N));
	float3 TangentY=normalize(cross(N,TangentX));
	return TangentX*H.x+TangentY*H.y+N*H.z;//dot结果cos(θ)
}

//======================================	三角形求交	============================================

//======================================	第一版间接光	============================================
float3 SpecularIBL(float3 N,float3 V,float roughness,samplerCUBE hdrimg,int SamplerNum)
{
	float3 specularLighting=0;
	int NumSampler=SamplerNum;
	roughness=min(roughness,0.9);
	for(int i=0;i<NumSampler;i++)
	{
		float2 Xi=Hammersley(i,NumSampler);
		float3 H=importSamplerGGX(Xi,roughness,N);//半球采样，获取随机灯光向量（roughness越大，束越小，roughness为1 的时候是整个半球像素的平均，为0的时候是单个法线方向的值）
		float3 L=2*dot(V,H)*H-V;
		float NdotL=max(0,dot(N,L));
		float NdotV=max(0,dot(N,V));
		
		float NdotH=max(0,dot(N,H)); 
		float VdotH=max(0,dot(H,V));
		
		if(NdotL>0)
		{
			
			float3 samplerColor=texCUBElod(hdrimg,float4(L,0)).rgb;//为每一支随机的反射向量求取颜色值，然后取平均

			
			float G=GGX_G(roughness,NdotV,NdotL);
			float Fc=pow(1-VdotH,5);

			specularLighting+=(samplerColor);
		}
	}
	specularLighting/=NumSampler;
	float3 outcolor= saturate( specularLighting);
	
	return outcolor;
}
//======================================	第一版间接光	============================================
//======================================	第二版间接光	============================================
	//******************************生成各阶specularMap的函数****************************
float3 PrefilterEnvMap(float roughness,float3 R ,samplerCUBE hdrimg)
{
	float3 N=R;
	float3 V=R;
	float3 PrefilteredColor=0;
	const int NumberSampler=1024;

	float TotalWeight=0;

	for(int i=0;i<NumberSampler;i++)
	{
		float2 Xi=Hammersley(i,NumberSampler);
		float3 H=importSamplerGGX(Xi,roughness,N);//根据对法线进行随即偏移计算随机向量 半角向量
		float3 L=2*dot(V,H)*H-V;//从随机向量计算出射光

		float NdotL=saturate(dot(N,L));
		if(NdotL>0)
		{
			PrefilteredColor+=texCUBElod(hdrimg,float4(L,0)).rgb*NdotL;
			TotalWeight+=NdotL;
		}
	}
	return PrefilteredColor/=TotalWeight;
}
	//******************************生成各阶specularMap的函数****************************
	//******************************生成LUT的函数****************************

// ----------------------------------------------------------------------------
float GeometrySmith(float3 N, float3 V, float3 L, float roughness)
{
	float a = roughness;
	float k = (a * a) / 2.0;

    float NdotV = max(dot(N, V), 0.0001);
    float NdotL = max(dot(N, L), 0.0001);
	//注意顺序
	float ggxV = NdotV / (NdotV * (1.0 - k) + k);
	float ggxL = NdotL / (NdotL * (1.0 - k) + k);

    return ggxV * ggxL;
}



float2 integrateBRDF(float NdotV ,float roughness, int numberSampler)
{
	float3 V;
	V.x=sqrt(1-NdotV*NdotV);//sin?
	V.y=0;
	V.z=NdotV;
	float a=0;
	float b=0;
	//const int numberSampler=1024;
	float3 N=float3(0,0,1);
	
	for(int i=0;i<numberSampler;i++)
	{
		float2 Xi=Hammersley(i,numberSampler);
		float3 H= importSamplerGGX(Xi,roughness,N);//根据对法线进行随即偏移计算随机向量 半角向量
		float3 L=2*dot(V,H)*H-V;//从随机向量计算出射光
		//float NdotL=saturate(L.z);
		//float NdotH= saturate(H.z);
		//float VdotH=saturate(dot(v,H));

		float NdotL=max(dot(N,L),0);
		float NdotH=max (dot(N,H),0);
		float VdotH=max(dot(V,H),0);


		if(NdotL>0)
		{
			//这两个的差别在效果上好像差不多
			//但是下面这个在LUT的体现上更接近网上的例图
			//float G=GGX_G(roughness,NdotV,NdotL);//这是直接套用了直接光的G项计算函数
			float G=GeometrySmith(float3(0,0,1),V,L,roughness);//这个函数源自知乎大佬：https://zhuanlan.zhihu.com/p/517120906
			 float G_vis=G*VdotH/(NdotH*NdotV);
			float fc=pow(1-VdotH,5);
			a+=(1-fc)*G_vis;
			b+=fc*G_vis;
		}
	}
	return float2(a,b)/numberSampler;
}
float3 ApproximateSpecularIBL(float3 SpecularColor,samplerCUBE tex,float roughness,float3 N,float3 V )
{
	float nov=saturate(dot(N,V));
	float3 R=2*dot(V,N)*N-V;
	float3 PrefilteredColor=PrefilterEnvMap(roughness,R,tex);
	float2 envBRDF=integrateBRDF(roughness,nov,512);//生成LUT的函数
	float3 outcolor=PrefilteredColor*(SpecularColor*envBRDF.x+envBRDF.y);
	return float3(envBRDF,1);
}

//==================================================LTC Mesh Lighting====================================
float rectangleSolidAngle(float3 O,float3 v1,float3 v2,float3 v3)
{
	float3 v01=v1-O;
	float3 v02=v2-O;
	float3 v03=v3-O;

	float3 n012=normalize(cross(v01,v02));
	float3 n023=normalize(cross(v02,v03));
	float3 n031=normalize(cross(v03,v01));

	float a02=acos(dot(-n012,n023));
	float a03=acos(dot(-n023,n012));
	float a01=acos(dot(-n031,n012));
	//　三角形的内角和是π弧度。对于边数为N的平面多边形，
	// 我们添加顶点到中央的连线化成N个三角形，再减去中央的一周 2π 弧度，得到内角和公式 (N - 2)π。
	// 借鉴这个思想，我们也能很容易得到球面多边形的面积为 ，去掉 就是立体角公式。
	return a01+a02+a03-2*3.1415926;

}
//这个是四边面锥体计算的简化版，上面是正常多边形的锥体计算
float rightPyramidSolidAngle(float distance, float halfWidth, float halfHeight)
{
	float a = halfWidth;
	float b = halfHeight;
	float h = distance;
	
	float hh = h * h;
	return 3 * asin(a * b / sqrt((a*a + hh) * (b*b + hh)));
}



float integrateEdge(float3 v1, float3 v2, float3 n)
{
	float theta = pow(acos(dot(v1, v2)),1);
	float3 l = normalize(cross(v1, v2));
	return theta *dot(l, n);

	//float theta_2=theta/sin(theta);
	//return theta_2*(v1.x*v2.y-v1.y*v2.x);
}
//emmmm法线和面的积分
float3 integrateQuad(float3 v1,float3 v2,float3 v3, float3 n)
{
	float sum=0;
	sum += integrateEdge(v1, v2, n);
	sum += integrateEdge(v2, v3, n);
	sum += integrateEdge(v3, v1, n);
	return sum*(1/(3.1415926*2));
}
float rayPlaneIntersect(float3 rayDir,float4 vPlane)
{	//					????								法线和入射光的夹角
	float result= -dot(vPlane, float4(0,0,0, 1.0)) / dot(vPlane.xyz, rayDir);
	return result ;
}
//试了下边函数，能正确出uv，不能正确出结果
float edgeFunction(float3 a,float3 b,float3 c)
{
    //叉乘
    float3 ba=b-a;
    float3 ca=c-a;
    //2x3行列式
    float CaCorssBa =ba.x*ca.y+ba.y*ca.z+ba.z*ca.x-ba.x*ca.z-ba.y*ca.x-ba.z*ca.y;
    return CaCorssBa;
}

//这里用的是光追的三角形击中测试算UV
float2 RayTriangle(float3 orig, float3 dir, float3 v0, float3 v1, float3 v2)
{	
	float2 texCoord=float2(0,0);
    float3 v0v1 = v1 - v0;
    float3 v0v2 = v2 - v0;
    float3 pvec = cross(dir, v0v2);
    float det = dot(v0v1, pvec);
    float invDet = 1 / det;

    float3 tvec = orig - v0;
    texCoord.x = dot(tvec, pvec) * invDet;

    float3 qvec = cross(tvec, v0v1);
    texCoord.y = dot(dir, qvec) * invDet;

    // return false when other triangle of quad should be sampled,
    // i.e. we went over the diagonal line
    return texCoord;
}



 //float4 _TexCoord[3];
 //UNITY_DECLARE_TEX2DARRAY(_LightTex);
 Texture2DArray<float4> _LightTex ;
 SamplerState sampler_LightTex;
float3 SampleLightTexture(float3 L1,float3 L2,float3 L3,float2 T1,float2 T2,float2 T3, float3 dir,int TexOffset)
{
	float3 v1=L2-L1;
	float3 v2=L3-L1;
	float3 PlaneOrtho=(cross(v1,v2));//叉乘可得两个向量组成的四边形面积，同时也是法线
	//计算三角形重心坐标
	float2 Coord=RayTriangle(0, PlaneOrtho, L1, L2,L3);
	
//===============***计算LOD***=================
	float4 Plane = float4(PlaneOrtho, -dot(PlaneOrtho,L1));
	float Distance2Plane=rayPlaneIntersect(dir, Plane);
	float planAreaSquared=dot(PlaneOrtho,PlaneOrtho);//计算面积的平方；
	float Distance=abs(Distance2Plane)/pow(planAreaSquared,0.25);
	Distance*=2048.0;
	Distance=log(Distance)/log(3.0);
	float low=floor(Distance);
	float hight=ceil(Distance);
	float amount = saturate(Distance-low);
//===============******通过光追三角形的方式算出重心坐标，然后插值点UV*******=============
	//把三边的负值钳制掉，不然会获得超出meshVertexUV的采样区域
	//这是第一个点的临边uv，下面公式里的saturate是第一个点的对
	//纹理插值用的是中心坐标插值
	//Coord=saturate(Coord);
	float2 TC_2= (1-Coord.x-Coord.y)*T1 + Coord.x*T2 + Coord.y*T3;
	low=clamp(low,0,8)+TexOffset;
	hight=clamp(hight,0,8)+TexOffset;
	float3 col_a=_LightTex.SampleLevel(sampler_LightTex,float3(TC_2,low),0).xyz;
	float3 col_b=_LightTex.SampleLevel(sampler_LightTex,float3(TC_2,hight),0).xyz;

	float3 result=lerp(col_a,col_b,amount);
	 float3 outDebug=float3(TC_2  ,0 );
	 
	return saturate( result);
}
float3 LTCGI_IntegrateEdge(float3 v1, float3 v2)
{
    float x = dot(v1, v2);
    float y = abs(x);

    float a = 0.8543985 + (0.4965155 + 0.0145206*y)*y;
    float b = 3.4175940 + (4.1616724 + y)*y;
    float v = a / b;
    float theta_sintheta = (x > 0.0) ? v : 0.5*rsqrt(max(1.0 - x*x, 1e-7)) - v;

    return cross(v1, v2) * theta_sintheta;
}
//sampler3D 
float3 ClipTriangle(float3 L1,float3 L2,float3 L3,float2 TexCoord1,float2 TexCoord2,float2 TexCoord3,int TexOffset)
{
	

	int config=0;
	//这个思路不错，光栅剔除或许用得上
	if(L1.z>0)config+=1;
	if(L2.z>0)config+=2;
	if(L3.z>0)config+=4;


	float3 T1,T2,T3,T4;//一个临时顶点坐标
	T1=L1;
	T2=L2;
	T3=L3;
	T4=T1;

	int n =0;
	//做不到，部分情况下会有不可名状的错误 
	  //switch(config)
	  //{
	 	 //case 0:
	 	 //break;
	 	 // L1 clip L2 L3							
	 	 //case 1:						//		 T1  L1 
	 	 //n=3;							//		   /\
	 	 ////T1=L1;						//	    T3/__\T2
	 	 //T2=	-L2.z * L1 +L1.z * L2;	//	  L3 /____\ L2
	 	 //T3= -L3.z * L1 +L1.z * L3;
	 	 //break;
	 	  //L2 clip L1 L3
	 	 //case 2:						 //		   L3 
	 	 //n = 3;						 //		   /\
	 	 //T1=	-L1.z*L2+L2.z*L1;		//	   T3 /  \
	 	 //T2=L2;						//	  L2 /_\__\ L1
	 	 //T3=-L3.z*L2+L2.z*L3;			//		T2  T1
	 	 //break;
	 	//L1 L2 ,clip L3
	 	//case 3:
	 	//n=4;						 //		  T1 L1 
	 	////T1=L1;					 //		   /\
	 	//T2=L2;					//	    T4/  \
	 	//T3=-L3.z*L2+L2.z*L3;		 //	  L3 /_\__\ L2
	 	//T4=-L3.z*L1+L1.z*L3;		 //		    T3  T2
	 	//break;
	 	////L3 clip L1 L2
	 	//case 4:						  //	   L1 
	 	//n=3;							 //		   /\
	 	//T1 =-L1.z*L3+L3.z*L1;			//	    T3/  \
	 	//T2 =-L2.z*L3+L3.z*L2;			//	  L3 /_\__\ L2
	 	//T3 =L3;						//	  T1    T2  
	 	//break;
	 	//
	 	//case 5:
	 	//n = 4;
	 	//T1=L1;
	 	//T2=-L2.z*L1+L1.z*L2;
	 	//T3=-L2.z*L3+L3.z*L2;
	 	//T4=L3;
	 	//break;
	 	//
	 	//case 6:
	 	//n=4;
	 	//T1=-L1.z*L2+L2.z*L1;
	 	//T2=L2;
	 	//T3=L3;
	 	//T4=-L1.z*L3+L3.z*L1;
	 	//break;
		//
		//case 7:
		//break;

	// }
	//复制过来的，为啥这就直接正确了（还是TM的if好使
	if(config==1)
	{
		n=3;
		T1=L1;
		T2=	-L2.z * L1 +L1.z * L2;
		T3= -L3.z * L1 +L1.z * L3;
	}
	if(config==2)
	{
		n=3;
		T1=-L1.z*L2+L2.z*L1;
		T2=L2;
		T3=-L3.z*L2+L2.z*L3;
	}
	if(config==3)
	{
		n=4;
		T1=L1;
		T2=L2;
		T3=-L3.z*L2+L2.z*L3;
		T4=-L3.z*L1+L1.z*L3;
	}
	if(config==4)
	{
		n=3;
		T1 =-L1.z*L3+L3.z*L1;
		T2 =-L2.z*L3+L3.z*L2;
		T3 =L3;
	}
	if(config==5)
	{
		n = 4;
		T1=L1;
		T2=-L2.z*L1+L1.z*L2;
		T3=-L2.z*L3+L3.z*L2;
		T4=L3;
	}
	if(config==6)
	{
		n=4;
		T1=-L1.z*L2+L2.z*L1;
		T2=L2;
		T3=L3;
		T4=-L1.z*L3+L3.z*L1;
	}

	if(config==0)//为0的时候是全裁切，7是全不裁切
	{
		return 0.0;
	}
	T1 = normalize(T1);
	T2 = normalize(T2);
	T3 = normalize(T3);
	T4 = normalize(T4);
	//四边形裁剪有三种可能，三角形裁剪过后只有两种，即三角和四边
	float3 sum=0;//逆时针排序
	if(n<4)
	{

		//sum += integrateEdge(T1, T3, normal);
		//sum += integrateEdge(T3, T2, normal);
		//sum += integrateEdge(T2, T1, normal);
		sum+=LTCGI_IntegrateEdge(T1, T3);
		sum+=LTCGI_IntegrateEdge(T3, T2);
		sum+=LTCGI_IntegrateEdge(T2, T1);
	 }
	else
	{
		sum=0;
		//sum += integrateEdge(T1, T4, normal);
		//sum += integrateEdge(T4, T3, normal);
		//sum += integrateEdge(T3, T2, normal);
		//sum += integrateEdge(T2, T1, normal);
		sum+=LTCGI_IntegrateEdge(T1, T4);
		sum+=LTCGI_IntegrateEdge(T4, T3);
		sum+=LTCGI_IntegrateEdge(T3, T2);
		sum+=LTCGI_IntegrateEdge(T2, T1);
	}
	float3 intensity=max(sum.z,0);
	if(TexOffset>=0)
	{
		intensity*=SampleLightTexture(L1,L2,L3,TexCoord1,TexCoord2,TexCoord3,normalize(sum),TexOffset);
	}
	
	//ray_Direction=normalize(sum);
	
	return (  intensity);
}

//sampler2D _ltcMagFresnel;
//sampler2D _ltcSpecular;
/*
float3 LTC_A(float3 v1,float3 v2,float3 v3, float3 n,float3 v,float3 worldPosition,float roughness)
{
	//转半球空间
	float3x3 HS_Matrix;
	float3 T=normalize(v-n*dot(v,n));
	float3 BiT=normalize(cross(n,T));
	HS_Matrix[0]=T;
	HS_Matrix[1]=BiT;
	HS_Matrix[2]=n;

     roughness=clamp( roughness,0.01,0.99);
     float costheta=(acos( dot(n, v)));
	float theta = saturate(costheta/1.57);
	float a = saturate (1-roughness);
    //这里算M矩阵的时候用的是二次方，但是这里用原始的a才是看起来比较正确的（为啥？
    float2  coord=float2(a,theta);
    coord=coord*((64-1.0)/64)+(0.5/64);
    float4 t1=tex2D(_ltcSpecular,coord);
    float4 mf=tex2D(_ltcMagFresnel,float2(coord.x,coord.y));
	float3x3 M_matrix;
	   M_matrix[0]=float3(t1.x , 0   ,t1.y);
	   M_matrix[1]=float3( 0   ,1 , 0);
	   M_matrix[2]=float3(t1.z, 0   , t1.w);
	 //我不理解，它的索引是按行的，填充却是按列填充的，你妈的! 为什么！
	 //float3x3 Minv=transpose(M_matrix);

	float3 edge_1=( v1-worldPosition);
	float3 edge_2=(v2-worldPosition);
	float3 edge_3=(v3-worldPosition);
	
	//float3 L1,L2,L3;
    //计算向量在空间矩阵上的投影（计算空间矩阵中的向量）
    float3 L1=(mul( HS_Matrix,edge_1));
	float3 L2=(mul( HS_Matrix,edge_2));
	float3 L3=(mul( HS_Matrix,edge_3));
    //矩阵在输出的时候转置过了
	float3 S1=(mul(L1,M_matrix));
	float3 S2=(mul(L2,M_matrix));
	float3 S3=(mul(L3,M_matrix));

	//float3 v_1=normalize(mul(v,(HS_Matrix)));
	//float3 spe= ( integrateQuad(S1,S2,S3,float3(0,0,1)));
    //float3 dif= (integrateQuad(L1,L2,L3,float3(0,0,1)));

    float3 dif= saturate(ClipTriangle(L1,L2,L3));
    //原文中好像还给specular乘了个立体角，我这里没算
    float3 spe= saturate( ClipTriangle(S1,S2,S3));
    spe*=mf.x*roughness+((1-roughness)*mf.y);//pbr中的镜面反射和漫反射的混合项
    float3 result=((spe)+dif);
    float3 outDebug=mf.x*roughness+((1-roughness)*mf.y);
   // result/=3.1415926;
	return result;
}*/

struct Gbuffer_LTC
{
	float4 ForwardTarget:SV_Target0;
	float4 Albedo:SV_Target1;
	float4 Normal:SV_Target2;
	float4 RoughnessAndMatellec:SV_Target3;
	float4 Emission:SV_Target4;
	float4 Depth:SV_Target5;
};

Gbuffer_LTC LodingLtcGbuffer(float4 result,float4 albedo,float4 normal,float roughness,float Matellec,float4 emission,float depth )
{
	Gbuffer_LTC o;
	o.ForwardTarget=result;
	o.Albedo=albedo;
	o.Normal=float4(normal.xyz*0.5f+0.5f,normal.w);
	o.RoughnessAndMatellec=float4(0,0,roughness,Matellec);
	o.Emission=emission;
	o.Depth=float4(depth,0,0,0);
	return o;
}