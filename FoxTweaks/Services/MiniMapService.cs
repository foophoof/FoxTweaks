using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.NativeWrapper;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Hosting;
using ZLinq;

namespace FoxTweaks.Services {
  public class MiniMapService(IGameGui gameGui,
                              ExcelSheet<Map> maps,
                              IClientState clientState,
                              IFramework framework,
                              IPluginLog pluginLog,
                              IUiBuilder uiBuilder,
                              IObjectTable objectTable) : IHostedService {
    private AtkUnitBasePtr NaviMapAddon => gameGui.GetAddonByName("_NaviMap");
    private Map? _currentMap = null;

    private float ZoneScale { get; set; } = 1f;
    private float NaviScale { get; set; } = 1f;
    private float Zoom { get; set; } = 1f;
    private float Rotation { get; set; }
    public bool IsVisible { get; private set; }
    private bool IsLocked { get; set; }
    private Vector2 MapPos { get; set; }
    private Vector2 MapSize { get; set; }
    private Vector2 PlayerCirclePos { get; set; }

    public float MinimapScale => ZoneScale * NaviScale * Zoom;

    private readonly uint _circleColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.957f, 0.533f, 0.051f, 1));
    private readonly uint _borderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.957f, 0.533f, 0.051f, 1) * 0.7f);

    public Task StartAsync(CancellationToken cancellationToken) {
      clientState.MapIdChanged += ClientStateOnMapIdChanged;
      ClientStateOnMapIdChanged(clientState.MapId);

      framework.Update += FrameworkOnUpdate;
      uiBuilder.Draw += UiBuilderOnDraw;

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
      clientState.MapIdChanged -= ClientStateOnMapIdChanged;
      framework.Update -= FrameworkOnUpdate;
      uiBuilder.Draw -= UiBuilderOnDraw;

      return Task.CompletedTask;
    }

    private void UiBuilderOnDraw() {
      if (!IsVisible) {
        return;
      }

      var drawList = ImGui.GetForegroundDrawList();
      var windowPos = ImGui.GetWindowViewport().Pos;

      var player = objectTable.LocalPlayer;
      if (player is null) {
        return;
      }

      var playerCirclePos = PlayerCirclePos + windowPos;
      playerCirclePos.Y -= 5f;

      var friends = objectTable.PlayerObjects.AsValueEnumerable(); //.Where(c => (c.StatusFlags & StatusFlags.Friend) != 0);
      foreach (var battleChara in friends) {
        if ((battleChara.StatusFlags & StatusFlags.AllianceMember) != 0 || (battleChara.StatusFlags & StatusFlags.PartyMember) != 0) {
          continue;
        }

        var relativePersonPos = new Vector2(player.Position.X, player.Position.Z) - new Vector2(battleChara.Position.X, battleChara.Position.Z);
        relativePersonPos *= MinimapScale;

        if (!IsLocked) {
          relativePersonPos = Vector2.Transform(relativePersonPos, Matrix3x2.CreateRotation(Rotation));
        }

        var personCirclePos = playerCirclePos - relativePersonPos;

        float distance = Vector2.Distance(playerCirclePos, personCirclePos);

        float minimapRadius = MapSize.X * 0.315f;
        if (distance > minimapRadius) {
          var originToObject = personCirclePos - playerCirclePos;
          originToObject *= minimapRadius / distance;
          personCirclePos = playerCirclePos + originToObject;
        }

        drawList.AddCircleFilled(personCirclePos, 6f, _circleColor);
        drawList.AddCircle(personCirclePos, 6f, _borderColor, 0, 2);
      }
    }

    private void ClientStateOnMapIdChanged(uint mapId) {
      if (mapId == 0) {
        return;
      }
      _currentMap = maps.GetRow(mapId);
      ushort sizeFactor = _currentMap?.SizeFactor ?? 100;
      ZoneScale = sizeFactor / 100f;
    }

    private void FrameworkOnUpdate(IFramework _) {
      var naviMapAddon = gameGui.GetAddonByName("_NaviMap");
      if (naviMapAddon.IsNull) {
        return;
      }
      if (!naviMapAddon.IsReady) {
        return;
      }
      if (!naviMapAddon.IsVisible) {
        IsVisible = false;
        return;
      }

      IsVisible = true;
      NaviScale = naviMapAddon.Scale;
      MapSize = new Vector2(218, 218) * NaviScale;
      MapPos = naviMapAddon.Position;
      PlayerCirclePos = MapPos + MapSize / 2;

      UpdateZoom(naviMapAddon);
      UpdateRotation(naviMapAddon);
      UpdateIsLocked(naviMapAddon);
    }

    private unsafe void UpdateZoom(AtkUnitBasePtr naviMapAddon) {
      if (naviMapAddon.IsNull) {
        return;
      }

      try {
        var addonBase = (AtkUnitBase*)naviMapAddon.Address;
        var baseComponent = addonBase->GetComponentByNodeId(18);
        if (baseComponent is null) {
          return;
        }

        var imageNode = baseComponent->GetImageNodeById(6);
        if (imageNode is null) {
          return;
        }

        Zoom = imageNode->ScaleX;
      }
      catch (Exception e) {
        pluginLog.Verbose(e, "exception in MiniMapService.UpdateZoom");
      }
    }

    private unsafe void UpdateRotation(AtkUnitBasePtr naviMapAddon) {
      if (naviMapAddon.IsNull) {
        return;
      }

      try {
        var addonBase = (AtkUnitBase*)naviMapAddon.Address;
        var frameNode = addonBase->GetNodeById(8);
        if (frameNode is null) {
          return;
        }

        Rotation = frameNode->Rotation;
      }
      catch (Exception e) {
        pluginLog.Verbose(e, "exception in MiniMapService.UpdateRotation");
      }
    }

    private unsafe void UpdateIsLocked(AtkUnitBasePtr naviMapAddon) {
      if (naviMapAddon.IsNull) {
        return;
      }

      try {
        var addonBase = (AtkUnitBase*)naviMapAddon.Address;
        var lockNode = addonBase->GetNodeById(4);
        if (lockNode is null) {
          return;
        }

        var lockCheckbox = lockNode->GetAsAtkComponentCheckBox();
        if (lockCheckbox is null) {
          return;
        }

        IsLocked = lockCheckbox->IsChecked;
      }
      catch (Exception e) {
        pluginLog.Verbose(e, "exception in MiniMapService.UpdateIsLocked");
      }
    }
  }
}