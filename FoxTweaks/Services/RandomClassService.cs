using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Hosting;
using ZLinq;

namespace FoxTweaks.Services {
  public class RandomClassService(ICommandManager commandManager, IPluginLog pluginLog, IChatGui chatGui, ExcelSheet<ClassJob> classJobs) : IHostedService {
    private enum JobType : byte {
      Tank = 1,
      PureHealer = 2,
      Melee = 3,
      PhysicalRanged = 4,
      MagicalRanged = 5,
      BarrierHealer = 6
    }

    private readonly Random _random = new();

    public Task StartAsync(CancellationToken cancellationToken) {
      commandManager.AddHandler(
          "/rndc",
          new CommandInfo(OnCommand) {
            HelpMessage = "Selects a random class."
          });

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
      commandManager.RemoveHandler("/rndc");

      return Task.CompletedTask;
    }

    private void OnCommand(string command, string arguments) {
      var roles = GetRolesToRoll(arguments).ToList();
      if (roles.Count == 0) {
        chatGui.PrintError("[FoxTweaks] /rndc: No roles picked");
        return;
      }

      var classJob = roles[_random.Next(roles.Count)];
      pluginLog.Debug($"rndc: chose role {classJob.Name}");

      var gearsetEntry = HighestGearsetForClassJob(classJob);
      if (gearsetEntry is null) {
        pluginLog.Debug($"rndc: couldn't find gearset for job {classJob.Name}");
        return;
      }

      chatGui.Print($"[FoxTweaks] Picked {classJob.Name}, gearset {gearsetEntry.Value.NameString}");
      EquipGearSet(gearsetEntry.Value);
    }

    private IEnumerable<ClassJob> GetRolesToRoll(string rolesToInclude) {
      var enumerable = Enumerable.Empty<ClassJob>();

      rolesToInclude = rolesToInclude.Trim();

      if (rolesToInclude.Contains('t') || rolesToInclude.Length == 0) {
        enumerable = enumerable.Concat(GetTanks());
      }
      if (rolesToInclude.Contains('h') || rolesToInclude.Length == 0) {
        enumerable = enumerable.Concat(GetHealers());
      }
      if (rolesToInclude.Contains('d') || rolesToInclude.Length == 0) {
        enumerable = enumerable.Concat(GetDps());
      }

      return enumerable;
    }

    private IEnumerable<ClassJob> GetTanks() {
      return classJobs
          .Where(c => (JobType)c.JobType == JobType.Tank)
          .Where(c => !c.IsLimitedJob);
    }

    private IEnumerable<ClassJob> GetHealers() {
      return classJobs
          .Where(c => (JobType)c.JobType is JobType.PureHealer or JobType.BarrierHealer)
          .Where(c => !c.IsLimitedJob);
    }

    private IEnumerable<ClassJob> GetDps() {
      return classJobs
          .Where(c => (JobType)c.JobType is JobType.Melee or JobType.PhysicalRanged or JobType.MagicalRanged)
          .Where(c => !c.IsLimitedJob);
    }

    private RaptureGearsetModule.GearsetEntry? HighestGearsetForClassJob(ClassJob classJob) {
      unsafe {
        var gearsetModule = GetRaptureGearsetModule();
        if (gearsetModule is null) {
          return null;
        }

        return gearsetModule->Entries
            .AsValueEnumerable()
            .Where(e => (e.Flags & RaptureGearsetModule.GearsetFlag.Exists) != 0)
            .Where(e => e.ClassJob == classJob.RowId)
            .MaxBy(e => e.ItemLevel);
      }
    }

    private void EquipGearSet(RaptureGearsetModule.GearsetEntry gearsetEntry) {
      pluginLog.Debug($"equipping gearset {gearsetEntry.Id}");

      unsafe {
        var gearsetModule = GetRaptureGearsetModule();
        if (gearsetModule is null) {
          return;
        }

        if (!gearsetModule->IsValidGearset(gearsetEntry.Id)) {
          pluginLog.Error($"gearset ID {gearsetEntry.Id} is not valid");
          return;
        }

        if (gearsetModule->EquipGearset(gearsetEntry.Id) != 0) {
          pluginLog.Error($"equipping gearset {gearsetEntry.Id} failed");
        }
      }
    }

    private unsafe RaptureGearsetModule* GetRaptureGearsetModule() {
      var uiModule = UIModule.Instance();
      if (uiModule is null) {
        pluginLog.Error("UIModule.Instance() is null");
        return null;
      }

      var gearsetModule = uiModule->GetRaptureGearsetModule();
      if (gearsetModule is null) {
        pluginLog.Error("UIModule->GetRaptureGearsetModule is null");
        return null;
      }

      return gearsetModule;
    }
  }
}