using UnityEngine;
using UnityEngine.UI;

public class PhospheneController : MonoBehaviour
{
    [Header("RawImage Output")]
    public RawImage outputRawImage; // Reference to the UI RawImage


    [Header("RGB RenderTexture")]
    public RenderTexture rgbTexture; 


    [Header("Edge Detection")]
    public Material edgeDetectionMaterial;
    private RenderTexture edgeTexture; // Texture to hold edge detection result

    [Header("Compute Shader Settings")]
    public ComputeShader phospheneComputeShader;
    public int phospheneCount = 1;
    private ComputeBuffer phospheneBuffer;
    private PhospheneData[] phosphenes;

    [Header("Phosphene Rendering")]
    public Material phospheneMaterial;
    private RenderTexture phospheneRenderTexture; // Phosphene render texture

    private MaterialPropertyBlock materialPropertyBlock;

    public Mesh instanceMesh;


    struct PhospheneData
    {
        public Vector2 position;
        public float m;
        public float current;
        public float charge;
        public float brightness;
    }

    void Start()
    {
        InitializeTextures();
        InitializePhospheneData();
    }

    void Update()
    {
        ApplyEdgeDetection();
   
        UpdatePhosphenes();
        //for (int i = 0; i < phospheneCount; i++)
        //{
        //    Debug.Log($"x= {phosphenes[i].position[0]}, y = {phosphenes[i].position[0]}");
        //}
            RenderPhosphenes();
    }

    void InitializeTextures()
    {
        int width = 512;
        int height = 512;

        // Edge detection texture
        edgeTexture = new RenderTexture(width, height, 0);
        edgeTexture.enableRandomWrite = true;
        edgeTexture.Create();

        // Phosphene simulation output texture
        phospheneRenderTexture = new RenderTexture(width, height, 0);
        phospheneRenderTexture.enableRandomWrite = true;
        phospheneRenderTexture.Create();

        // Assign the phosphene texture to RawImage
        outputRawImage.texture = phospheneRenderTexture;
    }

    Mesh CreateQuadMesh()
    {
        // Define the vertices of the quad (4 corners of a unit square)
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, -0.5f, 0f), // Bottom-left
            new Vector3( 0.5f, -0.5f, 0f), // Bottom-right
            new Vector3( 0.5f,  0.5f, 0f), // Top-right
            new Vector3(-0.5f,  0.5f, 0f)  // Top-left
        };

        // Define the triangles (order of indices to form a face)
        int[] triangles = new int[6]
        {
            0, 2, 1, // First triangle
            0, 3, 2  // Second triangle
        };

        // Define UVs (how the texture is mapped)
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0f, 0f), // Bottom-left
            new Vector2(1f, 0f), // Bottom-right
            new Vector2(1f, 1f), // Top-right
            new Vector2(0f, 1f)  // Top-left
        };

        // Create a new mesh and set its vertices, triangles, and UVs
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        // Optionally calculate normals for proper lighting
        mesh.RecalculateNormals();

        return mesh;
    }

    void InitializePhospheneData()
    {
        phosphenes = new PhospheneData[phospheneCount];

        // Get default cortex coordinates (defined inside the function)
        var x = new float[] { -0f,1f };  // Example x coordinates
        var y = new float[] { -0f, 1f };  // Example y coordinates

        // Get the minimum and maximum values for x and y
        float xMin = Mathf.Min(x);
        float xMaxValue = Mathf.Max(x);
        float yMin = Mathf.Min(y);
        float yMax = Mathf.Max(y);

        // Define the number of electrodes in each dimension
        int nElectrodesX = 10;  // Example number of electrodes in the x direction
        int nElectrodesY = 10;  // Example number of electrodes in the y direction

        // Generate the ranges using Mathf.Lerp to mimic np.linspace
        float[] xRange = new float[nElectrodesX];
        float[] yRange = new float[nElectrodesY];
        for (int i = 0; i < nElectrodesX; i++)
        {
            xRange[i] = Mathf.Lerp(xMin, xMaxValue, i / (float)(nElectrodesX - 1));
        }
        for (int i = 0; i < nElectrodesY; i++)
        {
            yRange[i] = Mathf.Lerp(yMin, yMax, i / (float)(nElectrodesY - 1));
        }

        // Fill the phosphene array based on the grid of cortex coordinates
        int index = 0;
        for (int i = 0; i < xRange.Length && index < phospheneCount; i++)
        {
            for (int j = 0; j < yRange.Length && index < phospheneCount; j++)
            {
                // Ensure we do not exceed phospheneCount
                if (index >= phospheneCount) break;

                // Log the x and y ranges for debugging
                //Debug.Log($"xRange[{i}] = {xRange[i]}, yRange[{j}] = {yRange[j]}");

                phosphenes[index] = new PhospheneData
                {
                    position = new Vector2(xRange[i], yRange[j]),
                    m = 1,
                    current = 1,
                    charge = 1,
                    brightness = 1
                };
                index++;
            }
        }


        phospheneBuffer = new ComputeBuffer(phospheneCount, sizeof(float) * 6);
    phospheneBuffer.SetData(phosphenes);
    }

    

    void ApplyEdgeDetection()
    {
        // Render the edge detection result into the edgeTexture
        Graphics.Blit(rgbTexture, edgeTexture, edgeDetectionMaterial);
    }

    void UpdatePhosphenes()
    {
        int kernel = phospheneComputeShader.FindKernel("UpdatePhosphenes");

        // Pass the edgeTexture to the compute shader as input
        phospheneComputeShader.SetTexture(kernel, "_EdgeTexture", edgeTexture);
        phospheneComputeShader.SetBuffer(kernel, "_Phosphenes", phospheneBuffer);
        phospheneComputeShader.SetFloat("_DeltaTime", Time.deltaTime);

        phospheneComputeShader.Dispatch(kernel, phospheneCount / 64, 1, 1);
    }

    void RenderPhosphenes()
    {
        RenderTexture.active = phospheneRenderTexture;
        GL.Clear(true, true, Color.clear);

        // Calculate matrices for each instance
        Matrix4x4[] matrices = new Matrix4x4[phospheneCount];
        for (int i = 0; i < phospheneCount; i++)
        {
            Vector2 pos = phosphenes[i].position;
            float radius = phosphenes[i].brightness * 0.1f; // Adjust scale factor as needed
            Vector3 position = new Vector3(pos.x, pos.y, 0);
            Vector3 scale = Vector3.one * radius * 2; // Scale to diameter
            matrices[i] = Matrix4x4.TRS(position, Quaternion.identity, scale);
        }

        // Draw all instances
        Graphics.DrawMeshInstanced(instanceMesh, 0, phospheneMaterial, matrices, phospheneCount);

        // Reset active render target
        RenderTexture.active = null;

    }



    void OnDestroy()
    {
        if (phospheneBuffer != null)
            phospheneBuffer.Release();
    }
}
