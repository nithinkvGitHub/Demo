using Microsoft.Extensions.Options;
using PicoCompanion.Core.Models;
using PicoCompanion.Core.Services;
using PicoCompanion.Service;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddWindowsService(options => options.ServiceName = "Pico Companion Service");

builder.Services
    .Configure<CompanionOptions>(builder.Configuration.GetSection("Companion"))
    .AddSingleton(sp => sp.GetRequiredService<IOptions<CompanionOptions>>().Value)
    .AddSingleton<SerialDongleTransportFactory>()
    .AddSingleton<PicoDongleClient>()
    .AddSingleton<Uf2FirmwareUpdater>()
    .AddSingleton<IForegroundApplicationDetector, WindowsApplicationDetector>()
    .AddSingleton<GameModeRuleMatcher>()
    .AddSingleton<DongleRuntimeState>()
    .AddSingleton(sp =>
    {
        var options = sp.GetRequiredService<CompanionOptions>();
        return new JsonFileStore<DongleConfiguration>(
            Path.Combine(options.DataDirectory, "dongle-config.json"));
    })
    .AddHostedService<DongleMonitorService>()
    .AddHostedService<ProcessModeMonitorService>()
    .AddHostedService<NamedPipeCommandService>();

await builder.Build().RunAsync().ConfigureAwait(false);
