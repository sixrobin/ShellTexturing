namespace ShellTexturing
{
    using Sirenix.OdinInspector;
    using UnityEngine;

    public class ShellTexturedEntity : MonoBehaviour
    {
        private static readonly int MASK_ID = Shader.PropertyToID("_Mask");
        private static readonly int SHELL_INDEX_ID = Shader.PropertyToID("_ShellIndex");
        private static readonly int SHELLS_COUNT_ID = Shader.PropertyToID("_ShellsCount");
        private static readonly int SHELL_HEIGHT_ID = Shader.PropertyToID("_ShellHeight");
        private static readonly int HEIGHT_PERCENTAGE_ID = Shader.PropertyToID("_HeightPercentage");

        [SerializeField, FoldoutGroup("References")]
        private GameObject _shellLayerModel;
        [SerializeField, FoldoutGroup("References")]
        private Material _shellLayerMaterial;
        [SerializeField, FoldoutGroup("References")]
        private ComputeShader _randomComputeShader;
        
        [SerializeField, Min(32), FoldoutGroup("Settings")]
        private int _resolution = 32;
        [SerializeField, FoldoutGroup("Settings")]
        protected float _height;
        [SerializeField, FoldoutGroup("Settings")]
        private AnimationCurve _heightDistribution = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField, Min(2), FoldoutGroup("Settings")]
        protected int _count;
        
        protected Material[] _shellMaterials;
        protected Transform[] _shellTransforms;
        private RenderTexture _maskTexture;
        private bool _dirty;

        #region SHELL GENERATION METHODS
        [Button]
        [ContextMenu("Refresh")]
        protected virtual void Refresh()
        {
            if (this._shellLayerModel == null)
                return;

            for (int i = this.transform.childCount - 1; i >= 0; --i)
                Destroy(this.transform.GetChild(i).gameObject);
            
            this.GenerateLayers();
        }
        
        private RenderTexture GenerateMask()
        {
            this._resolution = Mathf.Min(this._resolution, Constants.SHELL_MASK_MAX_RESOLUTION);
            int resolution = this._resolution - this._resolution % 8;

            RenderTexture layerTexture = new(resolution, resolution, 0, RenderTextureFormat.ARGB32)
            {
                enableRandomWrite = true,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point,
            };
            
            this._randomComputeShader.SetTexture(0, "Result", layerTexture);
            this._randomComputeShader.Dispatch(0, resolution / 8, resolution / 8, 1);
            
            return layerTexture;
        }
        
        private void GenerateLayers()
        {
            this._maskTexture = this.GenerateMask();

            this._shellTransforms = new Transform[this._count];
            this._shellMaterials = new Material[this._count];
            
            for (int i = 0; i < this._count; ++i)
            {
                (Transform shellTransform, Material shellMaterial)  = this.GenerateLayer(i);
                this._shellTransforms[i] = shellTransform;
                this._shellMaterials[i] = shellMaterial;
            }
        }
        
        protected virtual (Transform, Material) GenerateLayer(int index)
        {
            float percentage = index / (float)(this._count - 1);
            float heightPercentage = this._heightDistribution.Evaluate(percentage);

            GameObject shellLayer = Instantiate(this._shellLayerModel, this.transform.position, this._shellLayerModel.transform.rotation, this.transform);
            Material layerMaterial = new(this._shellLayerMaterial);
            shellLayer.GetComponent<MeshRenderer>().material = layerMaterial;

            layerMaterial.SetTexture(MASK_ID, this._maskTexture);
            layerMaterial.SetFloat(SHELLS_COUNT_ID, this._count);
            layerMaterial.SetFloat(SHELL_HEIGHT_ID, this._height);
            layerMaterial.SetFloat(HEIGHT_PERCENTAGE_ID, heightPercentage);
            layerMaterial.SetFloat(SHELL_INDEX_ID, index);

            return (shellLayer.transform, layerMaterial);
        }
        #endregion // SHELL GENERATION METHODS

        #region UNITY METHODS
        private void Awake()
        {
            this.Refresh();
        }
        
        protected virtual void Update()
        {
            if (this._dirty)
            {
                this.Refresh();
                this._dirty = false;
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                this._dirty = true;
        }
        #endregion // UNITY METHODS
    }
}