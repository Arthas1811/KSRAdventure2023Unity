using UnityEngine;

public class SwitchImage : MonoBehaviour
{
    public GameObject image1;
    public GameObject image2;

    void Start()
    {
        image1.SetActive(true);
        image2.SetActive(false);
    }
    public void OnClick()
    {
        if (image1.activeSelf)
        {
            image1.SetActive(false);
            image2.SetActive(true);
        }
        else
        {
            image1.SetActive(true);
            image2.SetActive(false);
        }
        

    }
}
