using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using YamlDotNet.Serialization;
using System.IO;
using System.Collections.Generic;
using System;
using System.Numerics;
using System.Collections;




using static PhospheneRenderer;


public class PhospheneRenderer : MonoBehaviour
{

    //Objects
    public EyeTrackingCapture EyeTacker;
    public SAMDoji model;

    //Struct variables
    private Phosphene[] phosphenes; // Your phosphene data structure
    private Phosphene[] phosphenes_cortex;


    //Render Textures and Raw Image output
    public RenderTexture phospheneRenderTexture;
    public RenderTexture inputTexture;
    public RenderTexture edgeTexture;
    public RenderTexture phospheneInputTexture;
    public RenderTexture semanticRenderTexture;
    public RawImage outputRawImage;
    public RectTransform canvasRectTransform;

    //Render Texture properties
    public int textureWidth = 1024;
    public int textureHeight = 1024;

    //Materials
    private Material phospheneMaterial;
    private Material edgeDetectionMaterial;

    //Compute Buffers
    private ComputeBuffer positionsBuffer;
    private ComputeBuffer sigmaBuffer;
    private ComputeBuffer positionsBufferSampled;
    private ComputeBuffer sigmaBufferSampled;
    private ComputeBuffer rfBuffer;
    private ComputeBuffer indexArrayBuffer;



    //Shaders
    public Shader phospheneShader; //Phosphene Renderer
    public Shader edgeDetectionShader;
    public ComputeShader phospheneComputeShader; //Sampler
    public ComputeShader Combine; //Combine Segmentation and Edge Detection 

    //Simulation properties (Might move to YAML);
    public static float a = 0.75f;
    public static float b = 120.0f;
    public static float k = 17.3f;
    public static float amp = 100e-6f;
    public static float fov = 200f;
    public static float radius_to_sigma = 0.5f;
    public static float current_spread = 675e-6f;
    public static float rf_size = 0.5f;
    public static float dropout_probability = 0.85f;
    public int phospheneCount;
    public string yamlFilePath = "C:\\Users\\Administrator\\Desktop\\phosphene_schemes\\grid_coords_squares_utah.yaml";

    public float offsetx = 0;
    public float offsety = 0;
    public float[] pointCoords;
    public float[] pointLabels;

    private bool switchMethods = true;

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
        InitializeBuffers();

        pointCoords = new float[2] { 512, 512 };
        pointLabels = new float[1] { 1.0f };


        //outputRawImage.texture = inputTexture;
        outputRawImage.texture = phospheneRenderTexture;
        //For testing Edge Detection
        //outputRawImage.texture = edgeTexture;

    }


    void Update()
    {

        int kernel = Combine.FindKernel("Combine");

        GazeOffset();
        if (switchMethods)
        {
            model.Seg(inputTexture, pointCoords, pointLabels);
            Graphics.Blit(model.mobileSAMPredictor.Result, phospheneInputTexture);

        }
        else { ApplyEdgeDetection();
            phospheneInputTexture = edgeTexture;
        }
            
        
        //Graphics.Blit(model.mobileSAMPredictor.Result, semanticRenderTexture);
        //// Set shader parameters
        //Combine.SetTexture(kernel, "_TexA", semanticRenderTexture);
        //Combine.SetTexture(kernel, "_TexB", edgeTexture);
        //Combine.SetTexture(kernel, "_Result", phospheneInputTexture);

        //// Dispatch the shader (adjust thread groups as needed)
        //int threadGroupsX = Mathf.CeilToInt(phospheneInputTexture.width / 8f);
        //int threadGroupsY = Mathf.CeilToInt(phospheneInputTexture.height / 8f);
        //Combine.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);



        RenderPhosphenes();
        // Check if the "F" key is pressed
        if (Input.GetKeyDown(KeyCode.F))
        {
            outputRawImage.texture = phospheneRenderTexture;

        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            outputRawImage.texture = phospheneInputTexture;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            outputRawImage.texture = inputTexture;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (switchMethods)
            {
                switchMethods = false;
                Graphics.SetRenderTarget(model.mobileSAMPredictor.Result);
                GL.Clear(true, true, Color.clear);
                Graphics.SetRenderTarget(null); // Reset target // Clear depth and color
            }
            else
            {
                switchMethods = true;
            }

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
            //rf = ((rf) / (2 * fov)) * 512.0f;
            rf = ((rf) / (2 * fov)) * textureWidth;

            //float x_tex = ((dipolex + fov) / (2 * fov)) * 512.0f;
            //float y_tex = ((dipoley + fov) / (2 * fov)) * 512.0f;

            //float x_tex_neg = ((dipolex_neg + fov) / (2 * fov)) * 512.0f;
            //float y_tex_neg = ((dipoley_neg + fov) / (2 * fov)) * 512.0f;

            float x_tex = ((dipolex + fov) / (2 * fov)) * textureWidth;
            float y_tex = ((dipoley + fov) / (2 * fov)) * textureWidth;

            float x_tex_neg = ((dipolex_neg + fov) / (2 * fov)) * textureWidth;
            float y_tex_neg = ((dipoley_neg + fov) / (2 * fov)) * textureWidth;
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

        semanticRenderTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true
        };
        semanticRenderTexture.Create();

        phospheneInputTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true
        };
        phospheneInputTexture.Create();


        edgeTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32) {
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
        //EyeTacker.GetEyeTracking();
        offsetx = EyeTacker.offsetx;
        offsety = EyeTacker.offsety;
        pointCoords = new float[2] { (float)EyeTacker.x, (float)EyeTacker.y };
    }


    void ApplyEdgeDetection()
    {
        // Render the edge detection result into the edgeTexture
        Graphics.Blit(inputTexture, edgeTexture, edgeDetectionMaterial);
    }

    public void InitializeBuffers(){

        // Prepare position data
        UnityEngine.Vector2[] positions = phosphenes.Select(p => new UnityEngine.Vector2(p.position_texture.x, p.position_texture.y)).ToArray();
        if (positionsBuffer != null)
            positionsBuffer.Release();
        positionsBuffer = new ComputeBuffer(phosphenes.Length, sizeof(float) * 2);
        positionsBufferSampled = new ComputeBuffer(phosphenes.Length, sizeof(float) * 2);
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
        sigmaBufferSampled = new ComputeBuffer(phosphenes.Length, sizeof(float));
        sigmaBuffer.SetData(sigmas);

        indexArrayBuffer = new ComputeBuffer(phospheneCount, sizeof(uint));
    }




    public void RenderPhosphenes()
    {

        int kernel = phospheneComputeShader.FindKernel("SampleImage");


        
        int[] zeroArray = new int[phospheneCount]; // All elements are 0 by default
        indexArrayBuffer.SetData(zeroArray);

        // Pass the edgeTexture to the compute shader as input
       
        

        //phospheneComputeShader.SetTexture(kernel, "_InputTexture", edgeTexture);
        phospheneComputeShader.SetTexture(kernel, "_InputTexture", phospheneInputTexture);
        phospheneComputeShader.SetBuffer(kernel, "_Positions", positionsBuffer);
        phospheneComputeShader.SetBuffer(kernel, "_Positions", positionsBuffer);
        phospheneComputeShader.SetBuffer(kernel, "_PositionsOut", positionsBufferSampled);
        phospheneComputeShader.SetBuffer(kernel, "_Sigma", sigmaBuffer);
        phospheneComputeShader.SetBuffer(kernel, "_SigmaOut", sigmaBufferSampled);
        phospheneComputeShader.SetBuffer(kernel, "_RFSize", rfBuffer);
        phospheneComputeShader.SetBuffer(kernel, "_IndexArray", indexArrayBuffer);
        phospheneComputeShader.SetInt("_Count", phospheneCount);
        phospheneComputeShader.SetFloat("_Fov", fov);
        //phospheneComputeShader.SetInt("_TextureSize", 512); //Make it adaptable 
        phospheneComputeShader.SetInt("_TextureSize", textureWidth);
        phospheneComputeShader.SetInt("_Offsetx", (int)offsetx);
        phospheneComputeShader.SetInt("_Offsety", (int)offsety);   


        phospheneComputeShader.Dispatch(kernel, Mathf.CeilToInt(phospheneCount /64f), 1, 1);


        phospheneMaterial.SetBuffer("_Positions", positionsBufferSampled);
        phospheneMaterial.SetBuffer("_Sigma", sigmaBufferSampled);
        phospheneMaterial.SetInt("_Count", phospheneCount);
        phospheneMaterial.SetFloat("_Offsetx", offsetx);
        phospheneMaterial.SetFloat("_Offsety", offsety);
        // Render to texture
        Graphics.SetRenderTarget(phospheneRenderTexture);
        GL.Clear(true, true, Color.clear);
        Graphics.Blit(null, phospheneRenderTexture, phospheneMaterial);
       




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
        if (rfBuffer != null)
        {
            rfBuffer.Release();
            rfBuffer = null;
        }
        if (indexArrayBuffer != null)
                {
                    indexArrayBuffer.Release();
                    indexArrayBuffer = null;
                }
        if (positionsBufferSampled != null)
        {
            positionsBufferSampled.Release();
            positionsBufferSampled = null;
        }
        if (sigmaBufferSampled != null)
        {
            sigmaBufferSampled.Release();
            sigmaBufferSampled = null;
        }
    }
}