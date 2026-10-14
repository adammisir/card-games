public enum PlayerType { Empty = 0, Human = 1, Bot = 2 }

// Ana menüden oyun sahnesine seçimleri taşır.
// Statik olduğu için sahne değişse de, RestartGame ile sahne yeniden yüklense de korunur.
public static class GameSettings
{
    public static PlayerType[] types = { PlayerType.Human, PlayerType.Bot, PlayerType.Bot, PlayerType.Bot };
    public static string[] names = { "", "", "", "" };

    // Oyunda gösterilecek isim: boş bırakılmışsa varsayılan isim
    public static string GetDisplayName(int index)
    {
        string typed = names[index] != null ? names[index].Trim() : "";
        if (!string.IsNullOrEmpty(typed)) return typed;
        return types[index] == PlayerType.Bot ? $"BOT {index + 1}" : $"OYUNCU {index + 1}";
    }

    public static int ActivePlayerCount()
    {
        int count = 0;
        foreach (PlayerType t in types)
        {
            if (t != PlayerType.Empty) count++;
        }
        return count;
    }
}
