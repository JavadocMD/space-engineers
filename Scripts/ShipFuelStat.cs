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
using VRage.Game.GUI.TextPanel;

namespace SpaceEngineers.ShipFuelStat {
  // H2 and battery stats for ships.
  public sealed class Program : MyGridProgram {
    #endregion

    const int DisplaySurfaceId = 0;
    const float FontSize = 0.9f;

    List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
    List<IMyGasTank> h2Tanks = new List<IMyGasTank>();
    List<IMyGasTank> o2Tanks = new List<IMyGasTank>();
    CockpitDisplay display;

    public Program() {
      Runtime.UpdateFrequency = UpdateFrequency.Once | UpdateFrequency.Update100;
      GridTerminalSystem.GetBlocksOfType(batteries, x => x.IsSameConstructAs(Me));
      GridTerminalSystem.GetBlocksOfType(o2Tanks, x => x.IsSameConstructAs(Me)
        && (x.BlockDefinition.SubtypeName == "OxygenTankSmall" || x.BlockDefinition.SubtypeName == ""));
      GridTerminalSystem.GetBlocksOfType(h2Tanks, x => x.IsSameConstructAs(Me)
        && (x.BlockDefinition.SubtypeName == "SmallHydrogenTank" || x.BlockDefinition.SubtypeName == "LargeHydrogenTank"));
      display = new CockpitDisplay(FindOne<IMyCockpit>(), DisplaySurfaceId, FontSize);
    }

    public void Main(string argument, UpdateType updateSource) {
      var eStored = Percent.Zero;
      var eUsage = Percent.Zero;
      var h2 = Percent.Zero;
      var o2 = Percent.Zero;
      foreach (var x in batteries) {
        eStored.Add(x.CurrentStoredPower, x.MaxStoredPower);
        eUsage.Add(x.CurrentOutput, x.MaxOutput);
      }
      foreach (var x in h2Tanks) {
        h2.Add((float)(x.Capacity * x.FilledRatio), x.Capacity);
      }
      foreach (var x in o2Tanks) {
        o2.Add((float)(x.Capacity * x.FilledRatio), x.Capacity);
      }

      var msg = $@"=== FUEL STATUS ===

= Hydrogen: {BarGraph(h2.Percentage())}
{(h2.value / 1000f):0.0} kL of {(h2.max / 1000f):0.0} kL

= Oxygen: {BarGraph(o2.Percentage())}
{(o2.value / 1000f):0.0} kL of {(o2.max / 1000f):0.0} kL

= Battery: {BarGraph(eStored.Percentage())}
{eStored.value:0.00} MWh of {eStored.max:0.00} MWh

= Usage: {BarGraph(eUsage.Percentage())}
{eUsage.value:0.00} MW of {eUsage.max:0.00} MW";

      display.Write(msg);
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

    T FindOne<T>() where T : class, IMyTerminalBlock {
      List<T> xs = new List<T>();
      GridTerminalSystem.GetBlocksOfType<T>(xs, x => x.IsSameConstructAs(Me));
      if (xs.Count == 0) {
        Echo($@"Error: Missing required block of type {nameof(T)}");
        Me.Enabled = false;
        return null;
      } else {
        return xs[0];
      }
    }

    class CockpitDisplay {
      IMyTextSurface surface;

      public CockpitDisplay(IMyCockpit cockpit, int surface, float fontSize = 0.9f) {
        this.surface = cockpit.GetSurface(surface);
        this.surface.ContentType = ContentType.TEXT_AND_IMAGE;
        this.surface.BackgroundColor = new Color(12, 12, 12);
        this.surface.FontSize = fontSize;
        this.surface.FontColor = new Color(65, 250, 0);
        this.surface.Font = "Monospace";
      }

      public void Write(string text) {
        this.surface.WriteText(text, false);
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
