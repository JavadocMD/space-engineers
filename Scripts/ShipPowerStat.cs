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

// Change this namespace for each script you create.
namespace SpaceEngineers.ShipPowerStat {
  public sealed class Program : MyGridProgram {
    // Your code goes between the next #endregion and #region
    #endregion

    List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
    IMyCockpit cockpit;

    public Program() {
      Runtime.UpdateFrequency = UpdateFrequency.Once | UpdateFrequency.Update10;
      GridTerminalSystem.GetBlocksOfType(batteries, x => x.IsSameConstructAs(Me));
      List<IMyTerminalBlock> cockpits = new List<IMyTerminalBlock>();
      GridTerminalSystem.SearchBlocksOfName("Cockpit", cockpits, x => x is IMyCockpit && x.IsSameConstructAs(Me));
      if (cockpits.Count < 1) {
        Echo("Error - Unable to find block named 'Cockpit'.");
      } else {
        cockpit = cockpits[0] as IMyCockpit;
      }
    }

    public void Main(string argument, UpdateType updateSource) {
      var stored = 0f;
      var capacity = 0f;
      var outCurr = 0f;
      var outMax = 0f;
      foreach (var b in batteries) {
        stored += b.CurrentStoredPower;
        capacity += b.MaxStoredPower;
        outCurr += b.CurrentOutput;
        outMax += b.MaxOutput;
      }
      var msg = $@"=== POWER SYSTEMS STATUS ===
  
= Storage: {BarGraph(stored, capacity)}
{stored:0.00} MWh of {capacity:0.00} MWh

= Usage: {BarGraph(outCurr, outMax)}
{outCurr:0.00} MW of {outMax:0.00} MW";

      cockpit.GetSurface(0).WriteText(msg, false);
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
