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
      var msg = "=== POWER STATUS ===\n\n";
      msg += String.Format("Storage:\n{0:0.00} of {1:0.00} MWh ({2:0}%)\n\n", stored, capacity, 100 * stored / capacity);
      msg += String.Format("Generation:\n{0:0.00} MW of {1:0.00} MW ({2:0}%)\n\n", genCurr, genMax, 100 * genCurr / genMax);
      msg += String.Format("Usage:\n{0:0.00} MW of {1:0.00} MW ({2:0}%)\n\n", outCurr, outMax, 100 * outCurr / outMax);
      foreach (var p in panels) {
        p.WriteText(msg, false);
      }
    }

    #region PreludeFooter
  }
}
#endregion
