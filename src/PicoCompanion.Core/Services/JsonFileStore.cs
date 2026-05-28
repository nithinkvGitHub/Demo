using System.Text.Json;
using PicoCompanion.Core.Protocol;

namespace PicoCompanion.Core.Services;

public sealed class JsonFileStore<T> where T : class, new()
{
    private readonly string _path;

    public JsonFileStore(string path)
    {
        _path = path;
    }

    public async Task<T> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
        {
            return new T();
        }

        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonDefaults.Options, cancellationToken)
            .ConfigureAwait(false)
            ?? new T();
    }

    public async Task SaveAsync(T value, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{_path}.tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, JsonDefaults.Options, cancellationToken)
                .ConfigureAwait(false);
        }

        File.Move(tempPath, _path, overwrite: true);
    }
}
