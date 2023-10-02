namespace GTA5CharacterGuesser.Pages;

using System.Reflection.Metadata;
using global::Microsoft.AspNetCore.Components;
using GTA5CharacterGuesser.Models;
using GTA5CharacterGuesser.Services;
using Microsoft.JSInterop;

public partial class Guess : ComponentBase, IAsyncDisposable
{
    [Inject] public required DataService Data { get; init; }
    [Inject] public required TwitchChatService Chat { get; init; }
    [Inject] public required IJSRuntime Js { get; init; }
    [Inject] public required NavigationManager NavMgr { get; init; }

    private DotNetObjectReference<Guess> _thisRef;

    private int _levelIndex = 0;
    private int _lineIndex = 0;
    private EGuessState _state = EGuessState.LevelIn;
    private DateTimeOffset _levelOutAnimTime = DateTimeOffset.UtcNow;
    private DateTimeOffset _guessStartTime = DateTimeOffset.UtcNow;
    private int _score = 0;

    private bool _revealAudio = false;

    private string _guessInput = string.Empty;

    private Dictionary<string, TwitchUser> _chatGuesses = new();
    private HashSet<string> _guessedUsers = new();
    private Dictionary<string, string> _useridToCharacterName = new();
    private Dictionary<string, int> _characterNameToGuessCount = new();

    private bool _resultCorrect = false;
    private int _resultPoints = 0;
    private bool _resultPlayVideo = false;

    private bool _isFinished = false;

    public Guess()
    {
        _thisRef = DotNetObjectReference.Create(this);
    }

    protected override async Task OnInitializedAsync()
    {
        await Js.InvokeVoidAsync("framesAddListener", _thisRef);

        while (!Data.IsInitialized)
            await Task.Delay(200);

        Chat.OnMessage += Chat_OnMessage;
        _state = EGuessState.LevelIn;
        StateHasChanged();
        await Task.Delay(2000);
        _state = EGuessState.LevelOut;
        StateHasChanged();
        await Task.Delay(500);
        _state = EGuessState.Guess;
        _guessStartTime = _levelOutAnimTime = DateTimeOffset.UtcNow;
    }

    private async Task OnGuess()
    {
        if (_state != EGuessState.Guess)
            return;

        _resultPoints = 0;

        var level = Data.GetLevel(_levelIndex);
        var line = Data.GetLine(_levelIndex, _lineIndex);

        if (Data.AreCharactersEqual(_guessInput, line.Answer))
        {
            var minPts = level.MinScore;
            var maxPts = level.MaxScore;

            if (_revealAudio)
            {
                minPts -= level.HintPenalty;
                maxPts -= level.HintPenalty;
            }

            var guessTimeSpan = TimeSpan.FromMilliseconds(level.Timer);
            var guessEndTime = _guessStartTime + guessTimeSpan;
            var guessTimeLeft = guessEndTime - DateTimeOffset.UtcNow;
            var guessTimeProgress = guessTimeLeft / guessTimeSpan;
            if (guessTimeProgress < 0)
                guessTimeProgress = 0;

            _resultCorrect = Data.AreCharactersEqual(_guessInput, line.Answer);
            _resultPoints = (int)Math.Round(minPts + (maxPts - minPts) * guessTimeProgress);
        }
        _score += _resultPoints;

        foreach (var kv in _chatGuesses)
        {
            var user = kv.Value;
            user.TotalPoints += user.RoundPoints;
            user.RoundPoints = 0;
        }

        foreach (var kv in _useridToCharacterName)
        {
            if (_characterNameToGuessCount.ContainsKey(kv.Value))
                _characterNameToGuessCount[kv.Value]++;
            else
                _characterNameToGuessCount[kv.Value] = 1;
        }

        _state = EGuessState.GuessOut;
        StateHasChanged();
        await Task.Delay(300);
        _state = EGuessState.Result;
    }

    private async void OnContinue()
    {
        if (_state != EGuessState.Result)
            return;
        _state = EGuessState.ResultOut;
        StateHasChanged();
        await Task.Delay(300);

        if (Data.IsLastLevel(_levelIndex) && Data.IsLastLine(_levelIndex, _lineIndex))
        {
            _isFinished = true;
        }
        else if (Data.IsLastLine(_levelIndex, _lineIndex))
        {
            _levelIndex++;
            _lineIndex = 0;
            _state = EGuessState.LevelIn;
            StateHasChanged();
            await Task.Delay(2000);
            _state = EGuessState.LevelOut;
            StateHasChanged();
            await Task.Delay(500);
            _levelOutAnimTime = DateTimeOffset.UtcNow;
        }
        else
            _lineIndex++;

        _state = EGuessState.Guess;
        _guessStartTime = DateTimeOffset.UtcNow;
        _revealAudio = false;
        _guessInput = string.Empty;
        _guessedUsers.Clear();
        _useridToCharacterName.Clear();
        _characterNameToGuessCount.Clear();
        _resultCorrect = false;
        _resultPoints = 0;
        _resultPlayVideo = false;
    }

    private void Chat_OnMessage(TwitchChatMessage data)
    {
        if (_state != EGuessState.Guess)
            return;
        var characters = Data.GetCharacterOptions(data.Message);
        if(characters.Count() != 1)
            return;
        var character = characters.First();

        if (!_chatGuesses.ContainsKey(data.UserId))
        {
            _chatGuesses[data.UserId] = new TwitchUser
            {
                Username = data.UserLogin,
                DisplayName = data.UserDisplayName
            };
        }

        _useridToCharacterName[data.UserId] = character;

        var level = Data.GetLevel(_levelIndex);
        var line = Data.GetLine(_levelIndex, _lineIndex);
        int currentPts = 0;

        if (Data.AreCharactersEqual(character, line.Answer))
        {
            var minPts = level.MinScore;
            var maxPts = level.MaxScore;

            if (_revealAudio)
            {
                minPts -= level.HintPenalty;
                maxPts -= level.HintPenalty;
            }
            var guessTimeSpan = TimeSpan.FromMilliseconds(level.Timer);
            var guessEndTime = _guessStartTime + guessTimeSpan;
            var guessTimeLeft = guessEndTime - DateTimeOffset.UtcNow;
            var guessTimeProgress = guessTimeLeft / guessTimeSpan;
            if (guessTimeProgress < 0)
                guessTimeProgress = 0;

            currentPts = (int)Math.Round(minPts + (maxPts - minPts) * guessTimeProgress);
        }

        _chatGuesses[data.UserId].RoundPoints = currentPts;
        _guessedUsers.Add(data.UserId);
    }

    [JSInvokable]
    public void OnFrame()
    {
        InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        Chat.OnMessage -= Chat_OnMessage;
        await Js.InvokeVoidAsync("framesRemoveListener", _thisRef);
    }

    private enum EGuessState
    {
        LevelIn, LevelOut, Guess, GuessOut, Result, ResultOut
    }

    private class TwitchUser
    {
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int TotalPoints { get; set; } = 0;
        public int RoundPoints { get; set; } = 0;
    }
}
