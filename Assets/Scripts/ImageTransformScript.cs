using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageTransformScript : MonoBehaviour
{
    static public float histagramEpsilon = 2f;

    static public bool inverted = true;

    static public TransformMethod transformMethod = TransformMethod.AbsoluteGradient;

    static public Texture2D TransformTexture(Texture2D texture)
    {
        byte[,] grayScaleArray = GetGrayScaleArray(texture);

        byte t = GetThreshold(grayScaleArray);

        Texture2D resultTexture = new Texture2D(grayScaleArray.GetLength(0), grayScaleArray.GetLength(1));

        for (int x = 0; x < grayScaleArray.GetLength(0); x++)
        {
            for (int y = 0; y < grayScaleArray.GetLength(1); y++)
            {
                if(inverted)
                {
                    if (grayScaleArray[x, y] <= t)
                    {
                        Color c = Color.black;
                        resultTexture.SetPixel(x, y, c);
                    }
                    else
                    {
                        Color c = Color.white;
                        resultTexture.SetPixel(x, y, c);
                    }
                }
                else
                {
                    if (grayScaleArray[x, y] > t)
                    {
                        Color c = Color.black;
                        resultTexture.SetPixel(x, y, c);
                    }
                    else
                    {
                        Color c = Color.white;
                        resultTexture.SetPixel(x, y, c);
                    }
                }
                
            }
        }
        resultTexture.Apply();

        return resultTexture;
    }

    private static byte GetThreshold(byte[,] grayScaleArray)
    {
        byte t = 0;
        switch (transformMethod)
        {
            case TransformMethod.AbsoluteGradient:
                {
                    t = GetThresholdWithGradient(grayScaleArray);
                    break;
                }
            case TransformMethod.AbsoluteHistogram:
                {
                    t = GetThresholdWithHistogram(grayScaleArray);
                    break;
                }
        }

        return t;
    }

    static public Texture2D ConvertToGrayscale(Texture2D texture)
    {
        Texture2D resultTexture = new Texture2D(texture.width, texture.height);
        Color32[] pixels = texture.GetPixels32();
        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                Color32 pixel = pixels[x + y * texture.width];
                int p = ((256 * 256 + pixel.r) * 256 + pixel.b) * 256 + pixel.g;
                int b = p % 256;
                p = Mathf.FloorToInt(p / 256);
                int g = p % 256;
                p = Mathf.FloorToInt(p / 256);
                int r = p % 256;
                float l = (0.2126f * r / 255f) + 0.7152f * (g / 255f) + 0.0722f * (b / 255f);
                Color c = new Color(l, l, l, 1);
                resultTexture.SetPixel(x, y, c);
            }
        }
        resultTexture.Apply();
        return resultTexture;
    }

    static public byte[,] GetGrayScaleArray(Texture2D texture)
    {
        byte[,] resultArray = new byte[texture.width, texture.height];
        Color32[] pixels = texture.GetPixels32();
        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                Color pixel = pixels[x + y * texture.width];
                resultArray[x, y] = (byte)(pixel.grayscale * 255);
            }
        }
        return resultArray;
    }

    static public byte GetThresholdWithHistogram(byte[,] grayScaleMatrix)
    {
        float t = 127;
        List<byte> G1 = new List<byte>(); // Коллекция пикселей с яркостью больше t
        List<byte> G2 = new List<byte>(); // Коллекция пикселей с яркостью меньше или равной t
        while (true)
        {
            for (int x = 0; x < grayScaleMatrix.GetLength(0); x++)
            {
                for (int y = 0; y < grayScaleMatrix.GetLength(1); y++)
                {
                    if (grayScaleMatrix[x, y] > t)
                    {
                        G1.Add(grayScaleMatrix[x, y]);
                    }
                    else
                    {
                        G2.Add(grayScaleMatrix[x, y]);
                    }
                }
            }

            //cound medium g1 and g2
            float m1 = GetCollectionMediumBrightness(G1);
            float m2 = GetCollectionMediumBrightness(G2);
            float newT = (m1 + m2) / 2;
            if (Mathf.Abs(newT - t) < histagramEpsilon)
            {
                return (byte)newT;
            }
            t = newT;
        }
    }

    static public float GetCollectionMediumBrightness(List<byte> list)
    {
        if (list.Count == 0)
        {
            return 0;
        }
        long sum = 0;
        foreach (byte brighness in list)
        {
            sum += brighness;
        }

        return (float)sum / (float)list.Count;
    }

    static public byte GetThresholdWithGradient(byte[,] grayScaleMatrix)
    {
        float numerator = 0;
        float denominator = 0;
        for (int x = 0; x < grayScaleMatrix.GetLength(0) - 1; x++)
        {
            for (int y = 0; y < grayScaleMatrix.GetLength(1) - 1; y++)
            {
                numerator += GetGradient(grayScaleMatrix, x, y) * grayScaleMatrix[x, y];
                denominator += GetGradient(grayScaleMatrix, x, y);
            }
        }
        return (byte)(numerator / denominator);
    }

    static public byte GetGradientX(byte[,] grayScaleMatrix, int x, int y)
    {
        return (byte)Mathf.Abs(grayScaleMatrix[x + 1, y] - grayScaleMatrix[x, y]);
    }

    static public byte GetGradientY(byte[,] grayScaleMatrix, int x, int y)
    {
        return (byte)Mathf.Abs(grayScaleMatrix[x, y + 1] - grayScaleMatrix[x, y]);
    }

    static public byte GetGradient(byte[,] grayScaleMatrix, int x, int y)
    {
        return (byte)Mathf.Max(GetGradientX(grayScaleMatrix, x, y), GetGradientY(grayScaleMatrix, x, y));
    }
}

public enum TransformMethod
{
    AbsoluteHistogram,
    AbsoluteGradient
}
