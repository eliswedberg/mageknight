using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MageKnightOnline.Models;

namespace MageKnightOnline.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<GameSession> GameSessions { get; set; }
    public DbSet<GamePlayer> GamePlayers { get; set; }
    public DbSet<GameAction> GameActions { get; set; }
    public DbSet<MageKnightCard> MageKnightCards { get; set; }
    public DbSet<PlayerHand> PlayerHands { get; set; }
    public DbSet<PlayerDeck> PlayerDecks { get; set; }
    public DbSet<PlayerDiscard> PlayerDiscards { get; set; }
    
    // Game Turn Management
    public DbSet<GameTurn> GameTurns { get; set; }
    public DbSet<TurnAction> TurnActions { get; set; }
    
    // Game Board and Map
    public DbSet<GameBoard> GameBoards { get; set; }
    public DbSet<BoardTile> BoardTiles { get; set; }
    public DbSet<PlayerPosition> PlayerPositions { get; set; }
    public DbSet<Site> Sites { get; set; }
    public DbSet<SiteEnemy> SiteEnemies { get; set; }
    
    // Combat System
    public DbSet<Combat> Combats { get; set; }
    public DbSet<CombatAction> CombatActions { get; set; }
    public DbSet<CombatParticipant> CombatParticipants { get; set; }
    
    // Spells
    public DbSet<Spell> Spells { get; set; }
    public DbSet<PlayerSpell> PlayerSpells { get; set; }
    
    // Artifacts
    public DbSet<Artifact> Artifacts { get; set; }
    public DbSet<PlayerArtifact> PlayerArtifacts { get; set; }
    
    // Units
    public DbSet<Unit> Units { get; set; }
    public DbSet<PlayerUnit> PlayerUnits { get; set; }
    
    // Game State
    public DbSet<GameState> GameStates { get; set; }
    public DbSet<GameEvent> GameEvents { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure GameSession relationships
        builder.Entity<GameSession>()
            .HasOne(gs => gs.HostUser)
            .WithMany()
            .HasForeignKey(gs => gs.HostUserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.Entity<GameSession>()
            .HasMany(gs => gs.Players)
            .WithOne(gp => gp.GameSession)
            .HasForeignKey(gp => gp.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<GameSession>()
            .HasMany(gs => gs.Actions)
            .WithOne(ga => ga.GameSession)
            .HasForeignKey(ga => ga.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure GamePlayer relationships
        builder.Entity<GamePlayer>()
            .HasOne(gp => gp.User)
            .WithMany()
            .HasForeignKey(gp => gp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<GamePlayer>()
            .HasMany(gp => gp.Actions)
            .WithOne(ga => ga.Player)
            .HasForeignKey(ga => ga.PlayerId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Configure unique constraints
        builder.Entity<GamePlayer>()
            .HasIndex(gp => new { gp.GameSessionId, gp.UserId })
            .IsUnique();
            
        builder.Entity<GamePlayer>()
            .HasIndex(gp => new { gp.GameSessionId, gp.PlayerNumber })
            .IsUnique();
        
        // Configure PlayerHand relationships
        builder.Entity<PlayerHand>()
            .HasOne(ph => ph.GamePlayer)
            .WithMany()
            .HasForeignKey(ph => ph.GamePlayerId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<PlayerHand>()
            .HasOne(ph => ph.Card)
            .WithMany()
            .HasForeignKey(ph => ph.CardId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure PlayerDeck relationships
        builder.Entity<PlayerDeck>()
            .HasOne(pd => pd.GamePlayer)
            .WithMany()
            .HasForeignKey(pd => pd.GamePlayerId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<PlayerDeck>()
            .HasOne(pd => pd.Card)
            .WithMany()
            .HasForeignKey(pd => pd.CardId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure PlayerDiscard relationships
        builder.Entity<PlayerDiscard>()
            .HasOne(pd => pd.GamePlayer)
            .WithMany()
            .HasForeignKey(pd => pd.GamePlayerId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<PlayerDiscard>()
            .HasOne(pd => pd.Card)
            .WithMany()
            .HasForeignKey(pd => pd.CardId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure GameTurn relationships
        builder.Entity<GameTurn>()
            .HasOne(gt => gt.GameSession)
            .WithMany()
            .HasForeignKey(gt => gt.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<GameTurn>()
            .HasOne(gt => gt.CurrentPlayer)
            .WithMany()
            .HasForeignKey(gt => gt.CurrentPlayerId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<GameTurn>()
            .HasMany(gt => gt.Actions)
            .WithOne(ta => ta.GameTurn)
            .HasForeignKey(ta => ta.GameTurnId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure TurnAction relationships
        builder.Entity<TurnAction>()
            .HasOne(ta => ta.Player)
            .WithMany()
            .HasForeignKey(ta => ta.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure GameBoard relationships
        builder.Entity<GameBoard>()
            .HasOne(gb => gb.GameSession)
            .WithMany()
            .HasForeignKey(gb => gb.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<GameBoard>()
            .HasMany(gb => gb.Tiles)
            .WithOne(bt => bt.GameBoard)
            .HasForeignKey(bt => bt.GameBoardId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<GameBoard>()
            .HasMany(gb => gb.PlayerPositions)
            .WithOne(pp => pp.GameBoard)
            .HasForeignKey(pp => pp.GameBoardId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<GameBoard>()
            .HasMany(gb => gb.Sites)
            .WithOne(s => s.GameBoard)
            .HasForeignKey(s => s.GameBoardId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure BoardTile relationships
        builder.Entity<BoardTile>()
            .HasOne(bt => bt.Site)
            .WithMany()
            .HasForeignKey(bt => bt.SiteId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Configure PlayerPosition relationships
        builder.Entity<PlayerPosition>()
            .HasOne(pp => pp.Player)
            .WithMany()
            .HasForeignKey(pp => pp.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure Site relationships
        builder.Entity<Site>()
            .HasOne(s => s.ConqueredByPlayer)
            .WithMany()
            .HasForeignKey(s => s.ConqueredByPlayerId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.Entity<Site>()
            .HasMany(s => s.Enemies)
            .WithOne(se => se.Site)
            .HasForeignKey(se => se.SiteId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure SiteEnemy relationships
        builder.Entity<SiteEnemy>()
            .HasOne(se => se.Site)
            .WithMany(s => s.Enemies)
            .HasForeignKey(se => se.SiteId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure Combat relationships
        builder.Entity<Combat>()
            .HasOne(c => c.GameSession)
            .WithMany()
            .HasForeignKey(c => c.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<Combat>()
            .HasOne(c => c.AttackingPlayer)
            .WithMany()
            .HasForeignKey(c => c.AttackingPlayerId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<Combat>()
            .HasOne(c => c.DefendingSite)
            .WithMany()
            .HasForeignKey(c => c.DefendingSiteId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.Entity<Combat>()
            .HasOne(c => c.DefendingPlayer)
            .WithMany()
            .HasForeignKey(c => c.DefendingPlayerId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.Entity<Combat>()
            .HasMany(c => c.Actions)
            .WithOne(ca => ca.Combat)
            .HasForeignKey(ca => ca.CombatId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<Combat>()
            .HasMany(c => c.Participants)
            .WithOne(cp => cp.Combat)
            .HasForeignKey(cp => cp.CombatId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure CombatAction relationships
        builder.Entity<CombatAction>()
            .HasOne(ca => ca.Player)
            .WithMany()
            .HasForeignKey(ca => ca.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<CombatAction>()
            .HasOne(ca => ca.Card)
            .WithMany()
            .HasForeignKey(ca => ca.CardId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Configure CombatParticipant relationships
        builder.Entity<CombatParticipant>()
            .HasOne(cp => cp.Player)
            .WithMany()
            .HasForeignKey(cp => cp.PlayerId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.Entity<CombatParticipant>()
            .HasOne(cp => cp.Enemy)
            .WithMany()
            .HasForeignKey(cp => cp.EnemyId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Configure Spell relationships
        builder.Entity<PlayerSpell>()
            .HasOne(ps => ps.GamePlayer)
            .WithMany()
            .HasForeignKey(ps => ps.GamePlayerId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<PlayerSpell>()
            .HasOne(ps => ps.Spell)
            .WithMany()
            .HasForeignKey(ps => ps.SpellId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure Artifact relationships
        builder.Entity<PlayerArtifact>()
            .HasOne(pa => pa.GamePlayer)
            .WithMany()
            .HasForeignKey(pa => pa.GamePlayerId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<PlayerArtifact>()
            .HasOne(pa => pa.Artifact)
            .WithMany()
            .HasForeignKey(pa => pa.ArtifactId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure Unit relationships
        builder.Entity<PlayerUnit>()
            .HasOne(pu => pu.GamePlayer)
            .WithMany()
            .HasForeignKey(pu => pu.GamePlayerId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<PlayerUnit>()
            .HasOne(pu => pu.Unit)
            .WithMany()
            .HasForeignKey(pu => pu.UnitId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure GameState relationships
        builder.Entity<GameState>()
            .HasOne(gs => gs.GameSession)
            .WithMany()
            .HasForeignKey(gs => gs.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<GameState>()
            .HasOne(gs => gs.CurrentPlayer)
            .WithMany()
            .HasForeignKey(gs => gs.CurrentPlayerId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.Entity<GameState>()
            .HasMany(gs => gs.Events)
            .WithOne(ge => ge.GameState)
            .HasForeignKey(ge => ge.GameStateId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure GameEvent relationships
        builder.Entity<GameEvent>()
            .HasOne(ge => ge.Player)
            .WithMany()
            .HasForeignKey(ge => ge.PlayerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
