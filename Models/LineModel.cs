namespace GTA5CharacterGuesser.Models;

public class LineModel
{
    public string Text { get; set; } = string.Empty;
    public string Audio { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Video { get; set; } = string.Empty;
    public bool IsGuessedCorrectly = false;
}
