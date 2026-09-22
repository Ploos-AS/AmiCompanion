using System.Security.Cryptography;

namespace AmiCompanion.Core.Checksums;

public static class ChecksumService
{
    public static ChecksumResult Compute(ReadOnlySpan<byte> data)
    {
        var sha256 = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
        return new ChecksumResult(sha256, ComputeCrc32(data));
    }

    public static async Task<ChecksumResult> ComputeFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);

        stream.Position = 0;
        var crc = await ComputeCrc32Async(stream, cancellationToken);
        return new ChecksumResult(Convert.ToHexString(hash).ToLowerInvariant(), crc);
    }

    private static uint ComputeCrc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xffffffff;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc >> 1) ^ (0xedb88320u & (uint)-(int)(crc & 1));
        }
        return ~crc;
    }

    private static async Task<uint> ComputeCrc32Async(Stream stream, CancellationToken cancellationToken)
    {
        uint crc = 0xffffffff;
        var buffer = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            foreach (var value in buffer.AsSpan(0, read))
            {
                crc ^= value;
                for (var bit = 0; bit < 8; bit++)
                    crc = (crc >> 1) ^ (0xedb88320u & (uint)-(int)(crc & 1));
            }
        }
        return ~crc;
    }
}
