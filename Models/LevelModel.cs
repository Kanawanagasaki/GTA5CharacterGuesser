namespace GTA5CharacterGuesser.Models;

public class LevelModel
{
    public string Title { get; set; } = string.Empty;
    public int MinScore { get; set; }
    public int MaxScore { get; set; }
    public int HintPenalty { get; set; }
    public int Timer { get; set; }
    public LineModel[] Lines { get; set; } = Array.Empty<LineModel>();
}
