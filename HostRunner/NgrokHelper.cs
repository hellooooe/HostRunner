using NgrokSharp;
using NgrokSharp.DTO;
using System.Text.Json;

namespace HostRunner;

internal static class NgrokHelper {

    public static async Task Run() {
        using INgrokManager ngrokManager = new NgrokManager();

        await ngrokManager.DownloadAndUnzipNgrokAsync();
        await ngrokManager.RegisterAuthTokenAsync(Environment.GetCommandLineArgs()[6]);

        ngrokManager.StartNgrok();

        StartTunnelDTO tunnel = new StartTunnelDTO {
            proto = "tcp",
            addr = "25565"
        };

        using HttpResponseMessage response = await ngrokManager.StartTunnelAsync(tunnel);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException();

        TunnelDetailDTO tunnelDetail =
            JsonSerializer.Deserialize<TunnelDetailDTO>(await response.Content.ReadAsStringAsync())!;
        Console.WriteLine($"Ngrok url: {tunnelDetail.PublicUrl}");
    }

}
