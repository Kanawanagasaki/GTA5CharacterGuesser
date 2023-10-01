namespace GTA5CharacterGuesser.Pages;

using System.Text.Json;
using System.Threading.Tasks;
using global::Microsoft.AspNetCore.Components;
using GTA5CharacterGuesser.Models;

public partial class Index : ComponentBase
{
    [Inject] public required HttpClient Http { get; init; }

    private bool _isDownloading = true;
    private bool _isSuccessfulDownlaod = false;
    private bool _isStarted = false;
    private bool _isFinished = false;

    private DataModel? _data;
    private string[] _characters = Array.Empty<string>();

    private int _levelIndex = 0;
    private int _lineIndex = 0;
    private bool _isGuessing = true;
    private bool _isGuessCorrect = false;

    private string _guess { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            {
                using var response = await Http.GetAsync("data.json");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _data = JsonSerializer.Deserialize<DataModel>(content, jsonOptions);
                    _isSuccessfulDownlaod = true;
                }
                else return;
            }

            {
                using var response = await Http.GetAsync("characters.json");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _characters = JsonSerializer.Deserialize<string[]>(content, jsonOptions) ?? Array.Empty<string>();
                    _isSuccessfulDownlaod = true;
                }
                else _isSuccessfulDownlaod = false;
            }

            if (_data is not null)
            {
                _data.Levels = _data.Levels.OrderBy(x => x.Order).ToArray();
                foreach (var level in _data.Levels)
                    level.Lines = level.Lines.OrderBy(x => Guid.NewGuid()).ToArray();
            }
        }
        finally
        {
            _isDownloading = false;
        }
    }

    private void OnGuess()
    {
        if (_data is null)
            return;

        var level = _data.Levels[_levelIndex];
        var line = level.Lines[_lineIndex];

        _isGuessing = false;
        line.IsGuessedCorrectly = _isGuessCorrect = line.Answer?.ToLower().Trim() == _guess.ToLower().Trim();
    }

    private void OnContinue()
    {
        if (_data is null)
            return;

        var level = _data.Levels[_levelIndex];

        _lineIndex++;
        if (level.Lines.Length <= _lineIndex)
        {
            _levelIndex++;
            _lineIndex = 0;
        }

        if (_data.Levels.Length <= _levelIndex)
        {
            _isFinished = true;
            _lineIndex = 0;
            _levelIndex = 0;
        }

        _guess = "";
        _isGuessing = true;
    }

    private void OnRestart()
    {
        if (_data is null)
            return;

        _guess = "";
        _isGuessing = true;
        _lineIndex = 0;
        _levelIndex = 0;
        _isFinished = false;
        foreach (var level in _data.Levels)
            foreach (var line in level.Lines)
                line.IsGuessedCorrectly = false;
    }
}
