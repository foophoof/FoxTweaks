using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.NativeWrapper;
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
                              IPluginLog pluginLog) : IHostedService {
    private AtkUnitBasePtr NaviMapAddon => gameGui.GetAddonByName("_NaviMap");
    private Map? currentMap = null;

    public float ZoneScale { get; private set; } = 1f;
    public float NaviScale { get; private set; } = 1f;
    public float Zoom { get; private set; } = 1f;
    public float Rotation { get; private set; }
    public bool IsVisible { get; private set; }
    public bool IsLocked { get; private set; }

    public Vector2 MapPos => NaviMapAddon.Position;
    public Vector2 MapSize => new(218 * NaviScale, 218 * NaviScale);
    
    public Vector2 PlayerCirclePos => new(NaviMapAddon.X + MapSize.X / 2, NaviMapAddon.Y + MapSize.Y / 2);

    public float MinimapScale => ZoneScale * NaviScale * Zoom;
    
    public Task StartAsync(CancellationToken cancellationToken) {
      clientState.MapIdChanged += ClientStateOnMapIdChanged;
      ClientStateOnMapIdChanged(clientState.MapId);
      
      framework.Update += FrameworkOnUpdate;
      
      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
      clientState.MapIdChanged -= ClientStateOnMapIdChanged;
      framework.Update -= FrameworkOnUpdate;
      
      return Task.CompletedTask;
    }

    private void ClientStateOnMapIdChanged(uint mapId) {
      if (mapId == 0) {
        return;
      }
      currentMap = maps.GetRow(mapId);
      ushort sizeFactor = currentMap?.SizeFactor ?? 100;
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
      
      UpdateZoom(naviMapAddon);
      UpdateRotation(naviMapAddon);
      UpdateIsLocked(naviMapAddon);
    }

    private unsafe void UpdateZoom(AtkUnitBasePtr naviMapAddon) {
      if (naviMapAddon.IsNull) return;
      
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
      if (naviMapAddon.IsNull) return;
      
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
      if (naviMapAddon.IsNull) return;

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