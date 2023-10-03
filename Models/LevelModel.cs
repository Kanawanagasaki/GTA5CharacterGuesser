namespace GTA5CharacterGuesser.Models;

public class LevelModel
{
    public string Title { get; set; } = string.Empty;
    public int MaxScore { get; set; }
    public int WrongPenalty { get; set; }
    public int AudioPenalty { get; set; }
    public LineModel[] Lines { get; set; } = Array.Empty<LineModel>();
}
