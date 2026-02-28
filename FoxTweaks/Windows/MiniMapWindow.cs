using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FoxTweaks.Services;
using ZLinq;

namespace FoxTweaks.Windows {
  public class MiniMapWindow : Window {
    private readonly IObjectTable _objectTable;
    private readonly MiniMapService _miniMapService;

    private readonly uint _circleColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.957f, 0.533f, 0.051f, 1));
    private readonly uint _borderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.957f, 0.533f, 0.051f, 1) * 0.7f);
    private Vector2 _windowPos;
    
    public MiniMapWindow(MiniMapService miniMapService, IObjectTable objectTable) : base("MiniMapWindow") {
      this.Size = new Vector2(200, 200);
      this.Position = new Vector2(200, 200);
      this.Flags |= ImGuiWindowFlags.NoInputs |
                    ImGuiWindowFlags.NoDecoration |
                    ImGuiWindowFlags.NoBackground |
                    ImGuiWindowFlags.NoBringToFrontOnFocus |
                    ImGuiWindowFlags.NoFocusOnAppearing |
                    ImGuiWindowFlags.NoNavFocus;
      this.ForceMainWindow = true;
      this.IsOpen = true;

      _miniMapService = miniMapService;
      _objectTable = objectTable;
      _windowPos = new Vector2();
    }

    public override void PreDraw() {
      _windowPos = ImGui.GetWindowViewport().Pos;

      this.Size = _miniMapService.MapSize;
      this.Position = _miniMapService.MapPos;
    }

    public override void Draw() {
      var drawList = ImGui.GetWindowDrawList();
      
      var player = _objectTable.LocalPlayer;
      if (player is null) {
        return;
      }
      
      var playerCirclePos = _miniMapService.PlayerCirclePos + _windowPos;
      playerCirclePos.Y -= 5f;
      
      var friends = _objectTable.PlayerObjects.AsValueEnumerable().Where(c => (c.StatusFlags & StatusFlags.Friend) != 0);
      foreach (var battleChara in friends) {
        if ((battleChara.StatusFlags & StatusFlags.AllianceMember) != 0 || (battleChara.StatusFlags & StatusFlags.PartyMember) != 0) {
          continue;
        }

        var relativePersonPos = new Vector2(player.Position.X, player.Position.Z) - new Vector2(battleChara.Position.X, battleChara.Position.Z);
        relativePersonPos *= _miniMapService.MinimapScale;

        if (!_miniMapService.IsLocked) {
          relativePersonPos = Vector2.Transform(relativePersonPos, Matrix3x2.CreateRotation(_miniMapService.Rotation));
        }
        
        var personCirclePos = playerCirclePos - relativePersonPos;

        float distance = Vector2.Distance(playerCirclePos, personCirclePos);

        float minimapRadius = _miniMapService.MapSize.X * 0.315f;
        if (distance > minimapRadius) {
          var originToObject = personCirclePos - playerCirclePos;
          originToObject *= minimapRadius / distance;
          personCirclePos = playerCirclePos + originToObject;
        }
        
        drawList.AddCircleFilled(personCirclePos, 6f, _circleColor);
        drawList.AddCircle(personCirclePos, 6f, _borderColor , 0, 2);
      }
    }
  }
}