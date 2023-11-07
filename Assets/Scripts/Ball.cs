namespace ShellTexturing
{
    using Sirenix.OdinInspector;
    using UnityEngine;

    [RequireComponent(typeof(Rigidbody))]
    public class Ball : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        private float _startForce = 10f;

        [Button]
        private void Launch()
        {
            Vector3 direction = Random.insideUnitSphere.normalized;
            this.GetComponent<Rigidbody>().velocity = direction * this._startForce;
        }
        
        private void Start()
        {
            this.Launch();
        }
    }
}