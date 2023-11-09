namespace ShellTexturing
{
    using Sirenix.OdinInspector;
    using UnityEngine;

    [RequireComponent(typeof(Rigidbody))]
    public class BallPhysics : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        private float _startForce = 10f;

        private Rigidbody _rigidbody;
        
        [Button]
        private void Launch()
        {
            this._rigidbody.velocity = Random.insideUnitSphere.normalized * this._startForce;
        }

        private void Awake()
        {
            this._rigidbody = this.GetComponent<Rigidbody>();
        }

        private void Start()
        {
            this.Launch();
        }
    }
}