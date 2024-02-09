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
    [MenuItem("LTC�ű�/LTC���ؾ���������,", false,2)]
    private static void ShowInWindow()
    {

        var window = GetWindow<LtcEditor>();
        window.Show();
    }


    private TextureSize size=TextureSize._32;

    private string introduce =  "*  ��ȷ�����Asset�ļ�����������·����\n" +
                                "      Asset ==> LTC ==> Script\n" +
                                "*   ע�⣡M����ļ�����̷ǳ���ʱ����ͼ����С��";

    private void OnGUI()
    {
        GUILayout.Label("���");
        GUILayout.TextArea(introduce);
        size=(TextureSize)EditorGUILayout.EnumPopup("��Ҫ����LUTͼ��Ĵ�С",size);
        
        if (GUILayout.Button("����BRDF�������"))
        {
            LTCsum.MainFunction((int)size);
        }
    }



    public float texSize = 10.0f;
    public int samplerNumber = 20;

}
