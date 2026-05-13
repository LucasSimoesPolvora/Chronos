using System.Net;
using System.Security.Cryptography;

public class TreeService
{
    private readonly string ObjectsPath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos", "objects");
    private readonly FileService _fileService = new();
    

    public Tree CreateProjectTree()
    {
        Tree tree = new()
        {
            Hash = "",
            Blobs = [],
            FileType = (FileStatusEnum)FileTypeEnum.Tree
        };

        VersionService vs = new();
        vs.GetVersionState(_fileService);

        BuildTreeRecursive(Directory.GetCurrentDirectory(), Directory.GetCurrentDirectory(), tree);
        
        tree.Hash = CalculateTreeHash(tree);
        return tree;
    }

    private void BuildTreeRecursive(string currentPath, string rootPath, Tree tree)
    {
        try
        {
            DirectoryInfo dirInfo = new(currentPath);
            foreach (FileInfo file in dirInfo.GetFiles())
            {
                string relativePath = Path.GetRelativePath(rootPath, file.FullName);
                string hash = _fileService.CalculateFileHash(file.FullName);
                FileStatusEnum? correspondence = _fileService.trackedFiles.Find(f => f.FilePath == file.FullName)?.Status;
                if(correspondence == FileStatusEnum.added || correspondence == FileStatusEnum.commited)
                {
                    tree.Blobs.Add(new Blob
                    {
                        FilePath = relativePath,
                        Hash = hash,
                        Status = correspondence.Value
                    });
                }
                
            }

            foreach (DirectoryInfo dir in dirInfo.GetDirectories())
            {
                if (dir.Name == ".chronos")
                {
                    continue;
                }

                BuildTreeRecursive(dir.FullName, rootPath, tree);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Permission denied accessing {currentPath}: {ex.Message}");
        }
    }

    private string CalculateTreeHash(Tree tree)
    {
        using (var sha256 = SHA256.Create())
        {
            var blobHashes = string.Join("|", tree.Blobs.Select(b => $"{b.FilePath}:{b.Hash}"));
            byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(blobHashes));
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }

    public string SaveTree(Tree tree)
    {
        try
        {
            if(!Directory.Exists(Path.Combine(ObjectsPath, tree.Hash[..2]))) 
                Directory.CreateDirectory(Path.Combine(ObjectsPath, tree.Hash[..2]));

            string treePath = Path.Combine(ObjectsPath, tree.Hash[..2], tree.Hash[2..]);
            
            if (File.Exists(treePath))
            {
                return treePath;
            }

            byte[] content = ToBinary(tree);
            File.WriteAllBytes(treePath, content);
            return treePath;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Permission denied saving tree: {ex.Message}");
        }
    }

    private byte[] ToBinary(Tree tree)
    {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write(VersionService.MAGIC_NUMBER);
            writer.Write((byte)tree.FileType);
            writer.Write(tree.Hash);
            writer.Write(tree.Blobs.Count);

            foreach (var blob in tree.Blobs)
            {
                writer.Write(blob.FilePath);
                writer.Write(blob.Hash);
            }

            return ms.ToArray();
        }
    }

    public Tree LoadTree(string treeHash)
    {
        string treePath = Path.Combine(ObjectsPath, treeHash[..2], treeHash[2..]);
        if (!File.Exists(treePath))
            throw new FileNotFoundException($"Tree {treeHash} not found.");
        byte[] content = File.ReadAllBytes(treePath);
        return FromBinary(content);
    }

    private Tree FromBinary(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            uint magic = reader.ReadUInt32();
            if (magic != VersionService.MAGIC_NUMBER)
                throw new InvalidDataException("Invalid tree object.");

            FileTypeEnum fileType = (FileTypeEnum)reader.ReadByte();
            if (fileType != FileTypeEnum.Tree)
                throw new InvalidDataException("Data is not a tree object.");

            string hash = reader.ReadString();
            int blobCount = reader.ReadInt32();
            List<Blob> blobs = new();
            for (int i = 0; i < blobCount; i++)
            {
                string filePath = reader.ReadString();
                string blobHash = reader.ReadString();
                blobs.Add(new Blob
                {
                    FilePath = filePath,
                    Hash = blobHash,
                    Status = FileStatusEnum.commited
                });
            }

            return new Tree
            {
                Hash = hash,
                Blobs = blobs,
                FileType = (FileStatusEnum)FileTypeEnum.Tree
            };
        }
    }
}