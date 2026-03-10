using DalaMock.Host.Mediator;
using Lumina.Excel.Sheets;
using System;

namespace FoxTweaks.Mediator;

/// <summary>
///     Request that a gearset for the matching job is equipped.
/// </summary>
/// <param name="ClassJob">The ClassJob to equip a gearset for.</param>
public record EquipGearsetForJobMessage(ClassJob ClassJob) : MessageBase;
