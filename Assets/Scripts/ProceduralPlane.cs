namespace ShellTexturing
{
    using Sirenix.OdinInspector;
    using UnityEngine;

    /// <summary>
    /// Generate a plane with a given density and some options to add random elevation.
    /// Base code from https://catlikecoding.com/unity/tutorials/procedural-grid/
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ProceduralPlane : MonoBehaviour
    {
        [SerializeField, FoldoutGroup("Settings")]
        private Vector2Int _density;
        [SerializeField, FoldoutGroup("Settings")]
        private float _scale;

        [SerializeField, FoldoutGroup("Settings/Elevation")]
        private Texture2D _elevationMap;
        [SerializeField, Min(0), FoldoutGroup("Settings/Elevation")]
        private int _elevationScale = 1;
        [SerializeField, FoldoutGroup("Settings/Elevation")]
        private float _elevationIntensity = 1f;

        private Vector3[] _vertices;
        private Mesh _mesh;

        private void Generate()
        {
            this._mesh = new Mesh { name = "Procedural Grid" };
            this.GetComponent<MeshFilter>().mesh = this._mesh;

            this._vertices = new Vector3[(this._density.x + 1) * (this._density.y + 1)];
            Vector2[] uv = new Vector2[this._vertices.Length];
            Vector4[] tangents = new Vector4[this._vertices.Length];
            int[] triangles = new int[this._density.x * this._density.y * 6];
            
            for (int i = 0, y = 0; y <= this._density.y; y++)
            {
                for (int x = 0; x <= this._density.x; x++, i++)
                {
                    float height = (this._elevationMap.GetPixel(x * this._elevationScale, y * this._elevationScale).r - 0.5f) * 2f * this._elevationIntensity;
                    
                    float posX = x * this._scale - (this._density.x * this._scale) * 0.5f;
                    float posZ = y * this._scale - (this._density.y * this._scale) * 0.5f;;
                    this._vertices[i] = new Vector3(posX, height, posZ);
                    
                    uv[i] = new Vector2(x / (float)this._density.x, y / (float)this._density.y);
                    tangents[i] = new Vector4(1f, 0f, 0f, -1f);
                }
            }

            for (int ti = 0, vi = 0, y = 0; y < this._density.y; y++, vi++)
            {
                for (int x = 0; x < this._density.x; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                    triangles[ti + 4] = triangles[ti + 1] = vi + this._density.x + 1;
                    triangles[ti + 5] = vi + this._density.x + 2;
                }
            }
            
            this._mesh.vertices = this._vertices;
            this._mesh.triangles = triangles;
            this._mesh.uv = uv;
            this._mesh.RecalculateNormals();
        }
        
        private void Awake()
        {
            this.Generate();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                this.Generate();
        }
    }
}