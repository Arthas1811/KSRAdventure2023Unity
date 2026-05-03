using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Framework.Minigames;

public class LockpickingGame : MonoBehaviour
{
    private int clicks = 0;
    public int MaxClicks = 0;
    public TextMeshProUGUI counter;
    private int tries = 10;
    public TextMeshProUGUI triedcounter;

    public NumberSelector dial1;
    public NumberSelector dial2;
    public NumberSelector dial3;
    public NumberSelector dial4;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button addCounterButton;
    private bool lockBreakingEnabled;
    private bool finished;
    private Sprite closedSprite;
    private Sprite openSprite;

    private int number1;
    private int number2;
    private int number3;
    private int number4;

    private void Awake()
    {
        dial1 = dial1 ?? FindDial("dial_1", "dial1", "Dial_1", "Dial1");
        dial2 = dial2 ?? FindDial("dial_2", "dial2", "Dial_2", "Dial2");
        dial3 = dial3 ?? FindDial("dial_3", "dial3", "Dial_3", "Dial3");
        dial4 = dial4 ?? FindDial("dial_4", "dial4", "Dial_4", "Dial4");

        counter = counter ?? FindText("countertext", "counter", "CounterText", "Counter");
        triedcounter = triedcounter ?? FindText("Tried", "TriedCounter", "TriedText", "tries");

        confirmButton = confirmButton ?? FindButton("confrmButton", "confirmButton", "ConfirmButton", "confirm");
        addCounterButton = addCounterButton ?? FindButton("button", "Button", "addCounterButton", "AddCounterButton");

        backgroundImage = backgroundImage ?? FindImage("Background", "BackgroundImage", "Background image", "background");
        if (backgroundImage != null)
        {
            closedSprite = LoadSprite("images/Minigames/Lockpicking/LockerVonVorne", "Images/Minigames/Lockpicking/LockerVonVorne");
            openSprite = LoadSprite("images/Minigames/Lockpicking/GeoeffneterLocker", "Images/Minigames/Lockpicking/GeoeffneterLocker");
            if (closedSprite != null) backgroundImage.sprite = closedSprite;
        }

        if (MaxClicks <= 0)
        {
            MaxClicks = 10;
        }

        if (triedcounter != null)
        {
            triedcounter.text = "Tries: " + tries.ToString();
        }

        WireDialButton(dial1);
        WireDialButton(dial2);
        WireDialButton(dial3);
        WireDialButton(dial4);

        WireButton(confirmButton, confirm);
        WireButton(addCounterButton, addCounter);

        SetButtonInteractable(addCounterButton, false);
    }

    public void confirm()
    {
        if (finished) return;
        if (dial1 == null || dial2 == null || dial3 == null || dial4 == null)
        {
            Debug.LogWarning("LockpickingGame: Dial references missing.");
            return;
        }
        number1 = dial1.currentNumber;
        number2 = dial2.currentNumber;
        number3 = dial3.currentNumber;
        number4 = dial4.currentNumber;
        if (number1 == 1 && number2 == 9 && number3 == 6 && number4 == 8)
        {
            finish(true);
        }
        else
        {
            tries = Mathf.Max(tries - 1, 0);
            if (triedcounter != null)
            {
                triedcounter.text = "Tries: " + tries.ToString();
            }
            if (tries == 0) EnableLockBreaking();
        }
    }

    public void addCounter()
    {
        if (finished || !lockBreakingEnabled) return;
        clicks += 1;
        Debug.Log(clicks);
        if (counter != null)
        {
            counter.text = clicks.ToString();
        }
        if (clicks >= MaxClicks)
        {
            finish(false);
        }
    }

    void finish(bool openedByCode)
    {
        if (finished) return;
        finished = true;
        Debug.Log("finished");
        if (backgroundImage != null && openSprite != null)
        {
            backgroundImage.sprite = openSprite;
        }
        SetButtonInteractable(confirmButton, false);
        SetButtonInteractable(addCounterButton, false);
        SetDialInteractable(dial1, false);
        SetDialInteractable(dial2, false);
        SetDialInteractable(dial3, false);
        SetDialInteractable(dial4, false);
        var gameWon = openedByCode || lockBreakingEnabled;
        MinigameReturnState.SetResult(true, gameWon);
        // play cutscene
        // add frog to inventory
    }

    private void EnableLockBreaking()
    {
        if (lockBreakingEnabled) return;
        lockBreakingEnabled = true;
        clicks = 0;
        if (counter != null)
        {
            counter.gameObject.SetActive(true);
            counter.text = clicks.ToString();
        }
        SetButtonInteractable(addCounterButton, true);
        if (addCounterButton != null)
        {
            addCounterButton.transform.SetAsLastSibling();
        }
        SetButtonInteractable(confirmButton, false);
        SetDialInteractable(dial1, false);
        SetDialInteractable(dial2, false);
        SetDialInteractable(dial3, false);
        SetDialInteractable(dial4, false);
    }

    private static NumberSelector FindDial(params string[] names)
    {
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var go = GameObject.Find(name);
            if (go == null) continue;
            var dial = go.GetComponent<NumberSelector>();
            if (dial != null) return dial;
        }
        return null;
    }

    private static TextMeshProUGUI FindText(params string[] names)
    {
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var go = GameObject.Find(name);
            if (go == null) continue;
            var text = go.GetComponent<TextMeshProUGUI>();
            if (text != null) return text;
            text = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null) return text;
        }
        return null;
    }

    private static Button FindButton(params string[] names)
    {
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var go = GameObject.Find(name);
            if (go == null) continue;
            var button = go.GetComponent<Button>();
            if (button != null) return button;
        }
        return null;
    }

    private static Image FindImage(params string[] names)
    {
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var go = GameObject.Find(name);
            if (go == null) continue;
            var image = go.GetComponent<Image>();
            if (image != null) return image;
        }
        return null;
    }

    private static void WireDialButton(NumberSelector dial)
    {
        if (dial == null) return;
        var button = dial.GetComponent<Button>();
        if (button == null) return;
        if (button.onClick.GetPersistentEventCount() == 0)
        {
            button.onClick.AddListener(dial.clickNumber);
        }
    }

    private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        if (button.onClick.GetPersistentEventCount() == 0)
        {
            button.onClick.AddListener(action);
        }
    }

    private static void SetButtonInteractable(Button button, bool enabled)
    {
        if (button == null) return;
        button.interactable = enabled;
    }

    private static void SetDialInteractable(NumberSelector dial, bool enabled)
    {
        if (dial == null) return;
        var button = dial.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = enabled;
        }
    }

    private static Sprite LoadSprite(string primaryPath, string fallbackPath)
    {
        var sprite = Resources.Load<Sprite>(primaryPath);
        return sprite != null ? sprite : Resources.Load<Sprite>(fallbackPath);
    }
}
