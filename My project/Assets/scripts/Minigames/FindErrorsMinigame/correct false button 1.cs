using UnityEngine;
using UnityEngine.UI;

public class correctfalsebutton1 : MonoBehaviour
{
    public GameObject rectangle;
    public FindErrorsMinigame main;

    public void showRectangle()
    {
        rectangle.SetActive(true);
        main.Count();
    }
}
