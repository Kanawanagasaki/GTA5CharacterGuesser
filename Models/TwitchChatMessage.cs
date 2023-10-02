namespace GTA5CharacterGuesser.Models;

public class TwitchChatMessage
{
    public required string Channel { get; init; }
    public required string Message { get; init; }
    public required string MessageId { get; init; }
    public required string UserId { get; init; }
    public required string UserLogin { get; init; }
    public required string UserDisplayName { get; init; }
}
