using Autofac;
using DalaMock.Host.Hosting;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FoxTweaks.Services;
using Lumina;
using Lumina.Excel;
using Microsoft.Extensions.DependencyInjection;

namespace FoxTweaks;

public sealed class Plugin : HostedPlugin
{
    public Plugin(IDalamudPluginInterface pluginInterface) : base(pluginInterface)
    {
        CreateHost();
        Start();
    }

    /// <summary>
    ///     Configures the optional services to register automatically for use in your plugin.
    /// </summary>
    /// <returns>A HostedPluginOptions configured with the options you required.</returns>
    public override HostedPluginOptions ConfigureOptions()
    {
        return new HostedPluginOptions
        {
            UseMediatorService = true,
        };
    }

    public override void ConfigureContainer(ContainerBuilder containerBuilder)
    {
        // While you can register services in the service collection, as long as you register a service as IHostedService(the AsImplementedInterfaces call) it will automatically be picked up by the host. This also avoids potential double registrations.
        containerBuilder.RegisterType<GearsetService>().AsSelf().AsImplementedInterfaces().SingleInstance();
        containerBuilder.RegisterType<JobSwitcherService>().AsSelf().AsImplementedInterfaces().SingleInstance();
        containerBuilder.RegisterType<MiniMapService>().AsSelf().AsImplementedInterfaces().SingleInstance();
        containerBuilder.RegisterType<RandomClassService>().AsSelf().AsImplementedInterfaces().SingleInstance();

        containerBuilder.Register(c => c.Resolve<IDataManager>().GameData).SingleInstance();

        // Sheets
        containerBuilder.RegisterGeneric(
            (context, parameters) =>
            {
                var gameData = context.Resolve<GameData>();
                var method = typeof(GameData).GetMethod(nameof(GameData.GetExcelSheet))?.MakeGenericMethod(parameters);
                return method!.Invoke(gameData, [null, null])!;
            }).As(typeof(ExcelSheet<>));
    }

    public override void ConfigureServices(IServiceCollection serviceCollection)
    {
    }
}
