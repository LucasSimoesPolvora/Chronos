public class IndexService
{
    private Index index = new();
    private readonly string IndexPath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos", "index.json");
    private readonly string ObjectsPath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos", "objects");

    public void LoadIndex()
    {
        if (File.Exists(IndexPath))
        {
            try
            {
                string json = File.ReadAllText(IndexPath);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    index = new Index();
                    return;
                }
                
                index = Index.FromJson(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading index: {ex.Message}");
                index = new Index();
            }
        }
    }

    public IndexEntry[] GetEntries()
    {
        return index.Entries.ToArray();
    }

    public void SaveIndex()
    {
        try
        {
            Directory.CreateDirectory(ObjectsPath);
            string json = index.ToJson();
            File.WriteAllText(IndexPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving index: {ex.Message}");
        }
    }

    public void AddOrUpdateEntry(string relativePath, string blobHash)
    {
        index.AddOrUpdateEntry(relativePath, blobHash);
    }

    public void MarkEntryDeleted(string relativePath)
    {
        index.MarkEntryDeleted(relativePath);
    }

    public void ClearIndex()
    {
        index.ClearIndex();
    }
}
