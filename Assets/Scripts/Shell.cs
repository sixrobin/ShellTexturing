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
    private static readonly int LOCAL_OFFSET_SHADER_ID = Shader.PropertyToID("_LocalOffset");
    private static readonly int LOCAL_OFFSET_INTENSITY_SHADER_ID = Shader.PropertyToID("_LocalOffsetIntensity");
    private static readonly int GLOBAL_WIND_DIRECTION_SHADER_ID = Shader.PropertyToID("_GlobalWindDirection");
    private static readonly int SHELL_INDEX_ID = Shader.PropertyToID("_ShellIndex");
    private static readonly int SHELLS_COUNT_ID = Shader.PropertyToID("_ShellsCount");
    private static readonly int STEP_MIN_ID = Shader.PropertyToID("_StepMin");
    private static readonly int STEP_MAX_ID = Shader.PropertyToID("_StepMax");
    
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

    [SerializeField, FoldoutGroup("Settings/Local Offset")]
    private Texture2D _localOffsetTexture = null;
    [SerializeField, Min(0f), FoldoutGroup("Settings/Local Offset")]
    private float _localOffsetIntensity = 1f;
    
    [SerializeField, Required, FoldoutGroup("Settings/Global wind")]
    private Transform _globalWindDirectionTarget = null;

    private bool _dirty;

    private void Refresh()
    {
        if (this._quadPrefab == null)
            return;

        for (int i = this.transform.childCount - 1; i >= 0; --i)
            Destroy(this.transform.GetChild(i).gameObject);
        
        this.GenerateQuads();
    }

    private RenderTexture GenerateMask()
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
        
        for (int i = 0; i < this._count; ++i)
            this.GenerateQuad(i);
    }

    private GameObject GenerateQuad(int index)
    {
        float step = this._height / (this._count - 1);
        float height = step * index;

        GameObject quadInstance = Instantiate(this._quadPrefab, Vector3.zero, this._quadPrefab.transform.rotation, this.transform);
        quadInstance.transform.Translate(Vector3.up * height, Space.World);
        // [TODO] More spacing between lower planes, and less spacing between higher planes.

        float percentage = index / (float)(this._count - 1);
        Color color = Color.Lerp(this._downColor, this._upColor, this._colorGradientCurve.Evaluate(percentage));

        Material quadMaterial = new(this._shellLayerMaterial);
        quadMaterial.SetTexture(MASK_SHADER_ID, this.GenerateMask()); // [TODO] No need to regenerate mask for each quad.
        quadMaterial.SetColor(COLOR_SHADER_ID, color);
        quadMaterial.SetFloat(RADIUS_SHADER_ID, this._radius);
        quadMaterial.SetTexture(DISPLACEMENT_SHADER_ID, this._displacementTexture);
        quadMaterial.SetFloat(DISPLACEMENT_INTENSITY_SHADER_ID, this._displacementIntensity);
        quadMaterial.SetFloat(DISPLACEMENT_SPEED_SHADER_ID, this._displacementSpeed);
        quadMaterial.SetFloat(DISPLACEMENT_SCALE_SHADER_ID, this._displacementScale);
        
        quadMaterial.SetTexture(LOCAL_OFFSET_SHADER_ID, this._localOffsetTexture);
        quadMaterial.SetFloat(LOCAL_OFFSET_INTENSITY_SHADER_ID, this._localOffsetIntensity);
        
        quadMaterial.SetFloat(SHELL_INDEX_ID, index);
        quadMaterial.SetFloat(SHELLS_COUNT_ID, this._count);
        quadMaterial.SetFloat(STEP_MIN_ID, this._maskInitRandomStep);
        quadMaterial.SetFloat(STEP_MAX_ID, this._maskLastRandomStep);
        
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
        
        Vector3 globalWindDirection = this._globalWindDirectionTarget.position;
        globalWindDirection.y = globalWindDirection.z;
        globalWindDirection *= 0.1f;
        Shader.SetGlobalVector(GLOBAL_WIND_DIRECTION_SHADER_ID, globalWindDirection);
    }

    private void OnValidate()
    {
        this._dirty = true;
    }
    #endregion // UNITY METHODS
}
