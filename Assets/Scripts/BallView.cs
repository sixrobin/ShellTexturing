namespace ShellTexturing
{
    using Sirenix.OdinInspector;
    using UnityEngine;

    public class BallView : ShellTexturedEntity
    {
        [SerializeField, Required, FoldoutGroup("References")]
        private BallPhysics _ballPhysics;

        [SerializeField, Min(0f), FoldoutGroup("Physics")]
        private float positionLerpSpeed = 1f;
        [SerializeField, Min(0f), FoldoutGroup("Physics")]
        private float angleLerpSpeed = 50f;
        
        private Vector3 _positionFollow;

        private void FollowPosition()
        {
            this.transform.position = this._ballPhysics.transform.position;

            for (int i = 0; i < this._shellMaterials.Length; ++i)
            {
                Material shellMaterial = this._shellMaterials[i];
                float percentage = i / (float)this._count;
                float lerpFactor = Time.deltaTime * this.positionLerpSpeed * (1f - percentage);

                this._positionFollow = Vector3.Lerp(this._positionFollow, this.transform.position, lerpFactor);

                shellMaterial.SetVector("_CurrentPosition", this.transform.position);
                shellMaterial.SetVector("_SmoothedPosition", this._positionFollow);
            }
        }
        
        private void FollowRotation()
        {
            // TODO: Try to move this to shader vertex function.
            
            Vector3 targetEulerAngles = this._ballPhysics.transform.eulerAngles;

            for (int i = 0; i < this._shellTransforms.Length; ++i)
            {
                Transform shellTransform = this._shellTransforms[i];
                float percentage = i / (float)this._count;
                float lerpFactor = Time.deltaTime * this.angleLerpSpeed * (1f - percentage);

                Vector3 shellEulerAngles = new(Mathf.LerpAngle(shellTransform.transform.eulerAngles.x, targetEulerAngles.x, lerpFactor),
                                               Mathf.LerpAngle(shellTransform.transform.eulerAngles.y, targetEulerAngles.y, lerpFactor),
                                               Mathf.LerpAngle(shellTransform.transform.eulerAngles.z, targetEulerAngles.z, lerpFactor));

                shellTransform.eulerAngles = shellEulerAngles;
            }
        }
        
        protected override void Update()
        {
            base.Update();
            
            this.FollowPosition();
            this.FollowRotation();
        }
    }
}