using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace FoxTweaks.Windows {
  public class ConfigWindow(Configuration config) : Window("FoxTweaks Config") {
    public override void Draw() {
      bool configOption = config.ConfigOption;

      if (ImGui.Checkbox("Config Option", ref configOption)) {
        config.ConfigOption = configOption;
      }
    }
  }
}