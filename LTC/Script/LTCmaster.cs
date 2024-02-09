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
struct SubMeshData//��������������ݣ�
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
    public Texture2D ltcTex;//����LTC��LUTͼ
    public Texture2D ltcTex_2;
    //private RenderTexture ScreneNormal;//Gbuffer����
    //private RenderTexture ScreneDepth;
    private RenderTexture TargetTexture;//����ͼ��

    public ComputeShader CS;//areaLight����

    //ComputeShader����ĸ������ݻ���
    private static List<AreaLighting> LightDate = new List<AreaLighting>();//�ƹ�����
    private static List<Vector3> VertexBuffer = new List<Vector3>();//�ƹⶥ�㻺��
    private static List<Vector2> TexcoordBuffer = new List<Vector2>();//�ƹ�UV
    private static List<int> IndexBuffer = new List<int>();//�ƹⶥ������
    private static List<SubMeshData> LightObjects = new List<SubMeshData>();//�����ƹ����������
    //================================ 
    //�Ƚ���RT����
    //blur��addRange�������ϣ���һ����¼TexIndex��tex����״̬��
    //����������LightArray?��һ����������һ��������
    //
    //================================
    //private static List<Texture> texList_1024;//���Խ׶�1024��
    private List<RenderTexture> LightTex_1024=new List<RenderTexture>();
    //����ǵ��ŵ�tex2DArray��ʽ��RT�����ڰ�RT��������һ��ͼƬ���͵�cs
    private RenderTexture TextureBuffer;

    private static ComputeBuffer VBO;
    private static ComputeBuffer TBO;
    private static ComputeBuffer IBO;
    private static ComputeBuffer OBO;

    private Material BlendMat;

    /*
    //�����Լ�д��Ч��һ��ʺ,ֱ����Cbuffer���ƣ����ڣ�ˤ��
    private static List<Renderer> LightRenderer = new List<Renderer>();
    private static List<Material> LightMaterial = new List<Material>();

    private void DrawAreaLight(RenderTexture target)
    {
        CommandBuffer DLCam = new CommandBuffer();
        //��Ϊֱ������colorTexture��depthBuffer�����Դ�����ʱ��Ҫ����Ȼ���һ�𴴽���
        //��������Ӧ���ò��������Ծ�������ν��
        RenderTexture TempBuffer = RenderTexture.GetTemporary(Screen.width, Screen.height, 8, RenderTextureFormat.ARGBHalf);
        
        DLCam.SetRenderTarget(TempBuffer.colorBuffer, TempBuffer.depthBuffer);
        DLCam.ClearRenderTarget(true, true, Color.black);
        for (int i = 0; i < LightRenderer.Count; i++)
        {   //����һ�»���pass����Ȼ�����unity���е�����pass������һ��
            //����Ϊʲô��submeshһ�������˰�������д�����ˣ������������ˤ��
            DLCam.DrawRenderer(LightRenderer[i], LightMaterial[i],0,0);
        }
        DLCam.Blit(TempBuffer, target);
        Graphics.ExecuteCommandBuffer(DLCam);

        RenderTexture.ReleaseTemporary(TempBuffer);
        DLCam.Release();
    }*/


    //���غ��������룩
    public static void RegisterObject(AreaLighting obj)
    {
        LightDate.Add(obj);
    }
    //���غ�����ж�أ�
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
    //��ȡmeshLight����DateBuffer;
    private void BuildAreaLightBuffer()
    {
        //����buffer֮ǰ�����֮ǰ��buffer
        ListClear();
        foreach (AreaLighting obj in LightDate)
        {
            //��ȡ���������������ϵ�mesh���
            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;

            int subMeshCount = mesh.subMeshCount;
            Vector3[] vertexs= mesh.vertices.Select(v => obj.transform.TransformPoint(v)).ToArray();
            int vertexNumber = VertexBuffer.Count;//�����������ʼ����
            VertexBuffer.AddRange(vertexs);
            TexcoordBuffer.AddRange(mesh.uv);

            // //========================����================================


            Material[] mat = obj.GetLightMaterials();//��ȡһ�����ˣ�����
            obj.GetLightTexture(mat);
            //ģ��ͼ������鳤��������ͼ�Ĳ��ʵĳ��ȣ���submeshCount�Բ��ϣ���������Ҫ�в��ʵ�ʱ���+1��
            int SubMeshTextureArrayIndex = 0;
            // //========================================================

            for (int i = 0; i < subMeshCount; i++)
            {
                int[] index = mesh.GetIndices(i);//ÿ��submesh��id��������

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
                //unity����֧�ִ�TextureArray,������RenderTexture��Arrayģʽ��������һ��tex�����Сͳһ
                int TexIndex=-1;//����ƹ�û����ͼ��ʱ�������� -1
                if (mat[i].HasProperty("_MainTex")&& obj.LightingTexture[SubMeshTextureArrayIndex]!=null)
                {//��ͼ����ʼ���������������BlurTextureArray�ǵ���RenderTexture�а�����9��ģ���������Ҫ*9
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
                IndexBuffer.AddRange(index.Select(each => each + vertexNumber));//����index������ÿ��Ԫ��eachִ��=>������
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
                //ע�������TexList��
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


    //��ȡ/������ͼ
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

    //����LUT��ͼ
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
        //��ȡԤ�����lutֵ����ɹ���M���������lutͼ
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

    //������Ⱦ��ʼǰ
    private void OnPreRender()
    {
        //����ֱ��ÿ֡ˢ�£�
        BuildAreaLightBuffer();
        //����ֻ��ʹ��1024�����Դ�Ԥ�輸��TextureSize,�ж�һ�µ�ǰ����ʹ�õ����ĸ�TextureBuffer
        BuildLightTextureBuffer( LightTex_1024, 1024);
        //������ֱ����cginc�ļ�����ģ����Բ�û�д���CS��
        //����׼��ȫ�ᵽcginc���㣬�������ڣ����˩d(�R���Q*)
        Shader.SetGlobalTexture("_LightTex", TextureBuffer);
        if (this.TryGetComponent(out MRT mrt) && mrt.enabled)
        {
            mrt.ManualRneder();
            CommandBuffer cam = new CommandBuffer();
            cam.name = "Area Lighting";
            Matrix4x4 view = ActCamera.worldToCameraMatrix;//worldToView
            Matrix4x4 projection = GL.GetGPUProjectionMatrix(ActCamera.projectionMatrix, false);
            Matrix4x4 worldToPro = projection * view;//����ռ�ת��Ļ�ռ�
            Matrix4x4 proToWorld = worldToPro.inverse;

            
            cam.SetComputeMatrixParam(CS, "_ProToWorld", proToWorld);
            cam.SetComputeVectorParam(CS, "_CameraPosition", ActCamera.transform.position);

            //����Gbuffer������
            //����-��Ե���CS��ResultTarget;
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

            CreateComputeBuffer(ref VBO, VertexBuffer, 4 * 3);//float���ĸ�byte��v3������float
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
            //��ʼ��Ⱦ֮ǰ��MRT�����Depthд�뵽������ϣ����������Ҳ�ܶ���MRT����Ƚ��а�͸����
            //ע�������ǳ�����Depth(����AreaLight)
            ActCamera.SetTargetBuffers(TargetTexture.colorBuffer,MRT.depthRT.depthBuffer);
        }
        else
        {
            Debug.LogWarning("******MRT�ű�δ����******");
            //return;
        }

    }
    //��Ⱦ��ɺ�
    private void OnPostRender() 
    {
        if (this.TryGetComponent(out MRT mrt) && mrt.enabled)
        {
            //����Ⱦ��ɺ󣬽���������Texture������CameraTarget�ϣ�
            //������ڲ�����OnRenderImage��unity���CameraTarget��ȡͼ���䵽Display��
            //���˼��η��ֻ������������ʵ�ã����ǻ��ǲ�����������OnGUI֮��Ĺ���||=_='
            //�����ܿ����Ļ�...������ȥUrp�ɣ��ⲻ�Ǽ������⣬��ȫ��unity���ù��ߵĿ������
            //������cameraTarget���ֶ����������ֲ�ȫ�����䣬���һ���ǵ�����һ��("=_=)
            CommandBuffer setTar=new CommandBuffer();
            setTar.name="����Ⱦ��Ϣ���°󶨵������";
            setTar.Blit(TargetTexture,BuiltinRenderTextureType.CameraTarget);
            Graphics.ExecuteCommandBuffer(setTar);
        }

    }
    //����
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
    
        if (this.TryGetComponent(out MRT mrt) && mrt.enabled)
        {
           //�����Ҫ����Ⱦ�˷ǰ�͸�����͵�ʹ�������������ֵ�޳��ˣ���ʵ�����ǰ�͸���ϳ�Ҳ�Ǵ�ģ�
           //���������û��������ã������������õķ�ʽ(�����ϵ�)���Ǹ����������������Ⱦһ��alpha,
           //���alpha���ǵðѰ�͸���ӽ�ȥ��Ȼ����ֵ�޳���ʱ��ѷ������������alpha���л�Ϻ��ٽ���bloom
           //ps:��������ָ���Ƕ�����ͼ������ĵƹ⣬bloom��ʱ����ò�Ҫ������ֵ�޳�
           if (this.TryGetComponent(out AreaLightBloom bloom) && bloom.enabled)
           {
                RenderTexture tempTex = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                //�ѷ���㴫��Bloom�ű���ȥ������
                bloom.PostImage_Bloom(TargetTexture, ref tempTex);
                Graphics.Blit(tempTex, destination);
                RenderTexture.ReleaseTemporary(tempTex);
           }
           else
           {
                //���û��bloom��ֱ�Ӱ�MRT�Ľ������ȥ
                Graphics.Blit(source, destination);
           }

        }
        else
        {  //���û��MRT��ֱ�Ӱ�������Ľ������ȥ
           Graphics.Blit(source, destination);
 
        }
    }
    private void OnDisable()
    {
        ListClear();
        //�ű������󣬲�����ҪComputeBuffer������ֱ�����ټ��ɣ�Releaseֻ�����������������������屾��
        //����֮ǰ���Զ�����Release()���������ڴ������
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
