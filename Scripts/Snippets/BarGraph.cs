#region Prelude
using Sandbox.ModAPI.Ingame;

namespace SpaceEngineers.BarGraph {
  // Draws a bar graph for a percentage value.
  public sealed class Program : MyGridProgram {
    #endregion

    struct Percent {
      public float value;
      public float max;

      public static Percent Zero = new Percent() {
        value = 0f,
        max = 0f
      };

      public void Add(float value, float max) {
        this.value += value;
        this.max += max;
      }

      public float Percentage() {
        return 100 * value / max;
      }
    }

    string BarGraph(float pct) {
      var s = "[";
      var i = 0f;
      for (; i < pct; i += 12.5f) {
        s += "#";
      }
      for (; i < 100; i += 12.5f) {
        s += "_";
      }
      s += $@"] ({pct:0}%)";
      return s;
    }

    #region PreludeFooter
  }
}
#endregion
