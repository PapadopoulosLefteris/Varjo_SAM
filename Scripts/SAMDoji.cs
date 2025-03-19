using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Doji.AI.Segmentation;
using Unity.Sentis;
using UnityEngine.UI;
using System.Diagnostics;  // For System.Diagnostics.Stopwatch

public class SAMDoji : MonoBehaviour
{
    public MobileSAM mobileSAMPredictor;
    
    private Texture2D tex;
    bool isprocessing;
   
  
    Tensor inputTensor;
    



    void Start()
    {
        
        isprocessing = false;
      
        // Initialize MobileSAM
        mobileSAMPredictor = new MobileSAM();
        mobileSAMPredictor.Backend = BackendType.GPUCompute;

  
        
    }

    public void Seg(RenderTexture inputTexture, float[] pointCoords, float[] pointLabels )
    {
        if (inputTensor != null)
        {
            inputTensor.Dispose();
            inputTensor = null;
        }
        // Timing step 1: Reading pixels from RenderTexture
        RenderTexture.active = inputTexture;
        tex = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, inputTexture.width, inputTexture.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        inputTensor = TextureConverter.ToTensor(tex);
        inputTensor.Reshape(new TensorShape(3, 1024,1024));
        Destroy(tex);
        var origSize = (1024, 1024);

        // Timing step 2: Initializing MobileSAM and setting backend
        if (!isprocessing)
        {
            isprocessing = true;
            mobileSAMPredictor.SetImage(inputTensor, origSize);
            if (!mobileSAMPredictor.m_Started) { 
                mobileSAMPredictor.Predict(pointCoords, pointLabels); 
            }
            
          
            
            isprocessing = false;
        }

    }

    
    private void OnDestroy()
    {
        mobileSAMPredictor.Dispose();
        inputTensor.Dispose();
    }
}
