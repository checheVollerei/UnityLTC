using UnityEditor;
using UnityEngine;
using LTCmath;


public class LtcEditor : EditorWindow
{
    // private on
    public enum TextureSize
    {
        _16 = 16,
        _32 = 32,
        _64 = 64,
        _128 = 128,
        _256 = 256,
        _512 = 512,
        //_1024 = 1024
    }
    [MenuItem("LTC脚本/LTC像素矩阵生成器,", false,2)]
    private static void ShowInWindow()
    {

        var window = GetWindow<LtcEditor>();
        window.Show();
    }


    private TextureSize size=TextureSize._32;

    private string introduce =  "*  请确保你的Asset文件夹下有以下路径：\n" +
                                "      Asset ==> LTC ==> Script\n" +
                                "*   注意！M矩阵的计算过程非常耗时！贴图尽量小点";

    private void OnGUI()
    {
        GUILayout.Label("简介");
        GUILayout.TextArea(introduce);
        size=(TextureSize)EditorGUILayout.EnumPopup("需要生成LUT图像的大小",size);
        
        if (GUILayout.Button("生成BRDF拟合数据"))
        {
            LTCsum.MainFunction((int)size);
        }
    }



    public float texSize = 10.0f;
    public int samplerNumber = 20;

}
