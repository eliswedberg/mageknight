-- Add GameSession table
CREATE TABLE GameSessions (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    Status INTEGER NOT NULL,
    MaxPlayers INTEGER NOT NULL,
    CurrentPlayers INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    StartedAt TEXT,
    EndedAt TEXT,
    HostUserId TEXT NOT NULL,
    FOREIGN KEY (HostUserId) REFERENCES AspNetUsers (Id) ON DELETE RESTRICT
);

-- Add GamePlayer table
CREATE TABLE GamePlayers (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    GameSessionId INTEGER NOT NULL,
    UserId TEXT NOT NULL,
    PlayerNumber INTEGER NOT NULL,
    Status INTEGER NOT NULL,
    JoinedAt TEXT NOT NULL,
    LeftAt TEXT,
    KnightName TEXT,
    Level INTEGER NOT NULL,
    Fame INTEGER NOT NULL,
    Reputation INTEGER NOT NULL,
    Crystals INTEGER NOT NULL,
    Mana INTEGER NOT NULL,
    HandSize INTEGER NOT NULL,
    DeckSize INTEGER NOT NULL,
    DiscardSize INTEGER NOT NULL,
    Wounds INTEGER NOT NULL,
    IsCurrentPlayer INTEGER NOT NULL,
    HasPassed INTEGER NOT NULL,
    FOREIGN KEY (GameSessionId) REFERENCES GameSessions (Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);

-- Add GameAction table
CREATE TABLE GameActions (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    GameSessionId INTEGER NOT NULL,
    PlayerId INTEGER,
    Type INTEGER NOT NULL,
    Description TEXT NOT NULL,
    Data TEXT,
    Timestamp TEXT NOT NULL,
    TurnNumber INTEGER NOT NULL,
    ActionSequence INTEGER NOT NULL,
    FOREIGN KEY (GameSessionId) REFERENCES GameSessions (Id) ON DELETE CASCADE,
    FOREIGN KEY (PlayerId) REFERENCES GamePlayers (Id) ON DELETE SET NULL
);

-- Create indexes
CREATE UNIQUE INDEX IX_GamePlayers_GameSessionId_UserId ON GamePlayers (GameSessionId, UserId);
CREATE UNIQUE INDEX IX_GamePlayers_GameSessionId_PlayerNumber ON GamePlayers (GameSessionId, PlayerNumber);
