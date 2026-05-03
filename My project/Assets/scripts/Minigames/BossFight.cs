using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections;
using Framework.Minigames;

public class BossFight : MonoBehaviour
{
    [SerializeField] private RawImage background;
    [SerializeField] private RawImage noRedbullBackground;
    [SerializeField] private RawImage playerHealthBar;
    [SerializeField] private RawImage bossHealthBar;

    private float maxBossHealth = 300.0f;
    private float maxPlayerHealth = 100.0f;
    private float bossHealth;
    private float playerHealth;
    [SerializeField] private TextMeshProUGUI attackInformationText;
    [SerializeField] private TextMeshProUGUI missText;
    [SerializeField] private TextMeshProUGUI bossMissText;

    [SerializeField] private AudioSource userHitsAudio;
    [SerializeField] private AudioSource bossHitsAudio;

    private bool canAttack = true;

    // add getters
    private bool gameEnded = false;
    public bool GameEnded { get { return gameEnded; } }
    private bool gameWon = false;
    public bool GameWon { get { return gameWon; } }

    private int redbullAmount;
    public int RedbullAmount { get { return redbullAmount; } set { redbullAmount = value; } }
    
    private Texture2D drinkRedBull;
    private Texture2D fightToPunch2;
    private Texture2D fightPosition; // of boss
    private Texture2D fightToKick;
    private Texture2D fightToPunch1;
    private Texture2D kickFromBoss;
    private Texture2D leftKick;
    private Texture2D leftKickAndRightPunch;
    private Texture2D noRedBull;
    private Texture2D openRedBull;
    private Texture2D punchFromBoss1;
    private Texture2D punchFromBoss2;
    private Texture2D pushBoss;
    private Texture2D getRedBullBoss;
    private Texture2D takeRedBullBoss;
    private Texture2D rightPunch;

    private AudioClip drinkSound;
    private AudioClip punchSound;
    private AudioClip pushSound;

    private float currentPushDamage = 1.0f;
    private float punchStreak = 0.0f;
    private float kickStreak = 0.0f;

    private float lastBossAttackTime;

    [SerializeField] private RawImage buttonsBackground;
    [SerializeField] private RawImage attackInformationBackground;

    private Texture2D box;
    private GameObject obj;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        obj = GameObject.Find("Background");
        background = obj.GetComponent<RawImage>();
        obj = GameObject.Find("NoRedBull");
        noRedbullBackground = obj.GetComponent<RawImage>();
        noRedbullBackground.gameObject.SetActive(false);
        obj = GameObject.Find("PlayerHealthBar");
        playerHealthBar = obj.GetComponent<RawImage>();
        obj = GameObject.Find("BossHealthBar");
        bossHealthBar = obj.GetComponent<RawImage>();
        obj = GameObject.Find("ActionInformationText");
        attackInformationText = obj.GetComponent<TextMeshProUGUI>();
        obj = GameObject.Find("MissText");
        missText = obj.GetComponent<TextMeshProUGUI>();
        obj = GameObject.Find("BossMissedText");
        missText.gameObject.SetActive(false);
        bossMissText = obj.GetComponent<TextMeshProUGUI>();
        bossMissText.gameObject.SetActive(false);
        obj = GameObject.Find("UserHitsAudio");
        userHitsAudio = obj.GetComponent<AudioSource>();
        obj = GameObject.Find("BossHitsAudio");
        bossHitsAudio = obj.GetComponent<AudioSource>();

        box = Resources.Load<Texture2D>("Images/Minigames/BossFight/box");
        obj = GameObject.Find("InformationBackground");
        attackInformationBackground = obj.GetComponent<RawImage>();
        attackInformationBackground.texture = box;
        obj = GameObject.Find("ButtonsBackground");
        buttonsBackground = obj.GetComponent<RawImage>();
        buttonsBackground.texture = box;

        bossHealth = maxBossHealth;
        playerHealth = maxPlayerHealth;
        drinkRedBull = Resources.Load<Texture2D>("Images/Minigames/BossFight/DrinkRedbull");
        fightToPunch2 = Resources.Load<Texture2D>("Images/Minigames/BossFight/FightPoitionToPunch_2");
        fightPosition = Resources.Load<Texture2D>("Images/Minigames/BossFight/FightPositionOfBoss");
        fightToKick = Resources.Load<Texture2D>("Images/Minigames/BossFight/FightPositionToKick");
        fightToPunch1 = Resources.Load<Texture2D>("Images/Minigames/BossFight/FightPositionToPunch_1");
        kickFromBoss = Resources.Load<Texture2D>("Images/Minigames/BossFight/KickFromBoss");
        leftKick = Resources.Load<Texture2D>("Images/Minigames/BossFight/LeftKick");
        leftKickAndRightPunch = Resources.Load<Texture2D>("Images/Minigames/BossFight/LeftKickAndRightPunch");
        noRedBull = Resources.Load<Texture2D>("Images/Minigames/BossFight/no_redbull");
        openRedBull = Resources.Load<Texture2D>("Images/Minigames/BossFight/OpenRedbull_2");
        punchFromBoss1 = Resources.Load<Texture2D>("Images/Minigames/BossFight/PunchFromBoss_1");
        punchFromBoss2 = Resources.Load<Texture2D>("Images/Minigames/BossFight/PunchFromBoss_2");
        pushBoss = Resources.Load<Texture2D>("Images/Minigames/BossFight/PushBoss");
        getRedBullBoss = Resources.Load<Texture2D>("Images/Minigames/BossFight/RedbullFromBoss_1");
        takeRedBullBoss = Resources.Load<Texture2D>("Images/Minigames/BossFight/RedbullFromBoss_2");
        rightPunch = Resources.Load<Texture2D>("Images/Minigames/BossFight/Right_Punch");
        background.texture = fightPosition;
        noRedbullBackground.texture = noRedBull;
        noRedbullBackground.gameObject.SetActive(false);
        attackInformationText.text = "";
        missText.gameObject.SetActive(false);
        playerHealthBar.color = Color.green;
        lastBossAttackTime = Time.time;

        pushSound = Resources.Load<AudioClip>("Audio/Minigames/BossFight/push");
        punchSound = Resources.Load<AudioClip>("Audio/Minigames/BossFight/punch");
        drinkSound = Resources.Load<AudioClip>("Audio/Minigames/BossFight/drink");

    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - lastBossAttackTime >= 15.0f)
        {
            canAttack = false;
            BossAttack();
        }
        if (bossHealth <= 0)
        {
            gameEnded = true;
            gameWon = true;
        }
        else if (playerHealth <= 0)
        {
            gameEnded = true;
        }

        if (gameEnded || gameWon)
        {
            MinigameReturnState.SetResult(gameEnded, gameWon);
        }
    }

    async public void RedbullButton()
    {
        if (canAttack)
        {
            UpdatePushDamage();
            canAttack = !canAttack;
            punchStreak = 0.0f;
            kickStreak = 0.0f;
            if (bossHealth >= 0.2 * maxBossHealth && playerHealth >= 0.7 * maxPlayerHealth)
            {
                background.texture = getRedBullBoss;
                await Task.Delay(750);
                background.texture = takeRedBullBoss;
                await Task.Delay(750);
                background.texture = openRedBull;
                await Task.Delay(750);
                background.texture = drinkRedBull;
                bossHitsAudio.PlayOneShot(drinkSound); // closer sound
                await playerHealthAnimationReload(playerHealth, maxPlayerHealth);
                await Task.Delay(2000);

                background.texture = fightPosition;
                canAttack = !canAttack;

            }
            else if (redbullAmount == 0)
            {
                noRedbullBackground.gameObject.SetActive(true);
                await Task.Delay(750);
                noRedbullBackground.gameObject.SetActive(false);
                canAttack = !canAttack;
            }
            else
            {
                redbullAmount--;
                background.texture = openRedBull;
                await Task.Delay(750);
                background.texture = drinkRedBull;
                bossHitsAudio.PlayOneShot(drinkSound); // closer sound
                await playerHealthAnimationReload(playerHealth, maxPlayerHealth);
                await Task.Delay(2000);
                background.texture = fightPosition;
                canAttack = !canAttack;
            }
        }
    }

    public void DisplayRedbullInfo()
    {
        attackInformationText.text = "H: >100 REQ: RB";
    }

    private void UpdatePushDamage()
    {
        currentPushDamage = 1.2f;
        if (currentPushDamage >= 6.0f)
        {
            currentPushDamage = 6.0f;
        }
    }

    public async void PushButton()
    {
        if (canAttack)
        {
            canAttack = !canAttack;
            punchStreak = 0.0f;
            kickStreak = 0.0f;
            if (Random.Range(0, 2) == 0)
            {
                background.texture = fightToPunch1;
            }
            else
            {
                background.texture = fightToPunch2;
            }
            await Task.Delay(750);
            if (Random.Range(1, 11) < 9)
            {
                background.texture = pushBoss;
                userHitsAudio.PlayOneShot(pushSound);
                await bossHealthAnimation(bossHealth, bossHealth - currentPushDamage);
                if (currentPushDamage < 6.0f)
                {
                    currentPushDamage *= 1.2f;
                }
                else
                {
                    currentPushDamage = 6.0f;
                }
            }
            else
            {
                UpdatePushDamage();
                missText.gameObject.SetActive(true);
            }
            await Task.Delay(1000);
            background.texture = fightPosition;
            missText.gameObject.SetActive(false);
            BossAttack();
        }
    }

    public void DisplayPushInfo()
    {
        attackInformationText.text = "Mul: 1.2 ACC: 90%";
    }

    public async void PunchButton()
    {
        if (canAttack)
        {
            UpdatePushDamage();
            kickStreak = 0.0f;
            canAttack = !canAttack;
            if (Random.Range(0, 2) == 0)
            {
                background.texture = fightToPunch1;
            }
            else
            {
                background.texture = fightToPunch2;
            }
            await Task.Delay(750);
            if (Random.Range(1, 11) < 8)
            {
                background.texture = rightPunch;
                userHitsAudio.PlayOneShot(punchSound);
                await bossHealthAnimation(bossHealth, bossHealth - 9.0f * Mathf.Pow(1.01f, punchStreak));
                punchStreak++;
            }
            else
            {
                missText.gameObject.SetActive(true);
                punchStreak = 0.0f;
            }
            await Task.Delay(1000);
            background.texture = fightPosition;
            missText.gameObject.SetActive(false);
            BossAttack();
        }
    }
    public void DisplayPunchInfo()
    {
        attackInformationText.text = "Dmg: 9 ACC: 80%";
    }

    public void ResetInfo()
    {
        attackInformationText.text = "";
    }

    public async void KickButton()
    {
        if (canAttack)
        {
            UpdatePushDamage();
            punchStreak = 0.0f;
            canAttack = !canAttack;
            background.texture = fightToKick;
            await Task.Delay(750);
            if (Random.Range(1, 11) < 3)
            {
                if (Random.Range(0, 3) < 2)
                {
                    background.texture = leftKick;
                    userHitsAudio.PlayOneShot(punchSound);
                    await bossHealthAnimation(bossHealth, bossHealth - 20.0f * Mathf.Pow(1.05f, kickStreak));
                    kickStreak++;
                }
                else
                {
                    background.texture = leftKickAndRightPunch;
                    userHitsAudio.PlayOneShot(punchSound);
                    await bossHealthAnimation(bossHealth, bossHealth - 22.0f * Mathf.Pow(1.1f, kickStreak));
                    kickStreak++;
                }
            }
            else
            {
                missText.gameObject.SetActive(true);
                kickStreak = 0.0f;
            }
            await Task.Delay(1000);
            background.texture = fightPosition;
            missText.gameObject.SetActive(false);
            BossAttack();
        }
    }

    public void DisplayKickInfo()
    {
        attackInformationText.text = "Dmg: 20 ACC: 30%";
    }

    public async void BossAttack()
    {
        lastBossAttackTime = Time.time;

        if (Random.Range(0, 4) == 0)
        {
            if (Random.Range(0, 2) == 0)
            {
                if (Random.Range(1, 11) < 9)
                {
                    background.texture = punchFromBoss1;
                    bossHitsAudio.PlayOneShot(punchSound);
                    await playerHealthAnimation(playerHealth, playerHealth - 9.0f);
                }
                else
                {
                    bossMissText.gameObject.SetActive(true);
                }
            }
            else
            {
                if (Random.Range(1, 11) < 8)
                {
                    background.texture = punchFromBoss2;
                    bossHitsAudio.PlayOneShot(punchSound);
                    await playerHealthAnimation(playerHealth, playerHealth - 10.0f);
                }
                else
                {
                    bossMissText.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            if (Random.Range(1, 11) < 6)
            {
                background.texture = kickFromBoss;
                bossHitsAudio.PlayOneShot(punchSound);
                await playerHealthAnimation(playerHealth, playerHealth - 25.0f);
            }
            else
            {
                bossMissText.gameObject.SetActive(true);
            }
        }
        await Task.Delay(1000);
        bossMissText.gameObject.SetActive(false);
        background.texture = fightPosition;
        lastBossAttackTime = Time.time; // reset again to make sure that the player does not get perma-attacked
        canAttack = !canAttack;
        return;
    }

    async Task playerHealthAnimation(float startHealth, float endHealth)
    {
        if (endHealth < 0)
        {
            endHealth = 0;
        }
        for (float i = startHealth; i >= endHealth; i--)
        {
            float newWidth = i;
            RectTransform rt = playerHealthBar.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(newWidth, rt.sizeDelta.y);
            await Task.Delay(5);
        }
        playerHealth = endHealth;
        UpdateHealthColor();
        if (playerHealth <= 0)
        {
            gameEnded = true;
        }
    }

    async Task playerHealthAnimationReload(float startHealth, float endHealth)
    {
        for (float i = startHealth; i <= endHealth; i++)
        {
            float newWidth = i;
            RectTransform rt = playerHealthBar.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(newWidth, rt.sizeDelta.y);
            await Task.Delay(15);
        }
        playerHealth = endHealth;
    }

    public void UpdateHealthColor()
    {
        if (playerHealth > 0.5 * maxPlayerHealth)
        {
            playerHealthBar.color = Color.green;
        }
        else if (playerHealth <= 0.2 * maxPlayerHealth)
        {
            playerHealthBar.color = Color.red;
        }
        else if (playerHealth <= 0.5 * maxPlayerHealth)
        {
            playerHealthBar.color = Color.yellow;
        }
    }

    async Task bossHealthAnimation(float startHealth, float endHealth)
    {
        if (endHealth < 0)
        {
            endHealth = 0;
        }
        for (float i = startHealth; i >= endHealth; i--)
        {
            float newWidth = i / 2;
            RectTransform rt = bossHealthBar.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(newWidth, rt.sizeDelta.y);
            await Task.Delay(5);
        }

        bossHealth = endHealth;
        if (bossHealth <= 0)
        {
            gameEnded = true;
            gameWon = true;
        }
    }


}
