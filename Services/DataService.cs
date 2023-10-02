namespace GTA5CharacterGuesser.Services;

using System.Text.Json;
using GTA5CharacterGuesser.Models;

public class DataService
{
    public event Action? OnStateChange;

    public bool IsInitialized { get; private set; } = false;
    public bool IsInitializing { get; private set; } = false;

    private HttpClient _http;
    private DataModel? _model;
    private (string normalized, string name)[] _characters = Array.Empty<(string, string)>();

    public DataService(HttpClient http)
    {
        _http = http;
    }

    public async Task Init()
    {
        if (IsInitializing)
            return;

        IsInitializing = true;
        OnStateChange?.Invoke();

        await InitData();
        await InitCharacters();

        IsInitializing = false;
        IsInitialized = true;
        OnStateChange?.Invoke();
    }

    private async Task InitData()
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            using var response = await _http.GetAsync("data.json");
            if (!response.IsSuccessStatusCode)
                return;

            var content = await response.Content.ReadAsStringAsync();
            _model = JsonSerializer.Deserialize<DataModel>(content, jsonOptions);
        }
        catch { }
    }

    private async Task InitCharacters()
    {
        try
        {
            using var response = await _http.GetAsync("characters.json");
            if (!response.IsSuccessStatusCode)
                return;

            var content = await response.Content.ReadAsStringAsync();
            var characters = JsonSerializer.Deserialize<string[]>(content) ?? Array.Empty<string>();
            _characters = new (string normalized, string name)[characters.Length];
            for (int i = 0; i < _characters.Length; i++)
                _characters[i] = (NormalizeString(characters[i]), characters[i]);

        }
        catch { }
    }

    public bool HasCharacter(string characterName)
    {
        var normalized = NormalizeString(characterName);
        return _characters.Any(x => x.normalized == normalized);
    }

    public string GetCharacter(string characterName)
    {
        var normalized = NormalizeString(characterName);
        return _characters.First(x => x.normalized == normalized).name;
    }

    public bool AreCharactersEqual(string str1, string str2)
        => NormalizeString(str1) == NormalizeString(str2);

    public IEnumerable<string> GetCharacterOptions(string characterName)
    {
        var normalized = NormalizeString(characterName);
        return _characters.Where(x => x.normalized.Contains(normalized)).Select(x => x.name);
    }

    private string NormalizeString(string str)
    {
        var arr = new char[str.Length];
        int index = 0;

        foreach (var ch in str)
        {
            if (!char.IsLetter(ch))
                continue;

            arr[index++] = char.ToLower(ch);
        }

        return new string(arr, 0, index);
    }

    public bool IsLastLevel(int levelIndex)
        => _model is null || _model.Levels.Length - 1 <= levelIndex;

    public LevelModel GetLevel(int levelIndex)
        => _model is null ? new LevelModel() : _model.Levels[Math.Clamp(levelIndex, 0, _model.Levels.Length - 1)];

    public bool IsLastLine(int levelIndex, int lineIndex)
        => _model is null || GetLevel(levelIndex).Lines.Length - 1 <= lineIndex;

    public LineModel GetLine(int levelIndex, int lineIndex)
    {
        if (_model is null) return new LineModel();
        var level = GetLevel(levelIndex);
        return level.Lines[Math.Clamp(lineIndex, 0, level.Lines.Length - 1)];
    }

    public int GetMaxPossibleScore()
        => _model is null ? 0 : _model.Levels.Sum(x => x.Lines.Length * x.MaxScore);
}
