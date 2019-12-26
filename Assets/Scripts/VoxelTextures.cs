using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoxelTextures : MonoBehaviour
{
    [SerializeField]
    private int3 cubeSize = new int3(16, 1, 16);

#if UNITY_EDITOR
    [Header("Textures")]
#endif
    [SerializeField]
    private int textureSize = 16;
    [SerializeField]
    private Texture2D topTexture = null;
    [SerializeField]
    private Texture2D northTexture = null;
    [SerializeField]
    private Texture2D westTexture = null;
    [SerializeField]
    private Texture2D eastTexture = null;
    [SerializeField]
    private Texture2D southTexture = null;
    [SerializeField]
    private Texture2D bottomTexture = null;

#if UNITY_EDITOR
    [Space]
#endif

    [SerializeField]
    private Material voxelMaterial = null;

    [SerializeField]
    [HideInInspector]
    private MeshFilter filter = null;
    [SerializeField]
    [HideInInspector]
    private MeshRenderer ren = null;

    private Texture2D atlasTexture;
    private Material mat;
    private Rect[] rects;

    private Dictionary<string, int2> textures = new Dictionary<string, int2>();

    private void Awake()
    {
        // Firstly generate the atlas.
        GenerateAtlas();

        // Create the voxel material using the new texture.
        mat = new Material(voxelMaterial)
        {
            mainTexture = atlasTexture
        };

        // Assign the material to the renderer.
        ren.material = mat;

        // Calculate how many items there are on each row and column.
        int xSize = atlasTexture.width / textureSize;
        int ySize = atlasTexture.height / textureSize;

        // Set the values we calculated on the shader.
        mat.SetInt("_AtlasX", xSize);
        mat.SetInt("_AtlasY", ySize);
        // Calculate the rect.
        mat.SetVector("_AtlasRec", new Vector4(1.0f / xSize, 1.0f / ySize));
    }

    private void GenerateAtlas()
    {
        List<Texture2D> uniqueTextures = new List<Texture2D>();
        // All of these if checks are pretty reduntant in this example
        // but in an actual voxel engine you would want some duplication checking.
        if (!uniqueTextures.Contains(topTexture))
        {
            uniqueTextures.Add(topTexture);
        }

        if (!uniqueTextures.Contains(northTexture))
        {
            uniqueTextures.Add(northTexture);
        }

        if (!uniqueTextures.Contains(eastTexture))
        {
            uniqueTextures.Add(eastTexture);
        }

        if (!uniqueTextures.Contains(westTexture))
        {
            uniqueTextures.Add(westTexture);
        }

        if (!uniqueTextures.Contains(southTexture))
        {
            uniqueTextures.Add(southTexture);
        }

        if (!uniqueTextures.Contains(bottomTexture))
        {
            uniqueTextures.Add(bottomTexture);
        }

        // Create the atlas texture.
        atlasTexture = new Texture2D(8192, 8192);
        rects = atlasTexture.PackTextures(uniqueTextures.ToArray(), 0, 8192, false);
        atlasTexture.filterMode = FilterMode.Point;

        for (int i = 0; i < uniqueTextures.Count; i++)
        {
            Rect uvs = rects[i];

            // If the texture hasn't been added, add it to the dictionary of textures.
            if (!textures.TryGetValue(uniqueTextures[i].name, out int2 coords))
            {
                // Calculate the X and Y index position of the texture using the rect UVs and texture size.
                coords = new int2((int)(atlasTexture.width * uvs.x / textureSize), (int)(atlasTexture.height * uvs.y / textureSize));
                textures.Add(uniqueTextures[i].name, coords);
            }
        }
    }

    private int2 GetTexture(string name)
    {
        return textures[name];
    }

    // Start is called before the first frame update
    void Start()
    {
        NativeList<float3> vertices = new NativeList<float3>(Allocator.TempJob);
        NativeList<int> indicies = new NativeList<int>(Allocator.TempJob);
        NativeList<float4> uvs = new NativeList<float4>(Allocator.TempJob);

        // The block is basically just to simulate actually having blocks. 
        // In this case we only have one.
        BlockData block = new BlockData()
        {
            topTexture = GetTexture(topTexture.name),
            bottomTexture = GetTexture(bottomTexture.name),
            northTexture = GetTexture(northTexture.name),
            eastTexture = GetTexture(eastTexture.name),
            southTexture = GetTexture(southTexture.name),
            westTexture = GetTexture(westTexture.name)
        };

        new BuildCubeJob()
        {
            size = cubeSize,
            block = block,
            vertices = vertices,
            indicies = indicies,
            uvs = uvs
        }.Run();

        Mesh mesh = new Mesh();
        mesh.SetVertices<float3>(vertices);
        mesh.SetIndices<int>(indicies, MeshTopology.Triangles, 0);
        mesh.SetUVs<float4>(0, uvs);

        mesh.RecalculateNormals();

        filter.mesh = mesh;

        vertices.Dispose();
        indicies.Dispose();
        uvs.Dispose();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        GetStandardComponents();
    }

    private void Reset()
    {
        GetStandardComponents();
    }

    private void GetStandardComponents()
    {
        if (filter == null)
        {
            filter = GetComponent<MeshFilter>();
        }

        if (ren == null)
        {
            ren = GetComponent<MeshRenderer>();
        }
    }
#endif
}

[BurstCompile]
public struct BuildCubeJob : IJob
{
    [ReadOnly]
    public BlockData block;
    [ReadOnly]
    public int3 size;

    [WriteOnly]
    public NativeList<float3> vertices;
    [WriteOnly]
    public NativeList<int> indicies;
    [WriteOnly]
    public NativeList<float4> uvs;

    public void Execute()
    {
        int vertexIndex = 0;

        // North
        {
            vertices.Add(new float3(0, 0, size.z));
            vertices.Add(new float3(size.x, 0, size.z));
            vertices.Add(new float3(0, size.y, size.z));
            vertices.Add(new float3(size.x, size.y, size.z));

            uvs.Add(new float4(0, 0, block.northTexture.x, block.northTexture.y));
            uvs.Add(new float4(size.x, 0, block.northTexture.x, block.northTexture.y));
            uvs.Add(new float4(0, size.y, block.northTexture.x, block.northTexture.y));
            uvs.Add(new float4(size.x, size.y, block.northTexture.x, block.northTexture.y));

            indicies.Add(vertexIndex + 1);
            indicies.Add(vertexIndex + 2);
            indicies.Add(vertexIndex);
            indicies.Add(vertexIndex + 1);
            indicies.Add(vertexIndex + 3);
            indicies.Add(vertexIndex + 2);

            vertexIndex += 4;
        }

        // East
        {
            vertices.Add(new float3(size.x, 0, 0));
            vertices.Add(new float3(size.x, 0, size.z));
            vertices.Add(new float3(size.x, size.y, 0));
            vertices.Add(new float3(size.x, size.y, size.z));

            uvs.Add(new float4(0, 0, block.eastTexture.x, block.eastTexture.y));
            uvs.Add(new float4(size.z, 0, block.eastTexture.x, block.eastTexture.y));
            uvs.Add(new float4(0, size.y, block.eastTexture.x, block.eastTexture.y));
            uvs.Add(new float4(size.z, size.y, block.eastTexture.x, block.eastTexture.y));

            indicies.Add(vertexIndex);
            indicies.Add(vertexIndex + 2);
            indicies.Add(vertexIndex + 1);
            indicies.Add(vertexIndex + 2);
            indicies.Add(vertexIndex + 3);
            indicies.Add(vertexIndex + 1);

            vertexIndex += 4;
        }

        // South
        {
            vertices.Add(new float3(0, 0, 0));
            vertices.Add(new float3(size.x, 0, 0));
            vertices.Add(new float3(0, size.y, 0));
            vertices.Add(new float3(size.x, size.y, 0));

            uvs.Add(new float4(0, 0, block.southTexture.x, block.southTexture.y));
            uvs.Add(new float4(size.x, 0, block.southTexture.x, block.southTexture.y));
            uvs.Add(new float4(0, size.y, block.southTexture.x, block.southTexture.y));
            uvs.Add(new float4(size.x, size.y, block.southTexture.x, block.southTexture.y));

            indicies.Add(vertexIndex);
            indicies.Add(vertexIndex + 2);
            indicies.Add(vertexIndex + 1);
            indicies.Add(vertexIndex + 2);
            indicies.Add(vertexIndex + 3);
            indicies.Add(vertexIndex + 1);

            vertexIndex += 4;
        }

        // West
        {
            vertices.Add(new float3(0, 0, 0));
            vertices.Add(new float3(0, size.y, 0));
            vertices.Add(new float3(0, 0, size.z));
            vertices.Add(new float3(0, size.y, size.z));

            uvs.Add(new float4(0, 0, block.westTexture.x, block.westTexture.y));
            uvs.Add(new float4(0, size.y, block.westTexture.x, block.westTexture.y));
            uvs.Add(new float4(size.z, 0, block.westTexture.x, block.westTexture.y));
            uvs.Add(new float4(size.z, size.y, block.westTexture.x, block.westTexture.y));

            indicies.Add(vertexIndex);
            indicies.Add(vertexIndex + 2);
            indicies.Add(vertexIndex + 1);
            indicies.Add(vertexIndex + 2);
            indicies.Add(vertexIndex + 3);
            indicies.Add(vertexIndex + 1);

            vertexIndex += 4;
        }

        // Up
        {
            vertices.Add(new float3(0, size.y, 0));
            vertices.Add(new float3(size.x, size.y, 0));
            vertices.Add(new float3(0, size.y, size.z));
            vertices.Add(new float3(size.x, size.y, size.z));

            uvs.Add(new float4(0, 0, block.topTexture.x, block.topTexture.y));
            uvs.Add(new float4(size.x, 0, block.topTexture.x, block.topTexture.y));
            uvs.Add(new float4(0, size.z, block.topTexture.x, block.topTexture.y));
            uvs.Add(new float4(size.x, size.z, block.topTexture.x, block.topTexture.y));

            indicies.Add(vertexIndex);
            indicies.Add(vertexIndex + 2);
            indicies.Add(vertexIndex + 1);
            indicies.Add(vertexIndex + 2);
            indicies.Add(vertexIndex + 3);
            indicies.Add(vertexIndex + 1);

            vertexIndex += 4;
        }

        // Down
        {
            vertices.Add(new float3(0, 0, 0));
            vertices.Add(new float3(0, 0, size.z));
            vertices.Add(new float3(size.x, 0, 0));
            vertices.Add(new float3(size.x, 0, size.z));

            uvs.Add(new float4(0, 0, block.bottomTexture.x, block.bottomTexture.y));
            uvs.Add(new float4(size.z, 0, block.bottomTexture.x, block.bottomTexture.y));
            uvs.Add(new float4(0, size.x, block.bottomTexture.x, block.bottomTexture.y));
            uvs.Add(new float4(size.z, size.x, block.bottomTexture.x, block.bottomTexture.y));

            indicies.Add(vertexIndex);
            indicies.Add(vertexIndex + 2);
            indicies.Add(vertexIndex + 1);
            indicies.Add(vertexIndex + 2);
            indicies.Add(vertexIndex + 3);
            indicies.Add(vertexIndex + 1);
        }
    }
}

public struct BlockData
{
    public int2 northTexture;
    public int2 eastTexture;
    public int2 westTexture;
    public int2 southTexture;
    public int2 topTexture;
    public int2 bottomTexture;
}