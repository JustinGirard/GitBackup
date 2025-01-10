using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

namespace FXV.ShieldEditorUtils
{
    [InitializeOnLoad]
    public class fxvEditorMeshUtils : MonoBehaviour
    {
        static Type type_HandleUtility;
        static MethodInfo meth_IntersectRayMesh;

        static fxvEditorMeshUtils()
        {
            var editorTypes = typeof(Editor).Assembly.GetTypes();

            type_HandleUtility = editorTypes.FirstOrDefault(t => t.Name == "HandleUtility");
            meth_IntersectRayMesh = type_HandleUtility.GetMethod("IntersectRayMesh",
                                                                  BindingFlags.Static | BindingFlags.NonPublic);
        }

        public static bool IntersectRayMesh(Ray ray, MeshFilter meshFilter, out RaycastHit hit)
        {
            return IntersectRayMesh(ray, meshFilter.mesh, meshFilter.transform.localToWorldMatrix, out hit);
        }

        public static bool IntersectRayMesh(Ray ray, Mesh mesh, out RaycastHit hit)
        {
            return IntersectRayMesh(ray, mesh, Matrix4x4.identity, out hit);
        }

        public static bool IntersectRayMesh(Ray ray, Mesh mesh, Matrix4x4 matrix, out RaycastHit hit)
        {
            var parameters = new object[] { ray, mesh, matrix, null };
            bool result = (bool)meth_IntersectRayMesh.Invoke(null, parameters);
            hit = (RaycastHit)parameters[3];
            return result;
        }

        public static Vector2 ClampUV(Vector2 uv)
        {
            return new Vector2(uv.x - Mathf.Floor(uv.x), uv.y - Mathf.Floor(uv.y));
        }
        public static float ClampUV(float uv)
        {
            return uv - Mathf.Floor(uv);
        }

        public static Color SampleTextureTriplanar(Texture2D tex, Vector3 pos, Vector3 normal, float scale)
        {
            Vector2 uvXY = new Vector2(pos.x, pos.y) * scale;
            Vector2 uvXZ = new Vector2(pos.x, pos.z) * scale;
            Vector2 uvYZ = new Vector2(pos.y, pos.z) * scale;

            float blendingVal = 0.2f;
            float blendingPow = 2.0f;

            Vector3 blending = (new Vector3(Mathf.Clamp01(Mathf.Abs(normal.x) - blendingVal), Mathf.Clamp01(Mathf.Abs(normal.y) - blendingVal), Mathf.Clamp01(Mathf.Abs(normal.z) - blendingVal))); // Change the 0.2 value to adjust blending
            blending = new Vector3(Mathf.Pow(blending.x, blendingPow), Mathf.Pow(blending.y, blendingPow), Mathf.Pow(blending.z, blendingPow));
            blending /= Vector3.Dot(blending, new Vector3(1.0f, 1.0f, 1.0f));

            Color retColor = Color.black;

            Color cYZ = blending.x * tex.GetPixel((int)(ClampUV(uvYZ.x) * tex.width), (int)(ClampUV(uvYZ.y) * tex.height));
            Color cXZ = blending.y * tex.GetPixel((int)(ClampUV(uvXZ.x) * tex.width), (int)(ClampUV(uvXZ.y) * tex.height));
            Color cXY = blending.z * tex.GetPixel((int)(ClampUV(uvXY.x) * tex.width), (int)(ClampUV(uvXY.y) * tex.height));

            return cXY + cXZ + cYZ;
        }

        public static Color SampleTextureTriplanar(Color[] pixels, int width, int height, Vector3 pos, Vector3 normal, float scale)
        {
            Vector2 uvXY = new Vector2(pos.x, pos.y) * scale;
            Vector2 uvXZ = new Vector2(pos.x, pos.z) * scale;
            Vector2 uvYZ = new Vector2(pos.y, pos.z) * scale;

            float blendingVal = 0.2f;
            float blendingPow = 2.0f;

            Vector3 blending = (new Vector3(Mathf.Clamp01(Mathf.Abs(normal.x) - blendingVal), Mathf.Clamp01(Mathf.Abs(normal.y) - blendingVal), Mathf.Clamp01(Mathf.Abs(normal.z) - blendingVal))); // Change the 0.2 value to adjust blending
            blending = new Vector3(Mathf.Pow(blending.x, blendingPow), Mathf.Pow(blending.y, blendingPow), Mathf.Pow(blending.z, blendingPow));
            blending /= Vector3.Dot(blending, new Vector3(1.0f, 1.0f, 1.0f));

            Color retColor = Color.black;

            int px = (int)(ClampUV(uvYZ.x) * width);
            int py = (int)(ClampUV(uvYZ.y) * height);

            Color tex = pixels[py * width + px];

            Color cYZ = blending.x * tex;
            Color cXZ = blending.y * tex;
            Color cXY = blending.z * tex;

            return cXY + cXZ + cYZ;
        }

        private struct FXVWorldPointInfo
        {
            public int x;
            public int y;
            public int pixelIndex;
            public Vector3 pos;
            public Vector3 worldPos;
            public Vector3 normalFlat;
            public Vector3 normalSmooth;
            public Vector3 tangent;
        };

        private class FXVTexPixelsInfo
        {
            public Color[] pixels;
            public int width;
            public int height;
        }

        private struct FXVRayPointInfo
        {
            public int pixelIndex;
            public int channel;
            public float randomRadius;
            public int rayType;
        };

        private class FXVRaycastJob
        {
            public FXVRaycastJob(int rays)
            {
                results = new NativeArray<RaycastHit>(rays, Allocator.TempJob);
                commands = new NativeArray<RaycastCommand>(rays, Allocator.TempJob);
                pointsInfo = new NativeArray<FXVRayPointInfo>(rays, Allocator.TempJob);
            }

            public void Finish()
            {
                results.Dispose();
                commands.Dispose();
                pointsInfo.Dispose();
            }

            public static RaycastCommand CreateRaycastCommand(Vector3 origin, Vector3 dir, float distance, int layerMask)
            {
#if UNITY_2022_2_OR_NEWER
                return new RaycastCommand(origin, dir, new QueryParameters(layerMask), distance);
#else
                return new RaycastCommand(origin, dir, distance, layerMask);
#endif
            }

            public static FXVRayPointInfo CreateFXVRayPointInfo(int pixelIndex, int channel, int rayType, float param)
            {
                FXVRayPointInfo rpi = new FXVRayPointInfo();
                rpi.pixelIndex = pixelIndex;
                rpi.channel = channel;
                rpi.randomRadius = param;
                rpi.rayType = rayType;

                return rpi;
            }

            public void AddNextRay(Vector3 origin, Vector3 dir, float distance, int layerMask, int pixelIndex, int channel, int rayType, float param)
            {
                commands[numRays] = CreateRaycastCommand(origin, dir, distance, layerMask);
                pointsInfo[numRays] = CreateFXVRayPointInfo(pixelIndex, channel, rayType, param);

                numRays++;
            }

            public void AddRay(int idx, Vector3 origin, Vector3 dir, float distance, int layerMask, int pixelIndex, int channel, int rayType, float param)
            {
                commands[idx] = CreateRaycastCommand(origin, dir, distance, layerMask);
                pointsInfo[idx] = CreateFXVRayPointInfo(pixelIndex, channel, rayType, param);
            }

            public void AddRay(int idx, RaycastCommand cmd, FXVRayPointInfo rpi)
            {
                commands[idx] = cmd;
                pointsInfo[idx] = rpi;
            }

            public void SetNumRays(int numRays)
            {
                this.numRays = numRays;
            }

            public NativeArray<RaycastHit> results;
            public NativeArray<RaycastCommand> commands;
            public NativeArray<FXVRayPointInfo> pointsInfo;
            public JobHandle job;
            private int numRays = 0;
        }

        private struct CreateEdgeDetectRayCastsJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<RaycastCommand> _commands;

            public ShieldRimTextureBake.BakeRimParams bakeParams;
            public int raysInRow;
            public float castSize;
            public float castStep;
            public Vector3 castCenter;

            public void Execute(int pointIndex)
            {
                int x = pointIndex % raysInRow - (int)(raysInRow * 0.5f);
                int y = pointIndex / raysInRow - (int)(raysInRow * 0.5f);

                Vector3 biTangent = Vector3.Cross(bakeParams.bakeTangent, bakeParams.bakeDirection);

                Vector3 worldPosition = castCenter - bakeParams.bakeTangent * (x * castStep) - biTangent * (y * castStep);

                int offset = pointIndex * 5;

                Vector3 positionOffset = Vector3.zero;
                _commands[offset] = FXVRaycastJob.CreateRaycastCommand(worldPosition + positionOffset - bakeParams.bakeDistance * bakeParams.bakeDirection, bakeParams.bakeDirection, bakeParams.bakeDistance * 2.0f, bakeParams.layerMask);
                offset++;

                positionOffset = bakeParams.bakeTangent * castStep;
                _commands[offset] = FXVRaycastJob.CreateRaycastCommand(worldPosition + positionOffset - bakeParams.bakeDistance * bakeParams.bakeDirection, bakeParams.bakeDirection, bakeParams.bakeDistance * 2.0f, bakeParams.layerMask);
                offset++;

                positionOffset = -bakeParams.bakeTangent * castStep;
                _commands[offset] = FXVRaycastJob.CreateRaycastCommand(worldPosition + positionOffset - bakeParams.bakeDistance * bakeParams.bakeDirection, bakeParams.bakeDirection, bakeParams.bakeDistance * 2.0f, bakeParams.layerMask);
                offset++;


                positionOffset = biTangent * castStep;
                _commands[offset] = FXVRaycastJob.CreateRaycastCommand(worldPosition + positionOffset - bakeParams.bakeDistance * bakeParams.bakeDirection, bakeParams.bakeDirection, bakeParams.bakeDistance * 2.0f, bakeParams.layerMask);
                offset++;

                positionOffset = -biTangent * castStep;
                _commands[offset] = FXVRaycastJob.CreateRaycastCommand(worldPosition + positionOffset - bakeParams.bakeDistance * bakeParams.bakeDirection, bakeParams.bakeDirection, bakeParams.bakeDistance * 2.0f, bakeParams.layerMask);
                offset++;
            }
        }

        public static Texture2D BakeUsingEdgeDetect(Transform trans, Mesh mesh, fxvEditorUVUtils.UVINDEX uvIndex, ShieldRimTextureBake.BakeRimParams bakeParams)
        {
            Texture2D tex = new Texture2D(bakeParams.textureWidth, bakeParams.textureHeight);
            Color[] finalColors = new Color[bakeParams.textureWidth * bakeParams.textureHeight];
            tex.SetPixels(finalColors);

            int numPixels = bakeParams.textureWidth * bakeParams.textureHeight;
            int numRaysPerPixel = 5;
            int numTasks = 64;
            int taskOffset = numPixels / numTasks;

            Task[] tasks = new Task[numTasks];

            int raysCount = numPixels * numRaysPerPixel;

            object _queueLock = new object();

            double startTime = EditorApplication.timeSinceStartup;

            int[] tris = mesh.triangles;
            Vector2[] uvs = null;
            if (uvIndex == fxvEditorUVUtils.UVINDEX.UV)
                uvs = mesh.uv;
            else if (uvIndex == fxvEditorUVUtils.UVINDEX.UV2)
                uvs = mesh.uv2;

            Vector3[] verts = mesh.vertices;
            Vector3[] normals = mesh.normals;

            Matrix4x4 localToWorldMatrix = trans.localToWorldMatrix;

            Renderer r = trans.GetComponent<Renderer>();
            float castSize = Mathf.Max(Mathf.Max(r.bounds.size.x, r.bounds.size.y), r.bounds.size.z);

            FXVRaycastJob raycastJob = new FXVRaycastJob(raysCount);

            CreateEdgeDetectRayCastsJob jobData = new CreateEdgeDetectRayCastsJob();
            jobData._commands = raycastJob.commands;
            jobData.bakeParams = bakeParams;
            jobData.raysInRow = bakeParams.textureWidth;
            jobData.castSize = castSize;
            jobData.castStep = castSize / bakeParams.textureWidth;
            jobData.castCenter = r.bounds.center;

            JobHandle handle = jobData.Schedule(numPixels, 128);

            raycastJob.job = RaycastCommand.ScheduleBatch(raycastJob.commands, raycastJob.results, 500, handle);
            raycastJob.job.Complete();

            double rayCastJobCompleteTime = EditorApplication.timeSinceStartup;

            List<Vector3> edging = new List<Vector3>();

            {
                int numResults = raycastJob.results.Length;
                numTasks = 64;
                taskOffset = numResults / numTasks;

                tasks = new Task[numTasks];

                for (int t = 0; t < numTasks; ++t)
                {
                    int taskId = t;
                    tasks[t] = Task.Run(
                        () =>
                        {
                            int tEnd = (taskId + 1) * taskOffset;
                            if (taskId == numTasks - 1)
                                tEnd = numResults;

                            for (int i = taskId * taskOffset; i < tEnd; ++i)
                            {
                                int rayInTest = i % numRaysPerPixel;

                                if (rayInTest == 0)
                                {
                                    RaycastHit rhi = raycastJob.results[i];

                                    RaycastHit rhi1 = raycastJob.results[i+1];
                                    RaycastHit rhi2 = raycastJob.results[i+2];
                                    RaycastHit rhi3 = raycastJob.results[i+3];
                                    RaycastHit rhi4 = raycastJob.results[i+4];

                                    if (rhi.colliderInstanceID != 0)
                                    {
                                        if (rhi1.colliderInstanceID == 0 || rhi2.colliderInstanceID == 0 || rhi3.colliderInstanceID == 0 || rhi4.colliderInstanceID == 0)
                                        {
                                            Vector3 edgingPoint = rhi.point - bakeParams.bakeDirection * Vector3.Dot(bakeParams.bakeDirection, rhi.point);
                                            edging.Add(edgingPoint);
                                        }
                                    }
                                }
                            }
                        });
                }

                for (int t = 0; t < numTasks; ++t)
                {
                    tasks[t].Wait();
                }
            }

            double gatherJobDataTime = EditorApplication.timeSinceStartup;

            //Cleanup
            raycastJob.Finish();


            float radiusR = 0.5f * castSize * bakeParams.radiusR;
            float radiusG = 0.5f * castSize * bakeParams.radiusG;
            float radiusB = 0.5f * castSize * bakeParams.radiusB;

            {
                int numResults = finalColors.Length;
                numTasks = 64;
                taskOffset = numResults / numTasks;

                tasks = new Task[numTasks];

                int edgesCount = edging.Count;

                for (int t = 0; t < numTasks; ++t)
                {
                    int taskId = t;
                    tasks[t] = Task.Run(
                        () =>
                        {
                            int tEnd = (taskId + 1) * taskOffset;
                            if (taskId == numTasks - 1)
                                tEnd = numResults;

                            for (int i = taskId * taskOffset; i < tEnd; ++i)
                            {
                                int x = i % bakeParams.textureWidth;
                                int y = i / bakeParams.textureWidth;

                                Vector2 uv = new Vector2((float)(x + 0.5f) / (float)(bakeParams.textureWidth), (float)(y + 0.5f) / (float)(bakeParams.textureHeight));

                                Vector3 pos, normalFlat, normalSmooth;
                                Vector3 tangent;

                                if (GetWorldPositionAndNormalFromUV(uv, uvIndex, tris, uvs, verts, normals, out pos, out normalFlat, out normalSmooth, out tangent))
                                {
                                    Vector3 point = localToWorldMatrix.MultiplyPoint3x4(pos);
                                    point = point - bakeParams.bakeDirection * Vector3.Dot(bakeParams.bakeDirection, point);
                                    float minDist = float.MaxValue;

                                    for (int j = 0; j < edgesCount; ++j)
                                    {
                                        float dist = (point - edging[j]).sqrMagnitude;
                                        if (dist < minDist)
                                        {
                                            minDist = dist;
                                        }
                                    }

                                    minDist = MathF.Sqrt(minDist);

                                    float edgeR = 1.0f - Mathf.Clamp01(minDist / radiusR);
                                    float edgeG = 1.0f - Mathf.Clamp01(minDist / radiusG);
                                    float edgeB = 1.0f - Mathf.Clamp01(minDist / radiusB);
                                    finalColors[i] = new Color(edgeR, edgeG, edgeB, 1.0f);
                                }
                                else
                                {
                                    finalColors[i] = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                                }
                            }
                        });
                }

                for (int t = 0; t < numTasks; ++t)
                {
                    tasks[t].Wait();
                }
            }

            tex.SetPixels(finalColors);

            double buildTextureTime = EditorApplication.timeSinceStartup;

            tex.Apply();

            double applyTime = EditorApplication.timeSinceStartup;

           /* Debug.Log("--------- Finished baking in " + (applyTime - startTime));
            Debug.Log("     raycastJobsTime = " + (rayCastJobCompleteTime - startTime));
            Debug.Log("     gatherJobDataTime = " + (gatherJobDataTime - rayCastJobCompleteTime));
            Debug.Log("     buildTextureTime = " + (buildTextureTime - gatherJobDataTime));
            Debug.Log("     applyTime = " + (applyTime - buildTextureTime));
            Debug.Log("--------- ");*/

            return tex;
        }

        public static Color[] GetNeighbourPixels(Texture2D tex, int x, int y)
        {
            List<Color> colors = new List<Color>();

            if (x > 0)
            {
                if (y > 0)
                    colors.Add(tex.GetPixel(x - 1, y - 1));
                colors.Add(tex.GetPixel(x - 1, y));
                if (y < tex.height - 1)
                    colors.Add(tex.GetPixel(x - 1, y + 1));
            }

            if (y > 0)
                colors.Add(tex.GetPixel(x, y - 1));
            //colors.Add(tex.GetPixel(x, y));
            if (y < tex.height - 1)
                colors.Add(tex.GetPixel(x, y + 1));

            if (x < tex.width - 1)
            {
                if (y > 0)
                    colors.Add(tex.GetPixel(x + 1, y - 1));
                colors.Add(tex.GetPixel(x + 1, y));
                if (y < tex.height - 1)
                    colors.Add(tex.GetPixel(x + 1, y + 1));
            }

            return colors.ToArray();
        }

        public static void ExpandOnEmptyPixels(Texture2D tex)
        {
            Color[][] modifiedPixels = new Color[tex.width][];
            for (int i = 0; i < tex.width; ++i)
            {
                modifiedPixels[i] = new Color[tex.height];

                for (int j = 0; j < tex.height; ++j)
                {
                    modifiedPixels[i][j] = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                }
            }

            for (int i = 0; i < tex.width; ++i)
            {
                for (int j = 0; j < tex.height; ++j)
                {
                    Color pixel = tex.GetPixel(i, j);

                    if (pixel.r > float.Epsilon || pixel.g > float.Epsilon || pixel.b > float.Epsilon)
                        continue;

                    Color[] neighbours = GetNeighbourPixels(tex, i, j);

                    foreach (Color n in neighbours)
                    {
                        if (n.r > pixel.r)
                            pixel.r = n.r;
                        if (n.g > pixel.g)
                            pixel.g = n.g;
                        if (n.b > pixel.b)
                            pixel.b = n.b;
                    }

                    modifiedPixels[i][j] = pixel;
                }
            }

            for (int i = 0; i < tex.width; ++i)
            {
                for (int j = 0; j < tex.height; ++j)
                {
                    if (modifiedPixels[i][j].a > 0.5f)
                        tex.SetPixel(i, j, modifiedPixels[i][j]);
                }
            }
        }

        public static bool GetWorldPositionAndNormalFromUV(Vector2 uv, fxvEditorUVUtils.UVINDEX uvIndex, Mesh mesh, out Vector3 pos, out Vector3 normalFalt, out Vector3 normalSmooth, out Vector3 tangent)
        {
            int[] tris = mesh.triangles;
            Vector2[] uvs = null;
            if (uvIndex == fxvEditorUVUtils.UVINDEX.UV)
                uvs = mesh.uv;
            else if (uvIndex == fxvEditorUVUtils.UVINDEX.UV2)
                uvs = mesh.uv2;

            Vector3[] verts = mesh.vertices;
            Vector3[] normals = mesh.normals;

            return GetWorldPositionAndNormalFromUV(uv, uvIndex, tris, uvs, verts, normals, out pos, out normalFalt, out normalSmooth, out tangent);
        }

        public static bool GetWorldPositionAndNormalFromUV(Vector2 uv, fxvEditorUVUtils.UVINDEX uvIndex, int[] tris, Vector2[] uvs, Vector3[] verts, Vector3[] normals, out Vector3 pos, out Vector3 normalFalt, out Vector3 normalSmooth, out Vector3 tangent)
        {
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector2 u1 = uvs[tris[i]]; // get the triangle UVs
                Vector2 u2 = uvs[tris[i + 1]];
                Vector2 u3 = uvs[tris[i + 2]];
                // calculate triangle area - if zero, skip it
                float a = Area(u1, u2, u3); if (a == 0) continue;
                // calculate barycentric coordinates of u1, u2 and u3
                // if anyone is negative, point is outside the triangle: skip it
                float a1 = Area(u2, u3, uv) / a; if (a1 < 0.0f) continue;
                float a2 = Area(u3, u1, uv) / a; if (a2 < 0.0f) continue;
                float a3 = Area(u1, u2, uv) / a; if (a3 < 0.0f) continue;
                // point inside the triangle - find mesh position by interpolation...

                Vector3 edge1 = verts[tris[i + 1]] - verts[tris[i]];
                Vector3 edge2 = verts[tris[i + 2]] - verts[tris[i]];

                Vector3 p3D = a1 * verts[tris[i]] + a2 * verts[tris[i + 1]] + a3 * verts[tris[i + 2]];

                pos = p3D;// transform.TransformPoint(p3D);

                Vector3 n3D = a1 * normals[tris[i]] + a2 * normals[tris[i + 1]] + a3 * normals[tris[i + 2]];

                normalSmooth = n3D.normalized;

                normalFalt = Vector3.Cross(edge1, edge2).normalized;//n3D.normalized;
                if (Vector3.Dot(n3D, normalFalt) < 0.0f)
                    normalFalt = -normalFalt;

                Vector3 c1 = Vector3.Cross(normalFalt, Vector3.forward);
                Vector3 c2 = Vector3.Cross(normalFalt, Vector3.up);

                if (c1.sqrMagnitude > c2.sqrMagnitude)
                {
                    tangent = c1.normalized;
                }
                else
                {
                    tangent = c2.normalized;
                }
                // tangent = (p3D - verts[tris[i]]).normalized;

                return true;
            }

            pos = Vector3.zero;
            normalFalt = Vector3.zero;
            normalSmooth = Vector3.zero;
            tangent = Vector3.zero;

            // point outside any uv triangle: return Vector3.zero
            return false;
        }

        public static float Area(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            Vector2 v1 = p1 - p3;
            Vector2 v2 = p2 - p3;
            return (v1.x * v2.y - v1.y * v2.x) / 2;
        }
    }
}