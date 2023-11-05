using Sirenix.OdinInspector;
using UnityEngine;

public class Shell : MonoBehaviour
{
    private static readonly int MASK_ID = Shader.PropertyToID("_Mask");
    private static readonly int COLOR_MIN_ID = Shader.PropertyToID("_ColorMin");
    private static readonly int COLOR_MAX_ID = Shader.PropertyToID("_ColorMax");
    private static readonly int RADIUS_ID = Shader.PropertyToID("_Radius");
    private static readonly int DISPLACEMENT_ID = Shader.PropertyToID("_Displacement");
    private static readonly int DISPLACEMENT_INTENSITY_ID = Shader.PropertyToID("_DisplacementIntensity");
    private static readonly int DISPLACEMENT_SPEED_ID = Shader.PropertyToID("_DisplacementSpeed");
    private static readonly int DISPLACEMENT_SCALE_ID = Shader.PropertyToID("_DisplacementScale");
    private static readonly int LOCAL_OFFSET_ID = Shader.PropertyToID("_LocalOffset");
    private static readonly int LOCAL_OFFSET_INTENSITY_ID = Shader.PropertyToID("_LocalOffsetIntensity");
    private static readonly int GLOBAL_WIND_DIRECTION_ID = Shader.PropertyToID("_GlobalWindDirection");
    private static readonly int SHELL_INDEX_ID = Shader.PropertyToID("_ShellIndex");
    private static readonly int SHELLS_COUNT_ID = Shader.PropertyToID("_ShellsCount");
    private static readonly int STEP_MIN_ID = Shader.PropertyToID("_StepMin");
    private static readonly int STEP_MAX_ID = Shader.PropertyToID("_StepMax");
    private static readonly int HEIGHT_PERCENTAGE_ID = Shader.PropertyToID("_HeightPercentage");

    [SerializeField, FoldoutGroup("References")]
    private GameObject _quadPrefab;
    [SerializeField, FoldoutGroup("References")]
    private Material _shellLayerMaterial;
    [SerializeField, FoldoutGroup("References")]
    private ComputeShader _randomComputeShader;

    [SerializeField, Min(32), FoldoutGroup("Settings")]
    private int _resolution = 32;
    [SerializeField, FoldoutGroup("Settings")]
    private float _height;

    [SerializeField, FoldoutGroup("Settings")]
    private AnimationCurve _heightDistribution = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField, Min(2), FoldoutGroup("Settings")]
    private int _count;
    [SerializeField, Min(0f), FoldoutGroup("Settings")]
    private float _radius = 1f;
    [SerializeField, Range(0f, 1f), FoldoutGroup("Settings")]
    private float _maskInitRandomStep = 0.9f;
    [SerializeField, Range(0f, 1f), FoldoutGroup("Settings")]
    private float _maskLastRandomStep = 0.1f;
    
    [SerializeField, FoldoutGroup("Settings/Color")]
    private Color _downColor = Color.black;
    [SerializeField, FoldoutGroup("Settings/Color")]
    private Color _upColor = Color.white;

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

    private RenderTexture _maskTexture;
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
        this._maskTexture = this.GenerateMask();
        
        for (int i = 0; i < this._count; ++i)
            this.GenerateQuad(i);
    }

    private GameObject GenerateQuad(int index)
    {
        float percentage = index / (float)(this._count - 1);
        float heightPercentage = this._heightDistribution.Evaluate(percentage);
        float height = Mathf.Lerp(0f, this._height, heightPercentage);

        GameObject quadInstance = Instantiate(this._quadPrefab, Vector3.zero, this._quadPrefab.transform.rotation, this.transform);
        quadInstance.transform.Translate(Vector3.up * height, Space.World);

        Material quadMaterial = new(this._shellLayerMaterial);
        quadInstance.GetComponent<MeshRenderer>().material = quadMaterial;

        quadMaterial.SetTexture(MASK_ID, this._maskTexture);
        quadMaterial.SetColor(COLOR_MIN_ID, this._downColor);
        quadMaterial.SetColor(COLOR_MAX_ID, this._upColor);
        quadMaterial.SetFloat(RADIUS_ID, this._radius);
        quadMaterial.SetFloat(HEIGHT_PERCENTAGE_ID, heightPercentage);
        quadMaterial.SetTexture(DISPLACEMENT_ID, this._displacementTexture);
        quadMaterial.SetFloat(DISPLACEMENT_INTENSITY_ID, this._displacementIntensity);
        quadMaterial.SetFloat(DISPLACEMENT_SPEED_ID, this._displacementSpeed);
        quadMaterial.SetFloat(DISPLACEMENT_SCALE_ID, this._displacementScale);
        quadMaterial.SetTexture(LOCAL_OFFSET_ID, this._localOffsetTexture);
        quadMaterial.SetFloat(LOCAL_OFFSET_INTENSITY_ID, this._localOffsetIntensity);
        quadMaterial.SetFloat(SHELL_INDEX_ID, index);
        quadMaterial.SetFloat(SHELLS_COUNT_ID, this._count);
        quadMaterial.SetFloat(STEP_MIN_ID, this._maskInitRandomStep);
        quadMaterial.SetFloat(STEP_MAX_ID, this._maskLastRandomStep);

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
        Shader.SetGlobalVector(GLOBAL_WIND_DIRECTION_ID, globalWindDirection);
    }

    private void OnValidate()
    {
        this._dirty = true;
    }
    #endregion // UNITY METHODS
}
