namespace GTA5CharacterGuesser.Pages;

using System.Reflection.Metadata;
using global::Microsoft.AspNetCore.Components;
using GTA5CharacterGuesser.Models;
using GTA5CharacterGuesser.Services;
using Microsoft.JSInterop;

public partial class Guess : ComponentBase, IDisposable
{
    [Inject] public required DataService Data { get; init; }
    [Inject] public required TwitchChatService Chat { get; init; }
    [Inject] public required IJSRuntime Js { get; init; }
    [Inject] public required NavigationManager NavMgr { get; init; }

    private DotNetObjectReference<Guess> _thisRef;
    private ElementReference? _videoRef;

    private int _levelIndex = 0;
    private int _lineIndex = 0;
    private EGuessState _state = EGuessState.LevelIn;
    private int _roundScore = 0;
    private int _score = 0;
    private int[] _roundsScores = Array.Empty<int>();

    private bool _isAudioPlayed = false;
    private bool _isAudioPlaying = false;

    private string _guessInput = string.Empty;

    private Dictionary<string, TwitchUser> _chatGuesses = new();
    private HashSet<string> _guessedUserLogins = new();
    private Dictionary<string, string> _loginToCharacterName = new();
    private Dictionary<string, int> _characterNameToGuessCount = new();

    private bool _resultCorrect = false;
    private bool _resultPlayVideo = false;
    private bool _resultShouldLoadVideo = false;
    private bool _isVideoPreparing = false;

    private bool _isFinished = false;

    public Guess()
    {
        _thisRef = DotNetObjectReference.Create(this);
    }

    protected override async Task OnInitializedAsync()
    {
        while (!Data.IsInitialized)
            await Task.Delay(200);

        _roundsScores = new int[Data.GetLevelsAmount()];

        var level = Data.GetLevel(_levelIndex);
        var line = Data.GetLine(_levelIndex, _lineIndex);

        _roundScore = level.MaxScore;

        Chat.OnMessage += Chat_OnMessage;
        _state = EGuessState.LevelIn;
        StateHasChanged();
        await Task.Delay(2000);
        _state = EGuessState.LevelOut;
        StateHasChanged();
        await Task.Delay(500);
        _state = EGuessState.Guess;

        StateHasChanged();

        _ = Js.InvokeVoidAsync("prepareAudio", _thisRef, line.Audio);
        _ = Js.InvokeVoidAsync("prepareVideo", _thisRef, line.Video);
        _isVideoPreparing = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_resultShouldLoadVideo)
        {
            var line = Data.GetLine(_levelIndex, _lineIndex);
            await Js.InvokeVoidAsync("loadVideo", _thisRef, _videoRef, line.Video);
            _resultShouldLoadVideo = false;
        }
    }

    private async Task OnGuess()
    {
        if (_state != EGuessState.Guess)
            return;

        var level = Data.GetLevel(_levelIndex);
        var line = Data.GetLine(_levelIndex, _lineIndex);

        if (Data.AreCharactersEqual(_guessInput, line.Answer))
        {
            _resultCorrect = true;
            _score += _roundScore;
            _roundsScores[_levelIndex] += _roundScore;
        }
        else
        {
            _resultCorrect = false;
            _roundScore -= level.WrongPenalty;
            if (_roundScore <= 0)
                _roundScore = 0;
        }

        _guessInput = string.Empty;

        if (_roundScore <= 0 || _resultCorrect)
        {
            await Js.InvokeVoidAsync("stopAudio", _thisRef, line.Audio);

            foreach (var user in _chatGuesses.Values)
            {
                if (user.RoundGuessCorrect)
                {
                    user.TotalScore += user.RoundScore;
                    user.RoundsScores[_levelIndex] += user.RoundScore;
                }
                user.RoundScore = 0;
            }

            _state = EGuessState.Result;
        }
    }

    private async Task OnAudioClick()
    {
        var level = Data.GetLevel(_levelIndex);
        var line = Data.GetLine(_levelIndex, _lineIndex);
        if (_isAudioPlaying)
            await Js.InvokeVoidAsync("stopAudio", _thisRef, line.Audio);
        else if (line.Audio is not null)
        {
            if (!_isAudioPlayed && _state == EGuessState.Guess)
            {
                _roundScore -= level.AudioPenalty;
                _isAudioPlayed = true;

                foreach (var user in _chatGuesses.Values)
                {
                    if (user.RoundGuessCorrect)
                        continue;
                    user.RoundScore -= level.AudioPenalty;
                    if (user.RoundScore < 0)
                        user.RoundScore = 0;
                }
            }
            _isAudioPlaying = true;
            await Js.InvokeVoidAsync("playAudio", _thisRef, line.Audio);
        }
    }

    private async Task OnRevealVideo()
    {
        var line = Data.GetLine(_levelIndex, _lineIndex);
        await Js.InvokeVoidAsync("stopAudio", _thisRef, line.Audio);
        _resultPlayVideo = true;
        _resultShouldLoadVideo = true;
    }

    [JSInvokable]
    public void OnVideoPrepared(string filename)
    {
        var line = Data.GetLine(_levelIndex, _lineIndex);
        if (filename == line.Video)
            _isVideoPreparing = false;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnAudioEnded(string path)
    {
        _isAudioPlaying = false;
        StateHasChanged();
    }

    private async Task OnContinue()
    {
        if (_state != EGuessState.Result)
            return;

        var line = Data.GetLine(_levelIndex, _lineIndex);
        await Js.InvokeVoidAsync("stopAudio", _thisRef, line.Audio);

        if (Data.IsLastLevel(_levelIndex) && Data.IsLastLine(_levelIndex, _lineIndex))
        {
            _isFinished = true;
        }
        else if (Data.IsLastLine(_levelIndex, _lineIndex))
        {
            _state = EGuessState.PreNextLevel;
            StateHasChanged();
            await Task.Delay(300);
            _levelIndex++;
            _lineIndex = 0;
            _state = EGuessState.LevelIn;
            StateHasChanged();
            await Task.Delay(2000);
            _state = EGuessState.LevelOut;
            StateHasChanged();
            await Task.Delay(500);
        }
        else
            _lineIndex++;

        var level = Data.GetLevel(_levelIndex);
        line = Data.GetLine(_levelIndex, _lineIndex);
        _state = EGuessState.Guess;
        _isAudioPlayed = false;
        _guessInput = string.Empty;
        _guessedUserLogins.Clear();
        _loginToCharacterName.Clear();
        _characterNameToGuessCount.Clear();
        _resultCorrect = false;
        _resultPlayVideo = false;
        _roundScore = level.MaxScore;

        if (!_isFinished)
        {
            foreach (var user in _chatGuesses.Values)
            {
                user.RoundScore = level.MaxScore;
                user.RoundGuessCorrect = false;
            }
        }

        StateHasChanged();

        _ = Js.InvokeVoidAsync("prepareAudio", _thisRef, line.Audio);
        _ = Js.InvokeVoidAsync("prepareVideo", _thisRef, line.Video);
        _isVideoPreparing = true;
    }

    private void Chat_OnMessage(TwitchChatMessage data)
    {
        if (_state != EGuessState.Guess)
            return;
        var characters = Data.GetCharacterOptions(data.Message);
        if (characters.Count() != 1)
            return;
        var character = characters.First();
        var level = Data.GetLevel(_levelIndex);
        var line = Data.GetLine(_levelIndex, _lineIndex);

        if (!_chatGuesses.ContainsKey(data.UserLogin))
        {
            _chatGuesses[data.UserLogin] = new TwitchUser
            {
                Username = data.UserLogin,
                DisplayName = data.UserDisplayName,
                Color = data.Color,
                RoundScore = _isAudioPlayed ? level.MaxScore - level.AudioPenalty : level.MaxScore,
                RoundsScores = new int[Data.GetLevelsAmount()]
            };
        }

        if (_loginToCharacterName.ContainsKey(data.UserLogin) && _characterNameToGuessCount.ContainsKey(_loginToCharacterName[data.UserLogin]))
        {
            _characterNameToGuessCount[_loginToCharacterName[data.UserLogin]]--;
            if (_characterNameToGuessCount[_loginToCharacterName[data.UserLogin]] <= 0)
                _characterNameToGuessCount.Remove(_loginToCharacterName[data.UserLogin]);
        }

        _loginToCharacterName[data.UserLogin] = character;
        if (_characterNameToGuessCount.ContainsKey(character))
            _characterNameToGuessCount[character]++;
        else
            _characterNameToGuessCount[character] = 1;

        if (Data.AreCharactersEqual(character, line.Answer))
            _chatGuesses[data.UserLogin].RoundGuessCorrect = true;
        else
        {
            _chatGuesses[data.UserLogin].RoundGuessCorrect = false;
            _chatGuesses[data.UserLogin].RoundScore -= level.WrongPenalty;
            if (_chatGuesses[data.UserLogin].RoundScore < 0)
                _chatGuesses[data.UserLogin].RoundScore = 0;
        }

        _guessedUserLogins.Add(data.UserLogin);
        StateHasChanged();
    }

    public void Dispose()
    {
        Chat.OnMessage -= Chat_OnMessage;
    }

    private enum EGuessState
    {
        LevelIn, LevelOut, Guess, Result, PreNextLevel
    }

    private class TwitchUser
    {
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Color { get; set; } = string.Empty;
        public int TotalScore { get; set; } = 0;
        public int RoundScore { get; set; } = 0;
        public bool RoundGuessCorrect { get; set; } = false;

        public required int[] RoundsScores { get; init; }
    }
}
