using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageTransformScript : MonoBehaviour
{
    // Эпсилон для подсчёта порога с помощью гистограммы
    static public float histagramEpsilon = 2f;

    // Альфа для адаптивной пороговой обработки
    static public float alpha = 0.3f;

    // Стартовый размер блока для адаптивной пороговой обработки
    static public int startK = 10;

    // Размер секции для нелинейного фильтра
    static public int sectionSize = 3;

    static public bool inverted = true;

    static public TransformMethod transformMethod = TransformMethod.AbsoluteHistogram;

    static public Texture2D TransformTexture(Texture2D texture)
    {
        byte[,] grayScaleArray = GetGrayScaleArray(texture);

        if (transformMethod == TransformMethod.AbsoluteGradient || transformMethod == TransformMethod.AbsoluteHistogram)
        {
            return AbsoluteTranform(grayScaleArray);
        }
        if (transformMethod == TransformMethod.Adoptive)
        {
            return AdoptiveTransform2(grayScaleArray);
        }
        if (transformMethod == TransformMethod.NonLinearMin || transformMethod == TransformMethod.NonLinearMedium
            || transformMethod == TransformMethod.NonLinearMax)
        {
            return NonlinearFilterTransform(grayScaleArray);
        }
        return null;
    }

    private static Texture2D AbsoluteTranform(byte[,] grayScaleArray)
    {
        byte t = GetThreshold(grayScaleArray);

        Texture2D resultTexture = new Texture2D(grayScaleArray.GetLength(0), grayScaleArray.GetLength(1));

        for (int x = 0; x < grayScaleArray.GetLength(0); x++)
        {
            for (int y = 0; y < grayScaleArray.GetLength(1); y++)
            {
                if (inverted)
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

    static public float GetP(byte[,] grayScaleMatrix, int m, int n, int k)
    {
        float sum = 0;
        int amount = 0;
        for (int x = -k; x <= k; x++)
        {
            if (m + x < 0 || m + x >= grayScaleMatrix.GetLength(0))
            {
                continue;
            }
            for (int y = -k; y <= k; y++)
            {
                if (n + y < 0 || n + y >= grayScaleMatrix.GetLength(1))
                {
                    continue;
                }
                sum += grayScaleMatrix[m + x, n + y];
                amount++;
            }
        }
        return sum / amount;
    }

    static public byte GetFMax(byte[,] grayScaleMatrix, int m, int n, int k)
    {
        byte max = 0;
        for (int x = -k; x <= k; x++)
        {
            if(m + x < 0 || m + x >= grayScaleMatrix.GetLength(0))
            {
                continue;
            }
            for (int y = -k; y <= k; y++)
            {
                if (n + y < 0 || n + y >= grayScaleMatrix.GetLength(1))
                {
                    continue;
                }
                if (grayScaleMatrix[m + x, n + y] > max)
                {
                    max = grayScaleMatrix[m + x, n + y];
                }
            }
        }
        return max;
    }

    static public byte GetFMin(byte[,] grayScaleMatrix, int m, int n, int k)
    {
        byte min = 255;
        for (int x = -k; x <= k; x++)
        {
            if (m + x < 0 || m + x >= grayScaleMatrix.GetLength(0))
            {
                continue;
            }
            for (int y = -k; y <= k; y++)
            {
                if (n + y < 0 || n + y >= grayScaleMatrix.GetLength(1))
                {
                    continue;
                }
                if (grayScaleMatrix[m + x, n + y] < min)
                {
                    min = grayScaleMatrix[m + x, n + y];
                }
            }
        }
        return min;
    }

    static public Texture2D NonlinearFilterTransform(byte[,] grayScaleArray)
    {
        int k = sectionSize / 2;

        byte[,] resultArray = new byte[grayScaleArray.GetLength(0), grayScaleArray.GetLength(1)];
        Texture2D resultTexture = new Texture2D(grayScaleArray.GetLength(0), grayScaleArray.GetLength(1));
 
        for (int x = k; x < grayScaleArray.GetLength(0) - k; x++)
        {
            for (int y = k; y < grayScaleArray.GetLength(1) - k; y++)
            {
                switch (transformMethod)
                {
                    case TransformMethod.NonLinearMin:
                        {
                            resultArray[x, y] = GetMinBrightness(grayScaleArray, x, y, k);
                            break;
                        }
                    case TransformMethod.NonLinearMedium:
                        {
                            resultArray[x, y] = GetMediumBrightness(grayScaleArray, x, y, k);
                            break;
                        }
                    case TransformMethod.NonLinearMax:
                        {
                            resultArray[x, y] = GetMaxBrightness(grayScaleArray, x, y, k);
                            break;
                        }
                }
                Color color = new Color(resultArray[x, y] / 255f, resultArray[x, y] / 255f, resultArray[x, y] / 255f, 1);
                resultTexture.SetPixel(x, y, color);
            }
        }
        resultTexture.Apply();

        return resultTexture;
    }

    static public byte GetMediumBrightness(byte[,] grayScaleArray, int m, int n, int k)
    {
        int matrixSize = 2 * k + 1;
        List<byte> sectionArray = new List<byte>(matrixSize * matrixSize);
        for (int x = - k; x <= k; x++)
        {
            for (int y = -k; y <= k; y++)
            {
                sectionArray.Add(grayScaleArray[m + x, n + y]);
            }
        }
        sectionArray.Sort();
        return sectionArray[matrixSize * matrixSize / 2];
    }

    static public byte GetMaxBrightness(byte[,] grayScaleArray, int m, int n, int k)
    {
        int matrixSize = 2 * k + 1;
        List<byte> sectionArray = new List<byte>(matrixSize * matrixSize);
        for (int x = -k; x <= k; x++)
        {
            for (int y = -k; y <= k; y++)
            {
                sectionArray.Add(grayScaleArray[m + x, n + y]);
            }
        }
        sectionArray.Sort();
        return sectionArray[matrixSize * matrixSize - 1];
    }

    static public byte GetMinBrightness(byte[,] grayScaleArray, int m, int n, int k)
    {
        int matrixSize = 2 * k + 1;
        List<byte> sectionArray = new List<byte>(matrixSize * matrixSize);
        for (int x = -k; x <= k; x++)
        {
            for (int y = -k; y <= k; y++)
            {
                sectionArray.Add(grayScaleArray[m + x, n + y]);
            }
        }
        sectionArray.Sort();
        return sectionArray[0];
    }

    static public Texture2D AdoptiveTransform(byte[,] grayScaleArray)
    {
        int k = startK;
        int blockSize = 2 * k + 1;
        bool increaseKFlag = false;
        byte[,] thresholds;
        Texture2D resultTexture = new Texture2D(grayScaleArray.GetLength(0), grayScaleArray.GetLength(1));

        while (true)
        {
            int widthBlocksAmount = grayScaleArray.GetLength(0) / blockSize;
            if (grayScaleArray.GetLength(0) % blockSize != 0)
            {
                widthBlocksAmount += 1;
            }
            int heightBlocksAmount = grayScaleArray.GetLength(1) / blockSize;
            if (grayScaleArray.GetLength(1) % blockSize != 0)
            {
                heightBlocksAmount += 1;
            }

            byte[,] thresholdsK = new byte[widthBlocksAmount, heightBlocksAmount];

            for (int x = 0; x < thresholdsK.GetLength(0); x++)
            {
                for (int y = 0; y < thresholdsK.GetLength(1); y++)
                {
                    int m = 0;
                    if (x * blockSize + 2 * k + 1 >= grayScaleArray.GetLength(0))
                    {
                        m = grayScaleArray.GetLength(0) - 1 - k;
                    }
                    else
                    {
                        m = x * blockSize + k;
                    }

                    int n = 0;
                    if (y * blockSize + 2 * k + 1 >= grayScaleArray.GetLength(1))
                    {
                        n = grayScaleArray.GetLength(1) - 1 - k;
                    }
                    else
                    {
                        n = y * blockSize + k;
                    }

                    byte fMax = GetFMax(grayScaleArray, m, n, k);
                    byte fMin = GetFMin(grayScaleArray, m, n, k);
                    float p = GetP(grayScaleArray, m, n, k);
                    float deltaFMax = Mathf.Abs(p - fMax);
                    float deltaFMin = Mathf.Abs(p - fMin);
                    if (deltaFMax > deltaFMin)
                    {
                        thresholdsK[x, y] = (byte)(alpha * (2f / 3f * fMin + 1f / 3f * p));
                    }
                    else if (deltaFMax < deltaFMin)
                    {
                        thresholdsK[x, y] = (byte)(alpha * (1f / 3f * fMin + 2f / 3f * p));
                    }
                    else
                    {
                        if (fMax != fMin)
                        {
                            increaseKFlag = true;
                            break;
                        }
                        else
                        {
                            thresholdsK[x, y] = (byte)(alpha * p);
                        }
                    }
                }
                if (increaseKFlag)
                {
                    break;
                }
            }
            if (increaseKFlag)
            {
                increaseKFlag = false;
                k++;
                blockSize = 2 * k + 1;
                continue;
            }
            thresholds = thresholdsK;
            break;
        }

        // Процесс бинаризации
        for (int x = 0; x < grayScaleArray.GetLength(0); x++)
        {
            int m = x / blockSize;
            for (int y = 0; y < grayScaleArray.GetLength(1); y++)
            {
                int n = y / blockSize;
                if (inverted)
                {
                    if (grayScaleArray[x, y] <= thresholds[m, n])
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
                    if (grayScaleArray[x, y] > thresholds[m, n])
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

    static public Texture2D AdoptiveTransform2(byte[,] grayScaleArray)
    {
        int k = startK;
        int blockSize = 2 * k + 1;
        Texture2D resultTexture = new Texture2D(grayScaleArray.GetLength(0), grayScaleArray.GetLength(1));

        for (int x = 0; x < grayScaleArray.GetLength(0); x++)
        {
            for (int y = 0; y < grayScaleArray.GetLength(1); y++)
            {
                int currentK;
                byte t = countT(grayScaleArray, x, y, k, out currentK);
                if (adoptiveCheckCriteria(grayScaleArray, x, y, currentK, t))
                {
                    Color c = inverted ? Color.black : Color.white;
                    resultTexture.SetPixel(x, y, c);
                }
                else
                {
                    Color c = inverted ? Color.white : Color.black;
                    resultTexture.SetPixel(x, y, c);
                }
            }
        }

        resultTexture.Apply();

        return resultTexture;
    }

    static public byte countT(byte[,] grayScaleArray, int m, int n, int k, out int newK)
    {
        int currentK = k;
        int blockSize = 2 * k + 1;
        while (true)
        {
            byte fMax = GetFMax(grayScaleArray, m, n, currentK);
            byte fMin = GetFMin(grayScaleArray, m, n, currentK);
            float p = GetP(grayScaleArray, m, n, k);
            float deltaFMax = Mathf.Abs(p - fMax);
            float deltaFMin = Mathf.Abs(p - fMin);
            if (deltaFMax > deltaFMin)
            {
                newK = currentK;
                return (byte)(alpha * (2f / 3f * fMin + 1f / 3f * p));
            }
            else if (deltaFMax < deltaFMin)
            {
                newK = currentK;
                return (byte)(alpha * (1f / 3f * fMin + 2f / 3f * p));
            }
            else
            {
                if (fMax != fMin)
                {
                    currentK++;
                    blockSize = 2 * currentK + 1;
                }
                else
                {
                    newK = currentK;
                    return (byte)(alpha * p);
                }
            }
        }
    }

    static public bool adoptiveCheckCriteria(byte[,] grayScaleArray, int m, int n, int k, byte t)
    {
        for (int x = -1; x <= 1; x++)
        {
            if (m + x <= 0 || m + x >= grayScaleArray.GetLength(0))
            {
                continue;
            }
            for (int y = -1; y <= 1; y++)
            {
                if (n + y <= 0 || n + y >= grayScaleArray.GetLength(1))
                {
                    continue;
                }
                if (Mathf.Abs(grayScaleArray[m, n] - GetP(grayScaleArray, m + x, n + y, k)) <= t)
                {
                    return false;
                }
            }
        }
        return true;
    }
}

public enum TransformMethod
{
    AbsoluteHistogram = 0,
    AbsoluteGradient = 1,
    Adoptive = 2,
    NonLinearMin = 3,
    NonLinearMedium = 4,
    NonLinearMax = 5,
}
