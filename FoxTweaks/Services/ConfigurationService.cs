using System;
using System.Threading;
using System.Threading.Tasks;
using DalaMock.Host.Hosting;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace FoxTweaks.Services {
  public class ConfigurationService : HostingAwareService {
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IPluginLog pluginLog;
    private readonly IFramework framework;
    private Configuration? configuration;

    public ConfigurationService(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog, IFramework framework, HostedEvents hostedEvents) : base(hostedEvents) {
      this.pluginInterface = pluginInterface;
      this.pluginLog = pluginLog;
      this.framework = framework;
    }

    public Configuration GetConfiguration() {
      if (configuration == null) {
        try {
          configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        }
        catch (Exception e) {
          pluginLog.Error(e, "Failed to log configuration");
          configuration = new Configuration();
        }
      }

      return configuration;
    }

    public void Save() {
      GetConfiguration().IsDirty = false;
      pluginInterface.SavePluginConfig(GetConfiguration());
    }

    public override void OnPluginEvent(HostedEventType eventType) {
      switch (eventType) {
        case HostedEventType.PluginStopping:
          framework.Update -= HandleAutosave;
          Save();
          break;

        case HostedEventType.PluginStarted:
          framework.Update += HandleAutosave;
          break;
      }
    }

    private void HandleAutosave(IFramework framework1) {
      if (configuration?.IsDirty ?? false) {
        pluginLog.Verbose("Configuration is dirty, saving.");
        Save();
      }
    }

    public override Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public override Task StopAsync(CancellationToken cancellationToken) {
      framework.Update -= HandleAutosave;
      return Task.CompletedTask;
    }
  }
}