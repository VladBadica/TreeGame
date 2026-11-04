using System.Numerics;

namespace TreeGame.Trees
{
    /// <summary>The raw data of a mesh, ready to hand to whatever draws it. Has no Unity dependency, so meshes can be checked on their own.</summary>
    public sealed class MeshData
    {
        internal MeshData(Vector3[] vertices, Vector3[] normals, Vector2[] uvs, int[] triangles)
        {
            Vertices = vertices;
            Normals = normals;
            Uvs = uvs;
            Triangles = triangles;
        }

        public Vector3[] Vertices { get; }

        public Vector3[] Normals { get; }

        public Vector2[] Uvs { get; }

        /// <summary>
        /// Three vertex indices per triangle, wound so a triangle (a, b, c) faces the way (b - a) × (c - a) points, which
        /// is the side Unity draws.
        /// </summary>
        public int[] Triangles { get; }
    }
}
