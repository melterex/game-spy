namespace authorization;

public class PlayerStats
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int TotalGames { get; set; }
    public int TotalWins { get; set; }
    public int WinsAsSpy { get; set; }
    public int WinsAsCivilian { get; set; }
    public double WinRate { get; set; }
}