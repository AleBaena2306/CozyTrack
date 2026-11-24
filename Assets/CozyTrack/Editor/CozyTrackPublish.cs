using KovusPride.Publishing;
using UnityEditor;

// COZY TRACK > Build > <portal>: una build lista para subir a cada portal web (ver KovusPridePublish).
// El portfolio sigue con COZY TRACK > Build > Web (móvil + PC).
public static class CozyTrackPublish
{
    private const string Game = "CozyTrack";

    [MenuItem("COZY TRACK/Build/CrazyGames", priority = 100)]
    public static void CrazyGames() => KovusPridePublish.Build(Store.CrazyGames, Game);

    [MenuItem("COZY TRACK/Build/Playgama", priority = 101)]
    public static void Playgama() => KovusPridePublish.Build(Store.Playgama, Game);

    [MenuItem("COZY TRACK/Build/Playgama · subir la última build", priority = 111)]
    public static void PlaygamaUpload() => KovusPridePublish.UploadLast(Store.Playgama, Game);

    [MenuItem("COZY TRACK/Build/GameDistribution", priority = 102)]
    public static void GameDistribution() => KovusPridePublish.Build(Store.GameDistribution, Game);

    [MenuItem("COZY TRACK/Build/itch.io", priority = 103)]
    public static void Itch() => KovusPridePublish.Build(Store.Itch, Game);

    [MenuItem("COZY TRACK/Build/Todos los portales", priority = 120)]
    public static void All() => KovusPridePublish.BuildAll(Game);
}
