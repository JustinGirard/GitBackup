using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FXV.ShieldEditorUtils
{
    public class fxvEditorTextureUtils : MonoBehaviour
    {
        public enum TEX_CHANNEL
        {
            CHANNEL_R = 0,
            CHANNEL_G = 1,
            CHANNEL_B = 2,
            CHANNEL_A = 3,
            CHANNEL_RGBA
        };

        protected static float Gaussian(float x, float mu, float sigma)
        {
            float a = (x - mu) / sigma;

            return Mathf.Exp(-0.5f * a * a);
        }

        protected static float[][] Generate2dMedianKernel()
        {
            float[][] kernel2d = new float[][] { new float[] { 1.0f, 1.0f, 1.0f },
                                              new float[] { 1.0f, 1.0f, 1.0f },
                                              new float[] { 1.0f, 1.0f, 1.0f } };


            float sum = 0;
            // compute values
            for (int row = 0; row < kernel2d.Length; row++)
            {
                for (int col = 0; col < kernel2d[row].Length; col++)
                {
                    sum += kernel2d[row][col];
                }
            }
            for (int row = 0; row < kernel2d.Length; row++)
            {
                for (int col = 0; col < kernel2d.LongLength; col++)
                {
                    kernel2d[row][col] /= sum;
                }
            }

            return kernel2d;
        }

        protected static float[][] Generate2dGaussianKernel(int kernelRadius, float strength)
        {
            float sigma = strength * kernelRadius;
            int size = 2 * kernelRadius + 1;
            float[][] kernel2d = new float[size][];


            float sum = 0;
            // compute values
            for (int row = 0; row < size; row++)
            {
                kernel2d[row] = new float[size];
                for (int col = 0; col < size; col++)
                {
                    float x = Gaussian(row, kernelRadius, sigma) * Gaussian(col, kernelRadius, sigma);
                    kernel2d[row][col] = x;
                    sum += x;
                }
            }

            for (int row = 0; row < size; row++)
                for (int col = 0; col < size; col++)
                    kernel2d[row][col] /= sum;

            return kernel2d;
        }

        protected static Color ApplyKernel(Texture2D tex, TEX_CHANNEL channel, int x, int y, float[][] kernel)
        {
            Color retColor = Color.black;
            int offset = (kernel.Length - 1) / 2;


            Color center = tex.GetPixel(x, y);

            if (channel != TEX_CHANNEL.CHANNEL_RGBA)
            {
                retColor = center;
                retColor[(int)channel] = 0.0f;
            }

            for (int i = 0; i < kernel.Length; ++i)
            {
                for (int j = 0; j < kernel[i].Length; ++j)
                {
                    int row = x - offset + i;
                    int col = y - offset + j;

                    if (row >= 0 && row < tex.width && col >= 0 && col < tex.height)
                    {
                        Color pix = tex.GetPixel(row, col);
                        if (channel == TEX_CHANNEL.CHANNEL_RGBA)
                            retColor += pix * kernel[i][j];
                        else
                            retColor[(int)channel] += pix[(int)channel] * kernel[i][j];
                    }
                }
            }

            return retColor;
        }

        public static Texture2D Denoise(Texture2D tex, TEX_CHANNEL channel)
        {
            float[][] kernel = Generate2dMedianKernel();

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
                    modifiedPixels[i][j] = ApplyKernel(tex, channel, i, j, kernel);
                }
            }

            for (int i = 0; i < tex.width; ++i)
            {
                for (int j = 0; j < tex.height; ++j)
                {
                    tex.SetPixel(i, j, modifiedPixels[i][j]);
                }
            }

            return tex;
        }

        public static Texture2D ApplyGaussianBlur(Texture2D tex, TEX_CHANNEL channel, int radius, float strength)
        {
            float[][] kernel = Generate2dGaussianKernel(radius, strength);

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
                    modifiedPixels[i][j] = ApplyKernel(tex, channel, i, j, kernel);
                }
            }

            for (int i = 0; i < tex.width; ++i)
            {
                for (int j = 0; j < tex.height; ++j)
                {
                    tex.SetPixel(i, j, modifiedPixels[i][j]);
                }
            }

            return tex;
        }

        public static void SetTextureReadable(Texture2D texture, bool isReadable)
        {
            if (texture == null)
                return;

            string assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                tImporter.textureType = TextureImporterType.Default;

                tImporter.isReadable = isReadable;

                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.Refresh();
            }
        }

        public static void SetTextureCompression(Texture2D texture, TextureImporterCompression compression)
        {
            if (texture == null)
                return;

            string assetPath = AssetDatabase.GetAssetPath(texture);

            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                tImporter.textureType = TextureImporterType.Default;

                tImporter.textureCompression = compression;

                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.Refresh();
            }
        }
    }
}