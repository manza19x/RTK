using System.Collections.Generic;
using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Generación de geometría plana sobre el césped (líneas, círculos, indicadores).
    /// Todas las formas miran hacia arriba (+Y).
    /// </summary>
    public sealed class FlatMeshBuilder
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();
        private readonly float height;

        public FlatMeshBuilder(float height)
        {
            this.height = height;
        }

        /// <summary>Rectángulo alineado con los ejes, dado por sus límites en X y Z.</summary>
        public void AddRect(float minX, float maxX, float minZ, float maxZ)
        {
            AddQuad(new Vector3(minX, height, minZ), new Vector3(minX, height, maxZ),
                    new Vector3(maxX, height, maxZ), new Vector3(maxX, height, minZ));
        }

        /// <summary>Línea recta de grosor <paramref name="width"/> entre dos puntos del plano.</summary>
        public void AddLine(Vector2 from, Vector2 to, float width)
        {
            Vector2 dir = (to - from).normalized;
            Vector2 side = new Vector2(-dir.y, dir.x) * (width * 0.5f);
            AddQuad(V(from - side), V(from + side), V(to + side), V(to - side));
        }

        /// <summary>Arco (o anillo completo) centrado en <paramref name="center"/>. Ángulos en grados, 0 = +X, 90 = +Z.</summary>
        public void AddArc(Vector2 center, float radius, float width, float startDegrees, float endDegrees, int segments)
        {
            float inner = Mathf.Max(0f, radius - width * 0.5f);
            float outer = radius + width * 0.5f;
            segments = Mathf.Max(2, segments);
            for (int i = 0; i < segments; i++)
            {
                float a0 = Mathf.Lerp(startDegrees, endDegrees, i / (float)segments) * Mathf.Deg2Rad;
                float a1 = Mathf.Lerp(startDegrees, endDegrees, (i + 1) / (float)segments) * Mathf.Deg2Rad;
                Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
                Vector2 d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
                AddQuad(V(center + d0 * inner), V(center + d0 * outer), V(center + d1 * outer), V(center + d1 * inner));
            }
        }

        /// <summary>Círculo relleno.</summary>
        public void AddDisc(Vector2 center, float radius, int segments)
        {
            segments = Mathf.Max(3, segments);
            for (int i = 0; i < segments; i++)
            {
                float a0 = i / (float)segments * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
                AddTriangle(V(center),
                            V(center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius),
                            V(center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius));
            }
        }

        /// <summary>Triángulo; se corrige el orden de vértices para que siempre mire hacia arriba.</summary>
        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            int start = vertices.Count;
            vertices.Add(a);
            if (Vector3.Cross(b - a, c - a).y >= 0f)
            {
                vertices.Add(b);
                vertices.Add(c);
            }
            else
            {
                vertices.Add(c);
                vertices.Add(b);
            }
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddTriangle(a, b, c);
            AddTriangle(a, c, d);
        }

        /// <summary>Vuelca la geometría en <paramref name="mesh"/> (se crea uno nuevo si es null).</summary>
        public Mesh ToMesh(Mesh mesh, string name)
        {
            if (mesh == null) mesh = new Mesh();
            mesh.Clear();
            mesh.name = name;
            mesh.indexFormat = vertices.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            var normals = new List<Vector3>(vertices.Count);
            for (int i = 0; i < vertices.Count; i++) normals.Add(Vector3.up);
            mesh.SetNormals(normals);
            mesh.RecalculateBounds();
            return mesh;
        }

        private Vector3 V(Vector2 p) => new Vector3(p.x, height, p.y);
    }
}
