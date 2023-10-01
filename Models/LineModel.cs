namespace GTA5CharacterGuesser.Models;

public class LineModel
{
    public string? Text { get; set; }
    public string? Audio { get; set; }
    public string? Answer { get; set; }
    public string? Youtube { get; set; }
    public bool IsGuessedCorrectly = false;
}
