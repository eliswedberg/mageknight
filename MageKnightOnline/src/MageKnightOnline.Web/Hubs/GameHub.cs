using Microsoft.AspNetCore.SignalR;

namespace MageKnightOnline.Web.Hubs;

/// <summary>
/// SignalR hub for real-time game communication.
/// </summary>
public class GameHub : Hub
{
    // Track which user is connected to which games
    private static readonly Dictionary<string, HashSet<string>> GameConnections = new();
    private static readonly Dictionary<string, string> ConnectionUsernames = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Join a game room to receive updates.
    /// </summary>
    public async Task JoinGame(string gameId, string username)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        
        lock (_lock)
        {
            if (!GameConnections.ContainsKey(gameId))
                GameConnections[gameId] = new HashSet<string>();
            
            GameConnections[gameId].Add(Context.ConnectionId);
            ConnectionUsernames[Context.ConnectionId] = username;
        }
        
        await Clients.Group(gameId).SendAsync("PlayerJoined", username, Context.ConnectionId);
    }

    /// <summary>
    /// Leave a game room.
    /// </summary>
    public async Task LeaveGame(string gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
        
        string? username = null;
        lock (_lock)
        {
            if (GameConnections.ContainsKey(gameId))
                GameConnections[gameId].Remove(Context.ConnectionId);
            
            if (ConnectionUsernames.TryGetValue(Context.ConnectionId, out var name))
            {
                username = name;
                ConnectionUsernames.Remove(Context.ConnectionId);
            }
        }
        
        await Clients.Group(gameId).SendAsync("PlayerLeft", username ?? "Unknown", Context.ConnectionId);
    }

    /// <summary>
    /// Notify all players in a game that the state has changed.
    /// </summary>
    public async Task NotifyGameStateChanged(string gameId, string actionDescription)
    {
        await Clients.OthersInGroup(gameId).SendAsync("GameStateChanged", actionDescription);
    }

    /// <summary>
    /// Send a chat message to all players in the game.
    /// </summary>
    public async Task SendMessage(string gameId, string message)
    {
        string? username = null;
        lock (_lock)
        {
            ConnectionUsernames.TryGetValue(Context.ConnectionId, out username);
        }
        
        await Clients.Group(gameId).SendAsync("ReceiveMessage", username ?? "Unknown", message, DateTime.UtcNow);
    }

    /// <summary>
    /// Notify that a player's turn has started.
    /// </summary>
    public async Task NotifyTurnStarted(string gameId, string playerName)
    {
        await Clients.Group(gameId).SendAsync("TurnStarted", playerName);
    }

    /// <summary>
    /// Notify that a round has ended.
    /// </summary>
    public async Task NotifyRoundEnded(string gameId, int roundNumber, bool isDay)
    {
        await Clients.Group(gameId).SendAsync("RoundEnded", roundNumber, isDay);
    }

    /// <summary>
    /// Notify that combat has started.
    /// </summary>
    public async Task NotifyCombatStarted(string gameId, string playerName, string siteName)
    {
        await Clients.Group(gameId).SendAsync("CombatStarted", playerName, siteName);
    }

    /// <summary>
    /// Notify that combat has ended.
    /// </summary>
    public async Task NotifyCombatEnded(string gameId, string playerName, bool victory)
    {
        await Clients.Group(gameId).SendAsync("CombatEnded", playerName, victory);
    }

    /// <summary>
    /// Notify that the game has ended.
    /// </summary>
    public async Task NotifyGameEnded(string gameId, string reason)
    {
        await Clients.Group(gameId).SendAsync("GameEnded", reason);
    }

    /// <summary>
    /// Notify that a player performed an action.
    /// </summary>
    public async Task NotifyAction(string gameId, string actionType, string actionDescription)
    {
        string? username = null;
        lock (_lock)
        {
            ConnectionUsernames.TryGetValue(Context.ConnectionId, out username);
        }
        
        await Clients.OthersInGroup(gameId).SendAsync("ActionPerformed", username ?? "Unknown", actionType, actionDescription);
    }

    /// <summary>
    /// Get list of connected players in a game.
    /// </summary>
    public Task<List<string>> GetConnectedPlayers(string gameId)
    {
        var players = new List<string>();
        lock (_lock)
        {
            if (GameConnections.TryGetValue(gameId, out var connections))
            {
                foreach (var conn in connections)
                {
                    if (ConnectionUsernames.TryGetValue(conn, out var name))
                        players.Add(name);
                }
            }
        }
        return Task.FromResult(players);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? username = null;
        List<string> gameIds = new();
        
        lock (_lock)
        {
            ConnectionUsernames.TryGetValue(Context.ConnectionId, out username);
            ConnectionUsernames.Remove(Context.ConnectionId);
            
            // Find all games this connection was in
            foreach (var kvp in GameConnections)
            {
                if (kvp.Value.Contains(Context.ConnectionId))
                {
                    kvp.Value.Remove(Context.ConnectionId);
                    gameIds.Add(kvp.Key);
                }
            }
        }
        
        // Notify all games about the disconnection
        foreach (var gameId in gameIds)
        {
            await Clients.Group(gameId).SendAsync("PlayerDisconnected", username ?? "Unknown");
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}
