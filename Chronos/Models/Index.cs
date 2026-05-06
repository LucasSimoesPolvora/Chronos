using System.Text.Json;

public class IndexEntry
{
    public required string RelativePath { get; set; }

    public required string BlobHash { get; set; }

    public FileStatusEnum Status { get; set; }  
}

public class Index
{
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
    /// Serializes index to JSON format
    /// </summary>
    public string ToJson()
    {
        JsonSerializerOptions options = new() { WriteIndented = true };
        JsonContext context = new(options);
        return JsonSerializer.Serialize(this, context.Index);
    }

    /// <summary>
    /// Deserializes index from JSON format
    /// </summary>
    public static Index FromJson(string json)
    {
        JsonContext context = new();
        return JsonSerializer.Deserialize(json, context.Index) ?? new Index();
    }

    public void MarkEntryDeleted(string relativePath)
    {
        IndexEntry? existing = GetEntry(relativePath);
        if (existing != null)
        {
            existing.Status = FileStatusEnum.deleted;
        }
    }

    public void ClearIndex()
    {
        Entries.RemoveAll(e => e.Status == FileStatusEnum.deleted);
    }
}
