namespace GTA5CharacterGuesser;

using System.Threading.Tasks;
using global::Microsoft.AspNetCore.Components;
using GTA5CharacterGuesser.Services;

public partial class App : ComponentBase
{
    [Inject] public required DataService Data { get; init; }
    [Inject] public required TwitchChatService TwitchChat { get; init; }

    protected override async Task OnInitializedAsync()
    {
        await Data.Init();
        await TwitchChat.Init();
    }
}
