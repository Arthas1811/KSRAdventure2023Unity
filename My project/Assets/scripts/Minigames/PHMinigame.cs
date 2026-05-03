using System.Runtime.Versioning;
using UnityEngine;
using UnityEngine.UI;
using Framework.Minigames;

public class PHMinigame : MonoBehaviour
{
    // Solved Images
    [SerializeField] private RawImage horizontalStick1;
    [SerializeField] private RawImage horizontalStick2;
    [SerializeField] private RawImage horizontalStick3;
    [SerializeField] private RawImage horizontalStick4;
    [SerializeField] private RawImage verticalStick1;
    [SerializeField] private RawImage verticalStick2;
    [SerializeField] private RawImage verticalStick3;
    [SerializeField] private RawImage verticalStick4;
    [SerializeField] private RawImage verticalStick5;
    [SerializeField] private RawImage verticalStick6;
    [SerializeField] private RawImage verticalStick7;
    [SerializeField] private RawImage battery;
    [SerializeField] private RawImage potentiometer;
    [SerializeField] private RawImage switch1;
    [SerializeField] private RawImage switch2;
    [SerializeField] private RawImage lamp;


    [SerializeField] private RawImage horizontalStick1Hint;
    [SerializeField] private RawImage horizontalStick2Hint;
    [SerializeField] private RawImage horizontalStick3Hint;
    [SerializeField] private RawImage horizontalStick4Hint;
    [SerializeField] private RawImage verticalStick1Hint;
    [SerializeField] private RawImage verticalStick2Hint;
    [SerializeField] private RawImage verticalStick3Hint;
    [SerializeField] private RawImage verticalStick4Hint;
    [SerializeField] private RawImage verticalStick5Hint;
    [SerializeField] private RawImage verticalStick6Hint;
    [SerializeField] private RawImage verticalStick7Hint;
    [SerializeField] private RawImage batteryHint;
    [SerializeField] private RawImage potentiometerHint;
    [SerializeField] private RawImage switch1Hint;
    [SerializeField] private RawImage switch2Hint;
    [SerializeField] private RawImage lampHint;

    // TBP blocks
    [SerializeField] private Image batteryTBP;
    [SerializeField] private Image potentiometerTBP;
    [SerializeField] private Image switch1TBP;
    [SerializeField] private Image switch2TBP;
    [SerializeField] private Image lampTBP;
    [SerializeField] private Image horizontalStick1TBP;
    [SerializeField] private Image horizontalStick2TBP;
    [SerializeField] private Image horizontalStick3TBP;
    [SerializeField] private Image horizontalStick4TBP;
    [SerializeField] private Image verticalStick1TBP;
    [SerializeField] private Image verticalStick2TBP;
    [SerializeField] private Image verticalStick3TBP;
    [SerializeField] private Image verticalStick4TBP;
    [SerializeField] private Image verticalStick5TBP;
    [SerializeField] private Image verticalStick6TBP;
    [SerializeField] private Image verticalStick7TBP;

    private int placedObjectsNum = 0;

    private bool gameWon = false;
    public bool GameWon { get { return gameWon; } }
    private bool _dialogueTriggered = false;

    private GameObject obj;

    private RawImage platform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        obj = GameObject.Find("Platform");
        platform = obj.GetComponent<RawImage>();

        obj = GameObject.Find("HorizontalStick1");
        horizontalStick1 = obj.GetComponent<RawImage>();
        obj = GameObject.Find("HorizontalStick2");
        horizontalStick2 = obj.GetComponent<RawImage>();
        obj = GameObject.Find("HorizontalStick3");
        horizontalStick3 = obj.GetComponent<RawImage>();
        obj = GameObject.Find("HorizontalStick4");
        horizontalStick4 = obj.GetComponent<RawImage>();
        obj = GameObject.Find("VerticalStick1");
        verticalStick1 = obj.GetComponent<RawImage>();
        obj = GameObject.Find("VerticalStick2");
        verticalStick2 = obj.GetComponent<RawImage>();
        obj = GameObject.Find("VerticalStick3");
        verticalStick3 = obj.GetComponent<RawImage>();
        obj = GameObject.Find("VerticalStick4");
        verticalStick4 = obj.GetComponent<RawImage>();
        obj = GameObject.Find("VerticalStick5");
        verticalStick5 = obj.GetComponent<RawImage>();
        obj = GameObject.Find("VerticalStick6");
        verticalStick6 = obj.GetComponent<RawImage>();
        obj = GameObject.Find("VerticalStick7");
        verticalStick7 = obj.GetComponent<RawImage>();
        obj = GameObject.Find("Battery");
        battery = obj.GetComponent<RawImage>();
        obj = GameObject.Find("Potentiometer");
        potentiometer = obj.GetComponent<RawImage>();
        obj = GameObject.Find("Switch1");
        switch1 = obj.GetComponent<RawImage>();
        obj = GameObject.Find("Switch2");
        switch2 = obj.GetComponent<RawImage>();
        obj = GameObject.Find("Lamp");
        lamp = obj.GetComponent<RawImage>();
        obj = GameObject.Find("HorizontalStickHint1");
        horizontalStick1Hint = obj.GetComponent<RawImage>();
        obj = GameObject.Find("HorizontalStickHint2");
        horizontalStick2Hint = obj.GetComponent<RawImage>();
        obj = GameObject.Find("HorizontalStickHint3");
        horizontalStick3Hint = obj.GetComponent<RawImage>();
        obj = GameObject.Find("HorizontalStickHint4");
        horizontalStick4Hint = obj.GetComponent<RawImage>();
        obj = GameObject.Find("VerticalStickHint1");
        verticalStick1Hint = obj.GetComponent<RawImage>();
        obj = GameObject.Find("VerticalStickHint2");
        verticalStick2Hint = obj.GetComponent<RawImage>();
        obj = GameObject.Find("VerticalStickHint3");
        verticalStick3Hint = obj.GetComponent<RawImage>();
        obj = GameObject.Find("VerticalStickHint4");
        verticalStick4Hint = obj.GetComponent<RawImage>();
        obj = GameObject.Find("VerticalStickHint5");
        verticalStick5Hint = obj.GetComponent<RawImage>();
        obj = GameObject.Find("VerticalStickHint6");
        verticalStick6Hint = obj.GetComponent<RawImage>();
        obj = GameObject.Find("VerticalStickHint7");
        verticalStick7Hint = obj.GetComponent<RawImage>();
        obj = GameObject.Find("BatteryHint");
        batteryHint = obj.GetComponent<RawImage>();
        obj = GameObject.Find("PotentiometerHint");
        potentiometerHint = obj.GetComponent<RawImage>();
        obj = GameObject.Find("Switch1Hint");
        switch1Hint = obj.GetComponent<RawImage>();
        obj = GameObject.Find("Switch2Hint");
        switch2Hint = obj.GetComponent<RawImage>();
        obj = GameObject.Find("LampHint");
        lampHint = obj.GetComponent<RawImage>();
        obj = GameObject.Find("BatteryTBP");
        batteryTBP = obj.GetComponent<Image>();
        obj = GameObject.Find("PotentiometerTBP");
        potentiometerTBP = obj.GetComponent<Image>();
        obj = GameObject.Find("Switch1TBP");
        switch1TBP = obj.GetComponent<Image>();
        obj = GameObject.Find("Switch2TBP");
        switch2TBP = obj.GetComponent<Image>();
        obj = GameObject.Find("LampTBP");
        lampTBP = obj.GetComponent<Image>();
        obj = GameObject.Find("HorizontalStick1TBP");
        horizontalStick1TBP = obj.GetComponent<Image>();
        obj = GameObject.Find("HorizontalStick2TBP");
        horizontalStick2TBP = obj.GetComponent<Image>();
        obj = GameObject.Find("HorizontalStick3TBP");
        horizontalStick3TBP = obj.GetComponent<Image>();
        obj = GameObject.Find("HorizontalStick4TBP");
        horizontalStick4TBP = obj.GetComponent<Image>();
        obj = GameObject.Find("VerticalStick1TBP");
        verticalStick1TBP = obj.GetComponent<Image>();
        obj = GameObject.Find("VerticalStick2TBP");
        verticalStick2TBP = obj.GetComponent<Image>();
        obj = GameObject.Find("VerticalStick3TBP");
        verticalStick3TBP = obj.GetComponent<Image>();
        obj = GameObject.Find("VerticalStick4TBP");
        verticalStick4TBP = obj.GetComponent<Image>();
        obj = GameObject.Find("VerticalStick5TBP");
        verticalStick5TBP = obj.GetComponent<Image>();
        obj = GameObject.Find("VerticalStick6TBP");
        verticalStick6TBP = obj.GetComponent<Image>();
        obj = GameObject.Find("VerticalStick7TBP");
        verticalStick7TBP = obj.GetComponent<Image>();

        // apply immages
        horizontalStick1.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/HorizontalStick");
        horizontalStick2.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/HorizontalStick");
        horizontalStick3.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/HorizontalStick");
        horizontalStick4.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/HorizontalStick");
        verticalStick1.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/VerticalStick");
        verticalStick2.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/VerticalStick");
        verticalStick3.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/VerticalStick");
        verticalStick4.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/VerticalStick");
        verticalStick5.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/VerticalStick");
        verticalStick6.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/VerticalStick");
        verticalStick7.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/VerticalStick");
        battery.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/Battery");
        potentiometer.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/Potentiometer");
        switch1.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/Switch1");
        switch2.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/Switch2");
        lamp.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/HorizontalLamp");
        horizontalStick1Hint.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/HorizontalStick_hint");
        horizontalStick2Hint.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/HorizontalStick_hint");
        horizontalStick3Hint.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/HorizontalStick_hint");
        horizontalStick4Hint.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/HorizontalStick_hint");
        verticalStick1Hint.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/VerticalStick_hint");
        verticalStick2Hint.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/VerticalStick_hint");
        verticalStick3Hint.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/VerticalStick_hint");
        verticalStick4Hint.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/VerticalStick_hint");
        verticalStick5Hint.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/VerticalStick_hint");
        verticalStick6Hint.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/VerticalStick_hint");
        verticalStick7Hint.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/VerticalStick_hint");
        batteryHint.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/Battery_hint");
        potentiometerHint.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/Potentiometer_hint");
        switch1Hint.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/Switch_hint");
        switch2Hint.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/Switch_hint");
        lampHint.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/HorizontalLamp_hint");

        platform.texture = Resources.Load<Texture2D>("Images/Minigames/PHMinigame/Platform");

        // hide all non starter objects
        horizontalStick1.gameObject.SetActive(false);
        horizontalStick2.gameObject.SetActive(false);
        horizontalStick3.gameObject.SetActive(false);
        horizontalStick4.gameObject.SetActive(false);
        verticalStick1.gameObject.SetActive(false);
        verticalStick2.gameObject.SetActive(false);
        verticalStick3.gameObject.SetActive(false);
        verticalStick4.gameObject.SetActive(false);
        verticalStick5.gameObject.SetActive(false);
        verticalStick6.gameObject.SetActive(false);
        verticalStick7.gameObject.SetActive(false);
        battery.gameObject.SetActive(false);
        potentiometer.gameObject.SetActive(false);
        switch1.gameObject.SetActive(false);
        switch2.gameObject.SetActive(false);
        lamp.gameObject.SetActive(false);
        HideAllHints();
    }

    // show hint functions
    public void ShowHintBattery()
    {
        ShowHintRawImage(battery, batteryHint, batteryTBP);
    }

    public void ShowHintPotentiomete()
    {
        ShowHintRawImage(potentiometer, potentiometerHint, potentiometerTBP);
    }

    public void ShowHintHorizontalStick1()
    {
        ShowHintRawImage(horizontalStick1, horizontalStick1Hint, horizontalStick1TBP);
    }

    public void ShowHintHorizontalStick2()
    {
        ShowHintRawImage(horizontalStick2, horizontalStick2Hint, horizontalStick2TBP);
    }

    public void ShowHintHorizontalStick3()
    {
        ShowHintRawImage(horizontalStick3, horizontalStick3Hint, horizontalStick3TBP);
    }

    public void ShowHintHorizontalStick4()
    {
        ShowHintRawImage(horizontalStick4, horizontalStick4Hint, horizontalStick4TBP);
    }

    public void ShowHintVerticalStick1()
    {
        ShowHintRawImage(verticalStick1, verticalStick1Hint, verticalStick1TBP);
    }

    public void ShowHintVerticalStick2()
    {
        ShowHintRawImage(verticalStick2, verticalStick2Hint, verticalStick2TBP);
    }

    public void ShowHintVerticalStick3()
    {
        ShowHintRawImage(verticalStick3, verticalStick3Hint, verticalStick3TBP);
    }

    public void ShowHintVerticalStick4()
    {
        ShowHintRawImage(verticalStick4, verticalStick4Hint, verticalStick4TBP);
    }

    public void ShowHintVerticalStick5()
    {
        ShowHintRawImage(verticalStick5, verticalStick5Hint, verticalStick5TBP);
    }

    public void ShowHintVerticalStick6()
    {
        ShowHintRawImage(verticalStick6, verticalStick6Hint, verticalStick6TBP);
    }

    public void ShowHintVerticalStick7()
    {
        ShowHintRawImage(verticalStick7, verticalStick7Hint, verticalStick7TBP);
    }

    public void ShowHintSwitch1()
    {
        ShowHintRawImage(switch1, switch1Hint, switch1TBP);
    }

    public void ShowHintSwitch2()
    {
        ShowHintRawImage(switch2, switch2Hint, switch2TBP);
    }

    public void ShowHintLamp()
    {
        ShowHintRawImage(lamp, lampHint, lampTBP);
    }

    // place object functions
    public void PlaceObjectHorizontalStick1()
    {
        PlaceObjectRawImage(horizontalStick1, horizontalStick1Hint, horizontalStick1TBP);
    }

    public void PlaceObjectHorizontalStick2()
    {
        PlaceObjectRawImage(horizontalStick2, horizontalStick2Hint, horizontalStick2TBP);
    }

    public void PlaceObjectHorizontalStick3()
    {
        PlaceObjectRawImage(horizontalStick3, horizontalStick3Hint, horizontalStick3TBP);
    }

    public void PlaceObjectHorizontalStick4()
    {
        PlaceObjectRawImage(horizontalStick4, horizontalStick4Hint, horizontalStick4TBP);
    }

    public void PlaceObjectVerticalStick1()
    {
        PlaceObjectRawImage(verticalStick1, verticalStick1Hint, verticalStick1TBP);
    }

    public void PlaceObjectVerticalStick2()
    {
        PlaceObjectRawImage(verticalStick2, verticalStick2Hint, verticalStick2TBP);
    }

    public void PlaceObjectVerticalStick3()
    {
        PlaceObjectRawImage(verticalStick3, verticalStick3Hint, verticalStick3TBP);
    }

    public void PlaceObjectVerticalStick4()
    {
        PlaceObjectRawImage(verticalStick4, verticalStick4Hint, verticalStick4TBP);
    }

    public void PlaceObjectVerticalStick5()
    {
        PlaceObjectRawImage(verticalStick5, verticalStick5Hint, verticalStick5TBP);
    }

    public void PlaceObjectVerticalStick6()
    {
        PlaceObjectRawImage(verticalStick6, verticalStick6Hint, verticalStick6TBP);
    }

    public void PlaceObjectVerticalStick7()
    {
        PlaceObjectRawImage(verticalStick7, verticalStick7Hint, verticalStick7TBP);
    }

    public void PlaceObjectPotentiometer()
    {
        PlaceObjectRawImage(potentiometer, potentiometerHint, potentiometerTBP);
    }

    public void PlaceObjectBattery()
    {
        PlaceObjectRawImage(battery, batteryHint, batteryTBP);
    }

    public void PlaceObjectSwitch1()
    {
        PlaceObjectRawImage(switch1, switch1Hint, switch1TBP);
    }

    public void PlaceObjectSwitch2()
    {
        PlaceObjectRawImage(switch2, switch2Hint, switch2TBP);
    }

    public void PlaceObjectLamp()
    {
        PlaceObjectRawImage(lamp, lampHint, lampTBP);
    }

    public void ShowHintRawImage(RawImage placedObject, RawImage objectHint, Image objectTBP)
    {
        HideAllHints();
        if (!placedObject.gameObject.activeInHierarchy && objectTBP.gameObject.activeInHierarchy)
        {
            objectHint.gameObject.SetActive(true);
        }
    }

    public void PlaceObjectRawImage(RawImage placedObject, RawImage objectHint, Image objectTBP)
    {
        if (objectHint.gameObject.activeInHierarchy && !placedObject.gameObject.activeInHierarchy)
        {
            objectTBP.gameObject.SetActive(false);
            objectHint.gameObject.SetActive(false);
            placedObject.gameObject.SetActive(true);
            placedObjectsNum++;
            CheckIfGameFinished();
        }
    }

    private void CheckIfGameFinished()
    {
        if (placedObjectsNum == 16)
        {
            GameEnd();
        }
    }

    private void GameEnd()
    {
        gameWon = true;
        MinigameReturnState.SetResult(null, gameWon);
        
        if (!_dialogueTriggered)
        {
            _dialogueTriggered = true;
            Debug.Log("[PHMinigame] Player won - triggering dialogue");
            MinigameDialogueHelper.TriggerDialogue("Ph", won: true, delaySeconds: 2f);
        }
    }

    private void HideAllHints()
    {
        horizontalStick1Hint.gameObject.SetActive(false);
        horizontalStick2Hint.gameObject.SetActive(false);
        horizontalStick3Hint.gameObject.SetActive(false);
        horizontalStick4Hint.gameObject.SetActive(false);
        verticalStick1Hint.gameObject.SetActive(false);
        verticalStick2Hint.gameObject.SetActive(false);
        verticalStick3Hint.gameObject.SetActive(false);
        verticalStick4Hint.gameObject.SetActive(false);
        verticalStick5Hint.gameObject.SetActive(false);
        verticalStick6Hint.gameObject.SetActive(false);
        verticalStick7Hint.gameObject.SetActive(false);
        batteryHint.gameObject.SetActive(false);
        potentiometerHint.gameObject.SetActive(false);
        switch1Hint.gameObject.SetActive(false);
        switch2Hint.gameObject.SetActive(false);
        lampHint.gameObject.SetActive(false);
    }
}
