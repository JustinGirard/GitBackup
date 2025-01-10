using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FXV.ShieldEditorUtils
{
    public class fxvEditorUVUtils : MonoBehaviour
    {
        public enum UVINDEX
        {
            UV = 0,
            UV2 = 1
        };

        public static void GenerateTopDownUV(Mesh mesh, UVINDEX uvIndex)
        {
            Vector3 min, max;

            min = max = Vector3.zero;

            for (int i = 0; i < mesh.vertices.Length; ++i)
            {
                Vector3 vertex = mesh.vertices[i];
                if (i == 0)
                {
                    min = vertex;
                    max = vertex;
                }
                else
                {
                    if (vertex.y < min.y)
                        min = vertex;

                    if (vertex.y > max.y)
                        max = vertex;
                }
            }

            Vector2[] newUVs = new Vector2[mesh.vertices.Length];

            for (int i = 0; i < mesh.vertices.Length; ++i)
            {
                Vector3 vertex = mesh.vertices[i];

                newUVs[i].x = 0.5f;
                newUVs[i].y = (vertex.y - min.y) / (max.y - min.y);
            }

            if (uvIndex == UVINDEX.UV)
                mesh.uv = newUVs;
            else if (uvIndex == UVINDEX.UV2)
                mesh.uv2 = newUVs;
        }

        public static void GenrateLightmapUV(Mesh mesh, UVINDEX uvIndex)
        {
            Vector2[] uvs = new Vector2[mesh.vertices.Length];

            Vector2[] all = Unwrapping.GeneratePerTriangleUV(mesh);
            int[] triangles = mesh.triangles;

            int count = 0;
            while (count < uvs.Length)
            {
                for (int i = 0; i < triangles.Length; i++)
                {
                    if (triangles[i] == count)
                    {
                        uvs[count] = all[i];
                        count++;
                    }
                }
            }

            if (uvIndex == UVINDEX.UV)
                mesh.uv = uvs;
            else if (uvIndex == UVINDEX.UV2)
                mesh.uv2 = uvs;
        }
    }
}