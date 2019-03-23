using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageTransfromScript : MonoBehaviour
{
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
}
