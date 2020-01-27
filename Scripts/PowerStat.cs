#region Prelude
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;

namespace SpaceEngineers.UWBlockPrograms.PowerStat {
  // Power status display for a station.
  public sealed class Program : MyGridProgram {
    #endregion

    List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
    List<IMyTextPanel> panels = new List<IMyTextPanel>();
    List<IMyPowerProducer> generators = new List<IMyPowerProducer>();

    public Program() {
      GridTerminalSystem.GetBlocksOfType(batteries, x => x.IsSameConstructAs(Me));
      GridTerminalSystem.GetBlocksOfType(panels, x => x.IsSameConstructAs(Me) && x.CustomName == "Panel - Power Status");
      GridTerminalSystem.GetBlocksOfType(generators, x => x.IsSameConstructAs(Me));
      generators = generators.FindAll(x => !x.BlockDefinition.SubtypeName.Contains("Battery"));
      Runtime.UpdateFrequency = UpdateFrequency.Once | UpdateFrequency.Update100;
    }

    public void Main(string argument, UpdateType updateSource) {
      var stored = Percent.Zero;
      var usage = Percent.Zero;
      var gen = Percent.Zero;
      foreach (var x in batteries) {
        stored.Add(x.CurrentStoredPower, x.MaxStoredPower);
        usage.Add(x.CurrentOutput, x.MaxOutput);
      }
      foreach (var x in generators) {
        gen.Add(x.CurrentOutput, x.MaxOutput);
      }

      var msg = $@"=== POWER SYSTEMS STATUS ===

= Storage: {BarGraph(stored.Percentage())}
{stored.value:0.00} MWh of {stored.max:0.00} MWh

= Generation: {BarGraph(gen.Percentage())}
{gen.value:0.00} MW of {gen.max:0.00} MW

= Usage: {BarGraph(usage.Percentage())}
{usage.value:0.00} MW of {usage.max:0.00} MW";

      foreach (var p in panels) {
        p.WriteText(msg, false);
      }
    }

    // Utils

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
