using System.IO;
using System.Text.Json;
using LaunchForge.Models;

namespace LaunchForge.Services;

public class SequenceSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented       = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task SaveAsync(Sequence sequence, string filePath)
    {
        sequence.ModifiedAt = DateTime.UtcNow;
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, sequence, Options);
    }

    public async Task<Sequence> LoadAsync(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<Sequence>(stream, Options)
               ?? throw new InvalidDataException("Sequence file was empty or invalid.");
    }
}
