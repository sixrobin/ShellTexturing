using Sirenix.OdinInspector;
using UnityEngine;

public class Shell : MonoBehaviour
{
    private static readonly int MASK_SHADER_ID = Shader.PropertyToID("_Mask");
    private static readonly int COLOR_SHADER_ID = Shader.PropertyToID("_Color");
    private static readonly int RADIUS_SHADER_ID = Shader.PropertyToID("_Radius");
    private static readonly int DISPLACEMENT_SHADER_ID = Shader.PropertyToID("_Displacement");
    private static readonly int DISPLACEMENT_INTENSITY_SHADER_ID = Shader.PropertyToID("_DisplacementIntensity");
    private static readonly int DISPLACEMENT_SPEED_SHADER_ID = Shader.PropertyToID("_DisplacementSpeed");
    private static readonly int DISPLACEMENT_SCALE_SHADER_ID = Shader.PropertyToID("_DisplacementScale");

    [SerializeField, FoldoutGroup("References")]
    private GameObject _quadPrefab;
    [SerializeField, FoldoutGroup("References")]
    private Material _shellLayerMaterial;
    [SerializeField, FoldoutGroup("References")]
    private ComputeShader _randomComputeShader;

    [SerializeField, FoldoutGroup("Color")]
    private Color _downColor = Color.black;
    [SerializeField, FoldoutGroup("Color")]
    private Color _upColor = Color.white;
    [SerializeField, FoldoutGroup("Color")]
    private AnimationCurve _colorGradientCurve;
    
    [SerializeField, Min(32), FoldoutGroup("Settings")]
    private int _resolution = 32;
    [SerializeField, FoldoutGroup("Settings")]
    private float _height;
    [SerializeField, Min(2), FoldoutGroup("Settings")]
    private int _count;
    [SerializeField, Min(0f), FoldoutGroup("Settings")]
    private float _radius = 1f;
    [SerializeField, Range(0f, 1f), FoldoutGroup("Settings")]
    private float _maskInitRandomStep = 0.9f;
    [SerializeField, Range(0f, 1f), FoldoutGroup("Settings")]
    private float _maskLastRandomStep = 0.1f;

    [SerializeField, FoldoutGroup("Settings/Displacement")]
    private Texture2D _displacementTexture = null;
    [SerializeField, Min(0f), FoldoutGroup("Settings/Displacement")]
    private float _displacementIntensity = 1f;
    [SerializeField, Min(0f), FoldoutGroup("Settings/Displacement")]
    private float _displacementSpeed = 1f;
    [SerializeField, Range(0f, 1f), FoldoutGroup("Settings/Displacement")]
    private float _displacementScale = 1f;

    private bool _dirty;
    
    private void Refresh()
    {
        if (this._quadPrefab == null)
            return;

        for (int i = this.transform.childCount - 1; i >= 0; --i)
            Destroy(this.transform.GetChild(i).gameObject);
        
        this.GenerateQuads();
    }

    private RenderTexture GenerateMask(int index)
    {
        RenderTexture layerTexture = new(this._resolution, this._resolution, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point,
        };
        
        this._randomComputeShader.SetTexture(0, "Result", layerTexture);
        this._randomComputeShader.Dispatch(0, this._resolution / 8, this._resolution / 8, 1);
        
        return layerTexture;
    }
    
    private void GenerateQuads()
    {
        this._resolution = Mathf.Min(this._resolution, 1024);
        
        // TODO: Add a simple quad below to fake an actual ground.
        
        float step = this._height / (this._count - 1);
        for (int i = 0; i < this._count; ++i)
            this.GenerateQuad(i, step * i);
    }

    private GameObject GenerateQuad(int index, float height)
    {
        GameObject quadInstance = Instantiate(this._quadPrefab, Vector3.zero, this._quadPrefab.transform.rotation, this.transform);
        quadInstance.transform.Translate(Vector3.up * height, Space.World);

        float percentage = index / (float)(this._count - 1);
        Color color = Color.Lerp(this._downColor, this._upColor, this._colorGradientCurve.Evaluate(percentage));
        
        Material quadMaterial = new(this._shellLayerMaterial);
        quadMaterial.SetTexture(MASK_SHADER_ID, this.GenerateMask(index));
        quadMaterial.SetColor(COLOR_SHADER_ID, color);
        quadMaterial.SetFloat(RADIUS_SHADER_ID, this._radius);
        quadMaterial.SetTexture(DISPLACEMENT_SHADER_ID, this._displacementTexture);
        quadMaterial.SetFloat(DISPLACEMENT_INTENSITY_SHADER_ID, this._displacementIntensity);
        quadMaterial.SetFloat(DISPLACEMENT_SPEED_SHADER_ID, this._displacementSpeed);
        quadMaterial.SetFloat(DISPLACEMENT_SCALE_SHADER_ID, this._displacementScale);
        quadMaterial.SetFloat("_ShellIndex", index);
        quadMaterial.SetFloat("_ShellsCount", this._count);
        quadMaterial.SetFloat("_StepMin", this._maskInitRandomStep);
        quadMaterial.SetFloat("_StepMax", this._maskLastRandomStep);
        
        quadInstance.GetComponent<MeshRenderer>().material = quadMaterial;

        return quadInstance;
    }

    #region UNITY METHODS
    private void Start()
    {
        this._dirty = true;
    }

    private void Update()
    {
        if (this._dirty)
        {
            this.Refresh();
            this._dirty = false;
        }
    }

    private void OnValidate()
    {
        this._dirty = true;
    }
    #endregion // UNITY METHODS
}
