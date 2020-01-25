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

namespace SpaceEngineers.ShipFuelStat {
  // H2 and battery stats for ships.
  public sealed class Program : MyGridProgram {
    #endregion

List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
List<IMyGasTank> h2Tanks = new List<IMyGasTank>();
IMyCockpit cockpit;

public Program() {
  Runtime.UpdateFrequency = UpdateFrequency.Once | UpdateFrequency.Update100;
  
  GridTerminalSystem.GetBlocksOfType(batteries, x => x.IsSameConstructAs(Me));
  GridTerminalSystem.GetBlocksOfType(h2Tanks, x => x.IsSameConstructAs(Me) && x.BlockDefinition.SubtypeName == "SmallHydrogenTank");

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

  var h2Curr = 0f;
  var h2Max = 0f;
  foreach (var x in h2Tanks) {
    h2Curr += (float)(x.Capacity * x.FilledRatio);
    h2Max += x.Capacity;
  }
  h2Curr /= 1000f;
  h2Max /= 1000f;

  var msg = $@"=== FUEL STATUS ===

= Hydrogen: {BarGraph(h2Curr, h2Max)}
{h2Curr:0.0} kL of {h2Max:0.0} kL

= Battery: {BarGraph(stored, capacity)}
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
