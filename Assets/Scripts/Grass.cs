namespace ShellTexturing
{
    using System.Linq;
    using UnityEngine;

    [RequireComponent(typeof(Shell))]
    public class Grass : MonoBehaviour
    {
        private const int RIPPLES_COUNT = 5;
        
        [SerializeField, Range(0f, 1f)]
        private float _contactHeightMultiplier = 1f;
        [SerializeField, Min(1f)]
        private float _rippleRecoverSpeed = 1f;

        private Shell _shell;
        private Material[] _shellLayers;
        private Vector3 _lastCollisionPoint;
        private int _nextRippleIndex;

        private void Start()
        {
            this._shell = this.GetComponent<Shell>();
            this._shellLayers = this.GetComponentsInChildren<MeshRenderer>().Select(o => o.sharedMaterial).ToArray();
        }

        private void Update()
        {            
            this._shellLayers = this.GetComponentsInChildren<MeshRenderer>().Select(o => o.sharedMaterial).ToArray();

            foreach (Material shellLayer in this._shellLayers)
            {
                for (int i = 0; i < RIPPLES_COUNT; ++i)
                {
                    Vector4 ripple = shellLayer.GetVector($"_Ripple{i + 1}");
                    ripple.w += Time.deltaTime * this._rippleRecoverSpeed;
                    shellLayer.SetVector($"_Ripple{i + 1}", ripple);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            this._lastCollisionPoint = collision.GetContact(0).point;
            this._lastCollisionPoint += this.transform.up * (this._shell.Height * this._contactHeightMultiplier);
            
            foreach (Material shellLayer in this._shellLayers)
            {
                int rippleIndex = ++this._nextRippleIndex % RIPPLES_COUNT;
                shellLayer.SetVector($"_Ripple{rippleIndex + 1}", new Vector4(this._lastCollisionPoint.x, this._lastCollisionPoint.y, this._lastCollisionPoint.z, 0f));
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(this._lastCollisionPoint, 0.1f);
        }
    }
}