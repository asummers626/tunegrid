namespace TuneGrid.App.Services;

internal static class AppPaths
{
    public static string DatabaseFile { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TuneGrid",
        "tunegrid.db");

    static AppPaths()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabaseFile)!);
    }
}
