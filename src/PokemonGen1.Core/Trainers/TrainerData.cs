namespace PokemonGen1.Core.Trainers;

public enum TrainerClass
{
    Youngster, BugCatcher, Lass, Sailor, JrTrainer, Hiker, Biker,
    Burglar, Engineer, Fisherman, Swimmer, CueBall, Gambler, Beauty,
    Psychic, Rocker, Juggler, Tamer, Birdkeeper, Blackbelt, Scientist,
    Gentleman, Rival, ProfOak, Champion, EliteFour, GymLeader,
    Channeler, RocketGrunt, CoolTrainer, SuperNerd, PokéManiac
}

public enum AIBehavior { Random, Smart, GymLeader, EliteFour, Champion }

public class TrainerData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public TrainerClass Class { get; set; }
    public string? Title { get; set; }
    public TrainerPokemon[] Party { get; set; } = Array.Empty<TrainerPokemon>();
    public int RewardMoney { get; set; }
    public string[] BeforeBattleDialog { get; set; } = Array.Empty<string>();
    public string[] AfterBattleDialog { get; set; } = Array.Empty<string>();
    public bool IsGymLeader { get; set; }
    public int? BadgeIndex { get; set; }
    public AIBehavior AiBehavior { get; set; }
}

public class TrainerPokemon
{
    public int SpeciesId { get; set; }
    public int Level { get; set; }
    public int[]? MoveOverrides { get; set; }
}
