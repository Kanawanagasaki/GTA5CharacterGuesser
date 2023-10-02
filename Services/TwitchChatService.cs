namespace GTA5CharacterGuesser.Services;

using GTA5CharacterGuesser.Models;
using Microsoft.JSInterop;

public class TwitchChatService : IAsyncDisposable
{
    public event Action<TwitchChatMessage>? OnMessage;

    private IJSRuntime _js;
    private DotNetObjectReference<TwitchChatService> _thisRef;

    public TwitchChatService(IJSRuntime js)
    {
        _js = js;
        _thisRef = DotNetObjectReference.Create(this);
    }

    public async Task Init()
    {
        await _js.InvokeVoidAsync("twitchChatAddListener", _thisRef);
    }

    [JSInvokable]
    public void OnTwitchChatMessage(TwitchChatMessage data)
    {
        OnMessage?.Invoke(data);
    }

    public async ValueTask DisposeAsync()
    {
        await _js.InvokeVoidAsync("twitchChatRemoveListener", _thisRef);
    }
}
