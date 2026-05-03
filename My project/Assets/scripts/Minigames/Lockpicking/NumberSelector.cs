using UnityEngine;
using TMPro;
public class NumberSelector : MonoBehaviour
{
    public int currentNumber = 0;
    public TextMeshProUGUI numberText;

    private void Awake()
    {
        if (numberText == null)
        {
            numberText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
        if (numberText != null)
        {
            numberText.text = currentNumber.ToString();
        }
    }
    
    public void clickNumber()
    {
        currentNumber++;
        if (currentNumber == 10)
        {
            currentNumber = 0;
        }
        numberText.text = currentNumber.ToString();

    }

}
