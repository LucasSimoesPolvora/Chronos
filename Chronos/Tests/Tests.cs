using System.Text;
using Xunit;

namespace Chronos.Tests
{
    public class TestEnvironmentHelper : IDisposable
    {
        private string _testDirectoryPath = string.Empty;
        private string _originalDirectory = string.Empty;

        public string SetupTestEnvironment()
        {
            _originalDirectory = Directory.GetCurrentDirectory();
            _testDirectoryPath = Path.Combine(Path.GetTempPath(), $"chronos_test_{Guid.NewGuid()}_{DateTime.Now.Ticks}");
            Directory.CreateDirectory(_testDirectoryPath);
            Directory.SetCurrentDirectory(_testDirectoryPath);
            Thread.Sleep(50);
            return _testDirectoryPath;
        }

        public string CreateTestFile(string filename, string content = "test content")
        {
            string filepath = Path.Combine(_testDirectoryPath, filename);
            string? directory = Path.GetDirectoryName(filepath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(filepath, content);
            return filepath;
        }

        public void Dispose()
        {
            try
            {
                Thread.Sleep(50);
                if (!string.IsNullOrEmpty(_originalDirectory) && Directory.Exists(_originalDirectory))
                {
                    Directory.SetCurrentDirectory(_originalDirectory);
                }
                if (Directory.Exists(_testDirectoryPath))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            Directory.Delete(_testDirectoryPath, true);
                            break;
                        }
                        catch
                        {
                            if (i < 2) Thread.Sleep(100);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up test environment: {ex.Message}");
            }
        }
    }
    
    public class IntegrationTests_ProjectInitialization
    {
        [Fact]
        public void ProjectInitialization_CreatesChronosStructure()
        {
            TestEnvironmentHelper env = new();

            // ARRANGE
            env.SetupTestEnvironment();
            env.CreateTestFile("file1.txt", "initial content");

            // ACT
            ProjectService.InitProject();

            // ASSERT
            string chronosPath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos");
            Assert.True(Directory.Exists(chronosPath), ".chronos directory should exist");
            Assert.True(Directory.Exists(Path.Combine(chronosPath, "objects")), "objects directory should exist");
            Assert.True(File.Exists(Path.Combine(chronosPath, "status")), "status file should exist");
        }

        [Fact]
        public void ProjectInitialization_IsProjectInitialized()
        {
            TestEnvironmentHelper env = new();
            // ARRANGE
            env.SetupTestEnvironment();

            // ACT
            ProjectService.InitProject();
            ProjectService projectService = new();

            // ASSERT
            Assert.True(projectService.isProjectInitialized());
        }
    }

    public class ErrorHandlingTests
    {
        [Fact]
        public void ErrorHandling_InvalidCommitHash()
        {
            TestEnvironmentHelper env = new();
            // ARRANGE
            env.SetupTestEnvironment();
            ProjectService.InitProject();
            CommitService commitService = new();

            // ACT & ASSERT
            string invalidHash = "0000000000000000000000000000000000000000000000000000000000000000";
            Exception ex = Assert.Throws<FileNotFoundException>(() =>
            {
                commitService.LoadCommit(invalidHash);
            });
            Assert.Contains("not found", ex.Message);
        }

        [Fact]
        public void ErrorHandling_NoFilesStaged()
        {
            TestEnvironmentHelper env = new();
            // ARRANGE
            env.SetupTestEnvironment();
            env.CreateTestFile("file1.txt", "content");
            ProjectService.InitProject();

            // ACT
            FileService fs = new();
            VersionService vs = new();
            bool canCommit = vs.CheckIfFilesStaged(fs);

            // ASSERT
            Assert.False(canCommit, "Should not be able to commit with no staged files");
        }
    }

    public class CommitService_Tests
    {
        [Fact]
        public void IsFileModified_WithChangedFile()
        {
            TestEnvironmentHelper env = new();
            // ARRANGE
            env.SetupTestEnvironment();
            string filepath = env.CreateTestFile("file1.txt", "original content");
            ProjectService.InitProject();
            CommitService commitService = new();

            // Compute original hash
            var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(File.ReadAllBytes(filepath));
            string originalHash = Convert.ToHexString(hashBytes).ToLower();

            // Modify the file
            File.WriteAllText(filepath, "modified content");

            // ACT
            bool isModified = commitService.IsFileModified(filepath, originalHash);

            // ASSERT
            Assert.True(isModified, "File should be detected as modified");
        }

        [Fact]
        public void IsFileModified_WithUnchangedFile()
        {
            TestEnvironmentHelper env = new();
            // ARRANGE
            env.SetupTestEnvironment();
            string filepath = env.CreateTestFile("file1.txt", "content");
            ProjectService.InitProject();
            CommitService commitService = new();

            // Compute hash
            var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(File.ReadAllBytes(filepath));
            string hash = Convert.ToHexString(hashBytes).ToLower();

            // ACT
            bool isModified = commitService.IsFileModified(filepath, hash);

            // ASSERT
            Assert.False(isModified, "File should not be detected as modified");
        }
    }

    /// <summary>
    /// TESTS UNITAIRES - IndexService
    /// 
    /// Résultats attendus:
    /// - AddOrUpdateEntry met à jour les entrées
    /// - MarkEntryDeleted définit le statut à deleted
    /// </summary>
    public class IndexService_Tests
    {
        [Fact]
        public void IndexService_AddOrUpdateEntry_UpdatesExistingEntry()
        {
            TestEnvironmentHelper env = new();
            // ARRANGE
            env.SetupTestEnvironment();
            ProjectService.InitProject();

            IndexService indexService = new();
            indexService.AddOrUpdateEntry("file1.txt", "hash1");

            // ACT
            indexService.AddOrUpdateEntry("file1.txt", "hash2");
            indexService.SaveIndex();

            IndexService loadedService = new();
            loadedService.LoadIndex();

            // ASSERT
            IndexEntry? entry = loadedService.GetEntries().FirstOrDefault(e => e.RelativePath == "file1.txt");
            Assert.NotNull(entry);
            Assert.Equal("hash2", entry.BlobHash);
        }

        [Fact]
        public void IndexService_MarkEntryDeleted()
        {
            TestEnvironmentHelper env = new();
            // ARRANGE
            env.SetupTestEnvironment();
            ProjectService.InitProject();

            IndexService indexService = new();
            indexService.AddOrUpdateEntry("file1.txt", "hash1");

            // ACT
            indexService.MarkEntryDeleted("file1.txt");
            indexService.SaveIndex();

            IndexService loadedService = new();
            loadedService.LoadIndex();

            // ASSERT
            IndexEntry? entry = loadedService.GetEntries().FirstOrDefault(e => e.RelativePath == "file1.txt");
            Assert.NotNull(entry);
            Assert.Equal(FileStatusEnum.deleted, entry.Status);
        }
    }

    public class VersionService_Tests
    {
        [Fact]
        public void VersionService_CheckIfFilesStaged()
        {
            TestEnvironmentHelper env = new();
            // ARRANGE
            env.SetupTestEnvironment();
            ProjectService.InitProject();

            // ACT
            FileService fs = new();
            VersionService vs = new();
            bool hasStaged = vs.CheckIfFilesStaged(fs);

            // ASSERT
            Assert.False(hasStaged, "Should return false when no files are staged");
        }
    }
}