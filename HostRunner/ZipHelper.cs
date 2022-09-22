using System.IO.Compression;

namespace HostRunner;

internal static class ZipHelper {

    public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName) {
        FileStream stream = new FileStream(destinationArchiveFileName, FileMode.Create);
        ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Create);

        DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirectoryName);
        int length = directoryInfo.FullName.Length + 1;

        foreach (string file in Directory.GetFiles(sourceDirectoryName, "", SearchOption.AllDirectories)) {
            try {
                zip.CreateEntryFromFile(file, new FileInfo(file).FullName.Substring(length).Replace('\\', '/'));
            } catch {
            }
        }

        zip.Dispose();
        stream.Dispose();
    }

}
