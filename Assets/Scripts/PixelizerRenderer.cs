using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PixelizerRenderer : MonoBehaviour
{
    private static readonly int sObjWorldMatrixID = Shader.PropertyToID("_ObjWorldMatrix");
    private static readonly int sWorldObjMatrixID = Shader.PropertyToID("_WorldObjMatrix");

    public int TextureSize = 32;
    public GameObject Target;
    public Material TargetMaterial;
    public Material PixelizerMaterial;
    public float VoxelSize = 0.1f;
    public GameObject PhysicVoxel;
    public float VoxelForce;
    public float VoxelTorque;

    [Range(1, 5)]
    public int ExplosionVoxelReductionRate = 1;

    private Camera mPixelatedCam;
    private Camera mMainCam;
    private Renderer[] mRenderers;
    private RenderTexture mRenderTexture;
    private CommandBuffer mCmdBuffer;
    private ComputeBuffer mVoxelsBuffer;
    private Texture2D mExplosionTexture;
    private Bounds mBounds;
    private ParticleSystem mSystem;

    private struct BufferVoxel
    {
        public Vector3 Pos;
        public Vector2 UV;
    }

    // --------------------------------------------------------------------

    private void Awake()
    {
        mMainCam = Camera.main; // This uses FindGameObjectsWithTag, sooo, not great you should have probably have a CameraManager instead

        mPixelatedCam = GetComponentInChildren<Camera>();
        AddCommandBufferToCamera(mPixelatedCam);

        CreateVoxelBuffer();
        InitMaterial();

        mSystem = GetComponentInChildren<ParticleSystem>();

        mExplosionTexture = new Texture2D(TextureSize, TextureSize, TextureFormat.ARGB32, false);
    }

    // --------------------------------------------------------------------

    private void AddCommandBufferToCamera(Camera cam)
    {
        mRenderTexture = new RenderTexture(TextureSize, TextureSize, 16);
        mRenderTexture.filterMode = FilterMode.Point;
        mRenderTexture.name = "PixelizerCam Texture"; // This will show up if you're using a graphic debugger
        RenderTargetIdentifier rtID = new RenderTargetIdentifier(mRenderTexture);

        mCmdBuffer = new CommandBuffer();
        mCmdBuffer.name = "Pixelated Voxels";
        mCmdBuffer.SetRenderTarget(rtID);
        mCmdBuffer.ClearRenderTarget(true, true, Color.clear, 1f);

        mRenderers = Target.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in mRenderers)
            mCmdBuffer.DrawRenderer(r, TargetMaterial);

        cam.AddCommandBuffer(CameraEvent.AfterEverything, mCmdBuffer);
    }

    // --------------------------------------------------------------------

    private void CreateVoxelBuffer()
    {
        mBounds = new Bounds();
        List<BufferVoxel> voxels = new List<BufferVoxel>();
        for (int i = 0; i < TextureSize; ++i)
        {
            for (int j = 0; j < TextureSize; ++j)
            {
                Vector3 voxelPos = new Vector3((i - TextureSize / 2.0f) * VoxelSize, j * VoxelSize, 0);
                mBounds.Encapsulate(voxelPos);
                voxels.Add(new BufferVoxel()
                {
                    Pos = voxelPos,
                    UV = new Vector2(i / (float)TextureSize, j / (float)TextureSize)
                });
            }
        }

        mVoxelsBuffer = new ComputeBuffer(TextureSize * TextureSize, sizeof(float) * 5, ComputeBufferType.Default);
        mVoxelsBuffer.SetData(voxels);
    }

    // --------------------------------------------------------------------

    private void InitMaterial()
    {
        PixelizerMaterial.SetFloat("_VoxelSize", VoxelSize);
        PixelizerMaterial.SetBuffer("_Voxels", mVoxelsBuffer);
        PixelizerMaterial.SetTexture("_PixelatedTexture", mRenderTexture);
    }

    // --------------------------------------------------------------------

    private void Update()
    {
        Vector3 newFwd = mMainCam.transform.forward;
        newFwd.y = 0;
        transform.SetPositionAndRotation(Target.transform.position, Quaternion.LookRotation(newFwd, Vector3.up));

        mPixelatedCam.Render();

        PixelizerMaterial.SetPass(0);
        PixelizerMaterial.SetMatrix(sObjWorldMatrixID, transform.localToWorldMatrix);
        PixelizerMaterial.SetMatrix(sWorldObjMatrixID, transform.worldToLocalMatrix);

        Graphics.DrawProcedural(PixelizerMaterial, mBounds, MeshTopology.Points, TextureSize * TextureSize);
    }

    // --------------------------------------------------------------------

    private void OnDestroy()
    {
        mCmdBuffer.Release();
        mVoxelsBuffer.Release();
    }

    // --------------------------------------------------------------------

    public void Pixelate()
    {
        SetPixelated(true);
        PixelizerMaterial.SetFloat("_InitTime", Time.time);
    }

    // --------------------------------------------------------------------

    public void Restore()
    {
        SetPixelated(false);
    }

    // --------------------------------------------------------------------

    private void SetPixelated(bool pixelated)
    {
        Animator animator = Target.GetComponentInChildren<Animator>();
        if (animator)
            animator.cullingMode = pixelated ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullUpdateTransforms;

        foreach (Renderer renderer in mRenderers)
        {
            renderer.gameObject.layer = pixelated ? (int)Layers.Pixelator : 0;
            SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
            if (skinned)
                skinned.updateWhenOffscreen = pixelated;
        }

        enabled = pixelated;
    }

    // --------------------------------------------------------------------

    public void Explode()
    {
        float halfTextSize = TextureSize * 0.5f;
        RenderTexture.active = mRenderTexture;

        mExplosionTexture.ReadPixels(new Rect(0, 0, TextureSize, TextureSize), 0, 0);
        mExplosionTexture.Apply();

        List<Vector3> voxels = new List<Vector3>();
        List<Color> colors = new List<Color>();
        for (int i = 0; i < TextureSize; i += ExplosionVoxelReductionRate)
        {
            for (int j = 0; j < TextureSize; j += ExplosionVoxelReductionRate)
            {
                Color col = mExplosionTexture.GetPixel(i, j);
                if (col.a > 0)
                {
                    Vector3 pos = new Vector3(-halfTextSize * VoxelSize + i * VoxelSize, j * VoxelSize);
                    Vector3 worldPos = transform.TransformPoint(pos);
                    voxels.Add(worldPos);
                    colors.Add(col);
                }
            }
        }

        ParticleSystem.MainModule mainMod = mSystem.main;
        mainMod.maxParticles = voxels.Count;
        mSystem.Emit(new ParticleSystem.EmitParams()
        {
            startSize = VoxelSize,
            angularVelocity = VoxelTorque
        }, voxels.Count);
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[voxels.Count];
        int count = mSystem.GetParticles(particles);

        for (int i = 0; i < voxels.Count; ++i)
        {
            particles[i].position = voxels[i];
            particles[i].startColor = colors[i];

            Vector3 force = (voxels[i] - transform.position).normalized;
            force *= VoxelForce;
            force.y = VoxelForce;
            force += transform.forward * Random.Range(-1f, 1f) * VoxelForce;

            particles[i].velocity = force;
        }

        mSystem.SetParticles(particles);

        enabled = false;
    }

#if UNITY_EDITOR

    // --------------------------------------------------------------------

    private void OnGUI()
    {
        if (Event.current.type.Equals(EventType.Repaint))
        {
            Graphics.DrawTexture(new Rect(10, 100, 100, 100), mRenderTexture);
        }
    }

    // --------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        float halfTextSize = TextureSize * 0.5f;
        for (int i = 0; i <= TextureSize; ++i)
        {
            // Horizontal
            Gizmos.DrawLine(
                transform.position + transform.right * -halfTextSize * VoxelSize + Vector3.up * i * VoxelSize,
                transform.position + transform.right * halfTextSize * VoxelSize + Vector3.up * i * VoxelSize);

            // Vertical
            Gizmos.DrawLine(
                transform.position + transform.right * (i - halfTextSize) * VoxelSize,
                transform.position + transform.right * (i - halfTextSize) * VoxelSize + Vector3.up * TextureSize * VoxelSize);
        }
    }

#endif
}