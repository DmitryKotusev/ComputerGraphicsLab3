using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Crosstales.FB;
using UnityEditor;

public class UIController : MonoBehaviour
{
    string pathToFile;
    Texture2D selectedTexture;
    public RawImage originalImage;
    public RawImage transformedImage;
    public Dropdown dropdown;
    public GameObject toggleObject;
    public Toggle invertToggle;

    private void Start()
    {
        originalImage.enabled = false;
        transformedImage.enabled = false;
        ImageTransformScript.inverted = invertToggle.isOn;
        //Debug.Log("Red: " + Color.red.grayscale);
        //Debug.Log("Green: " + Color.green.grayscale);
        //Debug.Log("Blue: " + Color.blue.grayscale);
        //Debug.Log("White: " + Color.white.grayscale);
    }

    public void Invert()
    {
        ImageTransformScript.inverted = invertToggle.isOn;
        Debug.Log(ImageTransformScript.inverted);
        UpdateImages();
    }

    public void SetTransformMethod()
    {
        ImageTransformScript.transformMethod = (TransformMethod)dropdown.value;
        if (ImageTransformScript.transformMethod != TransformMethod.NonLinearMin && ImageTransformScript.transformMethod != TransformMethod.NonLinearMedium
            && ImageTransformScript.transformMethod != TransformMethod.NonLinearMax)
        {
            toggleObject.SetActive(true);
        }
        else
        {
            toggleObject.SetActive(false);
        }
        Debug.Log(ImageTransformScript.transformMethod);
        UpdateImages();
    }

    public void ChooseFile()
    {
        // pathToFile = EditorUtility.OpenFilePanelWithFilters("Choose file", "", new string[] { "Image files", "png,jpg,jpeg" });
        pathToFile = FileBrowser.OpenSingleFile("Choose file", "", new string[] { "jpg", "png" });

        WWW www = new WWW("file:///" + pathToFile);

        selectedTexture = www.texture;

        UpdateOriginImage(www.texture);
        UpdateTransformedImage(www.texture);
    }

    public void UpdateImages()
    {
        UpdateOriginImage(selectedTexture);
        UpdateTransformedImage(selectedTexture);
    }

    private void UpdateTransformedImage(Texture2D texture)
    {
        if(pathToFile != "" && pathToFile != null)
        {
            transformedImage.enabled = true;
            Texture2D transformedTexture = ImageTransformScript.TransformTexture(texture);
            transformedImage.texture = transformedTexture;
            transformedImage.SizeToParent();
        }
        else
        {
            transformedImage.enabled = false;
        }
    }

    private void UpdateOriginImage(Texture2D texture)
    {
        if (pathToFile != "" && pathToFile != null)
        {
            originalImage.enabled = true;
            Texture2D grayScaledTexture = ImageTransformScript.ConvertToGrayscale(texture);
            originalImage.texture = grayScaledTexture;
            originalImage.SizeToParent();
        }
        else
        {
            originalImage.enabled = false;
        }
    }
}

static class CanvasExtensions
{
    public static Vector2 SizeToParent(this RawImage image, float padding = 0)
    {
        var parent = image.transform.parent.GetComponentInParent<RectTransform>();
        var imageTransform = image.GetComponent<RectTransform>();
        if (!parent) { return imageTransform.sizeDelta; } //if we don't have a parent, just return our current width; 
        padding = 1 - padding;
        float w = 0, h = 0;
        float ratio = image.texture.width / (float)image.texture.height;
        var bounds = new Rect(0, 0, parent.rect.width, parent.rect.height);
        if (Mathf.RoundToInt(imageTransform.eulerAngles.z) % 180 == 90)
        {
            //Invert the bounds if the image is rotated 
            bounds.size = new Vector2(bounds.height, bounds.width);
        }
        //Size by height first 
        h = bounds.height * padding;
        w = h * ratio;
        if (w > bounds.width * padding)
        { //If it doesn't fit, fallback to width; 
            w = bounds.width * padding;
            h = w / ratio;
        }
        imageTransform.sizeDelta = new Vector2(w, h);
        return imageTransform.sizeDelta;
    }
}
