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

namespace SpaceEngineers.UWBlockPrograms.GasStat {
  public sealed class Program : MyGridProgram {
    #endregion

    private static string H2TankSubtype = "LargeHydrogenTank";
    private static string O2TankSubtype = "";

    List<IMyGasTank> h2Tanks = new List<IMyGasTank>();
    List<IMyGasTank> o2Tanks = new List<IMyGasTank>();
    List<IMyTextPanel> panels = new List<IMyTextPanel>();

    public Program() {
      GridTerminalSystem.GetBlocksOfType(h2Tanks, x => x.IsSameConstructAs(Me) && x.BlockDefinition.SubtypeName == H2TankSubtype);
      GridTerminalSystem.GetBlocksOfType(o2Tanks, x => x.IsSameConstructAs(Me) && x.BlockDefinition.SubtypeName == O2TankSubtype);
      GridTerminalSystem.GetBlocksOfType(panels, x => x.IsSameConstructAs(Me) && x.CustomName == "Panel - Gas Status");
      Runtime.UpdateFrequency = UpdateFrequency.Once | UpdateFrequency.Update100;
    }

    public void Main(string argument, UpdateType updateSource) {
      var h2Curr = 0f;
      var h2Max = 0f;
      var o2Curr = 0f;
      var o2Max = 0f;
      foreach (var x in h2Tanks) {
        h2Curr += (float)(x.Capacity * x.FilledRatio);
        h2Max += x.Capacity;
      }
      foreach (var x in o2Tanks) {
        o2Curr += (float)(x.Capacity * x.FilledRatio);
        o2Max += x.Capacity;
      }
      h2Curr /= 1000.0f;
      h2Max /= 1000.0f;
      o2Curr /= 1000.0f;
      o2Max /= 1000.0f;

      var msg = $@"=== GAS STORAGE STATUS ===

= Hydrogen: {BarGraph(h2Curr, h2Max)}
{h2Curr:0.0} kL of {h2Max:0.0} kL

= Oxygen: {BarGraph(o2Curr, o2Max)}
{o2Curr:0.0} kL of {o2Max:0.0} kL";

      foreach (var p in panels) {
        p.WriteText(msg, false);
      }
    }

    private string BarGraph(float value, float max) {
      var pct = 100 * value / max;
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
