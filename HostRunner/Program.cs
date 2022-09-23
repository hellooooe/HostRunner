using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using HostRunner;
using MinecraftConnection.RCON;
using System.Diagnostics;
using System.IO.Compression;

const int SaveFrequency = 3_600_000;
const int MaxWorkTime = SaveFrequency * 5;

const string ZipFileName = "File.zip";
const string DirectoryName = "HostRunnerDownloads";

static void Save() {
    const string FileMime = "application/zip";

    // Zip.
    ZipHelper.CreateFromDirectory(DirectoryName, ZipFileName);

    using DriveService service = DriveHelper.GetService();

    string name = $"{DateTime.Now:s}.zip";

    Google.Apis.Drive.v3.Data.File driveFile = new Google.Apis.Drive.v3.Data.File();
    driveFile.Name = name;
    driveFile.Description = name;
    driveFile.MimeType = FileMime;
    driveFile.Parents = new string[] { Environment.GetCommandLineArgs()[5] };

    using FileStream stream = new FileStream(ZipFileName, FileMode.Open);
    FilesResource.CreateMediaUpload request = service.Files.Create(driveFile, stream, FileMime);
    request.Fields = "id";

    request.ProgressChanged += progress => {
        switch (progress.Status) {
            case UploadStatus.Starting:
                Console.WriteLine($"Staring uploading {driveFile.Name}");
                break;
            case UploadStatus.Uploading:
                Console.WriteLine(progress.BytesSent);
                break;
            case UploadStatus.Completed:
                Console.WriteLine("Upload complete.");
                break;
            case UploadStatus.Failed:
                Console.WriteLine("Upload failed.");
                break;
        }
    };

    IUploadProgress response = request.Upload();
    if (response.Status != UploadStatus.Completed)
        throw response.Exception;
}

DriveHelper.DownloadLatestPackage(ZipFileName);

if (Directory.Exists(DirectoryName))
    Directory.Delete(DirectoryName, true);
Directory.CreateDirectory(DirectoryName);

ZipFile.ExtractToDirectory(ZipFileName, DirectoryName);

bool isWorking = true;
Task task = Task.Run(() => {
    Process? process = null;

    AppDomain.CurrentDomain.UnhandledException += (_, _) => process?.Kill();
    AppDomain.CurrentDomain.ProcessExit += (_, _) => process?.Kill();

    while (isWorking) {
        process = Process.Start(new ProcessStartInfo {
            WorkingDirectory = DirectoryName,
            FileName = "java",
            Arguments = "-Xmx14G -jar mc.jar nogui"
        }) ?? throw new NullReferenceException();

        process.WaitForExit();
    }

    process = null;
    Save();
});

Process? tunnelProcess = Process.Start(new ProcessStartInfo {
    FileName = "ngrok",
    Arguments = "tcp 25565 --region eu"
}) ?? throw new NullReferenceException();

Task tunnel = Task.Run(() => {
    AppDomain.CurrentDomain.UnhandledException += (_, _) => tunnelProcess?.Kill();
    AppDomain.CurrentDomain.ProcessExit += (_, _) => tunnelProcess?.Kill();

    while (isWorking) {
        if (tunnelProcess is null) {
            tunnelProcess = Process.Start(new ProcessStartInfo {
                FileName = "ngrok",
                Arguments = "tcp 25565 --region eu"
            }) ?? throw new NullReferenceException();
        }

        tunnelProcess.WaitForExit();
        tunnelProcess = null;
    }
});

MinecraftRCON? rcon = null;
do {
    try {
        Thread.Sleep(1000);
        rcon = new MinecraftRCON("localhost", 25575, "password");
    } catch {
    }
} while (rcon is null);

for (int i = 0; i < MaxWorkTime / SaveFrequency; i++) {
    if (i != 0) {
        rcon.SendCommand("save-all");
        Thread.Sleep(30000);
        Save();
    }

    Thread.Sleep(SaveFrequency);
}

isWorking = false;

for (int i = 0; i < 3; i++)
    rcon.SendCommand("msg @a The server will restart in 30 seconds... The IP address will change!");

Thread.Sleep(30000);
rcon.SendCommand("stop");

tunnelProcess?.Kill();
tunnel.Wait();

task.Wait();
