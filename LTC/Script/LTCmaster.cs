using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using System.IO;
//using System.Runtime.Serialization.Formatters.Binary;
using LTCmath;
using System.Linq;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
//
//using Post;
struct SubMeshData//对于子网格的数据，
{
    public int StartIndex;
    public int EndIndex;

    public Vector3 MeshColor;
    public float SpecularIntensity;

    public int TextureStartIndex;

}

//[ExecuteAlways, ImageEffectAllowedInSceneView]
public class LTCmaster : MonoBehaviour
{
    private Camera ActCamera;
    public Texture2D ltcTex;//两张LTC的LUT图
    public Texture2D ltcTex_2;
    //private RenderTexture ScreneNormal;//Gbuffer数据
    //private RenderTexture ScreneDepth;
    private RenderTexture TargetTexture;//最终图像

    public ComputeShader CS;//areaLight计算

    //ComputeShader所需的各个数据缓存
    private static List<AreaLighting> LightDate = new List<AreaLighting>();//灯光链表
    private static List<Vector3> VertexBuffer = new List<Vector3>();//灯光顶点缓冲
    private static List<Vector2> TexcoordBuffer = new List<Vector2>();//灯光UV
    private static List<int> IndexBuffer = new List<int>();//灯光顶点索引
    private static List<SubMeshData> LightObjects = new List<SubMeshData>();//单个灯光物体的数据
    //================================ 
    //先建立RT链表？
    //blur完addRange到链表上？这一步记录TexIndex和tex启用状态？
    //根据链表创建LightArray?这一步单独建立一个函数？
    //
    //================================
    //private static List<Texture> texList_1024;//测试阶段1024；
    private List<RenderTexture> LightTex_1024=new List<RenderTexture>();
    //这个是单张的tex2DArray格式的RT，用于把RT数组打包成一张图片发送到cs
    private RenderTexture TextureBuffer;

    private static ComputeBuffer VBO;
    private static ComputeBuffer TBO;
    private static ComputeBuffer IBO;
    private static ComputeBuffer OBO;

    private Material BlendMat;

    /*
    //反正自己写的效率一坨屎,直接用Cbuffer绘制，开摆（摔！
    private static List<Renderer> LightRenderer = new List<Renderer>();
    private static List<Material> LightMaterial = new List<Material>();

    private void DrawAreaLight(RenderTexture target)
    {
        CommandBuffer DLCam = new CommandBuffer();
        //因为直接用了colorTexture的depthBuffer，所以创建的时候要把深度缓冲一起创建，
        //不过后期应该用不到，所以精度无所谓了
        RenderTexture TempBuffer = RenderTexture.GetTemporary(Screen.width, Screen.height, 8, RenderTextureFormat.ARGBHalf);
        
        DLCam.SetRenderTarget(TempBuffer.colorBuffer, TempBuffer.depthBuffer);
        DLCam.ClearRenderTarget(true, true, Color.black);
        for (int i = 0; i < LightRenderer.Count; i++)
        {   //限制一下绘制pass，不然它会把unity所有的内置pass都绘制一遍
            //啊？为什么把submesh一起限制了啊，不想写索引了，换相机！！（摔！
            DLCam.DrawRenderer(LightRenderer[i], LightMaterial[i],0,0);
        }
        DLCam.Blit(TempBuffer, target);
        Graphics.ExecuteCommandBuffer(DLCam);

        RenderTexture.ReleaseTemporary(TempBuffer);
        DLCam.Release();
    }*/


    //重载函数（载入）
    public static void RegisterObject(AreaLighting obj)
    {
        LightDate.Add(obj);
    }
    //重载函数（卸载）
    public static void UnregisterObject(AreaLighting obj)
    {
        LightDate.Remove(obj);
    }

    private void ListClear()
    {
        VertexBuffer.Clear();
        TexcoordBuffer.Clear();
        IndexBuffer.Clear();
        LightObjects.Clear();
        LightTex_1024.Clear();
    }
    //获取meshLight构建DateBuffer;
    private void BuildAreaLightBuffer()
    {
        //构建buffer之前先清空之前的buffer
        ListClear();
        foreach (AreaLighting obj in LightDate)
        {
            //获取这个组件所在物体上的mesh组件
            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;

            int subMeshCount = mesh.subMeshCount;
            Vector3[] vertexs= mesh.vertices.Select(v => obj.transform.TransformPoint(v)).ToArray();
            int vertexNumber = VertexBuffer.Count;//顶点数组的起始索引
            VertexBuffer.AddRange(vertexs);
            TexcoordBuffer.AddRange(mesh.uv);

            // //========================材质================================


            Material[] mat = obj.GetLightMaterials();//获取一个算了，好累
            obj.GetLightTexture(mat);
            //模糊图像的数组长度是有贴图的材质的长度，和submeshCount对不上，所以这里要有材质的时候才+1；
            int SubMeshTextureArrayIndex = 0;
            // //========================================================

            for (int i = 0; i < subMeshCount; i++)
            {
                int[] index = mesh.GetIndices(i);//每个submesh的id索引集合

                Color matCol = Color.white;
                float Intensity = 1.0f;
                if (mat[i].HasProperty("_LightColor"))
                {
                    matCol = mat[i].GetColor("_LightColor");
                }
                if (mat[i].HasProperty("_EmissiveIntensity"))
                {
                    Intensity = mat[i].GetFloat("_EmissiveIntensity");
                }
                //unity好像不支持传TextureArray,所以用RenderTexture的Array模式，限制是一组tex必须大小统一
                int TexIndex=-1;//这个灯光没有贴图的时候，索引是 -1
                if (mat[i].HasProperty("_MainTex")&& obj.LightingTexture[SubMeshTextureArrayIndex]!=null)
                {//贴图的起始索引，由于这里的BlurTextureArray是单个RenderTexture中包括了9层的，所以这里要*9
                    TexIndex = LightTex_1024.Count*9;

                    LightTex_1024.Add(obj.BlurTextureArray[SubMeshTextureArrayIndex]);
                    SubMeshTextureArrayIndex++;
                }
                
                int StartIndex = IndexBuffer.Count;
                LightObjects.Add(
                new SubMeshData() 
                { 
                    StartIndex = StartIndex, 
                    EndIndex = StartIndex + index.Length, 
                    MeshColor = new Vector3(matCol.r,matCol.g,matCol.b),
                    SpecularIntensity=Intensity,
                    TextureStartIndex= TexIndex
                }
                );
                IndexBuffer.AddRange(index.Select(each => each + vertexNumber));//对于index数组中每个元素each执行=>的命令
            }
            


        }
    }

    private void BuildLightTextureBuffer(List<RenderTexture> tex,int texSize )
    {
        if (TextureBuffer == null || TextureBuffer.volumeDepth != tex.Count*9)
        {
            if (TextureBuffer != null)
            {
                TextureBuffer.Release();
            }

                TextureBuffer = new RenderTexture(texSize, texSize, 0);
                //注意这里的TexList是
                TextureBuffer.volumeDepth = tex.Count * 9;
                TextureBuffer.dimension = TextureDimension.Tex2DArray;
                TextureBuffer.Create();

        }
        CommandBuffer BlitTexture = new CommandBuffer();
        BlitTexture.name = "Send Texture to ComputeShader";
        int index = 0;
        for (int i = 0; i < tex.Count; i++)
        {
            for (int j = 0; j < tex[i].volumeDepth; j++)
            {
                BlitTexture.Blit(tex[i], TextureBuffer,j, index);
                index++;
              //  Graphics
            }
        }
        Graphics.ExecuteCommandBuffer(BlitTexture);
        BlitTexture.Release();
    }


    //获取/创建贴图
    private void CreateTargetTexture(ref RenderTexture rt)
    {
        
        if (rt == null || rt.width != Screen.width || rt.height != Screen.height)
        {
            if (rt != null)
            {
                rt.Release();

            }
            rt = new RenderTexture(Screen.width, Screen.height,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);
            rt.enableRandomWrite = true;
            rt.Create();
          //  RenderTextureDescriptor
            
        }

    }

    //创建LUT贴图
    private void CreateLutTexture()
    {
        ltcTex = new Texture2D(MeshLightLUT.TextureSize, MeshLightLUT.TextureSize, TextureFormat.RGBAFloat, false, true)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        ltcTex_2 = new Texture2D(MeshLightLUT.TextureSize, MeshLightLUT.TextureSize, TextureFormat.RGBAFloat, false, true)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        Color[] col = new Color[MeshLightLUT.PixvelsNumber];

        Color[] col_2 = new Color[MeshLightLUT.PixvelsNumber];

        for (int i = 0; i < MeshLightLUT.PixvelsNumber; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                col[i][j] = MeshLightLUT.Specular[i, j];
                col_2[i][j] = MeshLightLUT.MagFresnel[i, j];
            }

        }
        ltcTex.SetPixels(col);
        ltcTex_2.SetPixels(col_2);
        ltcTex.Apply();
        ltcTex_2.Apply();


    }

    private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride) where T :struct
    {
        if (buffer != null)
        {
            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
            {
                buffer.Release();
                buffer = null;
            }
        
        }
        if (data.Count != 0)
        {
            if (buffer == null)
            {
                buffer = new ComputeBuffer(data.Count, stride);
            }
            buffer.SetData(data);
        }


    
    }

    private void SetComputeBuffer(string name, ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            CS.SetBuffer(0, name, buffer);
        }
    
    }
    void Start()
    {
        
    }
    CommandBuffer setDepthBuffer;
    void OnEnable()
    {
        ActCamera = this.GetComponent<Camera>();
        BlendMat = new(Resources.Load("BlendTex") as Shader);
        CS = Resources.Load("AreaLight") as ComputeShader;
        //获取预计算的lut值，组成构建M矩阵所需的lut图
        CreateLutTexture();
        //Shader.SetGlobalTexture("_ltcSpecular", ltcTex);
        //Shader.SetGlobalTexture("_ltcMagFresnel", ltcTex_2);
        CS.SetTexture(0,"_ltcSpecular", ltcTex);
        CS.SetTexture(0, "_ltcMagFresnel", ltcTex_2);
        //RenderTexture.Destroy(previewTex);
        //Texture2D.Destroy(ltcTex);

        //setDepthBuffer = new CommandBuffer();
//
        ////setDepthBuffer.Blit(TargetTexture, BuiltinRenderTextureType.CameraTarget);
        //setDepthBuffer.Blit(TargetTexture, BuiltinRenderTextureType.CameraTarget);
        ////setDepthBuffer.Blit(MRT.M_RT[5], BuiltinRenderTextureType.ResolvedDepth);
        ////Graphics.ExecuteCommandBuffer(setDepthBuffer);
        //ActCamera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque,setDepthBuffer);
        
    }
    private void Update()
    {
        
    }

    //场景渲染开始前
    private void OnPreRender()
    {
        //这里直接每帧刷新，
        BuildAreaLightBuffer();
        //这里只是使用1024，可以传预设几个TextureSize,判断一下当前物体使用的是哪个TextureBuffer
        BuildLightTextureBuffer( LightTex_1024, 1024);
        //这里是直接在cginc文件计算的，所以并没有传到CS里
        //后续准备全搬到cginc里算，但是现在！摆了ヾ(≧▽≦*)
        Shader.SetGlobalTexture("_LightTex", TextureBuffer);
        if (this.TryGetComponent(out MRT mrt) && mrt.enabled)
        {
            mrt.ManualRneder();
            CommandBuffer cam = new CommandBuffer();
            cam.name = "Area Lighting";
            Matrix4x4 view = ActCamera.worldToCameraMatrix;//worldToView
            Matrix4x4 projection = GL.GetGPUProjectionMatrix(ActCamera.projectionMatrix, false);
            Matrix4x4 worldToPro = projection * view;//世界空间转屏幕空间
            Matrix4x4 proToWorld = worldToPro.inverse;

            
            cam.SetComputeMatrixParam(CS, "_ProToWorld", proToWorld);
            cam.SetComputeVectorParam(CS, "_CameraPosition", ActCamera.transform.position);

            //这是Gbuffer的数据
            //清屏-针对的是CS的ResultTarget;
            CreateTargetTexture(ref TargetTexture);
            RenderTexture temp = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
            temp.name = "Clear";
            cam.Blit(temp, TargetTexture);
            RenderTexture.ReleaseTemporary(temp);
            GL.Clear(true, true, Color.red);
            //CS.SetBuffer()
            cam.SetComputeTextureParam(CS, 0, "_ForwardTarget", MRT.M_RT[0]);
            cam.SetComputeTextureParam(CS, 0, "_AlbedoTexture", MRT.M_RT[1]);
            cam.SetComputeTextureParam(CS, 0, "_NormalTexture", MRT.M_RT[2]);
            cam.SetComputeTextureParam(CS, 0, "_RougMateTexture", MRT.M_RT[3]);
            cam.SetComputeTextureParam(CS, 0, "_DepthTexture", MRT.M_RT[5]);
            cam.SetComputeTextureParam(CS, 0, "_Result", TargetTexture);

            CreateComputeBuffer(ref VBO, VertexBuffer, 4 * 3);//float是四个byte，v3是三个float
            CreateComputeBuffer(ref TBO, TexcoordBuffer, 4 * 2);
            CreateComputeBuffer(ref IBO, IndexBuffer, 4);
            CreateComputeBuffer(ref OBO, LightObjects, 4 * 2 + 4 * 3 + 4+4);
            SetComputeBuffer("_VertexBuffer", VBO);
            SetComputeBuffer("_TexcoordBuffer", TBO);
            SetComputeBuffer("_IndexBuffer", IBO);
            SetComputeBuffer("_LightObjects", OBO);
            //TargetTexture.
            cam.DispatchCompute(CS, 0, Screen.width / 8, Screen.height / 8, 1);
            //cam.Blit(TargetTexture,BuiltinRenderTextureType.CameraTarget);
            Graphics.ExecuteCommandBuffer(cam);
            cam.Release();
            Shader.SetGlobalTexture("_RenderTarget", TargetTexture);
            //开始渲染之前把MRT的输出Depth写入到主相机上，这样主相机也能读到MRT的深度进行半透明了
            //注意这里是场景的Depth(包含AreaLight)
            ActCamera.SetTargetBuffers(TargetTexture.colorBuffer,MRT.depthRT.depthBuffer);
        }
        else
        {
            Debug.LogWarning("******MRT脚本未载入******");
            //return;
        }

    }
    //渲染完成后
    private void OnPostRender() 
    {
        if (this.TryGetComponent(out MRT mrt) && mrt.enabled)
        {
            //在渲染完成后，将缓冲区的Texture，传到CameraTarget上；
            //这个是在不启用OnRenderImage的unity会从CameraTarget获取图像传输到Display上
            //试了几次发现还是这个方法更实用，但是还是不能适配类似OnGUI之类的功能||=_='
            //有人能看到的话...还是润去Urp吧，这不是技术问题，完全是unity内置管线的框架问题
            //明明有cameraTarget这种东西，但你又不全局适配，这调一个那调用另一个("=_=)
            CommandBuffer setTar=new CommandBuffer();
            setTar.name="将渲染信息重新绑定到相机上";
            setTar.Blit(TargetTexture,BuiltinRenderTextureType.CameraTarget);
            Graphics.ExecuteCommandBuffer(setTar);
        }

    }
    //后处理
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
    
        if (this.TryGetComponent(out MRT mrt) && mrt.enabled)
        {
           //主相机要是渲染了非半透明，就得使用主相机进行阈值剔除了（其实就算是半透明合成也是错的）
           //但是这样得话质量不好，对于主相机最好的方式(质量上的)还是给主相机单独进行渲染一个alpha,
           //这个alpha还是得把半透明加进去，然后阈值剔除得时候把发光层和主相机得alpha进行混合后再进行bloom
           //ps:质量不好指的是对于有图像纹理的灯光，bloom的时候最好不要进行阈值剔除
           if (this.TryGetComponent(out AreaLightBloom bloom) && bloom.enabled)
           {
                RenderTexture tempTex = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                //把发光层传到Bloom脚本里去做后处理
                bloom.PostImage_Bloom(TargetTexture, ref tempTex);
                Graphics.Blit(tempTex, destination);
                RenderTexture.ReleaseTemporary(tempTex);
           }
           else
           {
                //如果没开bloom，直接把MRT的结果传上去
                Graphics.Blit(source, destination);
           }

        }
        else
        {  //如果没开MRT，直接把主相机的结果传上去
           Graphics.Blit(source, destination);
 
        }
    }
    private void OnDisable()
    {
        ListClear();
        //脚本结束后，不再需要ComputeBuffer，所以直接销毁即可，Release只是清除，并不会销毁这个物体本身；
        //销毁之前会自动调用Release()函数进行内存清除；
        if (VBO!=null)VBO.Dispose();
        if (TBO != null) TBO.Dispose();
        if (IBO != null) IBO.Dispose();
        if (OBO != null) OBO.Dispose();
        LightDate.Clear();
        Destroy(TargetTexture);
        Destroy(TextureBuffer);
        Destroy(ltcTex);
        Destroy(ltcTex_2);
    }
}
