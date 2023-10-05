namespace GTA5CharacterGuesser.Models;

public class DataModel
{
    public LevelModel[] Levels { get; set; } = Array.Empty<LevelModel>();
    public string[] Characters { get; set; } = Array.Empty<string>();
}
