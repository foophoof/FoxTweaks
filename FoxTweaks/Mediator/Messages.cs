using System;
using DalaMock.Host.Mediator;
using Lumina.Excel.Sheets;

namespace FoxTweaks.Mediator;

/// <summary>
/// Request that a window is toggled.
/// </summary>
/// <param name="WindowType">The type of the window.</param>
public record ToggleWindowMessage(Type WindowType) : MessageBase;

/// <summary>
/// Request that a window is opened.
/// </summary>
/// <param name="WindowType">The type of the window.</param>
public record OpenWindowMessage(Type WindowType) : MessageBase;

/// <summary>
/// Request that a window is closed.
/// </summary>
/// <param name="WindowType">The type of the window.</param>
public record CloseWindowMessage(Type WindowType) : MessageBase;

/// <summary>
/// Request that a gearset for the matching job is equipped.
/// </summary>
/// <param name="ClassJob">The ClassJob to equip a gearset for.</param>
public record EquipGearsetForJobMessage(ClassJob ClassJob) : MessageBase;
