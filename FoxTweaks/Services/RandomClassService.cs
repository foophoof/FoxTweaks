using DalaMock.Host.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using FoxTweaks.Mediator;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Hosting;

namespace FoxTweaks.Services;

public class RandomClassService(ICommandManager commandManager, IPluginLog pluginLog, IChatGui chatGui, ExcelSheet<ClassJob> classJobs, IPlayerState playerState, MediatorService mediatorService) : IHostedService
{
    private enum JobType : byte
    {
        Tank = 1,
        PureHealer = 2,
        Melee = 3,
        PhysicalRanged = 4,
        MagicalRanged = 5,
        BarrierHealer = 6,
    }

    private readonly Random _random = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        commandManager.AddHandler(
            "/rndc",
            new CommandInfo(OnCommand)
            {
                HelpMessage = "Selects a random class.",
            });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        commandManager.RemoveHandler("/rndc");

        return Task.CompletedTask;
    }

    private void OnCommand(string command, string arguments)
    {
        var roles = GetRolesToRoll(arguments).ToList();
        if (roles.Count == 0)
        {
            chatGui.PrintError("/rndc: No roles picked", "FoxTweaks", 45);
            return;
        }

        var classJob = roles[_random.Next(roles.Count)];
        pluginLog.Debug($"rndc: chose role {classJob.Name}");

        mediatorService.Publish(new EquipGearsetForJobMessage(classJob));
    }

    private IEnumerable<ClassJob> GetRolesToRoll(string rolesToInclude)
    {
        var enumerable = Enumerable.Empty<ClassJob>();

        rolesToInclude = rolesToInclude.Trim();

        if (rolesToInclude.Contains('t') || rolesToInclude.Length == 0)
        {
            enumerable = enumerable.Concat(GetTanks());
        }
        if (rolesToInclude.Contains('h') || rolesToInclude.Length == 0)
        {
            enumerable = enumerable.Concat(GetHealers());
        }
        if (rolesToInclude.Contains('d') || rolesToInclude.Length == 0)
        {
            enumerable = enumerable.Concat(GetDps());
        }

        return enumerable;
    }

    private IEnumerable<ClassJob> GetTanks()
    {
        return GetValidJobs()
            .Where(c => (JobType)c.JobType == JobType.Tank);
    }

    private IEnumerable<ClassJob> GetHealers()
    {
        return GetValidJobs()
            .Where(c => (JobType)c.JobType is JobType.PureHealer or JobType.BarrierHealer);
    }

    private IEnumerable<ClassJob> GetDps()
    {
        return GetValidJobs()
            .Where(c => (JobType)c.JobType is JobType.Melee or JobType.PhysicalRanged or JobType.MagicalRanged);
    }

    private IEnumerable<ClassJob> GetValidJobs()
    {
        return classJobs
            .Where(c => !c.IsLimitedJob)
            .Where(c => playerState.ClassJob.RowId != c.RowId);
    }
}
