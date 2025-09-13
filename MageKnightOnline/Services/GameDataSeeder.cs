using MageKnightOnline.Data;
using MageKnightOnline.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace MageKnightOnline.Services;

public class GameDataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GameDataSeeder> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public GameDataSeeder(ApplicationDbContext context, ILogger<GameDataSeeder> logger, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Seed test user
            await SeedTestUserAsync();
            
            // Seed Mage Knight Cards
            await SeedMageKnightCardsAsync();
            
            // Seed Spells
            await SeedSpellsAsync();
            
            // Seed Artifacts
            await SeedArtifactsAsync();
            
            // Seed Units
            await SeedUnitsAsync();
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("Game data seeded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding game data");
        }
    }

    private async Task SeedTestUserAsync()
    {
        var testUser = await _userManager.FindByEmailAsync("test@example.com");
        if (testUser == null)
        {
            testUser = new ApplicationUser
            {
                UserName = "test@example.com",
                Email = "test@example.com",
                EmailConfirmed = true
            };
            
            var result = await _userManager.CreateAsync(testUser, "Test123!");
            if (result.Succeeded)
            {
                _logger.LogInformation("Test user created successfully");
            }
            else
            {
                _logger.LogError("Failed to create test user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedMageKnightCardsAsync()
    {
        if (await _context.MageKnightCards.AnyAsync()) return;

        var cards = new List<MageKnightCard>
        {
            // Basic Movement Cards
            new MageKnightCard { Name = "March", Type = CardType.BasicMove, Move = 1, Cost = 0, Set = "Base", ImageUrl = "/images/deed_basic_march.jpg" },
            new MageKnightCard { Name = "Swiftness", Type = CardType.BasicMove, Move = 2, Cost = 1, Set = "Base", ImageUrl = "/images/deed_basic_swiftness.jpg" },
            new MageKnightCard { Name = "Refreshing Walk", Type = CardType.BasicMove, Move = 3, Cost = 2, Set = "Base", ImageUrl = "/images/adv_action_refreshing_walk.jpg" },
            
            // Basic Attack Cards
            new MageKnightCard { Name = "Rage", Type = CardType.BasicAttack, Attack = 1, Cost = 0, Set = "Base", ImageUrl = "/images/deed_basic_rage.jpg" },
            new MageKnightCard { Name = "Battle Versatility", Type = CardType.BasicAttack, Attack = 2, Cost = 1, Set = "Base", ImageUrl = "/images/deed_basic_battle_versatility.jpg" },
            new MageKnightCard { Name = "Blood Rage", Type = CardType.BasicAttack, Attack = 3, Cost = 2, Set = "Base", ImageUrl = "/images/adv_action_blood_rage.jpg" },
            
            // Basic Block Cards
            new MageKnightCard { Name = "Stamina", Type = CardType.BasicBlock, Block = 1, Cost = 0, Set = "Base", ImageUrl = "/images/deed_basic_stamina.jpg" },
            new MageKnightCard { Name = "Battle Tranquility", Type = CardType.BasicBlock, Block = 2, Cost = 1, Set = "Base", ImageUrl = "/images/deed_basic_battle_tranquility.jpg" },
            new MageKnightCard { Name = "Ice Shield", Type = CardType.BasicBlock, Block = 3, Cost = 2, Set = "Base", ImageUrl = "/images/adv_action_ice_shield.jpg" },
            
            // Basic Influence Cards
            new MageKnightCard { Name = "Noble Manners", Type = CardType.BasicInfluence, Influence = 1, Cost = 0, Set = "Base", ImageUrl = "/images/deed_basic_noble_manners.jpg" },
            new MageKnightCard { Name = "Promise", Type = CardType.BasicInfluence, Influence = 2, Cost = 1, Set = "Base", ImageUrl = "/images/deed_basic_promise.jpg" },
            new MageKnightCard { Name = "Diplomacy", Type = CardType.BasicInfluence, Influence = 3, Cost = 2, Set = "Base", ImageUrl = "/images/adv_action_diplomacy.jpg" },
            
            // Advanced Cards
            new MageKnightCard { Name = "Path Finding", Type = CardType.AdvancedMove, Move = 4, Cost = 3, Set = "Base", ImageUrl = "/images/adv_action_path_finding.jpg" },
            new MageKnightCard { Name = "Crushing Blow", Type = CardType.AdvancedAttack, Attack = 4, Cost = 3, Set = "Base", ImageUrl = "/images/adv_action_crushing_blot.jpg" },
            new MageKnightCard { Name = "Steady Tempo", Type = CardType.AdvancedBlock, Block = 4, Cost = 3, Set = "Base", ImageUrl = "/images/adv_action_steady_tempo.jpg" },
            new MageKnightCard { Name = "Intimidate", Type = CardType.AdvancedInfluence, Influence = 4, Cost = 3, Set = "Base", ImageUrl = "/images/adv_action_intimidate.jpg" },
            
            // Special Cards
            new MageKnightCard { Name = "Crystallize", Type = CardType.Special, Influence = 2, Cost = 2, Set = "Base", ImageUrl = "/images/deed_basic_crystallize.jpg" },
            new MageKnightCard { Name = "Mana Draw", Type = CardType.Special, Fame = 1, Cost = 1, Set = "Base", ImageUrl = "/images/deed_basic_mana_draw.jpg" },
            new MageKnightCard { Name = "Will Focus", Type = CardType.Special, Reputation = 1, Cost = 1, Set = "Base", ImageUrl = "/images/deed_basic_will_focus.jpg" }
        };

        _context.MageKnightCards.AddRange(cards);
    }

    private async Task SeedSpellsAsync()
    {
        if (await _context.Spells.AnyAsync()) return;

        var spells = new List<Spell>
        {
            new Spell { Name = "Fireball", Type = SpellType.Attack, ManaCost = 2, Attack = 3, Range = 2, Set = "Base" },
            new Spell { Name = "Heal", Type = SpellType.Healing, ManaCost = 1, Set = "Base" },
            new Spell { Name = "Teleport", Type = SpellType.Movement, ManaCost = 3, Move = 5, Set = "Base" },
            new Spell { Name = "Charm", Type = SpellType.Influence, ManaCost = 2, Influence = 3, Set = "Base" },
            new Spell { Name = "Shield", Type = SpellType.Defense, ManaCost = 1, Block = 2, IsPersistent = true, Set = "Base" },
            new Spell { Name = "Lightning Bolt", Type = SpellType.Attack, ManaCost = 3, Attack = 5, Range = 3, Set = "Base" },
            new Spell { Name = "Invisibility", Type = SpellType.Utility, ManaCost = 2, IsPersistent = true, Set = "Base" },
            new Spell { Name = "Summon Elemental", Type = SpellType.Summoning, ManaCost = 4, Set = "Base" }
        };

        _context.Spells.AddRange(spells);
    }

    private async Task SeedArtifactsAsync()
    {
        if (await _context.Artifacts.AnyAsync()) return;

        var artifacts = new List<Artifact>
        {
            new Artifact { Name = "Sword of Justice", Type = ArtifactType.Weapon, Attack = 2, Cost = 3, Rarity = Rarity.Common, Set = "Base", ImageUrl = "/images/artifact_sword_of_justice.jpg" },
            new Artifact { Name = "Banner of Protection", Type = ArtifactType.Armor, Block = 2, Cost = 3, Rarity = Rarity.Common, Set = "Base", ImageUrl = "/images/artifact_banner_of_protection.jpg" },
            new Artifact { Name = "Banner of Courage", Type = ArtifactType.Boots, Move = 1, Cost = 2, Rarity = Rarity.Common, Set = "Base", ImageUrl = "/images/artifact_banner_of_courage.jpg" },
            new Artifact { Name = "Ruby Ring", Type = ArtifactType.Ring, Influence = 1, Cost = 2, Rarity = Rarity.Common, Set = "Base", ImageUrl = "/images/artifact_ruby_ring.jpg" },
            new Artifact { Name = "Amulet of Darkness", Type = ArtifactType.Amulet, Cost = 4, Rarity = Rarity.Uncommon, Set = "Base", ImageUrl = "/images/artifact_amulet_of_darkness.jpg" },
            new Artifact { Name = "Horn of Wrath", Type = ArtifactType.Staff, Attack = 3, Cost = 4, Rarity = Rarity.Uncommon, Set = "Base", ImageUrl = "/images/artifact_horn_of_wrath.jpg" },
            new Artifact { Name = "Book of Wisdom", Type = ArtifactType.Orb, Cost = 5, Rarity = Rarity.Rare, Set = "Base", ImageUrl = "/images/artifact_book_of_wisdom.jpg" },
            new Artifact { Name = "Banner of Fear", Type = ArtifactType.Cloak, Cost = 3, Rarity = Rarity.Uncommon, Set = "Base", ImageUrl = "/images/artifact_banner_of_fear.jpg" }
        };

        _context.Artifacts.AddRange(artifacts);
    }

    private async Task SeedUnitsAsync()
    {
        if (await _context.Units.AnyAsync()) return;

        var units = new List<Unit>
        {
            new Unit { Name = "Peasant", Type = UnitType.Infantry, Cost = 1, Attack = 1, Block = 1, Health = 1, Rarity = Rarity.Common, Set = "Base" },
            new Unit { Name = "Archer", Type = UnitType.Archer, Cost = 2, Attack = 2, Block = 1, Health = 1, Rarity = Rarity.Common, Set = "Base" },
            new Unit { Name = "Knight", Type = UnitType.Cavalry, Cost = 3, Attack = 3, Block = 2, Health = 2, Rarity = Rarity.Uncommon, Set = "Base" },
            new Unit { Name = "Mage", Type = UnitType.Mage, Cost = 4, Attack = 2, Block = 1, Health = 1, IsMage = true, Rarity = Rarity.Uncommon, Set = "Base" },
            new Unit { Name = "Elite Guard", Type = UnitType.Elite, Cost = 5, Attack = 4, Block = 3, Health = 3, IsElite = true, Rarity = Rarity.Rare, Set = "Base" },
            new Unit { Name = "Mercenary", Type = UnitType.Mercenary, Cost = 3, Attack = 2, Block = 2, Health = 2, IsMercenary = true, Rarity = Rarity.Common, Set = "Base" },
            new Unit { Name = "Dragon", Type = UnitType.Special, Cost = 8, Attack = 6, Block = 4, Health = 5, Rarity = Rarity.Legendary, Set = "Base" },
            new Unit { Name = "Elemental", Type = UnitType.Special, Cost = 6, Attack = 4, Block = 3, Health = 3, Rarity = Rarity.Epic, Set = "Base" }
        };

        _context.Units.AddRange(units);
    }
}
