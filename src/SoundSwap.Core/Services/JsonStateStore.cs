using System.Text.Json;
using System.Text.Json.Serialization;
using SoundSwap.Core.Models;

namespace SoundSwap.Core.Services;

/// <summary>Stores the state as indented JSON. Writes are atomic; a corrupt file is kept as .bak.</summary>
public sealed class JsonStateStore : IStateStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;

    /// <summary>Creates a store.</summary>
    /// /// <param name="path">Full path of the JSON file.</param>
    public JsonStateStore(string path) => _path = path;

    /// <summary>Loads the file. A missing file gives a default state; a corrupt file is renamed to <c>.bak</c> and a default state is returned.</summary>
    public AppState Load()
    {
        if (!File.Exists(_path)) return new AppState();
        try
        {
            using var stream = File.OpenRead(_path);
            return JsonSerializer.Deserialize<AppState>(stream, Options) ?? new AppState();
        }
        catch (JsonException)
        {
            File.Move(_path, _path + ".bak", overwrite: true);
            return new AppState();
        }
    }

    /// <summary>Saves atomically (temporary file, then replace) so a crash cannot leave a half-written file.</summary>
    /// /// <param name="state">The state to persist.</param>
    public void Save(AppState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(state, Options));
        File.Move(temp, _path, overwrite: true);
    }
}
