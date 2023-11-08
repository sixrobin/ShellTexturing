namespace ShellTexturing
{
    using Sirenix.OdinInspector;
    using UnityEngine;

    [DisallowMultipleComponent]
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
        private static readonly int SHELL_HEIGHT_ID = Shader.PropertyToID("_ShellHeight");
        private static readonly int STEP_MIN_ID = Shader.PropertyToID("_StepMin");
        private static readonly int STEP_MAX_ID = Shader.PropertyToID("_StepMax");
        private static readonly int HEIGHT_PERCENTAGE_ID = Shader.PropertyToID("_HeightPercentage");
        private static readonly int HEIGHT_SPACE_PERCENTAGE_ID = Shader.PropertyToID("_HeightSpacePercentage");
        private static readonly int GRAVITY_ID = Shader.PropertyToID("_Gravity");
        private static readonly int RIPPLE_DURATION_ID = Shader.PropertyToID("_RippleDuration");
        private static readonly int RIPPLE_CIRCLE_SMOOTHING_ID = Shader.PropertyToID("_RippleCircleSmoothing");
        private static readonly int RIPPLE_RING_SMOOTHING_ID = Shader.PropertyToID("_RippleRingSmoothing");
        private static readonly int RIPPLE_RADIUS_MULTIPLIER_ID = Shader.PropertyToID("_RippleRadiusMultiplier");
        private static readonly int RIPPLE_INTENSITY_MULTIPLIER_ID = Shader.PropertyToID("_RippleIntensityMultiplier");
        
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
        [SerializeField, Range(0f, 1f), FoldoutGroup("Settings")]
        private float _heightSpacePercentage = 0f;
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

        [SerializeField, FoldoutGroup("Settings/Gravity")]
        private float _gravity = 0f;

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

        [SerializeField, Min(0f), FoldoutGroup("Settings/Ripple")]
        private float _rippleDuration = 1f;
        [SerializeField, Min(0f), FoldoutGroup("Settings/Ripple")]
        private float _rippleCircleSmoothing = 0f;
        [SerializeField, Min(0f), FoldoutGroup("Settings/Ripple")]
        private float _rippleRingSmoothing = 0f;
        [SerializeField, Min(0f), FoldoutGroup("Settings/Ripple")]
        private float _rippleRadiusMultiplier = 1f;
        [SerializeField, Min(0f), FoldoutGroup("Settings/Ripple")]
        private float _rippleIntensityMultiplier = 1f;
        
        private RenderTexture _maskTexture;
        private bool _dirty;

        public float Height => this._height;

        [Button]
        [ContextMenu("Refresh")]
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
            
            // TODO: When resolution is not a multiple of 8, a visual issue occurs.
            // Either fix properly or clamp/round resolution to multiple of 8.
            
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

            GameObject quadInstance = Instantiate(this._quadPrefab, this.transform.position, this._quadPrefab.transform.rotation, this.transform);
            Material quadMaterial = new(this._shellLayerMaterial);
            quadInstance.GetComponent<MeshRenderer>().material = quadMaterial;

            quadMaterial.SetTexture(MASK_ID, this._maskTexture);
            quadMaterial.SetColor(COLOR_MIN_ID, this._downColor);
            quadMaterial.SetColor(COLOR_MAX_ID, this._upColor);
            quadMaterial.SetFloat(RADIUS_ID, this._radius);
            quadMaterial.SetFloat(HEIGHT_PERCENTAGE_ID, heightPercentage);
            quadMaterial.SetFloat(HEIGHT_SPACE_PERCENTAGE_ID, this._heightSpacePercentage);
            quadMaterial.SetTexture(DISPLACEMENT_ID, this._displacementTexture);
            quadMaterial.SetFloat(DISPLACEMENT_INTENSITY_ID, this._displacementIntensity);
            quadMaterial.SetFloat(DISPLACEMENT_SPEED_ID, this._displacementSpeed);
            quadMaterial.SetFloat(DISPLACEMENT_SCALE_ID, this._displacementScale);
            quadMaterial.SetTexture(LOCAL_OFFSET_ID, this._localOffsetTexture);
            quadMaterial.SetFloat(LOCAL_OFFSET_INTENSITY_ID, this._localOffsetIntensity);
            quadMaterial.SetFloat(SHELL_INDEX_ID, index);
            quadMaterial.SetFloat(SHELLS_COUNT_ID, this._count);
            quadMaterial.SetFloat(SHELL_HEIGHT_ID, this._height);
            quadMaterial.SetFloat(STEP_MIN_ID, this._maskInitRandomStep);
            quadMaterial.SetFloat(STEP_MAX_ID, this._maskLastRandomStep);
            quadMaterial.SetFloat(GRAVITY_ID, this._gravity);
            quadMaterial.SetFloat(RIPPLE_DURATION_ID, this._rippleDuration);
            quadMaterial.SetFloat(RIPPLE_CIRCLE_SMOOTHING_ID, this._rippleCircleSmoothing);
            quadMaterial.SetFloat(RIPPLE_RING_SMOOTHING_ID, this._rippleRingSmoothing);
            quadMaterial.SetFloat(RIPPLE_RADIUS_MULTIPLIER_ID, this._rippleRadiusMultiplier);
            quadMaterial.SetFloat(RIPPLE_INTENSITY_MULTIPLIER_ID, this._rippleIntensityMultiplier);
            
            quadMaterial.SetVector("_Ripple1", new Vector4(0f, 0f, 0f, 1000f));
            quadMaterial.SetVector("_Ripple2", new Vector4(0f, 0f, 0f, 1000f));
            quadMaterial.SetVector("_Ripple3", new Vector4(0f, 0f, 0f, 1000f));
            quadMaterial.SetVector("_Ripple4", new Vector4(0f, 0f, 0f, 1000f));
            quadMaterial.SetVector("_Ripple5", new Vector4(0f, 0f, 0f, 1000f));

            return quadInstance;
        }

        #region UNITY METHODS
        private void Awake()
        {
            this.Refresh();
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
}
