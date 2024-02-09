using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatGameObjectMatrix : MonoBehaviour
{
    // Start is called before the first frame update
    public  GameObject obj;
    public int MatrixSize=5;
    //public float 
    public float _Distance =1.5f;
    GameObject[] objMatrix;

   // Material mat;

    private void OnEnable()
    {
        //obj = this.gameObject;
        Vector3 inter = new Vector3(_Distance , 0, _Distance );
        objMatrix = new GameObject[MatrixSize * MatrixSize];
        this.transform.position = obj.transform.position;
        //Mesh mf = this.GetComponent<MeshFilter>().mesh;
        int index = 0;
        for (int i = MatrixSize-1; i >=0; i--)
        {
            for (int j = 0; j <=MatrixSize-1; j ++)
            {
                index += 2;
                //objMatrix[index] = GameObject.CreatePrimitive(PrimitiveType.);
                objMatrix[i+j* MatrixSize] = Instantiate(obj);
                //objMatrix[index].AddComponent<Renderer>() = this.GetComponent<Renderer>();
                objMatrix[i + j * MatrixSize].transform.parent = this.transform;
                objMatrix[i + j * MatrixSize].transform.position = obj.transform.position + new Vector3(j-MatrixSize/2, 0,i - MatrixSize/2) * _Distance ;
                //mat = objMatrix[i + j * MatrixSize].GetComponent<MeshRenderer>().material;
                //mat.SetFloat("_Roughness", (i + 0.0f) / (MatrixSize - 1.0f));
                //mat = objMatrix[i + j * MatrixSize].GetComponent<MeshRenderer>().material;
                //mat.SetFloat("_Metallic", (j + 0.0f) / (MatrixSize - 1.0f));
                objMatrix[i + j * MatrixSize].GetComponent<MeshRenderer>().material.SetFloat("_Roughness", (i + 0.0f) / (MatrixSize - 1.0f));
                objMatrix[i + j * MatrixSize].GetComponent<MeshRenderer>().material.SetFloat("_Metallic", (j + 0.0f) / (MatrixSize - 1.0f));

            }
        }
        obj.GetComponent<Renderer>().enabled = false;
    }
    private void OnDisable()
    {
        for (int i = 0; i < objMatrix.Length; i++)
        {
            GameObject.Destroy(objMatrix[i]);
        }
       // obj.GetComponent<Renderer>().enabled = true;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
