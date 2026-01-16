using Microsoft.AspNetCore.SignalR;

namespace MageKnightOnline.Web.Hubs;

/// <summary>
/// SignalR hub for real-time game communication.
/// </summary>
public class GameHub : Hub
{
    /// <summary>
    /// Join a game room to receive updates.
    /// </summary>
    public async Task JoinGame(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        await Clients.Group(gameId).SendAsync("PlayerJoined", Context.ConnectionId);
    }

    /// <summary>
    /// Leave a game room.
    /// </summary>
    public async Task LeaveGame(string gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
        await Clients.Group(gameId).SendAsync("PlayerLeft", Context.ConnectionId);
    }

    /// <summary>
    /// Notify all players in a game that the state has changed.
    /// </summary>
    public async Task NotifyGameStateChanged(string gameId)
    {
        await Clients.Group(gameId).SendAsync("GameStateChanged");
    }

    /// <summary>
    /// Send a chat message to all players in the game.
    /// </summary>
    public async Task SendMessage(string gameId, string username, string message)
    {
        await Clients.Group(gameId).SendAsync("ReceiveMessage", username, message, DateTime.UtcNow);
    }

    /// <summary>
    /// Notify that a player's turn has started.
    /// </summary>
    public async Task NotifyTurnStarted(string gameId, string playerId)
    {
        await Clients.Group(gameId).SendAsync("TurnStarted", playerId);
    }

    /// <summary>
    /// Notify that a player performed an action.
    /// </summary>
    public async Task NotifyAction(string gameId, string playerId, string actionType, object? actionData)
    {
        await Clients.Group(gameId).SendAsync("ActionPerformed", playerId, actionType, actionData);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Handle disconnection - could notify other players
        await base.OnDisconnectedAsync(exception);
    }
}
