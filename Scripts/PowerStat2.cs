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

namespace SpaceEngineers.UWBlockPrograms.PowerStat2 {
  // Power status display for a station.
  public sealed class Program : MyGridProgram {
    #endregion

    const float EpsilonMW = 0.000001f; // 1 W

    List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
    List<IMyTextPanel> displays = new List<IMyTextPanel>();
    List<IMySolarPanel> solars = new List<IMySolarPanel>();
    List<IMyReactor> reactors = new List<IMyReactor>();
    List<IMyPowerProducer> winds = new List<IMyPowerProducer>();

    public Program() {
      Runtime.UpdateFrequency = UpdateFrequency.Once | UpdateFrequency.Update100;
      GridTerminalSystem.GetBlocksOfType(displays, x => x.IsSameConstructAs(Me) && x.CustomName == "Panel - Power Status");
      GridTerminalSystem.GetBlocksOfType(batteries, x => x.IsSameConstructAs(Me));
      GridTerminalSystem.GetBlocksOfType(solars, x => x.IsSameConstructAs(Me));
      GridTerminalSystem.GetBlocksOfType(reactors, x => x.IsSameConstructAs(Me));
      GridTerminalSystem.GetBlocksOfType(winds, x => x.IsSameConstructAs(Me) && x.BlockDefinition.SubtypeName == "LargeBlockWindTurbine");
    }

    public void Main(string argument, UpdateType updateSource) {
      var stored = Percent.Zero;
      var input = Percent.Zero;
      var output = Percent.Zero;
      foreach (var x in batteries) {
        stored.Add(x.CurrentStoredPower, x.MaxStoredPower);
        input.Add(x.CurrentInput, x.MaxInput);
        output.Add(x.CurrentOutput, x.MaxOutput);
      }
      var flowDesc = input.value > output.value + EpsilonMW ? ">>> charging >>>"
       : input.value < output.value - EpsilonMW ? "<<< discharging <<<"
       : "--- neutral ---";

      var solarGen = Percent.Zero;
      foreach (var x in solars) {
        solarGen.Add(x.CurrentOutput, x.MaxOutput);
      }
      var reactorGen = Percent.Zero;
      foreach (var x in reactors) {
        reactorGen.Add(x.CurrentOutput, x.MaxOutput);
      }
      var windGen = Percent.Zero;
      foreach (var x in winds) {
        windGen.Add(x.CurrentOutput, x.MaxOutput);
      }
      var totalGen = solarGen.value + reactorGen.value + windGen.value;
      var totalUse = totalGen - input.value + output.value;

      var msg = $@"=== POWER SYSTEMS STATUS ===

= Battery

  STOR: {BarGraph(stored.Percentage())}
        {stored.value:0.0} / {stored.max:0.0} MWh

        {flowDesc}
   IN:  {input.value,4:N1} MW {BarGraph(input.Percentage(), false)}
  OUT:  {output.value,4:N1} MW {BarGraph(output.Percentage(), false)}

= Generation ({totalGen:0.0} of {totalUse:0.0} MW)

  SOLR: {BarGraph(solarGen.Percentage())}
        {solarGen.value:0.0} / {solarGen.max:0.0} MW

  WIND: {BarGraph(windGen.Percentage())}
        {windGen.value:0.0} / {windGen.max:0.0} MW

  NUCL: {BarGraph(reactorGen.Percentage())}
        {reactorGen.value:0.0} / {reactorGen.max:0.0} MW";

      foreach (var p in displays) {
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

    string BarGraph(float pct, Boolean withNumber = true) {
      var s = "[";
      var i = 0f;
      for (; i < pct; i += 12.5f) {
        s += "#";
      }
      for (; i < 100; i += 12.5f) {
        s += "_";
      }
      s += "]";
      if (withNumber) {
        s += $@" ({pct:0}%)";
      }
      return s;
    }

    #region PreludeFooter
  }
}
#endregion
