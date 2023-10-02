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
            InvokeAsync(StateHasChanged);
        }
    }

    private void OnStartClick()
    {
        if (Data.IsInitialized)
            NavMgr.NavigateTo("guess");
    }
}
