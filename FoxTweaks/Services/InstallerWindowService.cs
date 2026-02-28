using System.Threading;
using System.Threading.Tasks;
using DalaMock.Host.Mediator;
using Dalamud.Plugin;
using FoxTweaks.Mediator;
using FoxTweaks.Windows;
using Microsoft.Extensions.Hosting;

namespace FoxTweaks.Services {
  public class InstallerWindowService(
      IDalamudPluginInterface pluginInterface,
      MediatorService mediatorService) : IHostedService {
    private readonly MediatorService mediatorService = mediatorService;

    public IDalamudPluginInterface PluginInterface { get; } = pluginInterface;

    public Task StartAsync(CancellationToken cancellationToken) {
      PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
      PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
      PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
      PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

      return Task.CompletedTask;
    }

    private void ToggleMainUi() {
      mediatorService.Publish(new ToggleWindowMessage(typeof(MainWindow)));
    }

    private void ToggleConfigUi() {
      mediatorService.Publish(new ToggleWindowMessage(typeof(ConfigWindow)));
    }
  }
}