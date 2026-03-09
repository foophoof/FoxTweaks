using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Microsoft.Extensions.Hosting;
using ZLinq;

namespace FoxTweaks.Services {
  public class MiniMapService(IGameGui gameGui,
                              IFramework framework,
                              IPluginLog pluginLog,
                              IUiBuilder uiBuilder,
                              IObjectTable objectTable) : IHostedService {
    private bool _miniMapVisible;
    private readonly Queue<Vector2> _circlePositions = new();

    private readonly uint _circleColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.957f, 0.533f, 0.051f, 1));
    private readonly uint _borderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.957f, 0.533f, 0.051f, 1) * 0.7f);

    public Task StartAsync(CancellationToken cancellationToken) {
      framework.Update += FrameworkOnUpdate;
      uiBuilder.Draw += UiBuilderOnDraw;

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
      framework.Update -= FrameworkOnUpdate;
      uiBuilder.Draw -= UiBuilderOnDraw;

      return Task.CompletedTask;
    }

    private void UiBuilderOnDraw() {
      if (!_miniMapVisible) {
        return;
      }

      var drawList = ImGui.GetForegroundDrawList();
      while (_circlePositions.TryDequeue(out var personCirclePos)) {
        drawList.AddCircleFilled(personCirclePos, 2f, _circleColor);
        drawList.AddCircle(personCirclePos, 3f, _borderColor, 0, 2);
      }
    }

    private void FrameworkOnUpdate(IFramework _) {
      var origin = Vector2.Zero;
      bool isLocked = false;
      float rotation = 0f;
      float zoom = 1f;
      float addonNaviMapScale = 1f;

      unsafe {
        try {
          var naviMap = gameGui.GetAddonByName<AddonNaviMap>("_NaviMap");
          if (naviMap is null) {
            return;
          }

          var foxNaviMap = (FoxAddonNaviMap*)naviMap;

          _miniMapVisible = naviMap->IsVisible;
          if (!_miniMapVisible) {
            return;
          }

          var agentMap = AgentMap.Instance();
          if (agentMap is null) {
            return;
          }

          addonNaviMapScale = naviMap->Scale;
          origin = Vector2.Create(
              naviMap->X + (foxNaviMap->MapImage->X + foxNaviMap->MapImage->OriginX + foxNaviMap->MapBase->X) * addonNaviMapScale,
              naviMap->Y + (foxNaviMap->MapImage->Y + foxNaviMap->MapImage->OriginY + foxNaviMap->MapBase->Y) * addonNaviMapScale
          );
          isLocked = foxNaviMap->Atk2DNaviMap.NorthLockedUp;
          rotation = float.DegreesToRadians(foxNaviMap->Atk2DNaviMap.PlayerConeRotation);
          zoom = foxNaviMap->Atk2DNaviMap.MarkerPositionScaling * agentMap->CurrentMapSizeFactorFloat;
          addonNaviMapScale = naviMap->Scale;
        }
        catch (Exception e) {
          pluginLog.Verbose(e, "exception in MiniMapService.FrameworkOnUpdate");
        }
      }

      var player = objectTable.LocalPlayer;
      if (player is null) {
        return;
      }

      var playerCoordinates = Vector2.Create(player.Position.X, player.Position.Z);
      var rotationMatrix = Matrix3x2.Identity;
      if (!isLocked) {
        rotationMatrix = Matrix3x2.CreateRotation(rotation);
      }

      var friends = objectTable.PlayerObjects.AsValueEnumerable()
          .Where(c => (c.StatusFlags & StatusFlags.Friend) != 0)
          .Where(c => (c.StatusFlags & StatusFlags.AllianceMember) == 0)
          .Where(c => (c.StatusFlags & StatusFlags.PartyMember) == 0);
      foreach (var battleChara in friends) {
        var battleCharaCoordinates = Vector2.Create(battleChara.Position.X, battleChara.Position.Z);
        var battleCharaOffset = ClampVectorLength((battleCharaCoordinates - playerCoordinates) * zoom * addonNaviMapScale, 66f * addonNaviMapScale);

        if (!isLocked) {
          battleCharaOffset = Vector2.Transform(battleCharaOffset, rotationMatrix);
        }

        _circlePositions.Enqueue(origin + battleCharaOffset);
      }
    }

    private static Vector2 ClampVectorLength(Vector2 vector, float maxLength) {
      float length = vector.Length();
      if (length > maxLength) {
        return vector / length * maxLength;
      }

      return vector;
    }
  }
}

[StructLayout(LayoutKind.Explicit, Size = 0x3A90)]
internal unsafe struct FoxAddonNaviMap {
  [FieldOffset(0x238)] public FoxAtk2DNaviMap Atk2DNaviMap;
  [FieldOffset(0x15D8)] public AtkComponentNode* MapBase;
  [FieldOffset(0x15E0)] public AtkImageNode* MapImage;
  [FieldOffset(0x15F0)] public AtkImageNode* Mask;
  [FieldOffset(0x3A78)] public float MarkerPositionScaling;
}

[StructLayout(LayoutKind.Explicit, Size = 0x134D)]
internal unsafe struct FoxAtk2DNaviMap {
  [FieldOffset(0x28)] public float MarkerRadiusScale;
  [FieldOffset(0x2C)] public float MarkerPositionScaling;
  [FieldOffset(0x34)] public float PlayerConeRotation;
  [FieldOffset(0x134C)] public bool NorthLockedUp;
};