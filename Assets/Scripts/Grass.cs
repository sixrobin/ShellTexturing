namespace ShellTexturing
{
    using Sirenix.OdinInspector;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class Grass : ShellTexturedEntity
    {
        private static readonly int GLOBAL_WIND_DIRECTION_ID = Shader.PropertyToID("_GlobalWindDirection");

        [SerializeField, Required, FoldoutGroup("References")]
        private Transform _globalWindDirectionTarget = null;
        
        [SerializeField, Range(0f, 1f), FoldoutGroup("Physics")]
        private float _contactHeightMultiplier = 1f;
        [SerializeField, Min(1f), FoldoutGroup("Physics")]
        private float _rippleRecoverSpeed = 1f;

        private int _nextRippleIndex;

        protected override (Transform, Material) GenerateLayer(int index)
        {
            (Transform shellTransform, Material shellMaterial) = base.GenerateLayer(index);
            
            for (int i = 0; i < Constants.RIPPLES_COUNT; ++i)
                shellMaterial.SetVector($"_Ripple{i + 1}", new Vector4(0f, 0f, 0f, 1000f));

            return (shellTransform, shellMaterial);
        }

        protected override void Update()
        {
            base.Update();
            
            Vector3 globalWindDirection = this._globalWindDirectionTarget.position;
            globalWindDirection.y = globalWindDirection.z;
            globalWindDirection *= 0.1f;
            Shader.SetGlobalVector(GLOBAL_WIND_DIRECTION_ID, globalWindDirection);
            
            // TODO: Do this in shader? Register last absolute Time and compute difference using something like (lastAbsolute + _Time)?
            foreach (Material shellLayer in this._shellMaterials)
            {
                for (int i = 0; i < Constants.RIPPLES_COUNT; ++i)
                {
                    Vector4 ripple = shellLayer.GetVector($"_Ripple{i + 1}");
                    ripple.w += Time.deltaTime * this._rippleRecoverSpeed;
                    shellLayer.SetVector($"_Ripple{i + 1}", ripple);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            Vector3 contactPoint = collision.GetContact(0).point;
            contactPoint += this.transform.up * (this._height * this._contactHeightMultiplier);
            
            foreach (Material shellLayer in this._shellMaterials)
            {
                int rippleIndex = ++this._nextRippleIndex % Constants.RIPPLES_COUNT;
                shellLayer.SetVector($"_Ripple{rippleIndex + 1}", new Vector4(contactPoint.x, contactPoint.y, contactPoint.z, 0f));
            }
        }
    }
}
