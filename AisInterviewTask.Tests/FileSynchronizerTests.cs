using AisUriProviderApi;
using Moq;
using RichardSzalay.MockHttp;

namespace AisInterviewTask.Tests
{
    public class FileSynchronizerTests
    {
        private readonly string _destinationPath = Path.Combine(Directory.GetCurrentDirectory(), "TestDirectory");
        private readonly AisUriProvider _provider = new Mock<AisUriProvider>().Object;
        private readonly FileSynchronizer _fileSynchronizer;
        private readonly MockHttpMessageHandler _msgHandler = new();

        public FileSynchronizerTests()
        {
            _fileSynchronizer = new FileSynchronizer(_destinationPath, _provider, _msgHandler.ToHttpClient());
        }

        [Fact]
        public void LoadStoredFiles_DirectoryExists_ReturnsFileNames()
        {
            // Arrange
            Directory.CreateDirectory(_destinationPath);
            var filePath = Path.Combine(_destinationPath, "file1.txt");
            File.Create(filePath).Dispose();

            // Act
            var result = _fileSynchronizer.LoadStoredFiles();

            // Assert
            Assert.Contains("file1.txt", result);

            // Clean up
            Directory.Delete(_destinationPath, true);
        }

        [Fact]
        public void LoadStoredFiles_DirectoryDoesNotExist_CreatesDirectoryAndReturnsEmptyList()
        {
            // Arrange
            if (Directory.Exists(_destinationPath))
                Directory.Delete(_destinationPath, true);

            // Act
            var result = _fileSynchronizer.LoadStoredFiles();

            // Assert
            Assert.Empty(result);
            Assert.True(Directory.Exists(_destinationPath));
        }


        [Fact]
        public async Task DownloadFileAsync_ShouldDownloadFile()
        {
            // Arrange
            var fileUri = new Uri("http://example.com/file.txt");
            var destinationPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            MockHttpMessageHandler msgHandler = new();
            msgHandler.When("http://example.com/file.txt")
            .Respond("text/plain", "content");
            var synchronizer = new FileSynchronizer(destinationPath, _provider, msgHandler.ToHttpClient());

            // Act
            await synchronizer.DownloadFileAsync(fileUri);

            // Assert
            var directoryInfo = new DirectoryInfo(destinationPath);
            var fileNames = directoryInfo.GetFiles().Select(f => f.Name).ToList();
            var actualContent = File.ReadAllText(directoryInfo.GetFiles().Select(x => x.FullName).First());
            Assert.Single(fileNames);
            Assert.Contains("file.txt", fileNames);
            Assert.Equal("content", actualContent);

            // Clean up
            Directory.Delete(destinationPath, true);
        }

        [Fact]
        public async Task RefreshFileListAsync_ShouldDownloadFiles()
        {
            // Arrange
            var fileUris = new List<Uri>
            {
                new Uri("http://example.com/file1.txt"),
                new Uri("http://example.com/file2.txt"),
                new Uri("http://example.com/file3.txt")
            };
            var destinationPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            MockHttpMessageHandler msgHandler = new();
            msgHandler.When("http://example.com/file1.txt")
                .Respond("text/plain", "content");

            msgHandler.When("http://example.com/file2.txt")
                .Respond("text/plain", "content2");

            msgHandler.When("http://example.com/file3.txt")
                .Respond("text/plain", "content3");
            var synchronizer = new FileSynchronizer(destinationPath, _provider, msgHandler.ToHttpClient());

            // Act
            await synchronizer.RefreshFileListAsync(fileUris);

            // Assert
            var directoryInfo = new DirectoryInfo(destinationPath);
            var fileNames = directoryInfo.GetFiles().Select(f => f.Name).ToList();
            Assert.Equal(3, fileNames.Count);
            Assert.Contains("file1.txt", fileNames);
            Assert.Contains("file2.txt", fileNames);
            Assert.Contains("file3.txt", fileNames);

            // Clean up
            Directory.Delete(destinationPath, true);
        }

        [Fact]
        public void CleanupOldFiles_Success()
        {
            // Arrange
            var newFileNames = new List<string> { "file1.txt", "file3.txt" };
            Directory.CreateDirectory(_destinationPath);
            File.Create(Path.Combine(_destinationPath, "file2.txt")).Dispose();
            File.Create(Path.Combine(_destinationPath, newFileNames[0])).Dispose();
            File.Create(Path.Combine(_destinationPath, newFileNames[1])).Dispose();
            var fileSynchronizer = new FileSynchronizer(_destinationPath, _provider, _msgHandler.ToHttpClient());

            // Act
            fileSynchronizer.CleanupOldFiles(newFileNames);

            // Assert
            var storedFiles = fileSynchronizer.LoadStoredFiles();
            Assert.Equal(2, storedFiles.Count);
            Assert.Contains("file1.txt", storedFiles);
            Assert.Contains("file3.txt", storedFiles);
            Assert.DoesNotContain("file2.txt", storedFiles);
        }
    }
}