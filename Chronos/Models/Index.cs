using System.Text.Json.Serialization;

public class IndexEntry
{
    public required string RelativePath { get; set; }

    public required string BlobHash { get; set; }
}

public class Index
{
    private const uint MAGIC_NUMBER = 0x4348524F;
    private const ushort VERSION = 1;

    public List<IndexEntry> Entries { get; set; } = [];

    public IndexEntry? GetEntry(string relativePath)
    {
        return Entries.FirstOrDefault(e => e.RelativePath == relativePath);
    }

    public void AddOrUpdateEntry(string relativePath, string blobHash)
    {
        IndexEntry? existing = GetEntry(relativePath);
        if (existing != null)
        {
            existing.BlobHash = blobHash;
        }
        else
        {
            Entries.Add(new IndexEntry { RelativePath = relativePath, BlobHash = blobHash });
        }
    }

    public bool EntryExists(string relativePath)
    {
        return Entries.Any(e => e.RelativePath == relativePath);
    }

    /// <summary>
    /// Serializes index to binary format
    /// Format: [Magic:4][Version:2][EntryCount:4][Entry...]
    /// Each Entry: [PathLength:2][Path][HashLength:2][Hash]
    /// </summary>
    public byte[] ToBinary()
    {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            // Write header
            writer.Write(MAGIC_NUMBER);
            writer.Write(VERSION);
            writer.Write(Entries.Count);

            // Write entries
            foreach (var entry in Entries)
            {
                writer.Write((ushort)entry.RelativePath.Length);
                writer.Write(entry.RelativePath);
                writer.Write((ushort)entry.BlobHash.Length);
                writer.Write(entry.BlobHash);
            }

            return ms.ToArray();
        }
    }

    /// <summary>
    /// Deserializes index from binary format
    /// </summary>
    public static Index FromBinary(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            // Read and verify header
            uint magic = reader.ReadUInt32();
            if (magic != MAGIC_NUMBER)
                throw new InvalidOperationException("Invalid index file format: wrong magic number");

            ushort version = reader.ReadUInt16();
            if (version != VERSION)
                throw new InvalidOperationException($"Unsupported index version: {version}");

            int entryCount = reader.ReadInt32();
            var index = new Index();

            // Read entries
            for (int i = 0; i < entryCount; i++)
            {
                ushort pathLength = reader.ReadUInt16();
                string path = new string(reader.ReadChars(pathLength));

                ushort hashLength = reader.ReadUInt16();
                string hash = new string(reader.ReadChars(hashLength));

                index.Entries.Add(new IndexEntry { RelativePath = path, BlobHash = hash });
            }

            return index;
        }
    }
}
