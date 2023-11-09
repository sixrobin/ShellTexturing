namespace ShellTexturing
{
    using Sirenix.OdinInspector;
    using UnityEngine;

    public class BallView : ShellTexturedEntity
    {
        [SerializeField, Required, FoldoutGroup("References")]
        private BallPhysics _ballPhysics;

        [SerializeField, Required, FoldoutGroup("Physics")]
        private float angleLerpSpeed = 100f;

        private void FollowBallRotation()
        {
            // TODO: Try to move this to shader vertex function.
            
            this.transform.position = this._ballPhysics.transform.position;
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
            this.FollowBallRotation();
        }
    }
}