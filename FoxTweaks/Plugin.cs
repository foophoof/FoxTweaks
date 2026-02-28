using System.Reflection;
using Autofac;
using DalaMock.Host.Hosting;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FoxTweaks.Services;
using Lumina;
using Lumina.Excel;
using Microsoft.Extensions.DependencyInjection;

namespace FoxTweaks {
  public sealed class Plugin : HostedPlugin {
    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IPluginLog pluginLog,
        IFramework framework,
        ICommandManager commandManager,
        IDataManager dataManager,
        ITextureProvider textureProvider,
        IChatGui chatGui,
        IDtrBar dtrBar,
        IGameGui gameGui,
        IClientState clientState,
        IObjectTable objectTable)
        : base(pluginInterface, pluginLog, framework, commandManager, dataManager, textureProvider, chatGui, dtrBar, gameGui, clientState, objectTable) {
      CreateHost();
      Start();
    }

    /// <summary>
    /// Configures the optional services to register automatically for use in your plugin.
    /// </summary>
    /// <returns>A HostedPluginOptions configured with the options you required.</returns>
    public override HostedPluginOptions ConfigureOptions() => new() {
      UseMediatorService = true
    };

    public override void ConfigureContainer(ContainerBuilder containerBuilder) {
      // While you can register services in the service collection, as long as you register a service as IHostedService(the AsImplementedInterfaces call) it will automatically be picked up by the host. This also avoids potential double registrations.
      containerBuilder.RegisterType<CommandService>().AsSelf().AsImplementedInterfaces().SingleInstance();
      containerBuilder.RegisterType<ConfigurationService>().AsSelf().AsImplementedInterfaces().SingleInstance();
      containerBuilder.RegisterType<InstallerWindowService>().AsSelf().AsImplementedInterfaces().SingleInstance();
      containerBuilder.RegisterType<MiniMapService>().AsSelf().AsImplementedInterfaces().SingleInstance();
      containerBuilder.RegisterType<RandomClassService>().AsSelf().AsImplementedInterfaces().SingleInstance();
      containerBuilder.RegisterType<WindowService>().AsSelf().AsImplementedInterfaces().SingleInstance();

      // Register every class ending in Window inside our assembly with the container
      containerBuilder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
          .Where(t => t.Name.EndsWith("Window"))
          .As<Window>()
          .AsSelf()
          .AsImplementedInterfaces();

      containerBuilder.Register(c => c.Resolve<IDataManager>().GameData).SingleInstance();

      // Sheets
      containerBuilder.RegisterGeneric((context, parameters) => {
        var gameData = context.Resolve<GameData>();
        var method = typeof(GameData).GetMethod(nameof(GameData.GetExcelSheet))?.MakeGenericMethod(parameters);
        return method!.Invoke(gameData, [null, null])!;
      }).As(typeof(ExcelSheet<>));

      // Register the configuration with the container so that it's loaded/created when requested.
      containerBuilder.Register(s => {
        var configurationLoaderService = s.Resolve<ConfigurationService>();
        return configurationLoaderService.GetConfiguration();
      }).SingleInstance();
    }

    public override void ConfigureServices(IServiceCollection serviceCollection) {
    }
  }
}