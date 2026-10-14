using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Player : MonoBehaviour
{
    public int playerID;
    public List<Card> myCards = new List<Card>();

    [Header("Oyuncu UI")]
    //public SpriteRenderer areaBackground; // Eğer World Space Sprite ise
    public Image areaBackgroundImage; // UI Image kullanıyoruz
    public TextMeshProUGUI playerNameText; // Canvas UI Text'i (UGUI)

    public bool isBot;
    public bool isEmpty;
    public string playerName;

    // Arka plan rengini değiştiren yardımcı metod
    public void SetupBackground(GameObject bgObj)
    {
        if (bgObj == null) return;

        areaBackgroundImage = bgObj.GetComponent<Image>();
        playerNameText = bgObj.GetComponentInChildren<TextMeshProUGUI>();

        if (playerNameText != null)
        {
            playerNameText.text = string.IsNullOrEmpty(playerName) ? $"OYUNCU {playerID}" : playerName;
        }
    }

    public void SetBackgroundColor(Color color)
    {
        if (areaBackgroundImage != null)
        {
            areaBackgroundImage.color = color;
        }
    }


    public bool HasWon()
    {
        foreach (Card card in myCards)
        {
            if (!card.isClosed) return false;
        }
        return true;
    }

    public int GetRemainingCardCount()
    {
        int count = 0;
        foreach (Card card in myCards)
        {
            if (!card.isClosed) count++;
        }
        return count;
    }

    public int GetLastRemainingCardValue()
    {
        foreach (Card card in myCards)
        {
            if (!card.isClosed) return card.cardValue;
        }
        return 0;
    }

    public int GetSelectedCardsTotal()
    {
        int total = 0;
        foreach (Card card in myCards)
        {
            if (card.isSelected && !card.isClosed) total += card.cardValue;
        }
        return total;
    }

    public List<Card> GetSelectedCards()
    {
        List<Card> selected = new List<Card>();
        foreach (Card card in myCards)
        {
            if (card.isSelected && !card.isClosed) selected.Add(card);
        }
        return selected;
    }

    public void DeselectAllCards()
    {
        foreach (Card card in myCards)
        {
            if (card.isSelected && !card.isClosed) card.ToggleSelect();
        }
    }
}