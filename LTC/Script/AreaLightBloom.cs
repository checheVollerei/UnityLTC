using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEditor;

//[CustomEditor(typeof(LTCmaster))]


public enum BloomLayerDebug
{
    Reslut = 0,
    DownSampler = 1,
    UpSampler = 2,
    LuminClamp=3
}
//[ExecuteAlways]

//[ExecuteInEditMode]
public class AreaLightBloom : MonoBehaviour
{
    private Material mat;


    [Range(2, 9)]
    public int downSampleStep = 9;


    [Range(0.0f, 4.0f)]
    public float luminClamp = 1.0f;
    [Range(0.0f, 0.1f)]
    public float lumin= 0.02f;

    public BloomLayerDebug BloomLayer= BloomLayerDebug.Reslut;
    [Range(0, 9)]
    public int downSampler = 1;

    public void PostImage_Bloom(RenderTexture sou,ref RenderTexture dest)
    {
       Shader.SetGlobalFloat("_luminanceThreshole", luminClamp);
        if (mat != null)
        {
            //  mat.SetFloat("_luminanceThreshole", luminClamp);
            // 高亮像素筛选
            RenderTexture RT_threshold = RenderTexture.GetTemporary(sou.width, sou.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            RT_threshold.filterMode = FilterMode.Bilinear;
            Graphics.Blit(sou, RT_threshold, mat, 3);





            RenderTexture[] DownRT = new RenderTexture[downSampleStep];
            int downSize = 2;
            //mipingmap
            for (int i = 0; i < DownRT.Length; i++)
            {
                int rtw = sou.width / downSize;
                int rth = sou.height / downSize;

                DownRT[i] = RenderTexture.GetTemporary(rtw, rth, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                DownRT[i].filterMode = FilterMode.Bilinear;

                downSize *= 2;
            }
            //降采样，用高斯模糊降采样的结果会更稳定（平均），不会发生严重的跳变
            Graphics.Blit(RT_threshold, DownRT[0], mat, 0);
            //DownRT[1]-->DownRT[max];
            for (int i = 1; i < DownRT.Length; i++)
            {
                Graphics.Blit(DownRT[i-1], DownRT[i], mat, 0);
            }
            RenderTexture[] upRT = new RenderTexture[downSampleStep-1];

            for (int i = 0; i < upRT.Length; i++)
            {
                int rtw = DownRT[DownRT.Length - 2 - i].width;
                int rth= DownRT[DownRT.Length - 2 - i].height;
                upRT[i] = RenderTexture.GetTemporary(rtw, rth, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                upRT[i].filterMode = FilterMode.Bilinear;
            }
            //===================================
            //DownRT[max]-->混合源
            //DownRT[max-1]-->upRT[0]
            //DownRT[max-2]-->upRT[1]
            //DownRT[max-3]-->upRT[2]
            //===================================
            //DownRT[max]
            Shader.SetGlobalTexture("_PrevMip", DownRT[downSampleStep - 1]);
            //DownRT[max-1]
            Graphics.Blit(DownRT[downSampleStep -2], upRT[0], mat,1);

            for (int i = 1; i < DownRT.Length - 1; i++)
            {
               //RenderTexture prevTex = upRT[i - 1];
               //RenderTexture CurrTex = DownRT[DownRT.Length - 2 - i];
                Shader.SetGlobalTexture("_PrevMip", upRT[i - 1]);

                Graphics.Blit(DownRT[DownRT.Length - 2 - i], upRT[i], mat,1);
            }

            if (BloomLayer == BloomLayerDebug.Reslut)
            {
                Shader.SetGlobalTexture("_BloomTex", upRT[upRT.Length - 1]);
                Shader.SetGlobalFloat("_Lumin", lumin);
                Graphics.Blit(sou, dest, mat, 2);
            }
            else if (BloomLayer == BloomLayerDebug.DownSampler)
            {
                int arrayIndex = downSampler > DownRT.Length - 1 ? DownRT.Length - 1 : downSampler;
                Graphics.Blit(DownRT[arrayIndex], dest);
            }
            else if (BloomLayer == BloomLayerDebug.UpSampler)
            {
                int arrayIndex = downSampler > upRT.Length - 1 ? upRT.Length - 1 : downSampler;
                Graphics.Blit(upRT[arrayIndex], dest);
            }
            else if (BloomLayer == BloomLayerDebug.LuminClamp)
            {
                Graphics.Blit(RT_threshold, dest);
            }

            for (int i = 0; i < DownRT.Length; i++)
            {
                RenderTexture.ReleaseTemporary(DownRT[i]);
                //int idx = i > upRT.Length-1 ? upRT.Length-1:i;
                //
                //
                //RenderTexture.ReleaseTemporary(upRT[idx]);
            }
            for (int i = 0; i < upRT.Length; i++)
            {
                RenderTexture.ReleaseTemporary(upRT[i]);
            }

            RenderTexture.ReleaseTemporary(RT_threshold);
            /*
            int rtw = sou.width / downSampler;
            int rth = sou.height / downSampler;

            RenderTexture TempTex_1 = RenderTexture.GetTemporary(rtw, rth, 0);
            TempTex_1.filterMode = FilterMode.Bilinear;
            RenderTexture TempTex_2 = RenderTexture.GetTemporary(rtw, rth, 0);
            TempTex_2.filterMode = FilterMode.Bilinear;
            Graphics.Blit(sou, TempTex_1, mat,0);
            for (int i = 0; i < Iterations; i++)
            {
                mat.SetFloat("_BlurSize", 1.0f + i * blurSize);
                
                Graphics.Blit(TempTex_1, TempTex_2, mat, 1);
                Graphics.Blit(TempTex_2, TempTex_1, mat, 2);
            }
            mat.SetFloat("_LuminanceThreshold", luminanecThreshold);
            mat.SetTexture("_BloomTex", TempTex_1);
            Graphics.Blit(sou, dest, mat, 3);
            RenderTexture.ReleaseTemporary(TempTex_1);
            RenderTexture.ReleaseTemporary(TempTex_2);*/
        }
        else
        {
            Graphics.Blit(sou, dest);
        }
        //base.PostImage_a(sou, dest);
    }

    private void OnEnable()
    {
        mat = new Material(Resources.Load("BlurSample") as Shader);
    }
    private void Start()
    {
        
    }
    private void OnDisable()
    {
        if (mat != null) Material.Destroy(mat);
    }
}
