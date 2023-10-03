namespace GTA5CharacterGuesser.Pages;

using global::Microsoft.AspNetCore.Components;
using GTA5CharacterGuesser.Models;
using GTA5CharacterGuesser.Services;

public partial class Index : ComponentBase
{
    [Inject] public required NavigationManager NavMgr { get; init; }
    [Inject] public required DataService Data { get; init; }
    [Inject] public required TwitchChatService Chat { get; init; }

    private TwitchChatMessage? _correctMessage;
    private List<TwitchChatMessage> _chat = new();

    private string[] _cheeringPhrases = new[]
    {
        "Go, {username}, go!",
        "Yes, {username}, yes!",
        "{username}, you have got this!",
        "Way to go, {username}!",
        "{username}, you make it look easy!",
        "Awesome job, {username}!",
        "{username}, you are a star!",
        "Keep it up, {username}!",
        "{username}, you have the power!",
        "Great work, {username}!",
        "{username}, you are a champ!",
        "Well done, {username}!",
        "{username}, you are a legend!",
        "Bravo, {username}!",
        "{username}, you are a hero!",
        "Fantastic, {username}!"
    };

    protected override void OnInitialized()
    {
        Data.OnStateChange += () => InvokeAsync(StateHasChanged);
        Chat.OnMessage += Chat_OnMessage;
    }

    private void Chat_OnMessage(TwitchChatMessage data)
    {
        if(Data.HasCharacter(data.Message))
        {
            _correctMessage = data;
            _chat.Add(_correctMessage);
            while(_chat.Count > 20)
                _chat.RemoveAt(0);
            InvokeAsync(StateHasChanged);
        }
    }

    private void OnStartClick()
    {
        if (Data.IsInitialized)
            NavMgr.NavigateTo("guess");
    }
}
