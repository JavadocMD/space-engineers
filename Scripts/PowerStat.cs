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

    public void Save() {

    }

    public void Main(string argument, UpdateType updateSource) {
      var stored = 0f;
      var capacity = 0f;
      var outCurr = 0f;
      var outMax = 0f;
      var genCurr = 0f;
      var genMax = 0f;
      foreach (var b in batteries) {
        stored += b.CurrentStoredPower;
        capacity += b.MaxStoredPower;
        outCurr += b.CurrentOutput;
        outMax += b.MaxOutput;
      }
      foreach (var g in generators) {
        genCurr += g.CurrentOutput;
        genMax += g.MaxOutput;
      }

      var msg = $@"=== POWER SYSTEMS STATUS ===
  
= Storage: {BarGraph(stored, capacity)}
{stored:0.00} MWh of {capacity:0.00} MWh

= Generation: {BarGraph(genCurr, genMax)}
{genCurr:0.00} MW of {genMax:0.00} MW

= Usage: {BarGraph(outCurr, outMax)}
{outCurr:0.00} MW of {outMax:0.00} MW";

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
