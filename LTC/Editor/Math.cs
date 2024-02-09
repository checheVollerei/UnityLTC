using UnityEngine;
using System;


namespace LTCmath
{
    public class Float3x3
    {
       public  float[,] matrix;

        //给某个元素赋值
        public float this[int row, int column]
        {
            
            set { matrix[row, column] = value; }
            get { return matrix[row, column]; }
        }

        //构造函数
        public Float3x3()
        {
            matrix = new float[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    matrix[i, j] = 0.0f;
                }
            }
        
        }
        //有参构造函数
        public Float3x3(Vector3 m0, Vector3 m1, Vector3 m2)
        {
            matrix = new float[3, 3];
            for (int i = 0; i < 3; i++)
            {
                matrix[i ,0] = m0[i];
                matrix[i ,1] = m1[i];
                matrix[i ,2] = m2[i];
            }

        }


        //转置矩阵
        public static Float3x3 Transpose(Float3x3 m)
        {
            Float3x3 M_T = new Float3x3();

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    M_T[i, j] = m[j, i];
                }
            }
            return M_T;
        }

        //伴随矩阵   
        //=========================================
        //* 先算出余子式，然后转置
        //* 余子式： （-1）的i*j次方*（m）i行j列对应的子行列式
        //
        //=========================================
        public static Float3x3 AdjointM(Float3x3 M)
        {
            //余子式  
            Float3x3 C = new Float3x3();
            C[0, 0] = M[1, 1] * M[2, 2] - M[1, 2] * M[2, 1];
            C[0, 1] = -(M[1, 0] * M[2, 2] - M[1, 2] * M[2, 0]);
            C[0, 2] = M[1, 0] * M[2, 1] - M[1, 1] * M[2, 0];

            C[1, 0] = -(M[0, 1] * M[2, 2] - M[0, 2] * M[2, 1]);
            C[1, 1] = M[0, 0] * M[2, 2] - M[0, 2] * M[2, 0];
            C[1, 2] = -(M[0, 0] * M[2, 1] - M[0, 1] * M[2, 0]);

            C[2, 0] = M[0, 1] * M[1, 2] - M[0, 2] * M[1, 1];
            C[2, 1] = -(M[0, 0] * M[1, 2] - M[0, 2] * M[1, 0]);
            C[2, 2] = M[0, 0] * M[1, 1] - M[0, 1] * M[1, 0];
            //转置余子式
            Float3x3 adj_M = Float3x3.Transpose(C);

            return adj_M;
        }
        //3x3行列式
        public static float Determinant(Float3x3 M)
        {
            //用第一行和对应的子行列式计算3x3行列式
            float row1 = M[0, 0] * (M[1, 1] * M[2, 2] - M[1, 2] * M[2, 1]);
            float row2 = M[0, 1] * (M[1, 2] * M[2, 0] - M[1, 0] * M[2, 2]);
            float row3 = M[0, 2] * (M[1, 0] * M[2, 1] - M[1, 1] * M[2, 0]);

            float det = row1+ row2+ row3;

            return det;
        }

        //逆矩阵
        public static Float3x3 Inverse(Float3x3 M)
        {

            //伴随矩阵
            Float3x3 adjM = Float3x3.AdjointM(M);

            ////行列式
            float det = Float3x3.Determinant(M);

            //逆=伴随矩阵/行列式
            Float3x3 invM = new Float3x3();
            invM[0, 0] = adjM[0, 0] / det;
            invM[0, 1] = adjM[0, 1] / det;
            invM[0, 2] = adjM[0, 2] / det;

            invM[1, 0] = adjM[1, 0] / det;
            invM[1, 1] = adjM[1, 1] / det;
            invM[1, 2] = adjM[1, 2] / det;

            invM[2, 0] = adjM[2, 0] / det;
            invM[2, 1] = adjM[2, 1] / det;
            invM[2, 2] = adjM[2, 2] / det;

            //invM = Float3x3.Transpose(M);
            return invM;
        }

        //向量乘矩阵
        public static Vector3 Mul(Vector3 vec, Float3x3 M)
        {
            //注意C++中的矩阵是列分布
            //矩阵计算是行矩阵乘列向量（后乘）
            //
            Vector3 res = new Vector3();
            for (int i = 0; i < 3; i++)
            {
                res[i] = 0;
                for (int j = 0; j < 3; j++)
                {
                    res[i] += M[i, j] * vec[j];
                }
            }


            return res;
        }
        public static Float3x3 Mul(Float3x3 M1, Float3x3 M2)
        {
            Float3x3 res = new Float3x3();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j <3; j++)
                {

                    res[i, j] = 0;
                    for (int k = 0; k < 3; k++)
                    {
                        res[i, j] += M1[i, k] * M2[k, j];

                    }
                    
                }
            }

            return res;
        }
        public static Float3x3 Mul(float a, Float3x3 M)
        {
            Float3x3 res = new Float3x3();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    res[i, j] = a * M[i, j];
                }
            }
            return res;
        }

    }
    //这玩意封装个类纯属为了FitLTC里可以访问到
    public class BRDF
    {//GGX
        public static float Lambda(float alpha, float costheta)
        {
            float a = 1.0f / alpha / Mathf.Tan(Mathf.Acos(costheta));
            return (costheta < 1.0f) ? 0.5f * (-1.0f + Mathf.Sqrt(1.0f + 1.0f / a / a)) : 0.0f;
        }
       public static float  Eval(Vector3 V, Vector3 L, float alpha, ref float pdf)
        {
            if (V.z < 0)
            {
                pdf = 0.0f;
                return 0.0f;
            }
            float lambdaV = BRDF.Lambda(alpha, V.z);

            float G2;

            if (L.z <= 0.0f)
            {
                G2 = 0.0f;
            }
            else
            {
                float lambdaL = BRDF.Lambda(alpha, L.z);
                G2 = 1.0f / (1.0f + lambdaV + lambdaL);
            }
            Vector3 H = Vector3.Normalize(V + L);
            float slopex = H.x / H.z;
            float slopey = H.y / H.z;
            float D = 1.0f / (1.0f + (slopex * slopex + slopey * slopey) / alpha / alpha);

            D *= D;
            D /=(3.14159f * alpha * alpha * H.z * H.z * H.z * H.z);

            pdf = Mathf.Abs(D * H.z / 4.0f / Vector3.Dot(V, H));
            float res = D * G2 / 4.0f / V.z;
            return res;
        }
       public static Vector3  Sample(Vector3 V, float alpha, Vector2 random)
        {
            float phi = 2.0f * 3.14159f * random.x;
            float r = alpha * Mathf.Sqrt(random.y/(1-random.y));
            Vector3 N = Vector3.Normalize(new Vector3(r * Mathf.Cos(phi), r * Mathf.Sin(phi), 1.0f));
            Vector3 L = -V + 2.0f * N * Vector3.Dot(N, V);
            return L;
        }

    }
    public class LTC
    {
        public float amplitude;
        public float Fresnel;
        public float m11;

       //private float m11_;
       // public float m11
       // {
       //     get { return m11_; }
       //     set { m11_ = value; }
       // }


        public float m22; 
        public float m13;
        public float m23;
        public Vector3 X, Y, Z;

        public Float3x3 M;
        public Float3x3 invM;
        public float detM;



       // public float LTC.X

        public void UpdateLTC()
        {
            //注意C++中的矩阵分布是这样的
            //=========================================================
            // |  X.x ,Y.x ,Z.x  |      |  m11 ,0.0 ,m13  |
            // |  X.y ,Y.y ,Z.y  |      |  0.0 ,m22 ,0.0  |
            // |  X.z ,Y.z ,Z.z  |      |  0.0 ,0.0 ,1.0  |
            //=========================================================
            Float3x3 M1 = new Float3x3(X, Y, Z);
            Vector3 v1 = new Vector3(m11,   0 , 0);
            Vector3 v2 = new Vector3(0   , m22 , 0);
            Vector3 v3 = new Vector3(m13, 0.0f, 1.0f);
            Float3x3 M2 = new Float3x3(v1, v2, v3);
            //
            M = (Float3x3.Mul(M1, M2));
            invM = Float3x3.Inverse(M);
            detM =Mathf.Abs(Float3x3.Determinant(M));
        }
        public LTC()
        {
            amplitude = 1;
            Fresnel = 1;
            m11 = 1.0f;
            m22 = 1.0f;
            m13 = 0.0f;
            m23 = 0.0f;
            X = new Vector3(1.0f, 0, 0);
            Y = new Vector3(0, 1.0f, 0);
            Z = new Vector3(0, 0, 1.0f);
            UpdateLTC();
        }

        public float Eval(Vector3 L)
        {
            Vector3 Lorighnal = Vector3.Normalize(Float3x3.Mul( L,invM));
            Vector3 L_1 = Float3x3.Mul(Lorighnal, M);

           // float length = Mathf.Sqrt(L_1.x * L_1.x + L_1.y * L_1.y + L_1.z * L_1.z);
            float length = Vector3.Magnitude(L_1);
            float Jacobian = detM / (length * length * length);

            float D = 1.0f / 3.14159f * Mathf.Max(0.0f, Lorighnal.z);

            float res = amplitude * D / Jacobian;

            return res;
        
        }

        public Vector3 Sample(Vector2 random)
        {
            float theta = Mathf.Acos(Mathf.Sqrt(random.x));
            float phi = 2.0f * 3.14159f * random.y;
            Vector3 TempL = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Sin(theta) * Mathf.Sin(phi), Mathf.Cos(theta));
            Vector3 L = Vector3.Normalize(Float3x3.Mul(TempL, M));
            return L;
        }

    }




    public class FitLTC
    {
        public LTC fitLTC;
        public Vector3 ViewDir;
        public float alpha;
        public bool isotropic;
        private static int sampleNum;
        public FitLTC(LTC ltc, bool iso, Vector3 V, float a,int sn)
        {
            fitLTC = ltc;
            ViewDir = V;
            alpha = a;
            isotropic = iso;
            sampleNum = sn;
        }

        public void Update(Vector3 param)
        {
            float m11 = Mathf.Max(param[0], 0.00001f);
            float m22 = Mathf.Max(param[1], 0.00001f);
            float m13 = param[2];
           // float m23 = param[3];

            if (isotropic)
            {
                fitLTC.m11 = m11;
                fitLTC.m22 = m11;
                fitLTC.m13 = 0.0f;
               // fitLTC.m23 = 0.0f;
            }
            else
            {
                fitLTC.m11 = m11;
                fitLTC.m22 = m22;
                fitLTC.m13 = m13;
                //fitLTC.m23 = m23;
            
            }
            fitLTC.UpdateLTC();


        }
        public float ComputeError(LTC ltc, Vector3 V, float alpha)
        {
            double error = 0;

            Vector2 random = new Vector2(0.0f, 0.0f);

            for (int i = 0; i < sampleNum; ++i)
            {
                for (int j = 0; j < sampleNum; ++j)
                {
                    random.x = (j + 0.5f) / sampleNum;
                    random.y = (i + 0.5f) / sampleNum;

                    {
                        Vector3 L = ltc.Sample(random);

                        float pdf_brdf=0.0f;

                        float eval_brdf = BRDF.Eval(V, L, alpha, ref pdf_brdf);

                        float eval_ltc = ltc.Eval(L);

                        float pdf_ltc = eval_ltc / ltc.amplitude;

                        double error_1 = Mathf.Abs(eval_brdf - eval_ltc);

                        error_1 = error_1 * error_1 * error_1;
                        error += error_1 / (pdf_ltc + pdf_brdf);
                    }

                    {
                        Vector3 L = BRDF.Sample(V,alpha,random);

                        float pdf_brdf = 0.0f;

                        float eval_brdf = BRDF.Eval(V, L, alpha, ref pdf_brdf);

                        float eval_ltc = ltc.Eval(L);

                        float pdf_ltc = eval_ltc / ltc.amplitude;

                        double error_1 = Mathf.Abs(eval_brdf - eval_ltc);

                        error_1 = error_1 * error_1 * error_1;
                        error += error_1 / (pdf_ltc + pdf_brdf);

                    }



                }
            }


            return (float)error/(sampleNum*sampleNum);
        }
        public float Overload(Vector3 param)
        {
            Update(param);

            return ComputeError(fitLTC, ViewDir, alpha);


        }


    }
    public class LTCsum
    {
        private static Vector3 ComputeAverageDir(Vector3 V, float alpha, ref float amplitude,ref float fres, int NS)
        {
            Vector3 averageDir = new Vector3(0.0f, 0.0f, 0.0f);
            amplitude = 0.0f;
            fres = 0.0f;
            Vector2 random;
            for (int i = 0; i < NS; ++i)
                for (int j = 0; j < NS; ++j)
                {
                    random.x = (j + 0.5f) / NS;
                    random.y = (i + 0.5f) / NS;


                    Vector3 L = BRDF.Sample(V, alpha, random);

                    float pdf = 0.0f;
                    float eval = BRDF.Eval(V, L, alpha, ref pdf);
                    // averageDir += (pdf > 0) ? eval / pdf * L : new Vector3(0, 0, 0);
                    if (pdf > 0)
                    {
                        Vector3 H = Vector3.Normalize(V + L);
                        float weight = eval / pdf;

                        averageDir += weight * L;
                        amplitude += weight;
                        fres += weight * Mathf.Pow(1 - Mathf.Max(Vector3.Dot(V, H), 0.0f), 5.0f);
                    }
                }
            amplitude /= (NS * NS);
            fres /= (NS * NS);
            averageDir.y = 0.0f;
            return Vector3.Normalize(averageDir);

        }
        private static float NelderMead(ref Vector3 pmin, Vector3 start, float delta, float tolerance, int maxIters, FitLTC fitter)
        {
            //NelderMead 单纯形固有参数

            const float reflect = 1.0f;
            const float expand = 2.0f;
            const float contract = 0.5f;
            const float shrink = 0.5f;

            const int DIM = 3;
            const int pointsNum = DIM + 1;


            Vector3[] s = new Vector3[pointsNum];
            float[] f = new float[pointsNum];
            //======***move***=======
            s[0] = start;
            //======***move***=======

            for (int i = 1; i < pointsNum; i++)
            {
                s[i] = start;

                s[i][i - 1] += delta;
            }
            //求单形图上每个点的函数值
            for (int i = 0; i < pointsNum; i++)
            {
                f[i] = fitter.Overload(s[i]);
            }

            int lo = 0, hi, nh;

            for (int j = 0; j < maxIters; j++)
            {
                lo = hi = nh = 0;

                for (int i = 1; i < pointsNum; i++)
                {
                    if (f[i] < f[lo])
                        lo = i;
                    if (f[i] > f[hi])
                    {
                        nh = hi;
                        hi = i;
                    }
                    else if (f[i] > f[nh])
                        nh = i;
                }

                float a = Mathf.Abs(f[lo]);
                float b = Mathf.Abs(f[hi]);
                if (2.0f * Mathf.Abs(a - b) < (a + b) * tolerance)
                {
                    break;
                }
                Vector3 o = new Vector3(0.0f, 0.0f, 0.0f);

                for (int i = 0; i < pointsNum; i++)
                {
                    if (i == hi)
                        continue;
                    //=====***add***======
                    o = (o + s[i]);
                }
                for (int i = 0; i < DIM; i++)
                {
                    o[i] /= DIM;
                }

                Vector3 r = new Vector3();

                for (int i = 0; i < DIM; i++)
                {
                    r[i] = o[i] + reflect * (o[i] - s[hi][i]);
                }

                float fr = fitter.Overload(r);

                if (fr < f[nh])
                {
                    if (fr < f[lo])
                    {
                        Vector3 e = new Vector3();

                        for (int i = 0; i < DIM; i++)
                        {
                            e[i] = o[i] + expand * (o[i] - s[hi][i]);
                        }
                        float fe = fitter.Overload(e);
                        if (fe < fr)
                        {
                            s[hi] = e;
                            f[hi] = fe;
                            continue;
                        }
                    }

                    s[hi] = r;
                    f[hi] = fr;
                    continue;
                }
                Vector3 c = new Vector3();

                for (int i = 0; i < DIM; i++)
                {
                    c[i] = o[i] - contract * (o[i] - s[hi][i]);
                }

                float fc = fitter.Overload(c);

                if (fc < f[hi])
                {
                    s[hi] = c;
                    f[hi] = fc;
                    continue;
                }

                for (int k = 0; k < pointsNum; k++)
                {
                    if (k == lo)
                        continue;

                    for (int i = 0; i < DIM; i++)
                    {
                        s[k][i] = s[lo][i] + shrink * (s[k][i] - s[lo][i]);
                    }
                    f[k] = fitter.Overload(s[k]);

                }


            }
            pmin = s[lo];

            return f[lo];

            //return 0.0f;
        }

       private static LTC Fit(LTC ltc, Vector3 V, float alpha, float epsoilon, bool isotropic, int sn)
        {
            Vector3 startFit = new Vector3(ltc.m11, ltc.m22, ltc.m13);
            Vector3 resultFit = new Vector3(0.0f, 0.0f, 0.0f);

            FitLTC fitter = new FitLTC(ltc, isotropic, V, alpha, sn);

            float error = NelderMead(ref resultFit, startFit, epsoilon, 1e-5f, 100, fitter);


            fitter.Update(resultFit);

            return fitter.fitLTC;
        }




       private static void FitTab(ref Float3x3[] tab,ref Vector2[] tabMagFresnel,int N, int SampleNuber)
        {

            LTC ltc = new LTC();
            for (int a = N - 1; a >= 0; --a)
                for (int t = 0; t <= N - 1; ++t)
                {
                    float x = t / (float)(N - 1);

                    float ct = 1.0f - x * x;
                    float theta = Mathf.Min(1.57f, Mathf.Acos(ct));
                    //float theta = Mathf.Min(1.57f, t/(float)(N-1)*1.57079f);
                    Vector3 V = new Vector3(Mathf.Sin(theta), 0.0f, Mathf.Cos(theta));

                    float roughness = (a / (float)(N - 1));
                    float alpha = Mathf.Max((float)(roughness * roughness), 0.000001f);

                    //ltc.amplitude = ComputeNorm(V, alpha);

                    Vector3 averageDir = ComputeAverageDir(V, alpha, ref ltc.amplitude,ref ltc.Fresnel, SampleNuber);
                    bool isotropic;


                    if (t == 0)
                    {
                        ltc.X = new Vector3(1, 0, 0);
                        ltc.Y = new Vector3(0, 1, 0);
                        ltc.Z = new Vector3(0, 0, 1);
                        if (a == N - 1)
                        {
                            ltc.m11 = 1.0f;
                            ltc.m22 = 1.0f;
                        }
                        else
                        {
                            ltc.m11 = tab[a + 1 + t * N][0, 0];
                            ltc.m22 = tab[a + 1 + t * N][1, 1];
                        }

                        ltc.m13 = 0.0f;
                        ltc.m23 = 0.0f;
                        ltc.UpdateLTC();
                        isotropic = true;
                    }
                    else
                    {
                        Vector3 L = averageDir;
                        Vector3 T1 = new Vector3(L.z, 0, -L.x);
                        Vector3 T2 = new Vector3(0, 1.0f, 0);
                        ltc.X = T1;
                        ltc.Y = T2;
                        ltc.Z = L;
                        ltc.UpdateLTC();

                        isotropic = false;
                    }
                    const float epsilon = 0.05f;
                    LTC templtc = Fit(ltc, V, alpha, epsilon, isotropic, SampleNuber);
                    tab[a + t * N] = templtc.M;
                    //这两项不需要插值
                    tabMagFresnel[a + t * N][0] = ltc.amplitude;
                    tabMagFresnel[a + t * N][1] = ltc.Fresnel;
                    //tab[a + t * N][0, 0] = ltc.X[0];
                    //tab[a + t * N][0, 2] = ltc.X[2];
                    tab[a + t * N][0, 1] = 0;
                    tab[a + t * N][1, 0] = 0;
                    tab[a + t * N][2, 1] = 0;
                    tab[a + t * N][1, 2] = 0;

                    //tab[a+t*N] = Float3x3.Mul((float)(1.0f / tab[a + t * N][2, 2]), tab[a + t * N]);
                }
        }
        //这是一个写入C#的函数
        //保存成C#可以直接编译，需要用的时候直接获取就行，
        private static void SaveFloat3X3(string pix,string mf, int texSize)
        {
            string code =
                "using System;\n" +
                "\n" +
                "namespace LTCmath\n" +
                "{\n" +
                "public class MeshLightLUT\n" +
                "{\n" +
                "       public static int TextureSize   = " + texSize.ToString()+";\n"+
                "       public static int PixvelsNumber  = "+texSize.ToString() + "*" + texSize.ToString() +";\n"+
                "       public static float[,] Specular = new float[" + texSize.ToString() + "*" + texSize.ToString() + ",4]\n" +
                "       {\n" +
                        pix +
                "       };\n"+
                "       public static float[,] MagFresnel = new float[" + texSize.ToString() + "*" + texSize.ToString() + ",4]\n" +
                "       {\n" +
                        mf +
                "       };\n" +
                "}\n" +
                "}";
            var path = Application.dataPath + "/LTC/Script/ltcPixelValue.cs";
            System.IO.File.WriteAllText(path, code);
        }
       public static void MainFunction(int TextureSize)
        {
            Float3x3[] tab = new Float3x3[TextureSize * TextureSize];
            Vector2[] tabMagFresnel = new Vector2[TextureSize * TextureSize];

            for (int i = 0; i < TextureSize * TextureSize; i++)
            {
                tab[i] = new Float3x3();
                tabMagFresnel[i] = new Vector2(0.0f, 0.0f);
            }

            LTCsum.FitTab(ref tab, ref tabMagFresnel, TextureSize, TextureSize / 2);

            Float3x3 minv;
            string tabToString = "";
            string mfToString = "";
            for (int i = 0; i < TextureSize * TextureSize; i++)
            {
                minv = Float3x3.Inverse(tab[i]);

                //不想写矩阵除法了，开摆（摔！
                minv = Float3x3.Mul(1 / minv[1, 1], minv);
                //被列矩阵的分布和索引搞蒙了，我也不清楚这里为啥要转置一下，反正转置一下才是正确结果
                //嘛,过程不重要，啊哈哈哈哈
                // 
                minv = Float3x3.Transpose(minv);
                //写入到C#文本文件
                if (i < TextureSize * TextureSize - 1)
                {
                    tabToString += "{" + minv[0, 0].ToString() + "f" + ","
                        + minv[0, 2].ToString() + "f" + ","
                        + minv[2, 0].ToString() + "f" + ","
                        + minv[2, 2].ToString() + "f" + "},\n";
                    //X存的是Magnitude，Y分量存的是Fresnel,Z是立体角（？）
                    //我没算 直接在shader里做cosin积分，W凑个数
                    mfToString += "{" + tabMagFresnel[i][0].ToString() + "f" + ","
                        + tabMagFresnel[i][1].ToString() + "f" + ","
                        + "1.0" + "f" + ","
                        + "1.0" + "f" + "},\n";


                }
                else
                {
                    tabToString += "{" + minv[0, 0].ToString() + "f" + ","
                        + minv[0, 2].ToString() + "f" + ","
                        + minv[2, 0].ToString() + "f" + ","
                        + minv[2, 2].ToString() + "f" + "}\n";

                    mfToString += "{" + tabMagFresnel[i][0].ToString() + "f" + ","
                        + tabMagFresnel[i][1].ToString() + "f" + ","
                        + "1.0" + "f" + ","
                        + "1.0" + "f" + "}\n";
                }
            }
            //写入到C#文本文件
            SaveFloat3X3(tabToString, mfToString, TextureSize);
            //ltcTex.SetPixels(col);
            //ltcTex.Apply(false);
        }
    }
}