using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using UnityEngine.UI;
using Unity.Sentis.Layers;


public class Segment : MonoBehaviour
{
    public RenderTexture inputTexture;
    public ModelAsset modelAsset;
    public ModelAsset preprocessor;
    public RenderTexture segmented;
    public RawImage output_image;

    private TensorShape shape;
    public Model runtimeModel;
    public Model preprocessorModel;

    public Worker preprocessing_worker;
    public Worker worker;

    Tensor<float> inputTensor;
    Tensor<float> finalTensor;
    Tensor<float> image_embeddings;
    Tensor<float> high_res_features1;
    Tensor<float> high_res_features2;
    Tensor<int> orig_im_size;
    Tensor<float> mask_input;
    Tensor<float> has_mask_input;
    Tensor<float> coordinateTensor;
    Tensor<float> point_labels;

    int[] im_size;
    float[] point_label;
    TextureTransform out_transform;
    TextureTransform in_transform;
    bool isinferencePending;
    bool ispreprocessingPending;


    // Start is called before the first frame update
    void Start()
    {
        isinferencePending = false;
        ispreprocessingPending = false; 

        segmented = new RenderTexture(1024, 1024, 0, RenderTextureFormat.R8)
        {
            enableRandomWrite = true
        };
        segmented.Create();


        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, BackendType.GPUCompute);



        preprocessorModel = ModelLoader.Load(preprocessor);
        preprocessing_worker = new Worker(preprocessorModel, BackendType.GPUCompute);


        

        point_label = new float[] { 1.0f };
        im_size = new int[] { 1024, 1024 };


        image_embeddings = new Tensor<float>(new TensorShape(1, 256, 64, 64));
        point_labels = new Tensor<float>(new TensorShape(1), point_label);
        orig_im_size = new Tensor<int>(new TensorShape(2), im_size);
        mask_input = new Tensor<float>(new TensorShape(1, 1, 256, 256));
        has_mask_input = new Tensor<float>(new TensorShape(1));





        out_transform = new TextureTransform().SetDimensions(channels: 1);
        in_transform = new TextureTransform().SetDimensions(channels: 3);

        output_image.texture = segmented;

    }

    void Update()
    {
        inputTensor = TextureConverter.ToTensor(inputTexture, in_transform);
        Debug.Log(inputTensor);


        float startTime = Time.realtimeSinceStartup;

        Preprocess(inputTensor);
        //if (!ispreprocessingPending) {
        //    Preprocess(inputTensor);
        //    ispreprocessingPending = true;
        //}
        Debug.Break();
        //else if (ispreprocessingPending && image_embeddings.IsReadbackRequestDone())
        //{ RetrievePreprocessed();
        //  ispreprocessingPending= false;
        
        //}
        float endTime = Time.realtimeSinceStartup;
        //Debug.Log("Preprocessing time: " + (endTime - startTime) + " seconds, Pending: " + ispreprocessingPending);

        //startTime = Time.realtimeSinceStartup;
        //if (!isinferencePending) {
        //    Infer();
        //}
        //else if (isinferencePending && finalTensor.IsReadbackRequestDone())
        //{
        //    RetrieveInference();
        //}
        //endTime = Time.realtimeSinceStartup;
        //Debug.Log("Inference time: " + (endTime - startTime) + " seconds");




    }

    async void Preprocess(Tensor<float> inputTensor)
    {
        preprocessing_worker.Schedule(inputTensor);
        ispreprocessingPending = true;
    }
    async void RetrievePreprocessed()
    {
        image_embeddings = preprocessing_worker.PeekOutput("image_embeddings") as Tensor<float>;
        high_res_features1 = preprocessing_worker.PeekOutput("high_res_features1") as Tensor<float>;
        high_res_features2 = preprocessing_worker.PeekOutput("high_res_features2") as Tensor<float>;
    }
    void Infer()
    {
           
             
        float[] coordinateArray = new float[] { 512.0f, 512.0f }; // (x, y) coordinates
        coordinateTensor = new Tensor<float>(new TensorShape(1, 1, 2), coordinateArray);


        
        

        worker.SetInput("image_embeddings", image_embeddings);
        worker.SetInput("high_res_features1", high_res_features1);
        worker.SetInput("high_res_features2", high_res_features2);
        worker.SetInput("point_coords", coordinateTensor);
        worker.SetInput("point_labels", point_labels);
        worker.SetInput("mask_input", mask_input);
        worker.SetInput("has_mask_input", has_mask_input);
        worker.SetInput("orig_im_size", orig_im_size);
        worker.Schedule();



    }


    void RetrieveInference()
    {
        finalTensor = worker.PeekOutput("masks") as Tensor<float>;
        segmented = TextureConverter.ToTexture(finalTensor, out_transform);
    }

}
