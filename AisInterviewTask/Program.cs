using AisInterviewTask;

const string DESTINATION_PATH = "files";
const int DELAY = 5 * 60 * 1000;

var synchronizer = new FileSynchronizer(DESTINATION_PATH, new AisUriProviderApi.AisUriProvider(), new HttpClient());

var oldFiles = synchronizer.LoadStoredFiles();
if (oldFiles != null)
    Console.WriteLine($"Loaded stored files from the previous start-up:\n    {string.Join("    ", oldFiles)}");

while (true)
{
    var (newFileUris, newFileNames) = synchronizer.GetNewFilesData();

    await synchronizer.RefreshFileListAsync(newFileUris);
    synchronizer.CleanupOldFiles(newFileNames);

    var storedFiles = synchronizer.LoadStoredFiles();
    Console.WriteLine($"Stored files:\n    {string.Join("    ", storedFiles)}");

    await Task.Delay(DELAY);
}
