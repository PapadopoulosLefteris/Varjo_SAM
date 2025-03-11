using UnityEngine;
using Doji.AI.Segmentation;
using UnityEngine.UI;
using Unity.Sentis;


public class SegmentationExample : MonoBehaviour
{
    private MobileSAM _mobileSAMPredictor;
    public Texture TestImage;
    public RenderTexture Result;
    public RawImage outputRawImage;
    public int x;
    public int y;
    private void Start()
    {
        // Initialize the MobileSAM predictor
        _mobileSAMPredictor = new MobileSAM();

        _mobileSAMPredictor.Backend = BackendType.GPUCompute;

        Tensor<float> inputTensor = TextureConverter.ToTensor(TestImage);
        inputTensor.Reshape(new TensorShape(3, 1024, 1024));
        var origSize = (1024, 1024);
        // Set the image for segmentations
        //_mobileSAMPredictor.SetImage(TestImage);
        _mobileSAMPredictor.SetImage(inputTensor, origSize);
        x = 600;
        y = 500;

        Result = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true
        };
        Result.Create();

        //outputRawImage.texture = Result;
        // Perform segmentation
    }

    private void Update()
    {
        DoSegmentation();
        if (Input.GetKeyDown(KeyCode.R))
        {
            outputRawImage.texture = TestImage;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            outputRawImage.texture = Result;
        }

    }
    private void DoSegmentation()
    {
        if (TestImage == null)
        {
            Debug.LogError("TestImage is null. Assign a texture to TestImage.");
            return;
        }
        
        // Example prompt: a single point in the middle of the image
        float[] pointCoords = new float[] {x, y};
        float[] pointLabels = new float[] { 1f };  // 1 for foreground point

        // Perform segmentation
        _mobileSAMPredictor.Predict(pointCoords, pointLabels);

        // Retrieve the result
        Result = _mobileSAMPredictor.Result;

        // Use the result texture (e.g., display on UI, apply to a material, etc.)
    }

    private void OnDestroy()
    {
        // Clean up resources
        _mobileSAMPredictor.Dispose();
    }
}