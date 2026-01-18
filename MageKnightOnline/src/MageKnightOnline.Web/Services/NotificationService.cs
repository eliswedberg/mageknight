namespace MageKnightOnline.Web.Services;

/// <summary>
/// Service for managing in-app notifications.
/// </summary>
public class NotificationService
{
    private readonly List<GameNotification> _notifications = new();
    private const int MaxNotifications = 10;
    
    public event Action? OnNotificationsChanged;
    
    public IReadOnlyList<GameNotification> Notifications => _notifications.AsReadOnly();
    
    public int UnreadCount => _notifications.Count(n => !n.IsRead);
    
    public void AddNotification(GameNotification notification)
    {
        _notifications.Insert(0, notification);
        
        // Keep only the last N notifications
        while (_notifications.Count > MaxNotifications)
        {
            _notifications.RemoveAt(_notifications.Count - 1);
        }
        
        OnNotificationsChanged?.Invoke();
    }
    
    public void AddTurnNotification(Guid gameId, string gameName)
    {
        AddNotification(new GameNotification
        {
            Id = Guid.NewGuid(),
            Type = NotificationType.YourTurn,
            GameId = gameId,
            GameName = gameName,
            Message = $"It's your turn in {gameName}!",
            Timestamp = DateTime.UtcNow,
            IsRead = false
        });
    }
    
    public void AddGameStartedNotification(Guid gameId, string gameName)
    {
        AddNotification(new GameNotification
        {
            Id = Guid.NewGuid(),
            Type = NotificationType.GameStarted,
            GameId = gameId,
            GameName = gameName,
            Message = $"Game '{gameName}' has started!",
            Timestamp = DateTime.UtcNow,
            IsRead = false
        });
    }
    
    public void AddGameEndedNotification(Guid gameId, string gameName, string reason)
    {
        AddNotification(new GameNotification
        {
            Id = Guid.NewGuid(),
            Type = NotificationType.GameEnded,
            GameId = gameId,
            GameName = gameName,
            Message = $"Game '{gameName}' ended: {reason}",
            Timestamp = DateTime.UtcNow,
            IsRead = false
        });
    }
    
    public void MarkAsRead(Guid notificationId)
    {
        var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            OnNotificationsChanged?.Invoke();
        }
    }
    
    public void MarkAllAsRead()
    {
        foreach (var notification in _notifications)
        {
            notification.IsRead = true;
        }
        OnNotificationsChanged?.Invoke();
    }
    
    public void ClearNotifications()
    {
        _notifications.Clear();
        OnNotificationsChanged?.Invoke();
    }
    
    public void RemoveNotificationsForGame(Guid gameId)
    {
        _notifications.RemoveAll(n => n.GameId == gameId);
        OnNotificationsChanged?.Invoke();
    }
}

public class GameNotification
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public Guid GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; }
}

public enum NotificationType
{
    YourTurn,
    GameStarted,
    GameEnded,
    PlayerJoined,
    PlayerLeft,
    CombatStarted,
    RoundEnded
}
