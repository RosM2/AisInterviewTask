using AisUriProviderApi;

namespace AisInterviewTask
{
    public class FileSynchronizer : IDisposable
    {
        private readonly string _destinationPath;
        private readonly AisUriProvider _provider;
        private readonly HttpClient _client;
        private bool _disposed;

        public FileSynchronizer(string destinationPath, AisUriProvider provider, HttpClient client)
        {
            _destinationPath = destinationPath;
            _provider = provider;
            _client = client;
        }

        public List<string> LoadStoredFiles()
        {
            var directoryInfo = new DirectoryInfo(_destinationPath);
            if (!directoryInfo.Exists)
            {
                Console.WriteLine("Directory for storing data doesn't exist!\nWait some tome to create a new one automatically.");
                Directory.CreateDirectory(directoryInfo.FullName);
                Console.WriteLine($"Directory {_destinationPath} was succeseful created.");
            }
            return directoryInfo.GetFiles().Select(fileInfo => fileInfo.Name).ToList();
        }

        public async Task RefreshFileListAsync(List<Uri> newFileUris)
        {
            await Parallel.ForEachAsync(newFileUris, new ParallelOptions() { MaxDegreeOfParallelism = 3 }, async (uri, _) =>
            {
                await DownloadFileAsync(uri);
            });
        }

        public async Task DownloadFileAsync(Uri uri)
        {
            Console.WriteLine($"Downloading file {uri} ...");
            string fileName = Path.GetFileName(uri.LocalPath);
            using var s = await _client.GetStreamAsync(uri);

            var directoryInfo = new DirectoryInfo(_destinationPath);

            if (!directoryInfo.Exists)
            {
                Directory.CreateDirectory(directoryInfo.FullName);
            }
            using var fs = new FileStream($@"{_destinationPath}\{fileName}", FileMode.Create);
            await s.CopyToAsync(fs);
        }

        public void CleanupOldFiles(List<string> newFileNames)
        {
            var directoryInfo = new DirectoryInfo(_destinationPath);

            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                if (newFileNames.Contains(file.Name))
                    continue;
                file.Delete();
                Console.WriteLine($"Removed unnecessary/old file: {file.Name}");
            }
        }

        public (List<Uri>, List<string>) GetNewFilesData()
        {
            var newFileUris = _provider.Get().ToList();
            var newFileNames = newFileUris.Select(uri => Path.GetFileName(uri.LocalPath)).ToList();
            return (newFileUris, newFileNames);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _client.Dispose();
            _disposed = true;

            GC.SuppressFinalize(this);
        }
    }
}
