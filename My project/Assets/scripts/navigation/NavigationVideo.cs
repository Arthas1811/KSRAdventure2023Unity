using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using System.Collections;

public class NavigationVideo : MonoBehaviour
{
    private static NavigationVideo _instance;
    public static NavigationVideo Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("NavigationVideo");
                _instance = go.AddComponent<NavigationVideo>();
            }
            return _instance;
        }
    }

    VideoPlayer videoPlayer;
    bool enterPressed = false;
    bool skipRequested = false;
    Canvas skipCanvas;
    Button skipButton;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            enterPressed = true;
        }
    }

    void EnsureSkipUi()
    {
        if (skipCanvas != null) return;

        var canvasGo = new GameObject("VideoSkipCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        skipCanvas = canvasGo.GetComponent<Canvas>();
        skipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        skipCanvas.sortingOrder = 1000;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        var buttonGo = new GameObject("SkipButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(canvasGo.transform, false);
        var rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(180f, 64f);
        rect.anchoredPosition = new Vector2(-40f, -40f);

        var img = buttonGo.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);

        skipButton = buttonGo.GetComponent<Button>();
        skipButton.onClick.AddListener(() => skipRequested = true);

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(buttonGo.transform, false);
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelGo.GetComponent<Text>();
        label.text = "Skip";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.fontSize = 30;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void DestroySkipUi()
    {
        if (skipCanvas != null)
        {
            Destroy(skipCanvas.gameObject);
            skipCanvas = null;
            skipButton = null;
        }
    }

    public IEnumerator PlayVideo(string fileName)
    {
        enterPressed = false;
        skipRequested = false;
        GameObject camera = GameObject.Find("Main Camera");
        if (camera == null)
        {
            yield break;
        }

        videoPlayer = camera.AddComponent<UnityEngine.Video.VideoPlayer>();
        videoPlayer.renderMode = VideoRenderMode.CameraFarPlane;
        var navCanvasObj = GameObject.Find("NavigationCanvas");
        var navCanvas = navCanvasObj != null ? navCanvasObj.GetComponent<Canvas>() : null;
        if (navCanvas != null) navCanvas.enabled = false;
        var inventoryObj = GameObject.Find("InventoryCanvas(Clone)");
        var inventory = inventoryObj != null ? inventoryObj.GetComponent<Canvas>() : null;
        if (inventory != null) inventory.enabled = false;

        EnsureSkipUi();
        if (skipCanvas != null) skipCanvas.enabled = true;

        VideoClip videoClip = Resources.Load<VideoClip>(fileName);
        videoPlayer.clip = videoClip;
        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached += VideoFinished;

        videoPlayer.Prepare();
        yield return new WaitUntil(() => videoPlayer.isPrepared);

        float startTime = Time.realtimeSinceStartup;
        videoPlayer.Play();
        yield return null;
        Time.timeScale = 0f;
        float videoLength = CalcLen(videoClip);
        yield return new WaitUntil(() =>
            videoLength < (Time.realtimeSinceStartup - startTime) ||
            enterPressed ||
            skipRequested
        );
        Time.timeScale = 1f;
        if (enterPressed || skipRequested)
        {
            VideoEnd(videoPlayer);
        }
    }

    void VideoFinished(VideoPlayer videoPlayer)
    {
        videoPlayer.loopPointReached -= VideoFinished;
        VideoEnd(videoPlayer);
    }

    void VideoEnd(VideoPlayer videoPlayer)
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= VideoFinished;
            videoPlayer.Stop();
            var clip = videoPlayer.clip;
            Destroy(videoPlayer);
            if (clip != null)
            {
                Resources.UnloadAsset(clip);
            }
        }

        DestroySkipUi();

        var navCanvasObj = GameObject.Find("NavigationCanvas");
        var navCanvas = navCanvasObj != null ? navCanvasObj.GetComponent<Canvas>() : null;
        if (navCanvas != null) navCanvas.enabled = true;

        var inventoryObj = GameObject.Find("InventoryCanvas(Clone)");
        var inventory = inventoryObj != null ? inventoryObj.GetComponent<Canvas>() : null;
        if (inventory != null) inventory.enabled = true;

        videoPlayer = null;
    }

    float CalcLen(VideoClip clip)
    {
        ulong frameCount = videoPlayer.frameCount;
        double frameRate = videoPlayer.frameRate;

        double len = frameCount / frameRate;
        // Debug.Log($"Calculated length: {len}");
        // Debug.Log($"length: {clip.length}");
        return (float)len;
    }
}
