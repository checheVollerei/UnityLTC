using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum TextureStatus
{ 
    Static=0,
    Active=1

}


//强制添加必要组件
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
//[ExecuteInEditMode]
//[ExecuteAlways]
//[ExecuteAlways, ImageEffectAllowedInSceneView]
public class AreaLighting : MonoBehaviour
{
    public RenderTexture[] BlurTextureArray;
    public Texture[] LightingTexture;
    public Material BlurMat;

    public enum LightingState
    {
        solidColor,
        staticTexture,
        dynamicTexture
    }
    public LightingState[] ls=new LightingState[2];
    public Material[] GetLightMaterials()
    {
        Material[] mats =this.GetComponent<Renderer>().sharedMaterials;

        return mats;
    }
    //这里做个实时Blur，预Blur需要写Editor来检测Material面板
    public void GetLightTexture(Material[] mats)
    {
        //if (mat.HasTexture("_MainTex"))
        //{
        //Material[] mats = this.GetComponent<Renderer>().sharedMaterials;
        //输出数组，它的第一维是subMesh Material，第二维是blur后的各阶map
        if (LightingTexture.Length != mats.Length)
        {
            LightingTexture = new Texture[mats.Length];
        }
        
        int validLength=0;
        for (int i = 0; i < mats.Length; i++)
        {
            // RenderTexture.ReleaseTemporary
            if (mats[i].HasProperty("_MainTex"))
            {
                LightingTexture[i] = mats[i].GetTexture("_MainTex");
                validLength++;
            }
            else
            {
                continue;
            }
            
        }
        if (validLength > 0)//这个物体有最少一张贴图的情况下
        {
            if (BlurTextureArray == null || BlurTextureArray.Length != validLength)
            {
                BlurTextureArray = new RenderTexture[validLength];
            }
        }

        for (int i = 0; i < LightingTexture.Length; i++)
        {
            if (LightingTexture[i] != null)
            {
                if (BlurTextureArray[i] == null || BlurTextureArray[i].width != LightingTexture[i].width || BlurTextureArray[i].height != LightingTexture[i].height)
                {
                    if (BlurTextureArray[i] != null)
                    {
                        BlurTextureArray[i].Release();
                    }
                    BlurTextureArray[i] = new RenderTexture(LightingTexture[i].width, LightingTexture[i].height, 0);
                    BlurTextureArray[i].volumeDepth = 9;
                    BlurTextureArray[i].dimension = TextureDimension.Tex2DArray;
                    BlurTextureArray[i].filterMode = FilterMode.Bilinear;
                    BlurTextureArray[i].Create();
                }
                //当这个索引有信息的时候，进行blur
                BlurTexture(LightingTexture[i], ref BlurTextureArray[i]);
            }
            else
            {
                continue;
            }
        }
        
       // return getRT;
    }

    private void BlurTexture(Texture tex,ref RenderTexture outTex)
    {
        CommandBuffer BlurBuffer = new CommandBuffer();
        BlurBuffer.name = "BlurTexture";
        RenderTexture[] DownRT = new RenderTexture[9];
        int texSize=2;
        for (int i = 0; i < DownRT.Length; i++)
        {
            int rtw = tex.width / texSize;
            int rth = tex.height / texSize;
            DownRT[i] = RenderTexture.GetTemporary(rtw, rth, 0);
            DownRT[i].filterMode = FilterMode.Bilinear;
            texSize *= 2;
        }
        BlurBuffer.Blit(tex, DownRT[0], BlurMat, 0);
        for (int i = 1; i < DownRT.Length; i++)
        {
            BlurBuffer.Blit(DownRT[i - 1], DownRT[i], BlurMat, 0);

        }
       
        for (int i = 0; i < DownRT.Length; i++)
        {
            //差一次上采样
            BlurBuffer.Blit(DownRT[i], outTex, BlurMat, 0,i);
            RenderTexture.ReleaseTemporary(DownRT[i]);
        }

        Graphics.ExecuteCommandBuffer(BlurBuffer);
        BlurBuffer.Release();

    }


    private void OnEnable()
    {
        int subMeshCount = this.GetComponent<MeshFilter>().sharedMesh.subMeshCount;
        LightingTexture=new Texture[subMeshCount];

        BlurMat = new Material(Resources.Load("BlurSample")as Shader);
        //暂时关掉，后期需要把这里单独分个相机进行渲染
        //this.GetComponent<MeshRenderer>().enabled = false;
        LTCmaster.RegisterObject(this);
        //给物体换个layer,避免主相机的mrt渲染到信息，导致出错
       // this.gameObject.layer = LayerMask.NameToLayer("AreaLight");//已经改为获取shader的输出信息
    }
    private void OnDisable()
    {



        LTCmaster.UnregisterObject(this);
        Material.Destroy(BlurMat);
        //Texture是本地资产的引用，这个不能销毁
               // Destroy(tempTex_2[i]);

        for (int i = 0; i < BlurTextureArray.Length; i++)
        {
            Destroy(BlurTextureArray[i]);
        }
    }

    void Start()
    {
        //if (textest != null)
        //{
        //    BlurTextureArray = new RenderTexture(textest.width, textest.height, 0);
        //    BlurTextureArray.filterMode = FilterMode.Bilinear;
        //    BlurTextureArray.dimension = TextureDimension.Tex2DArray;
        //    BlurTextureArray.useMipMap = true;
        //    BlurTextureArray.volumeDepth = 9;
        //}
        //Texture[] tempTex = GetLightTexture();
        
        //if (tempTex.Length>0)
        //{
        //     tempTex_2 = new RenderTexture[tempTex.Length];
        //    for (int i = 0; i < tempTex.Length; i++)
        //    {
        //        tempTex_2[i] = new RenderTexture(tempTex[i].width, tempTex[i].height, 0);
        //        tempTex_2[i].filterMode = FilterMode.Bilinear;
        //        tempTex_2[i].dimension = TextureDimension.Tex2DArray;
        //        tempTex_2[i].useMipMap = true;
        //        tempTex_2[i].volumeDepth = 9;
        //        BlurTexture(tempTex[i], ref tempTex_2[i]);
        //    }
        //    
        //}


    }

    private void Update()
    {

        
        //if (this.GetComponent<Renderer>().sharedMaterial.HasFloat("_EmissiveIntensity"))
        //{
        //    test = this.GetComponent<Renderer>().sharedMaterial.GetFloat("_EmissiveIntensity");
        //}
    }

}
