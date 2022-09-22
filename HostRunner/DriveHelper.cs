using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Reflection;

namespace HostRunner;

internal static class DriveHelper {

    public static DriveService GetService() {
        string[] args = Environment.GetCommandLineArgs();

        TokenResponse tokenResponse = new TokenResponse {
            AccessToken = args[1],
            RefreshToken = args[2]
        };

        string applicationName = Assembly.GetEntryAssembly()!.GetName().Name!;
        string username = "example.com";

        GoogleAuthorizationCodeFlow apiCodeFlow = new GoogleAuthorizationCodeFlow(
            new GoogleAuthorizationCodeFlow.Initializer() {
                ClientSecrets = new ClientSecrets {
                    ClientId = args[3],
                    ClientSecret = args[4]
                },
                Scopes = new string[] { DriveService.Scope.Drive },
                DataStore = new FileDataStore(applicationName)
            }
        );

        UserCredential credential = new UserCredential(apiCodeFlow, username, tokenResponse);

        return new DriveService(new BaseClientService.Initializer {
            HttpClientInitializer = credential,
            ApplicationName = applicationName
        });
    }

    public static Google.Apis.Drive.v3.Data.File GetLatestPackage(DriveService service) {
        FilesResource.ListRequest fileList = service.Files.List();
        fileList.OrderBy = "createdTime desc";
        fileList.Q = $"mimeType!='application/vnd.google-apps.folder' and " +
            $"'{Environment.GetCommandLineArgs()[5]}' in parents";
        fileList.Fields = "files(id, name, size, mimeType)";

        return fileList.Execute().Files[0];
    }

    public static void DownloadLatestPackage(string destinationFile) {
        using DriveService service = GetService();

        FilesResource.GetRequest request = service.Files.Get(GetLatestPackage(service).Id);
        using FileStream stream = new FileStream(destinationFile, FileMode.Create);

        request.MediaDownloader.ProgressChanged += progress => {
            switch (progress.Status) {
                case DownloadStatus.Downloading:
                    Console.WriteLine(progress.BytesDownloaded);
                    break;
                case DownloadStatus.Completed:
                    Console.WriteLine("Download complete.");
                    break;
                case DownloadStatus.Failed:
                    Console.WriteLine("Download failed.");
                    break;
            }
        };

        request.Download(stream);
    }

}
