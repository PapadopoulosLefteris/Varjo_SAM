using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using YamlDotNet.Serialization;
using System.IO;
using System.Collections.Generic;
using System;
using System.Numerics;
using static PhospheneRenderer;


public class PhospheneRenderer : MonoBehaviour
{

    //Objects
    public EyeTrackingCapture EyeTacker;


    //Struct variables
    private Phosphene[] phosphenes; // Your phosphene data structure
    private Phosphene[] phosphenes_cortex;


    //Render Textures and Raw Image output
    public RenderTexture phospheneRenderTexture;
    public RenderTexture inputTexture;
    public RenderTexture edgeTexture;
    public RawImage outputRawImage;

    //Render Texture properties
    public int textureWidth = 512;
    public int textureHeight = 512;

    //Materials
    private Material phospheneMaterial;
    private Material edgeDetectionMaterial;

    //Compute Buffers
    private ComputeBuffer positionsBuffer;
    private ComputeBuffer sigmaBuffer;
    private ComputeBuffer rfBuffer;
    private ComputeBuffer indexArrayBuffer;
    private ComputeBuffer debugBuffer;
    private ComputeBuffer newbuff;
    private ComputeBuffer newsig;


    //Shaders
    public Shader phospheneShader; //Phosphene Renderer
    public Shader edgeDetectionShader;
    public ComputeShader phospheneComputeShader; //Sampler

    //Simulation properties (Might move to YAML);
    public static float a = 0.75f;
    public static float b = 120.0f;
    public static float k = 17.3f;
    public static float amp = 150e-6f;
    public static float fov = 180.0f;
    public static float radius_to_sigma = 0.5f;
    public static float current_spread = 675e-6f;
    public static float rf_size = 0.5f;
    public static float dropout_probability = 0.9f;
    public int phospheneCount;
    public string yamlFilePath = "C:\\Users\\Administrator\\Desktop\\phosphene_schemes\\grid_coords_squares_utah.yaml";

    public float offsetx = 0;
    public float offsety = 0;    

    // The class that represents the YAML structure
    public class PhosphenePositions
    {
        public List<float> x { get; set; }
        public List<float> y { get; set; }
    }

    struct Phosphene
    {
        public UnityEngine.Vector2 position;
        public UnityEngine.Vector2 position_texture;
        public float sigma;
        public float rf;

    }



    void Start()
    {
        InitializeRenderTexture();
        InitializePhosphenes();
        InitializeMaterial();

        outputRawImage.texture = phospheneRenderTexture;

        //For testing Edge Detection
        //outputRawImage.texture = edgeTexture;

    }


    void Update()
    {
        GazeOffset();
        ApplyEdgeDetection();
        RenderPhosphenes();
        // Check if the "F" key is pressed
        if (Input.GetKeyDown(KeyCode.F))
        {
            outputRawImage.texture = phospheneRenderTexture;

        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            outputRawImage.texture = edgeTexture;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            outputRawImage.texture = inputTexture;
        }
    }
    public Complex dipole(Complex w)
    {
        Complex e = Complex.Exp(w / k);  // Use complex exponentiation

        return (a * b * (e - 1)) / (b - a * e);
    }


    public static float CartesianToPolar(float x, float y)
    {
        return MathF.Sqrt(x * x + y * y);
    }


    public static float GetMagnification(float r)
    {
        return k * (1 / (r + a) - 1 / (r + b));
    }




    void InitializePhosphenes()
    {



        var yamlContent = File.ReadAllText(yamlFilePath);
        var deserializer = new DeserializerBuilder().Build();
        // Deserialize the YAML into the PhosphenePositions object
        var phosphenePositions = deserializer.Deserialize<PhosphenePositions>(yamlContent);

        // Create new lists for extended phosphene positions
        List<float> extendedX = new List<float>(phosphenePositions.x);
        List<float> extendedY = new List<float>(phosphenePositions.y);

        // Add mirrored phosphenes (negate x values)
        for (int i = 0; i < phosphenePositions.x.Count; i++)
        {
            extendedX.Add(-phosphenePositions.x[i]); // Mirror across y-axis
            extendedY.Add(phosphenePositions.y[i]);  // Keep y the same

        }


        int phospheneCount_initial = phosphenePositions.x.Count;
        phospheneCount = extendedX.Count;

        
        phosphenes = new Phosphene[phospheneCount];



        for (int i = 0; i < phospheneCount_initial; i++)
        {



            //Calculate the distortion of the cartesian coordinates of the phosphenes due to magnification
           
            Complex z = new Complex(extendedX[i], extendedY[i]);

            z = dipole(z);

            float dipolex = (float)z.Real;
            float dipoley = (float)z.Imaginary;

            float dipolex_neg = dipolex * (-1.0f);
            float dipoley_neg = dipoley * (-1.0f);

          

            float r = (float)z.Magnitude; //Correct
            float magnification = GetMagnification(r); //Correct
            float sigma = radius_to_sigma * MathF.Sqrt(amp / current_spread) / magnification; //Correct
            float rf = rf_size / magnification;
            rf = ((rf) / (2 * fov)) * 512.0f;



            float x_tex = ((dipolex + fov) / (2 * fov)) * 512.0f;
            float y_tex = ((dipoley + fov) / (2 * fov)) * 512.0f;

            float x_tex_neg = ((dipolex_neg + fov) / (2 * fov)) * 512.0f;
            float y_tex_neg = ((dipoley_neg + fov) / (2 * fov)) * 512.0f;
            // Create the phosphenes struct

            if (UnityEngine.Random.value < dropout_probability)
            {
                // Skip this phosphene
            }
            else
            {
                phosphenes[i] = new Phosphene
                {
                    position = new UnityEngine.Vector2(dipolex, dipoley),
                    position_texture = new UnityEngine.Vector2(x_tex, y_tex),
                    sigma = sigma * sigma,
                    rf = rf

                };
            }
            if (UnityEngine.Random.value < dropout_probability)
            {
                continue; // Skip this phosphene
            }
            phosphenes[phospheneCount_initial + i] = new Phosphene
            {
                position = new UnityEngine.Vector2(dipolex_neg, dipoley_neg),
                position_texture = new UnityEngine.Vector2(x_tex_neg, y_tex_neg),
                sigma = sigma * sigma,
                rf = rf
            };


        }
        phosphenes = phosphenes.Where(p => p.sigma > 0).ToArray();
        phospheneCount = phosphenes.Length;
    }



    void InitializeRenderTexture()
    {
        phospheneRenderTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true
        };
        phospheneRenderTexture.Create();


        edgeTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32){
            enableRandomWrite = true
        };
        edgeTexture.Create();



    }

    void InitializeMaterial()
    {
        if (phospheneShader == null)
            phospheneShader = Shader.Find("Hidden/PhospheneShader");

        edgeDetectionMaterial = new Material(edgeDetectionShader);
        phospheneMaterial = new Material(phospheneShader);
    }

    void GazeOffset()
    {
        EyeTacker.GetEyeTracking();
        offsetx = EyeTacker.x;
        offsety = EyeTacker.y;
    }


    void ApplyEdgeDetection()
    {
        // Render the edge detection result into the edgeTexture
        Graphics.Blit(inputTexture, edgeTexture, edgeDetectionMaterial);
    }

    public void RenderPhosphenes()
    {

        int kernel = phospheneComputeShader.FindKernel("SampleImage");


        // Prepare position data
        //UnityEngine.Vector2[] positions = phosphenes.Select(p => new UnityEngine.Vector2(p.position.x, p.position.y)).ToArray();
        UnityEngine.Vector2[] positions = phosphenes.Select(p => new UnityEngine.Vector2(p.position_texture.x, p.position_texture.y)).ToArray();

        // Create/release compute buffer
        if (positionsBuffer != null)
            positionsBuffer.Release();

        //Prepare position data for compute shader
        positionsBuffer = new ComputeBuffer(phosphenes.Length, sizeof(float) * 2);
        positionsBuffer.SetData(positions);

        // Prepare rf data 
        float[] rfs = phosphenes.Select(p => p.rf).ToArray();
        if (rfBuffer != null)
            rfBuffer.Release();
        rfBuffer = new ComputeBuffer(phosphenes.Length, sizeof(float));
        rfBuffer.SetData(rfs);


        

        // Prepare sigma data 
        float[] sigmas = phosphenes.Select(p => p.sigma).ToArray();
        if (sigmaBuffer != null)
            sigmaBuffer.Release();
        sigmaBuffer = new ComputeBuffer(phosphenes.Length, sizeof(float));
        sigmaBuffer.SetData(sigmas);


        // Release the previous buffer if it exists
        if (indexArrayBuffer != null)
            indexArrayBuffer.Release();

        // Create a new buffer and initialize it with zeros
        indexArrayBuffer = new ComputeBuffer(phospheneCount, sizeof(uint));
        int[] zeroArray = new int[phospheneCount]; // All elements are 0 by default
        indexArrayBuffer.SetData(zeroArray);

        // Pass the edgeTexture to the compute shader as input

        //Texture2D whiteTex = Texture2D.whiteTexture;
       
        if (debugBuffer != null)
            debugBuffer.Release();
        debugBuffer = new ComputeBuffer(phospheneCount,sizeof(float)*2);

        phospheneComputeShader.SetTexture(kernel, "_InputTexture", edgeTexture);
        phospheneComputeShader.SetBuffer(kernel, "_Positions", positionsBuffer);
        phospheneComputeShader.SetBuffer(kernel, "_RFSize", rfBuffer);
        phospheneComputeShader.SetBuffer(kernel, "_IndexArray", indexArrayBuffer);
        phospheneComputeShader.SetBuffer(kernel, "_Debug", debugBuffer);
        phospheneComputeShader.SetInt("_Count", phospheneCount);
        phospheneComputeShader.SetFloat("_Fov", fov);
        phospheneComputeShader.SetInt("_TextureSize", 512); //Make it adaptable 
        phospheneComputeShader.SetInt("_Offsetx", (int)offsetx);
        phospheneComputeShader.SetInt("_Offsety", (int)offsety);   


        phospheneComputeShader.Dispatch(kernel, Mathf.CeilToInt(phospheneCount /64f), 1, 1);

        UnityEngine.Vector2[] debugdata = new UnityEngine.Vector2[debugBuffer.count]; 
        debugBuffer.GetData(debugdata);

        indexArrayBuffer.GetData(zeroArray);
    
        // Create a list to store the selected positions
        List<UnityEngine.Vector2> selectedPositions = new List<UnityEngine.Vector2>();
        List<float> selectedSigmas = new List<float>();

       

        int newlength = 0;
        // Loop through the index array and select positions where the index is 1
        for (int i = 0; i < phospheneCount; i++)
        {
                        
            if (zeroArray[i] == 1)
            {
                // Add the position to the list if the corresponding index is 1
                //selectedPositions.Add(new UnityEngine.Vector2(phosphenes[i].position.x, phosphenes[i].position.y));
                selectedPositions.Add(new UnityEngine.Vector2(phosphenes[i].position_texture.x, phosphenes[i].position_texture.y));
                selectedSigmas.Add(phosphenes[i].sigma);
                newlength++;
            }
           
        }

        // Convert the list to an array
        UnityEngine.Vector2[] filteredPositions = selectedPositions.ToArray();
        float[] filteredSigmas = selectedSigmas.ToArray();

        phospheneMaterial.SetFloat("_Fov", fov);
        // Set shader properties
        if (filteredPositions.Length == 0)
        {
         
        }
        else
        {
            //Debug.Log("Content in Array");
            //Debug.Log($"{newlength}, {phospheneCount}");
            if (newbuff != null)
                newbuff.Release();

            if (newsig != null)
                newsig.Release();


            newbuff = new ComputeBuffer(newlength, sizeof(float) * 2);
            newsig = new ComputeBuffer(newlength, sizeof(float));

            newbuff.SetData(filteredPositions);
            newsig.SetData(filteredSigmas);

            phospheneMaterial.SetBuffer("_Positions", newbuff);
            phospheneMaterial.SetBuffer("_Sigma", newsig);
            phospheneMaterial.SetInt("_Count", newlength);
            phospheneMaterial.SetFloat("_Offsetx", offsetx);
            phospheneMaterial.SetFloat("_Offsety", offsety);
            // Render to texture
            Graphics.SetRenderTarget(phospheneRenderTexture);
            GL.Clear(true, true, Color.clear);
            
            Graphics.Blit(edgeTexture, phospheneRenderTexture, phospheneMaterial);
        }




    }

    void OnDisable()
    {
        if (positionsBuffer != null)
        {
            positionsBuffer.Release();
            positionsBuffer = null;
        }
        if (sigmaBuffer != null)
        {
            sigmaBuffer.Release();
            sigmaBuffer = null;
        }
        if (indexArrayBuffer != null)
        {
            indexArrayBuffer.Release();
            indexArrayBuffer = null;
        }
        if (newbuff != null)
        {
            newbuff.Release();
            newbuff = null;
        }
        if (newsig != null)
        {
            newsig.Release();
            newsig = null;
        }
    }
}