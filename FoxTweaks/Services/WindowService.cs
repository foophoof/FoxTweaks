using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DalaMock.Host.Factories;
using DalaMock.Host.Mediator;
using DalaMock.Shared.Interfaces;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using FoxTweaks.Mediator;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoxTweaks.Services {
  public class WindowService : DisposableMediatorSubscriberBase, IHostedService {
    private readonly WindowSystemFactory windowSystemFactory;
    private readonly IUiBuilder uiBuilder;
    private readonly IWindowSystem windowSystem;

    public WindowService(IEnumerable<Window> windows, ILogger<DisposableMediatorSubscriberBase> logger, MediatorService mediatorService, WindowSystemFactory windowSystemFactory,
                         IUiBuilder uiBuilder) : base(logger, mediatorService) {
      this.windowSystemFactory = windowSystemFactory;
      this.uiBuilder = uiBuilder;
      windowSystem = this.windowSystemFactory.Create("FoxTweaks");
      foreach (var window in windows) {
        windowSystem.AddWindow(window);
      }
    }

    public Task StartAsync(CancellationToken cancellationToken) {
      this.MediatorService.Subscribe<OpenWindowMessage>(this, OpenWindow);
      this.MediatorService.Subscribe<ToggleWindowMessage>(this, ToggleWindow);
      this.MediatorService.Subscribe<CloseWindowMessage>(this, CloseWindow);
      foreach (var window in windowSystem.Windows) {
        window.IsOpen = true;
      }
      uiBuilder.Draw += Draw;

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
      uiBuilder.Draw -= Draw;

      return Task.CompletedTask;
    }

    private void OpenWindow(OpenWindowMessage obj) {
      var window = windowSystem.Windows.FirstOrDefault(c => c.GetType() == obj.WindowType);
      window?.IsOpen = true;
    }

    private void CloseWindow(CloseWindowMessage obj) {
      var window = windowSystem.Windows.FirstOrDefault(c => c.GetType() == obj.WindowType);
      window?.IsOpen = false;
    }

    private void ToggleWindow(ToggleWindowMessage obj) {
      var window = windowSystem.Windows.FirstOrDefault(c => c.GetType() == obj.WindowType);
      window?.IsOpen = !window.IsOpen;
    }

    private void Draw() {
      windowSystem.Draw();
    }
  }
}