using UnityEngine;
using System.Collections;

public class AppWindow : MonoBehaviour
{
    [Header("Animation Settings")]
    public float animationSpeed = 15f;
    
    [Header("Recents Visuals (Fallback)")]
    public Sprite appIcon;
    public Color appBackgroundColor = Color.white;
    
    [HideInInspector] 
    public Sprite liveSnapshot; // The script will generate this automatically!

    private RectTransform rect;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void OpenApp()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AnimateScale(Vector3.one));
    }

    // Called when the Home button is pressed
    public void CloseApp()
    {
        StopAllCoroutines();
        StartCoroutine(CaptureScreenshotAndClose(true));
    }
    
    // Called when Recents is pressed so it hides instantly instead of shrinking
    public void SuspendAppInstantly() 
    {
        StopAllCoroutines();
        StartCoroutine(CaptureScreenshotAndClose(false));
    }

private IEnumerator CaptureScreenshotAndClose(bool animate)
    {
        // 1. Wait until the very end of the frame
        yield return new WaitForEndOfFrame();

        // 2. Find the exact boundaries of the app
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        
        // --- NEW PIXEL-PERFECT CONVERSION ---
        // We must translate the Canvas world space into literal monitor pixels
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        Camera renderCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(renderCam, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(renderCam, corners[2]);

        int startX = Mathf.RoundToInt(bottomLeft.x);
        int startY = Mathf.RoundToInt(bottomLeft.y);
        int width = Mathf.RoundToInt(topRight.x - bottomLeft.x);
        int height = Mathf.RoundToInt(topRight.y - bottomLeft.y);
        // ------------------------------------

        // 3. Take the literal screenshot!
        if (width > 0 && height > 0)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(startX, startY, width, height), 0, 0);
            tex.Apply();
            
            if (liveSnapshot != null && liveSnapshot.texture != null) Destroy(liveSnapshot.texture);
            liveSnapshot = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        // 4. Run the shrink animation
        if (animate)
        {
            Vector3 targetScale = Vector3.zero;
            while (Vector3.Distance(rect.localScale, targetScale) > 0.01f)
            {
                rect.localScale = Vector3.Lerp(rect.localScale, targetScale, Time.deltaTime * animationSpeed);
                yield return null;
            }
            rect.localScale = targetScale;
        }
        
        gameObject.SetActive(false);
    }

    private IEnumerator AnimateScale(Vector3 targetScale)
    {
        rect.localScale = Vector3.zero;
        while (Vector3.Distance(rect.localScale, targetScale) > 0.01f)
        {
            rect.localScale = Vector3.Lerp(rect.localScale, targetScale, Time.deltaTime * animationSpeed);
            yield return null;
        }
        rect.localScale = targetScale;
    }
}