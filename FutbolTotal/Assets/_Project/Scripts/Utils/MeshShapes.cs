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
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly float height;
        private readonly float uvTileSize;

        /// <param name="uvTileSize">Metros que ocupa una repetición de la textura (UV planas en XZ).</param>
        public FlatMeshBuilder(float height, float uvTileSize = 4f)
        {
            this.height = height;
            this.uvTileSize = Mathf.Max(0.01f, uvTileSize);
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
            if (Vector3.Cross(b - a, c - a).y < 0f)
            {
                Vector3 swap = b;
                b = c;
                c = swap;
            }
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            uvs.Add(new Vector2(a.x, a.z) / uvTileSize);
            uvs.Add(new Vector2(b.x, b.z) / uvTileSize);
            uvs.Add(new Vector2(c.x, c.z) / uvTileSize);
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
            mesh.SetUVs(0, uvs);
            var normals = new List<Vector3>(vertices.Count);
            for (int i = 0; i < vertices.Count; i++) normals.Add(Vector3.up);
            mesh.SetNormals(normals);
            mesh.RecalculateBounds();
            return mesh;
        }

        private Vector3 V(Vector2 p) => new Vector3(p.x, height, p.y);
    }

    /// <summary>
    /// Constructor de mallas 3D sencillas (cajas, cuadriláteros). Las normales se calculan al final.
    /// Lo usan las gradas, la red y otros elementos generados por código.
    /// </summary>
    public sealed class MeshBuilder3D
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();

        public int VertexCount => vertices.Count;

        /// <summary>Cuadrilátero a-b-c-d (orden horario visto desde la cara visible).</summary>
        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(1f, 0f));
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        /// <summary>Caja alineada con los ejes (seis caras hacia fuera).</summary>
        public void AddBox(Vector3 center, Vector3 size)
        {
            Vector3 h = size * 0.5f;
            Vector3 p000 = center + new Vector3(-h.x, -h.y, -h.z), p100 = center + new Vector3(h.x, -h.y, -h.z);
            Vector3 p010 = center + new Vector3(-h.x, h.y, -h.z), p110 = center + new Vector3(h.x, h.y, -h.z);
            Vector3 p001 = center + new Vector3(-h.x, -h.y, h.z), p101 = center + new Vector3(h.x, -h.y, h.z);
            Vector3 p011 = center + new Vector3(-h.x, h.y, h.z), p111 = center + new Vector3(h.x, h.y, h.z);
            AddQuad(p000, p010, p110, p100); // -Z
            AddQuad(p101, p111, p011, p001); // +Z
            AddQuad(p001, p011, p010, p000); // -X
            AddQuad(p100, p110, p111, p101); // +X
            AddQuad(p010, p011, p111, p110); // +Y
            AddQuad(p001, p000, p100, p101); // -Y
        }

        /// <summary>Hilo fino entre dos puntos: dos tiras cruzadas para que se vea desde cualquier ángulo.</summary>
        public void AddStrand(Vector3 from, Vector3 to, Vector3 sideA, Vector3 sideB, float width)
        {
            Vector3 a = sideA.normalized * (width * 0.5f);
            Vector3 b = sideB.normalized * (width * 0.5f);
            AddQuad(from - a, to - a, to + a, from + a);
            AddQuad(from - b, to - b, to + b, from + b);
        }

        public Mesh ToMesh(Mesh mesh, string name)
        {
            if (mesh == null) mesh = new Mesh();
            mesh.Clear();
            mesh.name = name;
            mesh.indexFormat = vertices.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
