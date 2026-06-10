using System;
using System.Collections;
using UnityEngine;

public class ScreenShotManager : MonoBehaviour
{
    public static ScreenShotManager Instance { get; private set; }

    public event ScreenShotEvent CreatedScreenshot;

    [SerializeField] Camera _screenShotCamera;

    bool _takingScreenshot;

    public delegate void ScreenShotEvent();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Another instance of ScreenshotManager already exists. Deleting this (new) instance.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public void StartTakingScreenshot(Action<byte[]> callback)
    {
        _screenShotCamera.enabled = true;
        StartCoroutine(TakeScreenshot(callback));
    }

    private IEnumerator TakeScreenshot(Action<byte[]> callback)
    {
        _takingScreenshot = true;

        yield return new WaitForSeconds(1);
        yield return new WaitForEndOfFrame();

        RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
        _screenShotCamera.targetTexture = renderTexture;
        _screenShotCamera.enabled = false; // this might need to be after wait for end of frame ( ?or somewhere else? )
        Debug.Log($"Has render texture");

        yield return new WaitForEndOfFrame(); // Wait for the rendering to complete
        _screenShotCamera.Render();

        Texture2D screenTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        yield return StartCoroutine(ReadScreenPixels(screenTexture, renderTexture)); // Asynchronous pixel reading

        _screenShotCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);

        // get the screenshot data
        // byte[] screenShotData = screenTexture.GetRawTextureData(); // Raw Data
        byte[] screenShotData = screenTexture.EncodeToPNG(); // Png

        Debug.Log("Screenshot taken.");

        // Invoke the callback with the raw data
        callback?.Invoke(screenShotData);
    }

    private IEnumerator ReadScreenPixels(Texture2D screenTexture, RenderTexture renderTexture)
    {
        // probably make them ratio of the screen size ( could also be done with code need to find a new optimized ratio )
        // int chunkWidth = 240; // Width of each chunk
        int chunkWidth = Screen.width / 8; // Width of each chunk
        // int chunkHeight = 135; // Height of each chunk
        int chunkHeight = Screen.height / 8; // Height of each chunk

        int width = screenTexture.width;
        int height = screenTexture.height;

        for (int y = height; y > 0; y -= chunkHeight)
        {
            int startY = Mathf.Max(0, y - chunkHeight); // Calculate the correct starting Y coordinate
            int blockHeight = Mathf.Min(chunkHeight, y);

            yield return new WaitForSeconds(1);
            for (int x = 0; x < width; x += chunkWidth)
            {
                int blockWidth = Mathf.Min(chunkWidth, width - x);
                Rect blockRect = new Rect(x, height - y, blockWidth, blockHeight);

                // Read pixels asynchronously
                // yield return new WaitForEndOfFrame(); // Doesnt need to happen
                RenderTexture.active = renderTexture;
                screenTexture.ReadPixels(blockRect, x, startY);
            }
        }

        // Apply changes after all pixels are read
        screenTexture.Apply();
    }

    public void CreatedScreenShot()
    {
        _takingScreenshot = false;
        CreatedScreenshot?.Invoke();
    }
}