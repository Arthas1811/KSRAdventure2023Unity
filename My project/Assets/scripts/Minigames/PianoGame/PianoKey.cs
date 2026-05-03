using UnityEngine;
using UnityEngine.UI;

public class PianoKey : MonoBehaviour
{
    public string noteId;
    public AudioClip sound;

    private Button button;
    private AudioSource audioSource;

    void Awake()
    {
        button = GetComponent<Button>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        button.onClick.AddListener(OnKeyPressed);
    }

    void OnKeyPressed()
    {
        audioSource.PlayOneShot(sound);
        GameManager.Instance.RegisterPlayerInput(noteId);
    }
}
