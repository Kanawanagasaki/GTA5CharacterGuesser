namespace GTA5CharacterGuesser;

using global::Microsoft.AspNetCore.Components;
using GTA5CharacterGuesser.Services;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] public required DataService Data { get; init; }

    private bool _shouldAskKey = true;
    private string _key = "";

    public async Task OnContinue()
    {
        await Data.Init(_key);
        _shouldAskKey = false;
    }
}