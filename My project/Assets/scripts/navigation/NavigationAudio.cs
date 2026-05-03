using UnityEngine;

public class NavigationAudio : MonoBehaviour
{
    private static NavigationAudio _instance;
    public static NavigationAudio Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("NavigationAudio");
                _instance = go.AddComponent<NavigationAudio>();
            }
            return _instance;
        }
    }

    public AudioSource musicSource;
    public AudioSource soundSource;


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        if (soundSource == null)
        {
            soundSource = gameObject.AddComponent<AudioSource>();
        }
    }
}
