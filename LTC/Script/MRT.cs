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
    //这里存放主相机的CullingMask，执行这个脚本需要剔除掉，避免重复渲染，不执行了就恢复
    private static LayerMask TempCullingMask;
    private static CameraClearFlags TempClearFlags;
    //这里存放渲染MRT时候的CullingMask,对于UI之类的，不执行MRT，还是由主相机绘制
    public LayerMask MrtCameraCullingMask=1;//默认Default
    //private RenderTargetIdentifier[] RTID = new RenderTargetIdentifier[2];
    
    public static RenderTexture depthRT;
    private static RenderBuffer DepthBuffer;
    public static RenderTexture[] M_RT = new RenderTexture[6];
    private static RenderBuffer[] M_Buffer = new RenderBuffer[6];
    //==============================
    //一张原始输出图像 用来与LTC的结果合成 ：前向和延迟的合成也需要(比如，ClusterLight)
    //一张基础色，做光照计算需要带入albedo 
    //一张normal
    //一张roughness&&matellec
    //Emission，对于光照模型进行bloom
    //Depth，来进行坐标重构，不直接从depthbuffer获取，而是直接输出需要进行多边形光照的物体的depth
    //毕竟meshLight也是可以投射阴影的，对吧，
    //==============================

    //================================
    //输出完成后，对于主相机和MRT相机计算完成的结果进行混合
    //================================
    private void OnEnable()
    {
        BlendMat = new(Resources.Load("BlendTex") as Shader);
        ExecuteCamera = this.GetComponent<Camera>();
        TempCullingMask = ExecuteCamera.cullingMask;//保存主相机的layerMask
        TempClearFlags=ExecuteCamera.clearFlags;
        //减去一层，，，，，WTF ？？？？？还能这样写？
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
            TempObj.transform.parent = this.transform;//将物体放到当前脚本的子集
            MRT_Camera = TempObj.AddComponent<Camera>();
            MRT_Camera.name = "DrawAreaLight";
        }

         MRT_Camera.CopyFrom(ExecuteCamera);
         MRT_Camera.backgroundColor = Color.black;
         //对于CameraClearFlags
         //Skybox：进行Target0和depth进行清除,所以如果进行skyBoxClear,需要手动对MRT进行清除
         //
         //
         MRT_Camera.clearFlags = CameraClearFlags.Skybox;
         MRT_Camera.depth = -10;//无所谓了，反正禁用掉手动Render了
         MRT_Camera.cullingMask = MrtCameraCullingMask;
         MRT_Camera.enabled = false;//关掉相机组件，进行手动Render

        //给主相机的layerMask设置为nothing
        //ExecuteCamera.cullingMask = 0;//注意这个要在copyCamera后
        //减去多层就可以这样
        ExecuteCamera.cullingMask = TempCullingMask - MrtCameraCullingMask;
        //关闭相机的背景清除
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
            //这里有群友说可以对RenderTexture进行硬件BiLiner,然后采样的时候进行point采样
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
            
//=======================================手动对MRT清屏====================================================
            //这一步如果用了colorClear其实就不用执行了
            
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
            //这里设置好相机状态后手动Render比较好控制（update中手动render的相机执行顺序必在自动相机前面-）
            //graphic也遵循这个顺序
            //后续cs之类的就没有必要插入渲染管线执行了
            MRT_Camera.Render();
        }
        else
        {
            Debug.LogError("MRT_Camera为空");
            return;
        }

    }

    private void OnDisable()
    {
        //在禁用这个脚本的时候，恢复对主相机LayerMask剔除
        ExecuteCamera.cullingMask = TempCullingMask;
        ExecuteCamera.clearFlags=TempClearFlags;//恢复主相机的清除设置
        Destroy(depthRT);
        for (int i = 0; i < M_RT.Length; i++)
        {
            Destroy(M_RT[i]);
        }
        GameObject.Destroy(TempObj);
        GameObject.Destroy(MRT_Camera);
    }
}
