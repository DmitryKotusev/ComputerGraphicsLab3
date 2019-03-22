using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crosstales.FB;

public class UIController : MonoBehaviour
{
    string pathToFile;

    public void ChooseFile()
    {
        pathToFile = FileBrowser.OpenSingleFile("Choose file", "", new string[] { "png", "jpg" });

        WWW www = new WWW("file:///" + pathToFile);

        UpdateOriginImage(www.texture);
        UpdateTransformedImage(www.texture);
    }

    private void UpdateTransformedImage(Texture2D texture)
    {

    }

    private void UpdateOriginImage(Texture2D texture)
    {

    }
}
