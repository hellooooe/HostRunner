using NgrokSharp;
using NgrokSharp.DTO;
using System.Text.Json;

namespace HostRunner;

internal static class NgrokHelper {

    public static async Task Run() {
        NgrokManager ngrokManager = new NgrokManager();

        await ngrokManager.DownloadAndUnzipNgrokAsync();
        await ngrokManager.RegisterAuthTokenAsync(Environment.GetCommandLineArgs()[6]);

        ngrokManager.StartNgrok(NgrokManager.Region.Europe);

        StartTunnelDTO tunnel = new StartTunnelDTO {
            name = typeof(NgrokHelper).Assembly.GetName().Name,
            proto = "tcp",
            addr = "25565"
        };

        using HttpResponseMessage response = await ngrokManager.StartTunnelAsync(tunnel);
        response.EnsureSuccessStatusCode();

        TunnelDetailDTO tunnelDetail =
            JsonSerializer.Deserialize<TunnelDetailDTO>(await response.Content.ReadAsStringAsync())!;
        Console.WriteLine($"Ngrok url: {tunnelDetail.PublicUrl}");
    }

}
