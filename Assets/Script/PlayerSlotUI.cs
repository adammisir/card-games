using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Ana menüdeki tek bir oyuncu satırı: [tür butonu] [isim input field]
public class PlayerSlotUI : MonoBehaviour
{
    [Header("UI Elemanları")]
    public Button typeButton;
    public Image typeImage;
    public TMP_InputField nameInput;

    [Header("Tür Görselleri")]
    public Sprite emptySprite;
    public Sprite humanSprite;
    public Sprite botSprite;

    public PlayerType Type { get; private set; }
    public event Action OnChanged;

    private int slotNumber;

    public void Init(int index, PlayerType startType, string startName)
    {
        slotNumber = index + 1;
        nameInput.text = startName;

        typeButton.onClick.RemoveAllListeners();
        typeButton.onClick.AddListener(CycleType);

        SetType(startType);
    }

    // İnsan -> Bot -> Boş -> İnsan ...
    void CycleType()
    {
        SetType((PlayerType)(((int)Type + 1) % 3));
    }

    void SetType(PlayerType newType)
    {
        Type = newType;

        switch (Type)
        {
            case PlayerType.Empty: typeImage.sprite = emptySprite; break;
            case PlayerType.Human: typeImage.sprite = humanSprite; break;
            case PlayerType.Bot:   typeImage.sprite = botSprite;   break;
        }

        // Boş oyuncunun ismi değiştirilemez (yazdığı isim silinmez, sadece kilitlenir)
        nameInput.interactable = Type != PlayerType.Empty;

        // Input boşken gri placeholder olarak varsayılan isim görünür
        if (nameInput.placeholder is TMP_Text placeholder)
        {
            placeholder.text = DefaultName();
        }

        OnChanged?.Invoke();
    }

    string DefaultName()
    {
        return Type == PlayerType.Bot ? $"BOT {slotNumber}" : $"OYUNCU {slotNumber}";
    }

    // Boş bırakılmışsa varsayılan ismi döndürür
    public string GetName()
    {
        string typed = nameInput.text.Trim();
        return string.IsNullOrEmpty(typed) ? DefaultName() : typed;
    }

    // Menüye geri dönüldüğünde input'ta varsayılan isim yazılı kalmasın diye ham metni ayrı verir
    public string GetRawName()
    {
        return nameInput.text.Trim();
    }
}
