using UnityEngine;
using UnityEngine.SceneManagement;

public class Leave : MonoBehaviour
{
    public void leave()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
