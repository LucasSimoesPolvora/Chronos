using System.Security.Cryptography;

public class CommitService
{
    private readonly string ObjectsPath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos", "objects");
    private readonly TreeService _treeService = new();
    private const uint MAGIC_NUMBER = 0x4348524F;

    public Commit CreateProjectCommit(string message)
    {
        Tree tree = _treeService.CreateProjectTree();
        _treeService.SaveTree(tree);

        Commit commit = new()
        {
            Hash = "",
            Message = message,
            Timestamp = DateTime.Now,
            TreeHash = tree.Hash,
            ParentHash = File.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".chronos", "HEAD")) ? File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), ".chronos", "HEAD")) : string.Empty,
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
            
            writer.Write(commit.Hash);
            writer.Write(commit.Message);
            writer.Write(commit.TreeHash);
            writer.Write(commit.ParentHash ?? "");
            writer.Write(commit.Timestamp.Ticks);

            return ms.ToArray();
        }
    }

    public bool IsFileModified(string filePath, string indexBlobHash)
    {
        if (!File.Exists(filePath))
            return true;

        using (var sha256 = SHA256.Create())
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            byte[] hashBytes = sha256.ComputeHash(fileBytes);
            string currentHash = Convert.ToHexString(hashBytes).ToLower();
            
            return currentHash != indexBlobHash;
        }
    }

    public Commit LoadCommit(string commitHash)
    {
        string commitPath = Path.Combine(ObjectsPath, commitHash[..2], commitHash[2..]);
        if (!File.Exists(commitPath))
            throw new FileNotFoundException($"Commit {commitHash} not found.");

        byte[] content = File.ReadAllBytes(commitPath);
        return FromBinary(content);
    }

    public Commit FromBinary(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            uint magic = reader.ReadUInt32();
            if (magic != MAGIC_NUMBER)
                throw new InvalidDataException("Invalid commit object.");

            FileTypeEnum fileType = (FileTypeEnum)reader.ReadByte();
            if (fileType != FileTypeEnum.Commit)
                throw new InvalidDataException("Data is not a commit object.");

            string hash = reader.ReadString();
            string message = reader.ReadString();
            string treeHash = reader.ReadString();
            string parentHash = reader.ReadString();
            long timestampTicks = reader.ReadInt64();
            
            DateTime timestamp = new(timestampTicks, DateTimeKind.Utc);

            return new Commit
            {
                Hash = hash,
                Message = message,
                TreeHash = treeHash,
                Timestamp = timestamp,
                ParentHash = parentHash,
                FileType = (FileStatusEnum)fileType
            };
        }
    }

}