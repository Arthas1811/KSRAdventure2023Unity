using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FalseButton : MonoBehaviour
{
    public void gameOver()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
