using UnityEngine;

public class Card : MonoBehaviour
{
    [Header("Kart Bilgileri")]
    public int cardValue;
    public int playerNumber;
    public SpriteRenderer spriteRenderer;

    [Header("Kart Durumları")]
    public bool isClosed = false;   // Kart tamamen kapandı/oyundan çıktı mı?
    public bool isSelected = false; // Oyuncu bu tur bu kartı seçti mi?

    [Header("Görsel Renkler")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Color closedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Gri ve yarı şeffaf


    private GameManager gameManager;

    void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
    }

    // Oyuncu karta tıkladığında çalışır (Kartta BoxCollider2D olmalı!)
    private void OnMouseDown()
    {
        // 1. Kart zaten kapatılmışsa hiçbir şey yapma
        if (isClosed) return;
      


        // 2. Sıra bu kartın ait olduğu oyuncuda değilse tıklamaya izin verme
        if (gameManager != null && gameManager.currentPlayerIndex + 1 != playerNumber)
        {
            Debug.Log($"Sıra sizde değil! Bu kart Oyuncu {playerNumber}'e ait.");
            return;
        }
        // Botun kartlarına insan tıklayamasın
        if (gameManager != null && gameManager.IsBotPlayer(playerNumber - 1)) return;

        // 3. Zarlar henüz atılmadıysa kart seçilemez
        if (gameManager != null && !gameManager.hasRolledDice)
        {
            Debug.Log("Önce zar atmalısınız!");
            return;
        }

        // Seçim durumunu tersine çevir (Seçiliyse kaldır, değilse seç)
        ToggleSelect();
    }

    public void ToggleSelect()
    {
        isSelected = !isSelected;

        if (isSelected)
        {
            spriteRenderer.color = selectedColor;
            // GameManager'a bu kartın seçildiğini haber vereceğiz
        }
        else
        {
            spriteRenderer.color = normalColor;
            // GameManager'a bu kartın seçiminin kaldırıldığını haber vereceğiz
        }
    }

    // Seçilen kartlar zarların toplamını tutturduğunda kartı tamamen kapatmak için
    public void CloseCard()
    {
        isClosed = true;
        isSelected = false;
        spriteRenderer.color = closedColor;

        // İsteğe bağlı: Kart tamamen kapandığında tıklamayı engellemek için collider'ı kapatabiliriz
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
    }
}