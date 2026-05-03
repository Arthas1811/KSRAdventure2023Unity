using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using Framework.Minigames;

public class BioMinigame : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI livesText;

    [SerializeField] private TextMeshProUGUI questionTitleText;

    [SerializeField] private TextMeshProUGUI gameTitleText;

    [SerializeField] private TextMeshProUGUI gameEndText;

    [SerializeField] private TextMeshProUGUI wrongButtonText;

    [SerializeField] private TextMeshProUGUI correctButtonText;

    [SerializeField] private Image correctButtonBackground;
    [SerializeField] private RawImage Background;

    [SerializeField] private Image wrongButtonBackground;

    [SerializeField] private AudioSource audioSource;

    private AudioClip correctSound;
    private AudioClip wrongSound;
    private AudioClip spongeBobSadSound;
    private AudioClip yippeSound;

    private int lives = 3;
    private int score = 0;

    private int currentQuestion = 0;

    private string[][] questions = new string[7][];

    private bool canAnswer = true;

    private bool gameWon = false;
    private bool gameEnded = false;
    private bool _dialogueTriggered = false;

    public bool GameWon { get { return gameWon; } }
    public bool GameEnded { get { return gameEnded; } }

    private GameObject obj;

    private Texture2D BackgroundImage;

    void Start()
    {
        obj = GameObject.Find("Question Text");
        questionText = obj.GetComponent<TextMeshProUGUI>();
        obj = GameObject.Find("Score");
        scoreText = obj.GetComponent<TextMeshProUGUI>();
        obj = GameObject.Find("Lives");
        livesText = obj.GetComponent<TextMeshProUGUI>();
        obj = GameObject.Find("Question Title");
        questionTitleText = obj.GetComponent<TextMeshProUGUI>();
        obj = GameObject.Find("Game Title");
        gameTitleText = obj.GetComponent<TextMeshProUGUI>();
        obj = GameObject.Find("Game End Title");
        gameEndText = obj.GetComponent<TextMeshProUGUI>();
        obj = GameObject.Find("FalschText");
        wrongButtonText = obj.GetComponent<TextMeshProUGUI>();
        obj = GameObject.Find("RichtigText");
        correctButtonText = obj.GetComponent<TextMeshProUGUI>();

        obj = GameObject.Find("Richtig");
        correctButtonBackground = obj.GetComponent<Image>();
        obj = GameObject.Find("Falsch");
        wrongButtonBackground = obj.GetComponent<Image>();
        obj = GameObject.Find("Background");
        Background = obj.GetComponent<RawImage>();

        obj = GameObject.Find("Audio Source");
        audioSource = obj.GetComponent<AudioSource>();


        BackgroundImage = Resources.Load<Texture2D>("Images/Minigames/BioMinigame/Cooles_Bild");
        Background.texture = BackgroundImage;

        correctSound = Resources.Load<AudioClip>("Audio/Minigames/BioMinigame/ding");
        wrongSound = Resources.Load<AudioClip>("Audio/Minigames/BioMinigame/buzzer-or-wrong-answer-20582");
        spongeBobSadSound = Resources.Load<AudioClip>("Audio/Minigames/BioMinigame/spongebob_sad_music");
        yippeSound = Resources.Load<AudioClip>("Audio/Minigames/BioMinigame/yippee");


        for (int i = 0; i < questions.Length; i++)
        {
            questions[i] = new string[2];
        }

        questions[0][0] = "Alle Säugetiere legen Eier";
        questions[0][1] = "Falsch";
        questions[1][0] = "Pflanzen produzieren Sauerstoff durch Photosynthese";
        questions[1][1] = "Richtig";
        questions[2][0] = "Ein Virus kann sich ohne Wirt vermehren";
        questions[2][1] = "Falsch";
        questions[3][0] = "Menschen haben 206 Knochen im Körper";
        questions[3][1] = "Richtig";
        questions[4][0] = "Pilze sind Pflanzen";
        questions[4][1] = "Falsch";
        questions[5][0] = "Der Herzschlag beträgt 70/min im Ruhezustand";
        questions[5][1] = "Richtig";
        questions[6][0] = "Fische atmen Luft durch die Lungen";
        questions[6][1] = "Falsch";

        ShowAllText();
    }

    public async void CorrectButton()
    {
        if (canAnswer)
        {
            canAnswer = false;
            await UpdateAll("Richtig");
        }
    }
    public async void WrongButton()
    {
        if (canAnswer)
        {
            canAnswer = false;
            await UpdateAll("Falsch");
        }
    }

    public async Task UpdateAll(string buttonPress)
    {
        if (currentQuestion < questions.Length)
        {
            if (buttonPress == questions[currentQuestion][1])
            {
                audioSource.PlayOneShot(correctSound);
                score++;
                await Task.Delay(1000);
            }
            else
            {
                audioSource.PlayOneShot(wrongSound);
                lives--;
                await Task.Delay(1000);
            }
            currentQuestion++;
        }
        if (lives == 0)
        {
            await EndScreen("lose");
        }
        else if (score == 5)
        {
            await EndScreen("win");
        }
        else
        {
            ShowAllText();
        }
    }

    public void ShowAllText()
    {
        questionText.text = questions[currentQuestion][0];
        scoreText.text = $"Score: {score}";
        livesText.text = $"Lives: {lives}";
        canAnswer = true;
    }

    public async Task EndScreen(string ending)
    {
        questionText.text = "";
        scoreText.text = "";
        livesText.text = "";
        questionTitleText.text = "";
        gameTitleText.text = "";
        wrongButtonText.text = "";
        wrongButtonBackground.color = new Color32(0, 0, 0, 0);
        correctButtonText.text = "";
        correctButtonBackground.color = new Color32(0, 0, 0, 0);


        if (ending == "lose")
        {
            audioSource.PlayOneShot(spongeBobSadSound);
            gameEndText.text = "You Lose!";
            gameEndText.color = new Color32(172, 0, 0, 255);
            await Task.Delay(1000);
            
            if (!_dialogueTriggered)
            {
                _dialogueTriggered = true;
                Debug.Log("[BioMinigame] Player lost - triggering dialogue");
                MinigameDialogueHelper.TriggerDialogue("Bio", won: false, delaySeconds: 2f);
            }
        }
        else
        {
            audioSource.PlayOneShot(yippeSound);
            gameEndText.text = "You Win!";
            gameEndText.color = new Color32(0, 146, 0, 255);
            await Task.Delay(1000);
            gameWon = true;
            
            if (!_dialogueTriggered)
            {
                _dialogueTriggered = true;
                Debug.Log("[BioMinigame] Player won - triggering dialogue");
                MinigameDialogueHelper.TriggerDialogue("Bio", won: true, delaySeconds: 2f);
            }
        }
        gameEnded = true;
        MinigameReturnState.SetResult(gameEnded, gameWon);
    }
}
