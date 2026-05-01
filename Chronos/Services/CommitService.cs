using System.Security.Cryptography;

public class CommitService
{
    private readonly string ObjectsPath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos", "objects");
    private readonly TreeService _treeService = new();
    private const uint MAGIC_NUMBER = 0x4348524F;

    public Commit CreateProjectCommit(string rootPath, string message)
    {
        // Create tree first
        Tree tree = _treeService.CreateProjectTree(rootPath);
        _treeService.SaveTree(tree);

        Commit commit = new()
        {
            Hash = "",
            Message = message,
            Timestamp = DateTime.UtcNow,
            TreeHash = tree.Hash,
            FileType = (FileStatusEnum)FileTypeEnum.Commit
        };
        
        commit.Hash = CalculateCommitHash(commit);
        return commit;
    }

    private string CalculateCommitHash(Commit commit)
    {
        using (var sha256 = SHA256.Create())
        {
            string commitContent = $"{commit.TreeHash}|{commit.Message}|{commit.Timestamp:O}";
            byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(commitContent));
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }

    public string SaveCommit(Commit commit)
    {
        try
        {
            if(!Directory.Exists(Path.Combine(ObjectsPath, commit.Hash[..2]))) 
                Directory.CreateDirectory(Path.Combine(ObjectsPath, commit.Hash[..2]));

            string commitPath = Path.Combine(ObjectsPath, commit.Hash[..2], commit.Hash[2..]);
            
            if (File.Exists(commitPath))
            {
                return commitPath;
            }

            byte[] content = ToBinary(commit);
            File.WriteAllBytes(commitPath, content);
            return commitPath;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Permission denied saving commit: {ex.Message}");
        }
    }

    private byte[] ToBinary(Commit commit)
    {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write(MAGIC_NUMBER);
            writer.Write((byte)commit.FileType);
            
            writer.Write((ushort)commit.Hash.Length);
            writer.Write(commit.Hash);
            
            writer.Write((ushort)commit.Message.Length);
            writer.Write(commit.Message);
            
            writer.Write((ushort)commit.TreeHash.Length);
            writer.Write(commit.TreeHash);
            
            // Write timestamp
            writer.Write(commit.Timestamp.Ticks);

            return ms.ToArray();
        }
    }
}