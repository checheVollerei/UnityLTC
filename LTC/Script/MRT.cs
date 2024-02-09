using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]

//[ExecuteAlways, ImageEffectAllowedInSceneView]
public class MRT : MonoBehaviour
{

    private Material BlendMat;
    private Camera ExecuteCamera;
    private static Camera MRT_Camera;
    private GameObject TempObj;
    //�������������CullingMask��ִ������ű���Ҫ�޳����������ظ���Ⱦ����ִ���˾ͻָ�
    private static LayerMask TempCullingMask;
    private static CameraClearFlags TempClearFlags;
    //��������ȾMRTʱ���CullingMask,����UI֮��ģ���ִ��MRT�����������������
    public LayerMask MrtCameraCullingMask=1;//Ĭ��Default
    //private RenderTargetIdentifier[] RTID = new RenderTargetIdentifier[2];
    
    public static RenderTexture depthRT;
    private static RenderBuffer DepthBuffer;
    public static RenderTexture[] M_RT = new RenderTexture[6];
    private static RenderBuffer[] M_Buffer = new RenderBuffer[6];
    //==============================
    //һ��ԭʼ���ͼ�� ������LTC�Ľ���ϳ� ��ǰ����ӳٵĺϳ�Ҳ��Ҫ(���磬ClusterLight)
    //һ�Ż���ɫ�������ռ�����Ҫ����albedo 
    //һ��normal
    //һ��roughness&&matellec
    //Emission�����ڹ���ģ�ͽ���bloom
    //Depth�������������ع�����ֱ�Ӵ�depthbuffer��ȡ������ֱ�������Ҫ���ж���ι��յ������depth
    //�Ͼ�meshLightҲ�ǿ���Ͷ����Ӱ�ģ��԰ɣ�
    //==============================

    //================================
    //�����ɺ󣬶����������MRT���������ɵĽ�����л��
    //================================
    private void OnEnable()
    {
        BlendMat = new(Resources.Load("BlendTex") as Shader);
        ExecuteCamera = this.GetComponent<Camera>();
        TempCullingMask = ExecuteCamera.cullingMask;//�����������layerMask
        TempClearFlags=ExecuteCamera.clearFlags;
        //��ȥһ�㣬��������WTF ������������������д��
        //ExecuteCamera.cullingMask &= ~(1<<LayerMask.NameToLayer("AreaLight"));
        if (this.transform.Find("DrawAreaLight"))
        {
            TempObj = this.transform.Find("DrawAreaLight").gameObject;
            if (TempObj.GetComponent<Camera>())
            {
                MRT_Camera = TempObj.GetComponent<Camera>();
            }
            else
            {
                MRT_Camera = TempObj.AddComponent<Camera>();
            }
        }
        else
        {
            TempObj = new GameObject("DrawAreaLight");
            TempObj.transform.parent = this.transform;//������ŵ���ǰ�ű����Ӽ�
            MRT_Camera = TempObj.AddComponent<Camera>();
            MRT_Camera.name = "DrawAreaLight";
        }

         MRT_Camera.CopyFrom(ExecuteCamera);
         MRT_Camera.backgroundColor = Color.black;
         //����CameraClearFlags
         //Skybox������Target0��depth�������,�����������skyBoxClear,��Ҫ�ֶ���MRT�������
         //
         //
         MRT_Camera.clearFlags = CameraClearFlags.Skybox;
         MRT_Camera.depth = -10;//����ν�ˣ��������õ��ֶ�Render��
         MRT_Camera.cullingMask = MrtCameraCullingMask;
         MRT_Camera.enabled = false;//�ص��������������ֶ�Render

        //���������layerMask����Ϊnothing
        //ExecuteCamera.cullingMask = 0;//ע�����Ҫ��copyCamera��
        //��ȥ���Ϳ�������
        ExecuteCamera.cullingMask = TempCullingMask - MrtCameraCullingMask;
        //�ر�����ı������
        ExecuteCamera.clearFlags=CameraClearFlags.Nothing;
        //ExecuteCamera.cullingMask &= ~(1 << MrtCameraCullingMask.value);

    }
    
    public void TargetTexturBlend(RenderTexture source, ref RenderTexture BlendeTarget)
    {
        if(BlendMat!=null)
        {
            BlendMat.SetTexture("_BlendTarget",BlendeTarget);
            Graphics.Blit(source,BlendeTarget,BlendMat);
        }


    }
    private void Update()
    {
    }
    private static void CreateTex(ref RenderTexture tex, int depth, RenderTextureFormat format,string name, RenderTextureReadWrite rw)
    {
        if (tex == null || tex.width != Screen.width || tex.height != Screen.height)
        {
            if (tex != null)
            {
                tex.Release();

            }
            tex = new RenderTexture(Screen.width, Screen.height, depth, format,rw);
            tex.name = name;
            tex.Create();
        }
    
    }

    public  void ManualRneder()
    {
        if (MRT_Camera != null)
        {

            CreateTex(ref M_RT[0], 0, RenderTextureFormat.ARGBHalf, "forwardTarget", RenderTextureReadWrite.Linear);
            CreateTex(ref M_RT[1], 0, RenderTextureFormat.ARGBHalf, "Albedo", RenderTextureReadWrite.Linear);
            //������Ⱥ��˵���Զ�RenderTexture����Ӳ��BiLiner,Ȼ�������ʱ�����point����
            CreateTex(ref M_RT[2], 0, RenderTextureFormat.ARGB2101010, "Normal", RenderTextureReadWrite.Linear);
            CreateTex(ref M_RT[3], 0, RenderTextureFormat.ARGBHalf, "Roughness&&Matellec", RenderTextureReadWrite.Linear);
            CreateTex(ref M_RT[4], 0, RenderTextureFormat.ARGBFloat, "Emission", RenderTextureReadWrite.Linear);
            CreateTex(ref M_RT[5], 32, RenderTextureFormat.RFloat, "Depth", RenderTextureReadWrite.Linear);
           // M_RT[5].filterMode = FilterMode.Point;
            CreateTex(ref depthRT, 32, RenderTextureFormat.R16, "Depth", RenderTextureReadWrite.Linear);
            for (int i = 0; i < M_RT.Length; i++)
            {
                M_RT[i].filterMode = FilterMode.Bilinear;
                M_Buffer[i] = M_RT[i].colorBuffer;
            }
            DepthBuffer = depthRT.depthBuffer;
            
//=======================================�ֶ���MRT����====================================================
            //��һ���������colorClear��ʵ�Ͳ���ִ����
            
            CommandBuffer Clear = new CommandBuffer();
            Clear.name = "MrtClear";
            for (int i = 0; i < 6; i++)
            {
                RenderTexture tempTex = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
                Clear.Blit(tempTex, M_RT[i]);
                RenderTexture.ReleaseTemporary(tempTex);
            }
            RenderTexture temp = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
            Clear.Blit(temp, depthRT);
            RenderTexture.ReleaseTemporary(temp);
            Graphics.ExecuteCommandBuffer(Clear);
            
//======================================================================================================
            MRT_Camera.SetTargetBuffers(M_Buffer, DepthBuffer);
            //�������ú����״̬���ֶ�Render�ȽϺÿ��ƣ�update���ֶ�render�����ִ��˳������Զ����ǰ��-��
            //graphicҲ��ѭ���˳��
            //����cs֮��ľ�û�б�Ҫ������Ⱦ����ִ����
            MRT_Camera.Render();
        }
        else
        {
            Debug.LogError("MRT_CameraΪ��");
            return;
        }

    }

    private void OnDisable()
    {
        //�ڽ�������ű���ʱ�򣬻ָ��������LayerMask�޳�
        ExecuteCamera.cullingMask = TempCullingMask;
        ExecuteCamera.clearFlags=TempClearFlags;//�ָ���������������
        Destroy(depthRT);
        for (int i = 0; i < M_RT.Length; i++)
        {
            Destroy(M_RT[i]);
        }
        GameObject.Destroy(TempObj);
        GameObject.Destroy(MRT_Camera);
    }
}
