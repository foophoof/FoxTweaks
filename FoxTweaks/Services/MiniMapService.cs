using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using ZLinq;

namespace FoxTweaks.Services;

public class MiniMapService(
    IGameGui gameGui,
    IFramework framework,
    IPluginLog pluginLog,
    IUiBuilder uiBuilder,
    IObjectTable objectTable,
    IClientState clientState
) : IHostedService
{
    private readonly uint _borderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.957f, 0.533f, 0.051f, 1) * 0.7f);
    private readonly uint _circleColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.957f, 0.533f, 0.051f, 1));
    private readonly Queue<Vector2> _circlePositions = new(objectTable.Length);
    private float _mapSizeFactor = 1f;
    private bool _miniMapVisible;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        framework.Update += FrameworkOnUpdate;
        uiBuilder.Draw += UiBuilderOnDraw;
        clientState.MapIdChanged += ClientStateOnMapIdChanged;

        framework.RunOnFrameworkThread(
            () =>
            {
                ClientStateOnMapIdChanged(clientState.MapId);
            });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        framework.Update -= FrameworkOnUpdate;
        uiBuilder.Draw -= UiBuilderOnDraw;
        clientState.MapIdChanged -= ClientStateOnMapIdChanged;

        return Task.CompletedTask;
    }

    private void UiBuilderOnDraw()
    {
        if (!_miniMapVisible)
        {
            return;
        }

        var drawList = ImGui.GetBackgroundDrawList();
        while (_circlePositions.TryDequeue(out var personCirclePos))
        {
            drawList.AddCircleFilled(personCirclePos, 2f, _circleColor);
            drawList.AddCircle(personCirclePos, 3f, _borderColor, 0, 2);
        }
    }

    private void FrameworkOnUpdate(IFramework _)
    {
        try
        {
            EnqueueCircles();
        }
        catch (Exception e)
        {
            pluginLog.Verbose(e, "exception in MiniMapService.FrameworkOnUpdate");
        }
    }

    private unsafe void ClientStateOnMapIdChanged(uint mapId)
    {
        var agentMap = AgentMap.Instance();
        if (agentMap is null)
        {
            pluginLog.Warning("MinimapService.ClientStateOnMapIdChanged: AgentMap is null");
            _mapSizeFactor = 1f;
            return;
        }

        _mapSizeFactor = agentMap->CurrentMapSizeFactorFloat;
    }

    private unsafe void EnqueueCircles() {

        var naviMap = gameGui.GetAddonByName<AddonNaviMap>("_NaviMap");
        if (naviMap is null)
        {
            return;
        }

        _miniMapVisible = naviMap->IsVisible;
        if (!_miniMapVisible)
        {
            return;
        }

        var origin = OriginForNaviMap(naviMap);
        var playerCoordinates = XZOnly(objectTable.LocalPlayer?.Position ?? Vector3.Zero);
        var rotationMatrix = CreateRotationMatrix(naviMap->NaviMap.PlayerConeRotation, !naviMap->NaviMap.NorthLockedUp);

        foreach (var battleChara in objectTable.PlayerObjects)
        {
            if ((battleChara.StatusFlags & StatusFlags.AllianceMember) != 0 || (battleChara.StatusFlags & StatusFlags.PartyMember) != 0)
            {
                continue;
            }
            var battleCharaCoordinates = XZOnly(battleChara.Position);
            var battleCharaOffset = ClampVectorLength((battleCharaCoordinates - playerCoordinates) * (naviMap->NaviMap.MarkerPositionScaling * _mapSizeFactor) * naviMap->Scale, 66f * naviMap->Scale);

            if (!naviMap->NaviMap.NorthLockedUp)
            {
                battleCharaOffset = Vector2.Transform(battleCharaOffset, rotationMatrix);
            }

            _circlePositions.Enqueue(origin + battleCharaOffset);
        }
    }

    private static unsafe Vector2 OriginForNaviMap(AddonNaviMap* naviMap)
    {
        if (naviMap is null)
        {
            return Vector2.Zero;
        }

        var naviMapPos = Vector2.Create(naviMap->X, naviMap->Y);
        var naviMapMapImagePos = Vector2.Create(naviMap->MapImage->X, naviMap->MapImage->Y);
        var naviMapMapImageOrigin = Vector2.Create(naviMap->MapImage->OriginX, naviMap->MapImage->OriginY);
        var naviMapMapBasePos = Vector2.Create(naviMap->MapBase->X, naviMap->MapBase->Y);

        return naviMapPos + (naviMapMapImagePos + naviMapMapImageOrigin + naviMapMapBasePos) * naviMap->Scale;
    }

    private static Matrix3x2 CreateRotationMatrix(float rotation, bool enableRotation)
    {
        return enableRotation ? Matrix3x2.CreateRotation(float.DegreesToRadians(rotation)) : Matrix3x2.Identity;
    }

    private static Vector2 XZOnly(Vector3 xyz)
    {
        return Vector2.Create(xyz.X, xyz.Z);
    }

    private static Vector2 ClampVectorLength(Vector2 vector, float maxLength)
    {
        var length = vector.Length();
        if (length <= maxLength)
        {
            return vector;
        }

        return vector / length * maxLength;
    }
}
