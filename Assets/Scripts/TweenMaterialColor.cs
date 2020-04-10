using UnityEngine;

public class TweenMaterialColor : MonoBehaviour
{
    private static readonly int sColorID = Shader.PropertyToID("_Color");

    public Color From;
    public Color To;
    public float StartDelay;
    public float Time;

    private float mCurrentTime;
    private MeshRenderer mRenderer;
    private MaterialPropertyBlock mMatBlock;

    // --------------------------------------------------------------------

    private void Awake()
    {
        mMatBlock = new MaterialPropertyBlock();

        mRenderer = GetComponent<MeshRenderer>();
        mRenderer.GetPropertyBlock(mMatBlock);
    }

    // --------------------------------------------------------------------

    private void OnEnable()
    {
        mCurrentTime = -StartDelay;
        UpdateColor();
    }

    // --------------------------------------------------------------------

    private void Update()
    {
        mCurrentTime += UnityEngine.Time.deltaTime;

        UpdateColor();
    }

    // --------------------------------------------------------------------

    private void UpdateColor()
    {
        Color col = Color.Lerp(From, To, mCurrentTime / Time);
        mMatBlock.SetColor(sColorID, col);
        mRenderer.SetPropertyBlock(mMatBlock);
    }
}