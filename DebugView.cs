using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace ReedsCrabUtils
{
    public class DebugView
    {
        public enum ColVis
        {
            All,
            NoTriggers,
            OnlyTriggers,
            COUNT,
        }

        public float Alpha = 0.3f;
        public ColVis ColliderVisibility = ColVis.All;
        public bool ShowNames = false;

        // InitMeshes() needs to be called on start
        // PollMeshes() needs to be called on update
        private Dictionary<int, Mesh> _cachedTerrainMeshes = new Dictionary<int, Mesh>();
        private const float MESH_DES_TIME = 0.5f;

        ~DebugView()
        {
            Object.Destroy(MeshMaterial);
            Object.Destroy(MeshHullMaterial);
            Object.Destroy(BoxMaterial);
            Object.Destroy(SphereMaterial);
            Object.Destroy(TerrainMaterial);
            Object.Destroy(CapsuleMaterial);

            Object.Destroy(SphereMesh);
            Object.Destroy(SubdivPlaneMesh);
            Object.Destroy(BoxMesh);
            Object.Destroy(CylinderMesh);
        }

        public readonly struct RenderNameData
        {
            public readonly string Name;
            public readonly Vector3 WorldPosition;

            public RenderNameData(string name, Vector3 worldPosition)
            {
                Name = name;
                WorldPosition = worldPosition;
            }

            // Must be called from OnGUI!
            public void Render(Camera worldCam, Vector2 screenSize)
            {
                var screenPosition = worldCam.WorldToScreenPoint(WorldPosition);
                if (screenPosition.z < 0)
                {
                    // Cull everything behind the player because it gets annoying
                    return;
                }

                // Also reverse y position
                GUI.Label(new Rect(screenPosition.x, screenSize.y - screenPosition.y, 200f, 30f), Name);
            }
        }

        public List<RenderNameData> RenderNameDatas = new List<RenderNameData>();

        public void PollMeshes(Transform overlapSource)
        {
            if (ShowNames)
            {
                RenderNameDatas.Clear();
            }

            var colResults = Physics.OverlapSphere(overlapSource.position, 100);
            foreach (var colResult in colResults)
            {
                RenderMesh(colResult.gameObject);

                if (ShowNames)
                {
                    RenderNameDatas.Add(new RenderNameData(colResult.name, colResult.transform.position));
                }
            }
        }

        public void InitMeshes()
        {
            _cachedTerrainMeshes.Clear();
            GenerateBaseMeshes();

            var shader = Mod.ModInstance.CustomBundle.LoadAsset<Shader>("Wireframe");

            MeshMaterial = new Material(shader);
            //MeshMaterial.color = meshColor;
            //MeshMaterial.SetVector("_FaceColor", meshColor);

            MeshHullMaterial = new Material(shader);
            //MeshHullMaterial.color = convexColor;
            //MeshHullMaterial.SetVector("_FaceColor", convexColor);

            BoxMaterial = new Material(shader);
            //BoxMaterial.color = boxColor;
            //BoxMaterial.SetVector("_FaceColor", boxColor);

            SphereMaterial = new Material(shader);
            //SphereMaterial.color = sphereColor;
            //SphereMaterial.SetVector("_FaceColor", sphereColor);

            TerrainMaterial = new Material(shader);
            //TerrainMaterial.color = terrainColor;
            //TerrainMaterial.SetVector("_FaceColor", terrainColor);

            CapsuleMaterial = new Material(shader);
            //CapsuleMaterial.color = capColor;
            //CapsuleMaterial.SetVector("_FaceColor", capColor);

            SetMaterialColors();
        }

        public void SetMaterialColors()
        {
            float localA = 1;
            Vector4 meshColor = new Vector4(0f, 1f, 0f, localA);
            Vector4 convexColor = new Vector4(0.5f, 0f, 0.5f, localA);
            Vector4 terrainColor = new Vector4(1f, 0f, 0f, localA);
            Vector4 boxColor = new Vector4(0f, 0f, 1f, localA);
            Vector4 capColor = new Vector4(0.5f, 0.6f, 0.2f, localA);
            Vector4 sphereColor = new Vector4(0.2f, 0.2f, 0.8f, localA);

            MeshMaterial.color = meshColor;
            MeshHullMaterial.color = convexColor;
            BoxMaterial.color = boxColor;
            SphereMaterial.color = sphereColor;
            TerrainMaterial.color = terrainColor;
            CapsuleMaterial.color = capColor;
        }

        public Material MeshMaterial;
        public Material MeshHullMaterial;
        public Material BoxMaterial;
        public Material SphereMaterial;
        public Material TerrainMaterial;
        public Material CapsuleMaterial;

        public void RenderMesh(GameObject sourceObject)
        {
            if (!sourceObject.TryGetComponent(out Collider c) ||
                (ColliderVisibility == ColVis.NoTriggers && c.isTrigger) ||
                (ColliderVisibility == ColVis.OnlyTriggers && !c.isTrigger) || 
                c.enabled == false)
            {
                return;
            }

            if (c is MeshCollider meshCol)
            {
                Graphics.DrawMesh(meshCol.sharedMesh, meshCol.transform.localToWorldMatrix, MeshMaterial,
                    Mod.ModInstance.DbgLayer);
            }
            else if (c is BoxCollider boxCol)
            {
                var boxMesh = GetMeshCopy(BoxMesh);
                var vertCopy = boxMesh.vertices;

                for (int j = 0; j < boxMesh.vertices.Length; j++)
                {
                    vertCopy[j] = Vector3.Scale(vertCopy[j], boxCol.size);
                    vertCopy[j] = Vector3.Scale(vertCopy[j], boxCol.transform.lossyScale);
                }

                boxMesh.vertices = vertCopy;

                boxMesh.RecalculateBounds();
                Graphics.DrawMesh(boxMesh, boxCol.bounds.center, boxCol.transform.rotation, BoxMaterial,
                    Mod.ModInstance.DbgLayer);
                GameObject.Destroy(boxMesh, MESH_DES_TIME);
            }
            else if (c is TerrainCollider tCol && sourceObject.TryGetComponent(out Terrain terrain))
            {
                if (!_cachedTerrainMeshes.TryGetValue(tCol.GetInstanceID(), out var terrainMesh))
                {
                    // gotta build it :(
                    var data = tCol.terrainData;
                    var bounds = data.bounds;

                    terrainMesh = GetMeshCopy(SubdivPlaneMesh);

                    var terrainPos = terrain.GetPosition();

                    var vertCopy = terrainMesh.vertices;
                    var count = vertCopy.Length;

                    for (var i = 0; i < count; i++)
                    {
                        var newVal = vertCopy[i];
                        newVal = Vector3.Scale(newVal, bounds.size);
                        var worldPos = terrainPos + newVal;
                        newVal.y = terrain.SampleHeight(worldPos);
                        vertCopy[i] = newVal;
                    }

                    terrainMesh.vertices = vertCopy;
                    terrainMesh.RecalculateBounds();
                    terrainMesh.RecalculateNormals();

                    Debug.Log($"[ReedsCrabUtils] Generated terrain mesh {tCol.GetInstanceID()}");
                    _cachedTerrainMeshes.Add(tCol.GetInstanceID(), terrainMesh);
                }

                Graphics.DrawMesh(terrainMesh, tCol.transform.localToWorldMatrix, TerrainMaterial,
                    Mod.ModInstance.DbgLayer);
            }
            else if (c is SphereCollider sphereCol)
            {
                var sphereMesh = GetMeshCopy(SphereMesh);
                var vertCopy = sphereMesh.vertices;
                for (var i = 0; i < vertCopy.Length; i++)
                {
                    vertCopy[i] *= sphereCol.radius;
                }

                sphereMesh.vertices = vertCopy;

                Graphics.DrawMesh(sphereMesh, sphereCol.transform.localToWorldMatrix, SphereMaterial,
                    Mod.ModInstance.DbgLayer);
                GameObject.Destroy(sphereMesh, MESH_DES_TIME);
            }
            else if (c is CapsuleCollider capCol)
            {
                var scale = capCol.transform.lossyScale;
                //var largestAxisScale = Mathf.Max(scale.x, scale.z);

                var cylMesh = GetMeshCopy(CylinderMesh);
                var vertCopy = cylMesh.vertices;
                var scaleVec = new Vector3(capCol.radius, capCol.height, capCol.radius);
                for (var i = 0; i < vertCopy.Length; i++)
                {
                    vertCopy[i] = Vector3.Scale(vertCopy[i], scaleVec);

                    if (capCol.direction == 0)
                    {
                        vertCopy[i] = RotatePointAroundAxis(vertCopy[i], 90, Vector3.forward, Vector3.zero);
                    }
                    else if (capCol.direction == 2)
                    {
                        vertCopy[i] = RotatePointAroundAxis(vertCopy[i], 90, Vector3.right, Vector3.zero);
                    }

                    vertCopy[i] += capCol.center;
                }

                //var lowerPoint = vertCopy[vertCopy.Length - 2];
                //vertCopy[vertCopy.Length - 2] = new Vector3(lowerPoint.x, -capCol.height / 2f, lowerPoint.z); 

                //var upper = vertCopy[vertCopy.Length - 1];
                //vertCopy[vertCopy.Length - 1] = new Vector3(upper.x, capCol.height / 2f, upper.z); 

                cylMesh.vertices = vertCopy;

                /*
                var scaledHeight = capCol.height * scale.y;

                var sph0Mesh = GetMeshCopy(SphereMesh);
                var vertCopySph0 = sph0Mesh.vertices;
                var offset = Vector3.up * (scaledHeight / 2f - largestAxisScale / 2f);
                for (var i = 0; i < vertCopySph0.Length; i++)
                {
                    vertCopySph0[i] *= capCol.radius * largestAxisScale;
                    vertCopySph0[i] -= offset;
                    vertCopySph0[i] += capCol.center;
                }

                sph0Mesh.vertices = vertCopySph0;

                var sph1Mesh = GetMeshCopy(SphereMesh);
                var vertCopySph1 = sph1Mesh.vertices;
                for (var i = 0; i < vertCopySph1.Length; i++)
                {
                    vertCopySph1[i] *= capCol.radius * largestAxisScale;
                    vertCopySph1[i] += offset;
                    vertCopySph1[i] += capCol.center;
                }

                sph1Mesh.vertices = vertCopySph1;
                */

                // Scaling around the non-aligned axis still has funky behaviour, since scaling capsules has to result in uniform radius caps
                Graphics.DrawMesh(cylMesh, capCol.transform.localToWorldMatrix, CapsuleMaterial,
                    Mod.ModInstance.DbgLayer);
                //Graphics.DrawMesh(sph0Mesh, capCol.transform.position, Quaternion.identity, CapsuleMaterial, Mod.ModInstance.DbgLayer);
                //Graphics.DrawMesh(sph1Mesh, capCol.transform.position, Quaternion.identity, CapsuleMaterial, Mod.ModInstance.DbgLayer);

                GameObject.Destroy(cylMesh, MESH_DES_TIME);
                //GameObject.Destroy(sph0Mesh, MESH_DES_TIME);
                //GameObject.Destroy(sph1Mesh, MESH_DES_TIME);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector3 RotatePointAroundAxis(Vector3 point, float angle, Vector3 axis, Vector3 pivot)
        {
            // Translate to origin of rotation
            point -= pivot;

            // Perform rotation
            Quaternion rotation = Quaternion.AngleAxis(angle, axis);
            point = rotation * point;

            // Translate back to original position
            point += pivot;

            return point;
        }

        // Base meshes so we don't have to rebuild them
        public Mesh SphereMesh;
        public Mesh SubdivPlaneMesh;
        public Mesh BoxMesh;
        public Mesh CylinderMesh;

        public Mesh GetMeshCopy(Mesh baseMesh)
        {
            return new Mesh()
            {
                // THIS NEEDS TO BE SET FIRST: CLEARS DATA
                indexFormat = baseMesh.indexFormat,
                vertices = baseMesh.vertices.ToArray(),
                triangles = baseMesh.triangles.ToArray(),
            };
        }

        public void GenerateBaseMeshes()
        {
            // Plane
            if (SubdivPlaneMesh == null)
            {
                var numVerticesX = 1000;
                var numVerticesZ = 1000;

                // Generate subdivided plane
                var vertices = new List<Vector3>();
                for (int y = 0; y < numVerticesZ; y++)
                {
                    for (int x = 0; x < numVerticesX; x++)
                    {
                        float tx = x / (float)(numVerticesX - 1);
                        float ty = y / (float)(numVerticesZ - 1);

                        vertices.Add(new Vector3(tx, 0, ty));
                    }
                }

                var indices = new List<int>();

                for (int y = 0; y < (numVerticesZ - 1); y++)
                {
                    for (int x = 0; x < (numVerticesX - 1); x++)
                    {
                        int quad = y * numVerticesX + x;

                        indices.Add(quad);
                        indices.Add(quad + numVerticesX);
                        indices.Add(quad + numVerticesX + 1);

                        indices.Add(quad);
                        indices.Add(quad + numVerticesX + 1);
                        indices.Add(quad + 1);
                    }
                }

                SubdivPlaneMesh = new Mesh();
                SubdivPlaneMesh.indexFormat = IndexFormat.UInt32;
                SubdivPlaneMesh.vertices = vertices.ToArray();
                SubdivPlaneMesh.triangles = indices.ToArray();
            }

            // Cube
            if (BoxMesh == null)
            {
                Vector3[] verts =
                {
                    new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
                    new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
                    new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f)
                };

                var triangles = new[]
                {
                    0, 2, 1, 0, 3, 2, 2, 3, 4, 2, 4, 5, 1, 2, 5, 1, 5, 6, 0, 7, 4, 0, 4, 3, 5, 4, 7, 5, 7, 6, 0, 6,
                    7, 0, 1, 6
                };

                BoxMesh = new Mesh();
                BoxMesh.vertices = verts;
                BoxMesh.triangles = triangles;
                BoxMesh.RecalculateNormals();
            }

            // sphere
            if (SphereMesh == null)
            {
                int resolution = 10;
                float radius = 1f;

                Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
                Vector2[] uv = new Vector2[vertices.Length];
                int[] triangles = new int[resolution * resolution * 6];

                for (int i = 0; i <= resolution; i++)
                {
                    for (int j = 0; j <= resolution; j++)
                    {
                        float u = (float)j / resolution;
                        float v = (float)i / resolution;
                        float x = radius * Mathf.Sin(v * Mathf.PI) * Mathf.Cos(u * 2 * Mathf.PI);
                        float y = radius * Mathf.Sin(v * Mathf.PI) * Mathf.Sin(u * 2 * Mathf.PI);
                        float z = radius * Mathf.Cos(v * Mathf.PI);
                        vertices[i * (resolution + 1) + j] = new Vector3(x, y, z);
                        uv[i * (resolution + 1) + j] = new Vector2(u, v);
                    }
                }

                for (int i = 0; i < resolution; i++)
                {
                    for (int j = 0; j < resolution; j++)
                    {
                        triangles[6 * (i * resolution + j) + 0] = i * (resolution + 1) + j;
                        triangles[6 * (i * resolution + j) + 1] = i * (resolution + 1) + j + 1;
                        triangles[6 * (i * resolution + j) + 2] = (i + 1) * (resolution + 1) + j;
                        triangles[6 * (i * resolution + j) + 3] = i * (resolution + 1) + j + 1;
                        triangles[6 * (i * resolution + j) + 4] = (i + 1) * (resolution + 1) + j + 1;
                        triangles[6 * (i * resolution + j) + 5] = (i + 1) * (resolution + 1) + j;
                    }
                }

                // oops
                Array.Reverse(triangles);

                Vector3[] normals = new Vector3[vertices.Length];
                for (var vert = 0; vert < vertices.Length; vert++)
                {
                    normals[vert] = vertices[vert].normalized;
                }

                SphereMesh = new Mesh();
                SphereMesh.normals = normals;
                SphereMesh.vertices = vertices;
                SphereMesh.triangles = triangles;
            }

            if (CylinderMesh == null)
            {
                int resolution = 10;
                float height = 1f;

                Vector3[] vertices = new Vector3[resolution * 2 + 2];
                List<int> triangles = new List<int>();

                // Vert layout
                // [0..resolution] lower disk, exclusive
                // [resolution..2 * resolution] // upper disk, exclusive
                // [resolution * 2, resolution * 2 + 1] // lower, upper caps

                // lower disc
                var increment = (Mathf.PI * 2f) / resolution;

                for (var i = 0; i < resolution; i++)
                {
                    var theta = i * increment;
                    var x = Mathf.Cos(theta);
                    var z = Mathf.Sin(theta);
                    var y = -height / 2f;

                    vertices[i] = new Vector3(x, y, z);
                }

                for (var i = resolution; i < resolution * 2; i++)
                {
                    var theta = i * increment;
                    var x = Mathf.Cos(theta);
                    var z = Mathf.Sin(theta);
                    var y = height / 2f;

                    vertices[i] = new Vector3(x, y, z);
                }

                vertices[resolution * 2] = new Vector3(0, -height / 2f, 0f);
                vertices[resolution * 2 + 1] = new Vector3(0, height / 2f, 0f);

                // connect 'em with tris
                var toTopOffset = resolution;
                for (var i = 0; i < resolution; i++)
                {
                    triangles.Add(i);
                    triangles.Add(i + toTopOffset);
                    triangles.Add((i + 1) % (resolution));

                    triangles.Add(i + toTopOffset);
                    triangles.Add((i + 1 + toTopOffset) >= resolution * 2 ? toTopOffset : (i + 1 + toTopOffset));
                    triangles.Add((i + 1) % resolution);
                }

                for (var i = 0; i < resolution; i++)
                {
                    var index = i;
                    var nextIndex = (i + 1) % resolution;

                    triangles.Add(nextIndex);
                    triangles.Add(resolution * 2);
                    triangles.Add(index);

                    index = i + resolution;
                    nextIndex = (nextIndex + resolution) >= resolution * 2 ? resolution + 1 : (nextIndex + resolution);

                    triangles.Add(index);
                    triangles.Add(resolution * 2 + 1);
                    triangles.Add(nextIndex);
                }

                CylinderMesh = new Mesh();
                CylinderMesh.vertices = vertices;
                CylinderMesh.triangles = triangles.ToArray();
                CylinderMesh.Optimize();
                CylinderMesh.RecalculateNormals();
                CylinderMesh.RecalculateBounds();
            }
        }
    }
}