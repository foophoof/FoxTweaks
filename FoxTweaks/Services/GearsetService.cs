using DalaMock.Host.Mediator;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FoxTweaks.Mediator;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using ZLinq;

namespace FoxTweaks.Services;

public class GearsetService(IPluginLog pluginLog, ILogger<GearsetService> logger, MediatorService mediatorService) : DisposableMediatorSubscriberBase(logger, mediatorService), IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        MediatorService.Subscribe<EquipGearsetForJobMessage>(this, EquipGearsetForJob);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        MediatorService.UnsubscribeAll(this);

        return Task.CompletedTask;
    }

    private unsafe void EquipGearsetForJob(EquipGearsetForJobMessage obj)
    {
        var gearsetEntry = HighestGearsetForClassJob(obj.ClassJob);
        if (!gearsetEntry.HasValue)
        {
            pluginLog.Debug($"Couldn't find gearset for job {obj.ClassJob.Name}");
            return;
        }

        pluginLog.Debug($"equipping gearset {gearsetEntry.Value.Id}");

        var gearsetModule = GetRaptureGearsetModule();
        if (gearsetModule is null)
        {
            return;
        }

        if (!gearsetModule->IsValidGearset(gearsetEntry.Value.Id))
        {
            pluginLog.Error($"gearset ID {gearsetEntry.Value.Id} is not valid");
            return;
        }

        if (gearsetModule->EquipGearset(gearsetEntry.Value.Id) != 0)
        {
            pluginLog.Error($"equipping gearset {gearsetEntry.Value.Id} failed");
        }
    }

    private RaptureGearsetModule.GearsetEntry? HighestGearsetForClassJob(ClassJob classJob)
    {
        unsafe
        {
            var gearsetModule = GetRaptureGearsetModule();
            if (gearsetModule is null)
            {
                return null;
            }

            return gearsetModule->Entries
                .AsValueEnumerable()
                .Where(e => (e.Flags & RaptureGearsetModule.GearsetFlag.Exists) != 0)
                .Where(e => e.ClassJob == classJob.RowId)
                .MaxBy(e => e.ItemLevel);
        }
    }

    private unsafe RaptureGearsetModule* GetRaptureGearsetModule()
    {
        var uiModule = UIModule.Instance();
        if (uiModule is null)
        {
            pluginLog.Error("UIModule.Instance() is null");
            return null;
        }

        var gearsetModule = uiModule->GetRaptureGearsetModule();
        if (gearsetModule is null)
        {
            pluginLog.Error("UIModule->GetRaptureGearsetModule is null");
            return null;
        }

        return gearsetModule;
    }
}
