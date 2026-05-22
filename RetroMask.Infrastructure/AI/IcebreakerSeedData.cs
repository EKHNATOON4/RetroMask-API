namespace RetroMask.Infrastructure.AI;

public static class IcebreakerSeedData
{
    public static IEnumerable<IcebreakerGameTemplate> GetTemplates() =>
    [
        new("TwoTruthsOneLie", "Two Truths and a Lie",
            "Each participant shares 3 statements — 2 true, 1 false. Team votes on the lie."),
        new("WouldYouRather", "Would You Rather?",
            "Team votes between two funny or thought-provoking options."),
        new("QuickFire", "Quick Fire Questions",
            "A rapid-fire round of light-hearted questions about work style and preferences."),
        new("GifReaction", "GIF Reaction",
            "Participants pick a GIF that describes their week. Great for remote teams."),
        new("OneWord", "One Word Check-In",
            "Each person shares one word that describes how they feel starting this retro."),
        new("PersonalTrivia", "Team Trivia",
            "Fun trivia questions about the team's history and achievements."),
    ];

    public record IcebreakerGameTemplate(string Type, string Title, string Description);
}
