using Dalamud.Bindings.ImGui;
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
    private readonly Queue<Vector2> _circlePositions = new();
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

    private unsafe void FrameworkOnUpdate(IFramework _)
    {
        try
        {
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

            var origin = Vector2.Create(
                naviMap->X + (naviMap->MapImage->X + naviMap->MapImage->OriginX + naviMap->MapBase->X) * naviMap->Scale,
                naviMap->Y + (naviMap->MapImage->Y + naviMap->MapImage->OriginY + naviMap->MapBase->Y) * naviMap->Scale
            );

            var player = objectTable.LocalPlayer;
            if (player is null)
            {
                return;
            }

            var playerCoordinates = Vector2.Create(player.Position.X, player.Position.Z);
            var rotationMatrix = Matrix3x2.Identity;
            if (!naviMap->NaviMap.NorthLockedUp)
            {
                rotationMatrix = Matrix3x2.CreateRotation(float.DegreesToRadians(naviMap->NaviMap.PlayerConeRotation));
            }

            var friends = objectTable.PlayerObjects.AsValueEnumerable()
                .Where(c => (c.StatusFlags & StatusFlags.Friend) != 0)
                .Where(c => (c.StatusFlags & StatusFlags.AllianceMember) == 0)
                .Where(c => (c.StatusFlags & StatusFlags.PartyMember) == 0);
            foreach (var battleChara in friends)
            {
                var battleCharaCoordinates = Vector2.Create(battleChara.Position.X, battleChara.Position.Z);
                var battleCharaOffset = ClampVectorLength((battleCharaCoordinates - playerCoordinates) * (naviMap->NaviMap.MarkerPositionScaling * _mapSizeFactor) * naviMap->Scale, 66f * naviMap->Scale);

                if (!naviMap->NaviMap.NorthLockedUp)
                {
                    battleCharaOffset = Vector2.Transform(battleCharaOffset, rotationMatrix);
                }

                _circlePositions.Enqueue(origin + battleCharaOffset);
            }
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
