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

namespace SpaceEngineers.UWBlockPrograms.OreStat {
  // Ore cargo stats for ships.
  public sealed class Program : MyGridProgram {
    #endregion

List<IMyTerminalBlock> storage;
IMyCockpit cockpit;

List<MyInventoryItem> items = new List<MyInventoryItem>();
VRage.MyFixedPoint[] ore = new VRage.MyFixedPoint[10];

public Program() {
  Runtime.UpdateFrequency = UpdateFrequency.Once | UpdateFrequency.Update100;

  var blocks = new List<IMyTerminalBlock>();
  GridTerminalSystem.GetBlocks(blocks);
  storage = (from b in blocks
             where b.IsSameConstructAs(Me) && b.HasInventory
             select b).ToList();

  List<IMyTerminalBlock> cockpits = new List<IMyTerminalBlock>();
  GridTerminalSystem.SearchBlocksOfName("Cockpit", cockpits, x => x is IMyCockpit && x.IsSameConstructAs(Me));
  if (cockpits.Count < 1) {
    Echo("Error - Unable to find block named 'Cockpit'.");
  } else {
    cockpit = cockpits[0] as IMyCockpit;
  }
}

public float ToFloat(VRage.MyFixedPoint n) {
  return n.RawValue / 100000f;
}

public void Main(string argument, UpdateType updateSource) {
  var mass = VRage.MyFixedPoint.Zero;
  var currVolume = VRage.MyFixedPoint.Zero;
  var maxVolume = VRage.MyFixedPoint.Zero;
  items.Clear();

  foreach (var s in storage) {
    var inv = s.GetInventory();
    currVolume += inv.CurrentVolume;
    maxVolume += inv.MaxVolume;
    mass += inv.CurrentMass;
    inv.GetItems(items, x => x.Type.TypeId == "MyObjectBuilder_Ore");
  }

  for (int i = 0; i < 10; i++) {
    ore[i] = VRage.MyFixedPoint.Zero;
  }

  foreach (var x in items) {
    switch (x.Type.SubtypeId) {
      case "Stone":
        ore[0] += x.Amount;
        break;
      case "Ice":
        ore[1] += x.Amount;
        break;
      case "Iron":
        ore[2] += x.Amount;
        break;
      case "Nickel":
        ore[3] += x.Amount;
        break;
      case "Cobalt":
        ore[4] += x.Amount;
        break;
      case "Magnesium":
        ore[5] += x.Amount;
        break;
      case "Silicon":
        ore[6] += x.Amount;
        break;
      case "Silver":
        ore[7] += x.Amount;
        break;
      case "Gold":
        ore[8] += x.Amount;
        break;
      case "Platinum":
        ore[9] += x.Amount;
        break;
    }
  }

  int st = ore[0].ToIntSafe(),
      ic = ore[1].ToIntSafe(),
      fe = ore[2].ToIntSafe(),
      ni = ore[3].ToIntSafe(),
      co = ore[4].ToIntSafe(),
      mg = ore[5].ToIntSafe(),
      si = ore[6].ToIntSafe(),
      ag = ore[7].ToIntSafe(),
      au = ore[8].ToIntSafe(),
      pt = ore[9].ToIntSafe();

  var msg = $@"=== CARGO STATUS ===

= Hold: {BarGraph(ToFloat(currVolume), ToFloat(maxVolume))}
{ToFloat(currVolume):N1} kL of {ToFloat(maxVolume):N1} kL
Mass: {ToFloat(mass):N1} kg

= Ore (tonnes):
St: {(st / 1000f),5:N1}      Mg: {(mg / 1000f),5:N1}
Ic: {(ic / 1000f),5:N1}      Si: {(si / 1000f),5:N1}
Fe: {(fe / 1000f),5:N1}      Ag: {(ag / 1000f),5:N1}
Ni: {(ni / 1000f),5:N1}      Au: {(au / 1000f),5:N1}
Co: {(co / 1000f),5:N1}      Pt: {(pt / 1000f),5:N1}";

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
