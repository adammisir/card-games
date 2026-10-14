using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Oyuncu Satırları (4 adet)")]
    public PlayerSlotUI[] slots;

    [Header("Kontroller")]
    public Button startButton;
    public TMP_Text warningText; // "En az 2 oyuncu seçmelisin" (isteğe bağlı)

    [Header("Sahne")]
    public string gameSceneName = "GameScene"; // Build Settings'te ekli olmalı

    void Start()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            // Oyundan menüye dönüldüğünde son seçimler korunur
            slots[i].Init(i, GameSettings.types[i], GameSettings.names[i]);
            slots[i].OnChanged += Refresh;
        }

        startButton.onClick.AddListener(StartGame);
        Refresh();
    }

    int ActiveCount()
    {
        int count = 0;
        foreach (PlayerSlotUI slot in slots)
        {
            if (slot.Type != PlayerType.Empty) count++;
        }
        return count;
    }

    void Refresh()
    {
        bool enoughPlayers = ActiveCount() >= 2;
        startButton.interactable = enoughPlayers;

        if (warningText != null)
        {
            warningText.gameObject.SetActive(!enoughPlayers);
        }
    }

    void StartGame()
    {
        if (ActiveCount() < 2) return; // Güvenlik: buton kapalı olsa bile kontrol

        for (int i = 0; i < slots.Length; i++)
        {
            GameSettings.types[i] = slots[i].Type;
            GameSettings.names[i] = slots[i].GetRawName();
        }

        SceneManager.LoadScene(gameSceneName);
    }
}
