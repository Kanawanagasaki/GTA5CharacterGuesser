namespace GTA5CharacterGuesser.Models;

public class LevelModel
{
    public string? Title { get; set; }
    public int Order { get; set; }
    public LineModel[] Lines { get; set; } = Array.Empty<LineModel>();
}
