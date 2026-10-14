using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // UI elemanları için
using TMPro; // TextMeshPro için
using UnityEngine.SceneManagement; // Sahne yüklemek/yeniden başlatmak için
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    [Header("Kart Ayarları")]
    public GameObject cardPrefab;
    public Sprite[] cardSprites;

    [Header("Zar Referansları")]
    public Dice dice1;
    public Dice dice2;

    [Header("Oyun Durumu")]
    public int currentPlayerIndex = 0;
    public int currentDiceTotal = 0;
    public bool hasRolledDice = false;

    [Header("Oyuncu Listesi")]
    public List<Player> players = new List<Player>();

    [Header("Bot Ayarları")]
    //public bool[] botPlayers = new bool[4] { true, true, true, false }; // Oyuncu 4 (alt) insan
    public float botThinkDelay = 1f;   // Zar atmadan/onaylamadan önce bekleme
    public float botSelectDelay = 0.4f; // Her kart seçimi arası bekleme

    private Coroutine botRoutine;
    private bool botActing = false; // Sadece botun kendi çağrılarına izin verir

    //[Header("Oyuncu Prefab'i")]
    //public GameObject playerAreaPrefab; // Inspector'dan hazırladığımız Prefab'i buraya atayacağız

    [Header("Kazanma Ekranı UI")]
    public GameObject winPanel;
    public TextMeshProUGUI winText;

    [Header("Oyuncu Alanı Renkleri")]
    public Color defaultAreaColor = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Bekleyen oyuncu
    public Color turnAreaColor = Color.yellow;                          // Sırası gelen oyuncu
    public Color noMoveAreaColor = Color.red;                           // Pas yapmak zorunda olan
    public Color winnerAreaColor = Color.blue;                          // Kazanan oyuncu

    [Header("UI Canvas & Prefab")]
    public Transform uiCanvasTransform;
    public GameObject playerBackgroundPrefab;

    [Header("1. UI Panellerin Pozisyon Ayarları (Canvas/Pixel)")]
    [Tooltip("Sırasıyla 1, 2, 3 ve 4. Oyuncunun UI Panel Merkez Pozisyonları")]
    public Vector2[] playerUIPositions = new Vector2[4]
    {
        new Vector2(0f, 380f),   // Oyuncu 1 (Üst)
        new Vector2(-650f, 0f),  // Oyuncu 2 (Sol)
        new Vector2(650f, 0f),   // Oyuncu 3 (Sağ)
        new Vector2(0f, -380f)   // Oyuncu 4 (Alt)
    };

    [Header("2. Kartların Dünya Pozisyon Ayarları (World Space)")]
    [Tooltip("Sırasıyla 1, 2, 3 ve 4. Oyuncunun Kart Gruplarının Merkez Pozisyonları")]
    public Vector3[] playerWorldPositions = new Vector3[4]
    {
        new Vector3(0f, 6.4f, 0f),   // Oyuncu 1 (Üst)
        new Vector3(-9.4f, 0f, 0f),   // Oyuncu 2 (Sol)
        new Vector3(9.4f, 0f, 0f),    // Oyuncu 3 (Sağ)
        new Vector3(0f, -6.4f, 0f)   // Oyuncu 4 (Alt)
    };

    [Header("3. Kart Düzeni")]
    public float cardSpacing = 1.2f; // Aynı satırdaki kartlar arası mesafe
    public float rowSpacing = 1.2f;  // İki satır arası mesafe

    private bool isDiceRolling = false;

    // Pozisyon ve Rotation Döndüren Yardımcı Metotlar:
    Vector2 GetPlayerCenterUIPosition(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < playerUIPositions.Length)
            return playerUIPositions[playerIndex];
        return Vector2.zero;
    }

    public Vector3 GetPlayerWorldPosition(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < playerWorldPositions.Length)
            return playerWorldPositions[playerIndex];
        return Vector3.zero;
    }

    Quaternion GetPlayerRotation(int playerIndex)
    {
        switch (playerIndex)
        {
            case 0: return Quaternion.Euler(0, 0, 180); // Üst (Ters)
            case 1: return Quaternion.Euler(0, 0, -90); // Sol
            case 2: return Quaternion.Euler(0, 0, 90);  // Sağ
            case 3: return Quaternion.identity;        // Alt (Düz)
            default: return Quaternion.identity;
        }
    }
    private void OnValidate()
    {
        // Yalnızca oyun çalışırken ve oyuncular oluşturulmuşsa pozisyonları canlı güncelle
        if (Application.isPlaying && players != null && players.Count > 0)
        {
            UpdatePositionsInRealTime();
        }
    }

    public void UpdatePositionsInRealTime()
    {
        for (int p = 0; p < players.Count; p++)
        {
            ApplyPlayerLayout(p);
        }
    }

    // Oyuncunun grubunu (pozisyon + ROTASYON + kartlar + UI paneli) tek yerden yerleştirir.
    // Hem oyun başında hem Inspector'dan canlı güncellemede aynı kod çalışır.
    void ApplyPlayerLayout(int p)
    {
        if (p < 0 || p >= players.Count || players[p] == null) return;
        Player player = players[p];



        // 1. Kart grubunun konumu VE dönüşü (kartlar local olduğu için birlikte döner)
        player.transform.SetPositionAndRotation(GetPlayerWorldPosition(p), GetPlayerRotation(p));

        // 2. Kartları grubun local uzayında diz
        LayoutCards(player, p);

        // 3. UI paneli
        if (player.areaBackgroundImage != null)
        {
            RectTransform rect = player.areaBackgroundImage.GetComponent<RectTransform>();
            if (rect != null)
            {
                // anchoredPosition, anchor'a göre çalışır; merkeze sabitliyoruz
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = GetPlayerCenterUIPosition(p);
                rect.localRotation = GetPlayerRotation(p);
                rect.localScale = Vector3.one;
            }
        }
    }

    void LayoutCards(Player player, int p)
    {
        for (int i = 0; i < player.myCards.Count; i++)
        {
            if (player.myCards[i] == null) continue;

            int valueIndex = i / 2; // 0..8  (kart değeri - 1)
            int row = i % 2;        // 0 veya 1

            // Alt oyuncuda soldan sağa 1..9. Diğerleri bunun döndürülmüş hali.
            // Oyuncu 3'te (sağ) 1'in yukarıda olması için sırayı ters çeviriyoruz.
            int column = (p == 2) ? 8 - valueIndex : valueIndex;

            float x = (column - 4) * cardSpacing;
            float y = (row - 0.5f) * rowSpacing;

            Transform t = player.myCards[i].transform;
            t.localPosition = new Vector3(x, y, 0f);
            t.localRotation = Quaternion.identity;
        }
    }

    void Start()
    {
        // Oyun başında kazanma panelinin kapalı olduğundan emin olalım
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        InitializePlayersAndCards();
        StartBotTurnIfNeeded();
    }

    void Update()
    {
        // 1. R Tuşu: Her an oyunu yeniden başlatabilmek için (Kazanma ekranında da çalışır)
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
            return;
        }
        // Sıra botteyse insan girdisini yok say
        if (IsBotPlayer(currentPlayerIndex)) return;

        // Eğer oyun bittiyse (Win Panel açıksa) diğer klavye girdilerini dinleme
        if (winPanel != null && winPanel.activeSelf)
        {
            return;
        }

        // 2. Space Tuşu: Zar atma
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RollDice();
        }

        // 3. Enter veya Keypad Enter: Hamleyi onaylama (Kart kapatma)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmMove();
        }

        // 4. P Tuşu: Pas geçme
        if (Input.GetKeyDown(KeyCode.P))
        {
            PassTurn();
        }
    }

    void UpdatePlayerAreaVisuals()
    {
        for (int p = 0; p < players.Count; p++)
        {
            Player player = players[p];
            // bu animasyon kalp atısı gibi atıyor
            //RectTransform r = players[p].areaBackgroundImage.rectTransform;
            //r.DOKill();
            //r.localScale = Vector3.one;
            //if (p == currentPlayerIndex)
            //    r.DOScale(1.08f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

            if (player.HasWon())
            {
                player.SetBackgroundColor(winnerAreaColor); // MAVİ
            }
            else if (p == currentPlayerIndex)
            {
                // Zar atıldıysa ve yapılabilecek hiçbir hamle yoksa
                if (hasRolledDice && !CanMakeMove(player, currentDiceTotal))
                {
                    player.SetBackgroundColor(noMoveAreaColor); // KIRMIZI
                }
                else
                {
                    player.SetBackgroundColor(turnAreaColor); // SARI
                }
            }
            else
            {
                player.SetBackgroundColor(defaultAreaColor); // VARSAYILAN / SİYAH
            }
        }
    }

    // Oyuncunun açık kartlarıyla hedef zarı elde edip edemeyeceğini hesaplayan fonksiyon
    bool CanMakeMove(Player player, int targetSum)
    {
        List<int> openCardValues = new List<int>();
        foreach (Card card in player.myCards)
        {
            if (!card.isClosed)
            {
                openCardValues.Add(card.cardValue);
            }
        }

        return HasCombinationForSum(openCardValues, openCardValues.Count, targetSum);
    }

    // Rekürsif Toplam Kontrolü
    bool HasCombinationForSum(List<int> cards, int n, int sum)
    {
        if (sum == 0) return true;
        if (n == 0 || sum < 0) return false;

        return HasCombinationForSum(cards, n - 1, sum) ||
               HasCombinationForSum(cards, n - 1, sum - cards[n - 1]);
    }

    void InitializePlayersAndCards()
    {
        for (int p = 0; p < 4; p++)
        {
            GameObject playerObj = new GameObject($"Player_{p + 1}");
            playerObj.transform.SetParent(transform);

            Player playerScript = playerObj.AddComponent<Player>();
            playerScript.playerID = p + 1;
            playerScript.isBot = GameSettings.types[p] == PlayerType.Bot;
            playerScript.isEmpty = GameSettings.types[p] == PlayerType.Empty;
            playerScript.playerName = GameSettings.GetDisplayName(p);
            players.Add(playerScript);

            // UI Arka Plan Paneli (konum/rotasyon ApplyPlayerLayout içinde verilir)
            if (playerBackgroundPrefab != null && uiCanvasTransform != null)
            {
                GameObject bgObj = Instantiate(playerBackgroundPrefab, uiCanvasTransform, false);
                playerScript.SetupBackground(bgObj);
            }

            // Kartları oluştur
            for (int card = 0; card < 18; card++)
            {
                int cardValue = card / 2;

                GameObject newCard = Instantiate(cardPrefab, playerObj.transform);

                Card cardScript = newCard.GetComponent<Card>();
                if (cardScript != null)
                {
                    cardScript.cardValue = cardValue + 1;
                    cardScript.playerNumber = p + 1;

                    int spriteIndex = p * 9 + cardValue;
                    if (cardSprites != null && spriteIndex < cardSprites.Length)
                    {
                        cardScript.spriteRenderer.sprite = cardSprites[spriteIndex];
                    }

                    playerScript.myCards.Add(cardScript);
                }
            }

            // Pozisyon + rotasyon + kart dizilimi + UI
            ApplyPlayerLayout(p);
            if (playerScript.isEmpty)
            {
                // Boş oyuncunun kartları baştan kapalı, paneli de gizli
                foreach (Card c in playerScript.myCards) c.CloseCard();
                if (playerScript.areaBackgroundImage != null)
                    playerScript.areaBackgroundImage.gameObject.SetActive(false);
            }
        }
        currentPlayerIndex = 0;
        while (players[currentPlayerIndex].isEmpty) currentPlayerIndex++;
        UpdatePlayerAreaVisuals();
    }

    public void RollDice()
    {
        if (hasRolledDice || isDiceRolling) return;

        Player activePlayer = players[currentPlayerIndex];

        bool useSingleDice = (activePlayer.GetRemainingCardCount() == 1) &&
                             (activePlayer.GetLastRemainingCardValue() < 6);

        StartCoroutine(RollDiceRoutine(useSingleDice));
    }

    IEnumerator RollDiceRoutine(bool singleDiceMode)
    {
        isDiceRolling = true;

        int result1 = 0;
        int result2 = 0;
        bool dice1Finished = false;
        bool dice2Finished = false;

        StartCoroutine(dice1.RollAnimation(result =>
        {
            result1 = result;
            dice1Finished = true;
        }));

        if (singleDiceMode)
        {
            dice2Finished = true;
            result2 = 0;
        }
        else
        {
            StartCoroutine(dice2.RollAnimation(result =>
            {
                result2 = result;
                dice2Finished = true;
            }));
        }

        yield return new WaitUntil(() => dice1Finished && dice2Finished);

        currentDiceTotal = result1 + result2;
        hasRolledDice = true;
        isDiceRolling = false;
        UpdatePlayerAreaVisuals();
    }

    public void ConfirmMove()
    {
        if (!hasRolledDice) return;
        if (IsBotPlayer(currentPlayerIndex) && !botActing) return;

        Player activePlayer = players[currentPlayerIndex];
        int selectedTotal = activePlayer.GetSelectedCardsTotal();

        if (selectedTotal == currentDiceTotal)
        {
            List<Card> selectedCards = activePlayer.GetSelectedCards();
            foreach (Card card in selectedCards)
            {
                card.CloseCard();
            }

            // Oyuncu kazandı mı kontrol et
            if (activePlayer.HasWon())
            {
                UpdatePlayerAreaVisuals();
                ShowWinScreen(activePlayer.playerID);
                return;
            }

            NextTurn();
        }
        else
        {
            activePlayer.DeselectAllCards();
        }
    }

    public void PassTurn()
    {
        if (!hasRolledDice) return;
        if (IsBotPlayer(currentPlayerIndex) && !botActing) return;
        Player activePlayer = players[currentPlayerIndex];
        activePlayer.DeselectAllCards();

        NextTurn();
    }

    public void NextTurn()
    {
        hasRolledDice = false;
        currentDiceTotal = 0;
        do
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % 4;
        }
        while (players[currentPlayerIndex].isEmpty);

        UpdatePlayerAreaVisuals();
        StartBotTurnIfNeeded();
    }

    // Kazanan paneli gösterme metodu
    void ShowWinScreen(int winnerID)
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            if (winText != null)
            {
                winText.text = $"TEBRİKLER!\nOYUNCU {winnerID} KAZANDI!";
            }
        }
    }
    public void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    // Yeniden Başlat Butonuna Bağlanacak Metod
    public void RestartGame()
    {
        // Aktif sahneyi yeniden yükler
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public bool IsBotPlayer(int playerIndex)
    {
        return playerIndex >= 0 && playerIndex < players.Count && players[playerIndex].isBot;
    }

    void StartBotTurnIfNeeded()
    {
        if (botRoutine != null) return;
        if (winPanel != null && winPanel.activeSelf) return;
        if (!IsBotPlayer(currentPlayerIndex)) return;

        botRoutine = StartCoroutine(BotTurnRoutine());
    }

    IEnumerator BotTurnRoutine()
    {
        Player bot = players[currentPlayerIndex];

        yield return new WaitForSeconds(botThinkDelay);
        botActing = true;
        RollDice();
        botActing = false;
        yield return new WaitUntil(() => hasRolledDice);
        yield return new WaitForSeconds(botThinkDelay);

        List<Card> combo = FindCardCombination(bot, currentDiceTotal);

        if (combo == null)
        {
            botRoutine = null;
            botActing = true;
            PassTurn();
            botActing = false;
            yield break;
        }

        foreach (Card card in combo)
        {
            card.ToggleSelect();
            yield return new WaitForSeconds(botSelectDelay);
        }

        yield return new WaitForSeconds(botThinkDelay * 0.5f);

        botRoutine = null;
        botActing = true;
        ConfirmMove();
        botActing = false;
    }

    // Büyük kartları öncelikli olarak toplamı tutturan kombinasyonu bulur, yoksa null döner
    List<Card> FindCardCombination(Player player, int target)
    {
        List<Card> open = new List<Card>();
        foreach (Card c in player.myCards)
        {
            if (!c.isClosed) open.Add(c);
        }
        open.Sort((a, b) => b.cardValue.CompareTo(a.cardValue)); // büyükten küçüğe

        List<Card> chosen = new List<Card>();
        return SearchCombination(open, 0, target, chosen) ? chosen : null;
    }

    bool SearchCombination(List<Card> cards, int startIndex, int remaining, List<Card> chosen)
    {
        if (remaining == 0) return true;

        for (int i = startIndex; i < cards.Count; i++)
        {
            if (cards[i].cardValue > remaining) continue;

            chosen.Add(cards[i]);
            if (SearchCombination(cards, i + 1, remaining - cards[i].cardValue, chosen)) return true;
            chosen.RemoveAt(chosen.Count - 1);
        }
        return false;
    }
}