using DalaMock.Host.Mediator;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using FoxTweaks.Mediator;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FoxTweaks.Services;

public class JobSwitcherService(ICommandManager commandManager, ExcelSheet<ClassJob> classJobs, MediatorService mediatorService) : IHostedService
{
    private readonly HashSet<string> _registeredCommands = [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var classJob in classJobs.Where(cj => cj.RowId != 0 && !cj.Abbreviation.IsEmpty && !cj.Name.IsEmpty))
        {
            var acronym = classJob.Abbreviation.ToString();
            var name = classJob.Name.ToString();
            var command = "/" + acronym;

            commandManager.AddHandler(command, new CommandInfo((_, _) =>
            {
                mediatorService.Publish(new EquipGearsetForJobMessage(classJob));
            })
            {
                HelpMessage = $"Switches to {name} Class/Job", ShowInHelp = false,
            });

            _registeredCommands.Add(command);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var registeredCommand in _registeredCommands)
        {
            commandManager.RemoveHandler(registeredCommand);
        }
        _registeredCommands.Clear();

        return Task.CompletedTask;
    }
}
