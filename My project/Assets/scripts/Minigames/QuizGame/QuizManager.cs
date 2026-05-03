using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[System.Serializable]
public class Question
{
    public string question;
    public string[] answers;
    public int correctIndex;    
}

[System.Serializable]
public class QuestionList
{
    public Question[] questions;
}

public class QuizManager : MonoBehaviour
{
    [Header("JSON file (in Assets/Resources)")]
    public string jsonFileName = "questions";

    [Header("UI")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI answerLogText;
    public TextMeshProUGUI PointsScored; 
    public Button[] answerButtons; 
    public int points = 0;

    [Header("Behavior")]
    public float feedbackDelay = 2f; 

    private QuestionList quizData;
    private int currentQuestion = 0;
    private bool _dialogueTriggered = false;

    private static readonly string QuizResourceFolder = "jsons/QuizGame";

    void Start()
    {
        if (answerButtons == null || answerButtons.Length != 4)
        {
            Debug.LogError("QuizManager: Need 4 answer buttons.");
            return;
        }
        if (questionText == null || answerLogText == null || PointsScored == null)
        {
            Debug.LogError("QuizManager: Assign questionText, answerLogText, and PointsScored in inspector");
            return;
        }

        LoadQuestionsFromResources();

        if (quizData == null || quizData.questions == null || quizData.questions.Length == 0) {
            Debug.LogError("QuizManager: No questions from JSON.");
            return;
        }

        answerLogText.text = "";

        ShowQuestion();
    }

    void LoadQuestionsFromResources()
    {
        if (string.IsNullOrWhiteSpace(jsonFileName))
        {
            Debug.LogError("QuizManager: jsonFileName is empty.");
            return;
        }

        TextAsset ta = null;
        var resourcePaths = GetQuestionResourcePaths(jsonFileName);
        foreach (var path in resourcePaths)
        {
            ta = Resources.Load<TextAsset>(path);
            if (ta != null)
            {
                break;
            }
        }

        if (ta == null)
        {
            Debug.LogError($"QuizManager: Could not find {jsonFileName}.json. Tried: {string.Join(", ", resourcePaths)}");
            return;
        }

        try
        {
            quizData = JsonUtility.FromJson<QuestionList>(ta.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError("QuizManager: JSON error: " + e.Message);
        }
    }

    private static string[] GetQuestionResourcePaths(string resourceName)
    {
        if (resourceName.Contains("/") || resourceName.Contains("\\"))
        {
            return new[] { resourceName };
        }

        return new[] { resourceName, $"{QuizResourceFolder}/{resourceName}" };
    }

    void ShowQuestion()
    {
        if (currentQuestion >= quizData.questions.Length)
        {
            questionText.text = "Quiz Over!";
            answerLogText.text = "";

            int questionsLength = quizData.questions.Length;
            PointsScored.text = "Score: " + points.ToString() + "/" + questionsLength.ToString();
            foreach (var b in answerButtons) b.gameObject.SetActive(false);
            
            if (!_dialogueTriggered)
            {
                _dialogueTriggered = true;
                bool won = points >= questionsLength / 2;
                Debug.Log($"[QuizGame] Quiz completed - Score: {points}/{questionsLength}, won: {won}");
                MinigameDialogueHelper.TriggerDialogue("Geo", won: won, delaySeconds: 2f);
            }
            return;
        }   

        var q = quizData.questions[currentQuestion];
        questionText.text = q.question;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            btn.onClick.RemoveAllListeners();

            if (q.answers != null && i < q.answers.Length)
            {
                btn.gameObject.SetActive(true);
                var idx = i; 
                var label = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) {
                    label.text = q.answers[i];
                };
                btn.interactable = true;
                btn.onClick.AddListener(() => OnAnswer(idx));
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

    void OnAnswer(int index)
    {
        foreach (var b in answerButtons) b.interactable = false;

        var correct = quizData.questions[currentQuestion].correctIndex;
        questionText.text = "";
        if (index == correct) {
            answerLogText.text = "Correct!";
            points = points + 1;
        } else {
            answerLogText.text = "Wrong!";
        }

        StartCoroutine(AdvanceAfterDelay(feedbackDelay));
    }

    IEnumerator AdvanceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        answerLogText.text = "";
        currentQuestion++;
        ShowQuestion();
    }
}
