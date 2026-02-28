using Dalamud.Configuration;
using Newtonsoft.Json;

namespace FoxTweaks {
  public class Configuration : IPluginConfiguration {
    private bool configOption;

    public int Version { get; set; }

    [JsonIgnore]
    public bool IsDirty { get; set; }

    public bool ConfigOption {
      get => configOption;
      set {
        configOption = value;
        IsDirty = true;
      }
    }
  }
}