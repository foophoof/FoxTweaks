using System.Threading;
using System.Threading.Tasks;
using DalaMock.Host.Mediator;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using FoxTweaks.Mediator;
using FoxTweaks.Windows;
using Microsoft.Extensions.Hosting;

namespace FoxTweaks.Services {
  public class CommandService(ICommandManager commandManager, MediatorService mediatorService) : IHostedService {
    private readonly MediatorService mediatorService = mediatorService;
    private readonly string[] commandName = { "/sampleplugin", "/samplepluginalias" };

    public ICommandManager CommandManager { get; } = commandManager;

    public Task StartAsync(CancellationToken cancellationToken) {
      foreach (string name in commandName) {
        CommandManager.AddHandler(
            name,
            new CommandInfo(OnCommand) {
              HelpMessage = "Shows the sample plugin main window."
            });
      }

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
      foreach (string name in commandName) {
        CommandManager.RemoveHandler(name);
      }

      return Task.CompletedTask;
    }

    private void OnCommand(string command, string arguments) {
      mediatorService.Publish(new ToggleWindowMessage(typeof(MainWindow)));
    }
  }
}